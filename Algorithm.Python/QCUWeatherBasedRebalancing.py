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

from datetime import date, timedelta
import decimal

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Data import SubscriptionDataSource
from QuantConnect.Python import PythonData

class QCUWeatherBasedRebalancing(QCAlgorithm):    
    '''Initialize: Storage for our custom data:
    Source: http://www.wunderground.com/history/
    Make sure to link to the actual file download URL if using dropbox.
    url = "https://dl.dropboxusercontent.com/u/44311500/KNYC.csv"'''
    
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
        if self.weather not in data: return

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
        source = "https://www.wunderground.com/history/airport/{0}/{1}/1/1/CustomHistory.html?dayend=31&monthend=12&yearend={1}&format=1".format(config.Symbol, date.Year);
        source = "https://dl.dropboxusercontent.com/u/44311500/KNYC.csv"
        return SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);       


    def Reader(self, config, line, date, isLive):
        # If first character is not digit, pass
        if not (line.strip() and line[0].isdigit()): return None
        
        data = line.split(',')
        weather = Weather()
        weather.Symbol = config.Symbol
        weather.Time = DateTime.ParseExact(data[0], "M/d/yyyy", None).AddHours(20) # Make sure we only get this data AFTER trading day - don't want forward bias.
        weather.Value = decimal.Decimal(data[2])
        weather["MaxC"] = float(data[1])
        weather["MinC"] = float(data[3])

        return weather