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
from AlgorithmImports import *
from Portfolio.CustomAddRemoveRegressionAlgorithm import *

class CustomAddRemoveRegressionAlgorithm(QCAlgorithm):
    '''Example algorithm of using CustomAddRemoveRegressionAlgorithm'''

    def Initialize(self):
        # Set starting date, cash and ending date of the backtest
        self.SetStartDate(2017, 1, 1)
        self.SetEndDate(2017, 12, 1)
        self.SetCash(1000000)

        self.SetSecurityInitializer(lambda security: security.SetMarketPrice(self.GetLastKnownPrice(security)))
        
        # Subscribe to data of the selected stocks
        self.symbols = [self.AddEquity(ticker, Resolution.Daily).Symbol for ticker in ["SPY"]]

        self.AddAlpha(ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1)))
        self.SetPortfolioConstruction(CustomAddRemoveRegressionAlgorithm())

        self._lastAction = None
    
    def OnData(self, data):
        if self._lastAction is not None and self._lastAction.date() == self.Time.date():
            return
        
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", .5)
            self._lastAction = self.Time
        
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", .5)
            self._lastAction = self.Time

    def OnOrderEvent(self, orderEvent):
        if orderEvent.Status == OrderStatus.Submitted:
            self.Debug("{0}: Submitted: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))
        if orderEvent.Status == OrderStatus.Filled:
            self.Debug("{0}: Filled: {1}".format(self.Time, self.Transactions.GetOrderById(orderEvent.OrderId)))

