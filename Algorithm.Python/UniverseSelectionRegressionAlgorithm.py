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

from datetime import datetime

import clr
clr.AddReference("System.Core")
clr.AddReference("QuantConnect.Common")
clr.AddReference("QuantConnect.Algorithm")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Orders import *
from QuantConnect.Data.UniverseSelection import *


class UniverseSelectionRegressionAlgorithm(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    def __init__(self):
        self.__delistedSymbols = []
        self.__changes = None


    def CoarseSelectionFunction(self, coarse):
        return [c.Symbol for c in coarse if c.Symbol.Value == "GOOG" or c.Symbol.Value == "GOOCV" or c.Symbol.Value == "GOOAV" or c.Symbol.Value == "GOOGL"] 


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2014,03,22)  #Set Start Date
        self.SetEndDate(2014,04,07)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        # security that exists with no mappings
        self.AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily)
        # security that doesn't exist until half way in backtest (comes in as GOOCV)
        self.AddSecurity(SecurityType.Equity, "GOOG", Resolution.Daily)

        self.UniverseSettings.Resolution = Resolution.Daily        
        
        self.AddUniverse(self.CoarseSelectionFunction)

        
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if self.Transactions.OrdersCount == 0:
            self.MarketOrder("SPY", 100)

        for kvp in data.Delistings:
            self.__delistedSymbols.append(kvp.Key)

        pyTime = datetime(self.Time)

        if pyTime.date == datetime(2014, 4, 7):
            self.Liquidade()
            return

        if self.__changes is not None:

            if all(security.Symbol in data.Bars for security in self.__changes.AddedSecurities):
                for security in self.__changes.AddedSecurities:
                    self.Log("{0}: Added Security: {1}".format(pyTime, security.Symbol))
                    self.MarketOnOpenOrder(security.Symbol, 100)

                for security in self.__changes.RemovedSecurities:
                    self.Log("{0}: Removed Security: {1}".format(pyTime, security.Symbol))
                    if security.Symbol not in self.__delistedSymbols:
                        self.Log("Not in delisted: {0}:".format(security.Symbol))
                        self.MarketOnOpenOrder(security.Symbol, -100)

            self.__changes = None;


    def OnSecuritiesChanged(self, changes):
        self.__changes = changes


    def OnOrderEvent(self, orderEvent):
            if orderEvent.Status == OrderStatus.Submitted:
                self.Log("{0}: Submitted: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))
            if orderEvent.Status == OrderStatus.Filled:
                self.Log("{0}: Filled: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))