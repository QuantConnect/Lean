# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData
from QCAlgorithm import QCAlgorithm
from datetime import datetime, timedelta
import decimal

### <summary>
### Using weather in NYC to rebalance portfolio. Assumption is people are happier when its warm.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="strategy example" />
class QCUWeatherBasedRebalancing(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,1,1)  #Set Start Date
        self.SetEndDate(2016,1,1)    #Set End Date
        self.SetCash(25000)          #Set Strategy Cash

        self.AddEquity("SPY", Resolution.Daily)
        self.symbol = self.Securities["SPY"].Symbol

        # KNYC is NYC Central Park. Find other locations at
        # https://www.wunderground.com/history/
        self.AddData(Weather, "KNYC", Resolution.Minute)
        self.weather = self.Securities["KNYC"].Symbol

        self.tradingDayCount = 0
        self.rebalanceFrequency = 10


    # When we have a new event trigger, buy some stock:
    def OnData(self, data):
        if not data.ContainsKey(self.weather): return

        # Scale from -5C to +25C :: -5C == 100%, +25C = 0% invested
        fraction = -(data[self.weather].MinC + 5) / 30 if self.weather in data else 0
        #self.Debug("Faction {0}".format(faction))

        # Rebalance every 10 days:
        if self.tradingDayCount >= self.rebalanceFrequency:
            self.SetHoldings(self.symbol, fraction)
            self.tradingDayCount = 0


    def OnEndOfDay(self):
        self.tradingDayCount += 1


class Weather(PythonData):
    ''' Weather based rebalancing'''

    def GetSource(self, config, date, isLive):
        source = "https://dl.dropboxusercontent.com/u/44311500/KNYC.csv"
        source = "https://www.wunderground.com/history/airport/{0}/{1}/1/1/CustomHistory.html?dayend=31&monthend=12&yearend={1}&format=1".format(config.Symbol, date.year)
        return SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile)


    def Reader(self, config, line, date, isLive):
        # If first character is not digit, pass
        if not (line.strip() and line[0].isdigit()): return None

        data = line.split(',')
        weather = Weather()
        weather.Symbol = config.Symbol
        weather.Time = datetime.strptime(data[0], '%Y-%m-%d') + timedelta(hours=20) # Make sure we only get this data AFTER trading day - don't want forward bias.
        # If the second column is an invalid value (empty string), return None. The algorithm will discard it.
        if not data[2]: return None
        weather.Value = decimal.Decimal(data[2])
        weather["Max.C"] = float(data[1])   # Using a dot in the propety name, it will capitalize the first letter of each word:
        weather["Min.C"] = float(data[3])   # Max.C -> MaxC and Min.C -> MinC

        return weather