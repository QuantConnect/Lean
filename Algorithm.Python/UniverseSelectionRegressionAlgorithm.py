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
AddReference("System.Core")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Orders import *
from QuantConnect.Data.UniverseSelection import *
from QCAlgorithm import QCAlgorithm
from datetime import datetime

### <summary>
### Universe Selection regression algorithm simulates an edge case. In one week, Google listed two new symbols, delisted one of them and changed tickers.
### </summary>
### <meta name="tag" content="regression test" />
class UniverseSelectionRegressionAlgorithm(QCAlgorithm):
    
    def Initialize(self):
        
        self.SetStartDate(2014,3,22)   #Set Start Date
        self.SetEndDate(2014,4,7)      #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        # security that exists with no mappings
        self.AddEquity("SPY", Resolution.Daily)
        # security that doesn't exist until half way in backtest (comes in as GOOCV)
        self.AddEquity("GOOG", Resolution.Daily)

        self.UniverseSettings.Resolution = Resolution.Daily
        self.AddUniverse(self.CoarseSelectionFunction)

        self.delistedSymbols = []
        self.changes = None


    def CoarseSelectionFunction(self, coarse):
        return [ c.Symbol for c in coarse if c.Symbol.Value == "GOOG" or c.Symbol.Value == "GOOCV" or c.Symbol.Value == "GOOAV" or c.Symbol.Value == "GOOGL" ]


    def OnData(self, data):
        if self.Transactions.OrdersCount == 0:
            self.MarketOrder("SPY", 100)

        for kvp in data.Delistings:
            self.delistedSymbols.append(kvp.Key)
        
        if self.changes is None:
            return

        if not all(data.Bars.ContainsKey(x.Symbol) for x in self.changes.AddedSecurities):
            return 
        
        for security in self.changes.AddedSecurities:
            self.Log("{0}: Added Security: {1}".format(self.Time, security.Symbol))
            self.MarketOnOpenOrder(security.Symbol, 100)

        for security in self.changes.RemovedSecurities:
            self.Log("{0}: Removed Security: {1}".format(self.Time, security.Symbol))
            if security.Symbol not in self.delistedSymbols:
                self.Log("Not in delisted: {0}:".format(security.Symbol))
                self.MarketOnOpenOrder(security.Symbol, -100)

        self.changes = None 


    def OnSecuritiesChanged(self, changes):
        self.changes = changes


    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Submitted:
            self.Log("{0}: Submitted: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))
        if orderEvent.Status == OrderStatus.Filled:
            self.Log("{0}: Filled: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))