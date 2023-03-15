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

### <summary>
### Demonstration algorithm showing how to disable fill on stale prices for market orders
### <meta name="tag" content="trading and orders" />
### </summary>
class MarketFillOnNextBarRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.SetCash(10000)

        # Do not allow fill on stale prices
        self.Settings.FillOnStalePrices = False

        self.symbol = self.AddEquity("AAPL", Resolution.Hour, Market.USA, True, 1).Symbol
        self.AddEquity("SPY", Resolution.Minute, Market.USA, True, 1)

    def OnData(self, data):
        if self.Time.day != 8: return

        if self.Time.hour == 9 and self.Time.minute == 45:
            lastData = self.Securities[self.symbol].GetLastData()

            # log price from market close of 10/7/2013 at 4 PM
            self.Log(f"{self.Time} - latest price: {lastData.EndTime} - {lastData.Price}")

            # this market order will be filled at the open of the next hourly bar (10:00 AM)
            self.SetHoldings(self.symbol, 0.85)

            if self.Portfolio.Invested:
                # order filled at price of last market close
                raise Exception("Unexpected fill on fill-forward bar with stale price.")

        if self.Time.hour == 9 and self.Time.minute == 50:
            openOrders = self.Transactions.GetOpenOrders(self.symbol)
            if not openOrders:
                raise Exception("Pending market order was expected on current bar.")

        elif self.Time.hour == 10 and self.Time.minute == 0:
             if not self.Portfolio.Invested:
                raise Exception("Order fill was expected on current bar.")
