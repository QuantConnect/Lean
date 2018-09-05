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
from datetime import date, timedelta, datetime
import decimal as d

### <summary>
### This algorithm shows how to grab symbols from an external api each day
### and load data using the universe selection feature. In this example we
### define a custom data type for the NYSE top gainers and then short the
### top 2 gainers each day
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="universes" />
### <meta name="tag" content="custom universes" />
class CustomDataUniverseAlgorithm(QCAlgorithm):

    def Initialize(self):

        # Data ADDED via universe selection is added with Daily resolution.
        self.UniverseSettings.Resolution = Resolution.Daily

        self.SetStartDate(2015,1,5)
        self.SetEndDate(2015,7,1)
        self.SetCash(100000)

        self.AddEquity("SPY", Resolution.Daily)
        self.SetBenchmark("SPY")

        # add a custom universe data source (defaults to usa-equity)
        self.AddUniverse(NyseTopGainers, "universe-nyse-top-gainers", Resolution.Daily, self.nyseTopGainers)
    
    def nyseTopGainers(self, data):
        return [ x.Symbol for x in data if x["TopGainersRank"] <= 2 ]


    def OnData(self, slice):
        pass
    
    def OnSecuritiesChanged(self, changes):
        self._changes = changes

        for security in changes.RemovedSecurities:
            #  liquidate securities that have been removed
            if security.Invested:
                self.Liquidate(security.Symbol)
                self.Log("Exit {0} at {1}".format(security.Symbol, security.Close))

        for security in changes.AddedSecurities:
            # enter short positions on new securities
            if not security.Invested and security.Close != 0:
                qty = self.CalculateOrderQuantity(security.Symbol, -0.25)
                self.MarketOnOpenOrder(security.Symbol, qty)
                self.Log("Enter {0} at {1}".format(security.Symbol, security.Close))

        
class NyseTopGainers(PythonData):
    def __init__(self):
        self.count = 0
        self.last_date = datetime.min

    def GetSource(self, config, date, isLiveMode):
        url = "http://www.wsj.com/mdc/public/page/2_3021-gainnyse-gainer.html" if isLiveMode else \
            "https://www.dropbox.com/s/vrn3p38qberw3df/nyse-gainers.csv?dl=1"

        return SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile)
    
    def Reader(self, config, line, date, isLiveMode):
        
        if not isLiveMode:
            # backtest gets data from csv file in dropbox
            if not (line.strip() and line[0].isdigit()): return None
            csv = line.split(',')
            nyse = NyseTopGainers()
            nyse.Time = datetime.strptime(csv[0], "%Y%m%d")
            nyse.EndTime = nyse.Time + timedelta(1)
            nyse.Symbol = Symbol.Create(csv[1], SecurityType.Equity, Market.USA)
            nyse["TopGainersRank"] = int(csv[2])
            return nyse

        if self.last_date != date:
            # reset our counter for the new day
            self.last_date = date
            self.count = 0
        
        # parse the html into a symbol
        if not line.startswith('<a href=\"/public/quotes/main.html?symbol='):
            # we're only looking for lines that contain the symbols
            return None
        
        last_close_paren = line.rfind(')')
        last_open_paren = line.rfind('(')
        if last_open_paren == -1 or last_close_paren == -1:
            return None

        symbol_string = line[last_open_paren + 1:last_close_paren]
        nyse = NyseTopGainers()
        nyse.Time = date
        nyse.EndTime = nyse.Time + timedelta(1)
        nyse.Symbol = Symbol.Create(symbol_string, SecurityType.Equity, Market.USA)
        nyse["TopGainersRank"] = self.count
        self.count = self.count + 1
        return nyse