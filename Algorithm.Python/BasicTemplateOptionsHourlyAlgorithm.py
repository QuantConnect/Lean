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
### This example demonstrates how to add options for a given underlying equity security.
### It also shows how you can prefilter contracts easily based on strikes and expirations, and how you
### can inspect the option chain to pick a specific option contract to trade.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
class BasicTemplateOptionsHourlyAlgorithm(QCAlgorithm):
    UnderlyingTicker = "AAPL"

    def Initialize(self):
        self.SetStartDate(2014, 6, 6)
        self.SetEndDate(2014, 6, 9)
        self.SetCash(100000)

        equity = self.AddEquity(self.UnderlyingTicker, Resolution.Hour)
        option = self.AddOption(self.UnderlyingTicker, Resolution.Hour)
        self.option_symbol = option.Symbol

        # set our strike/expiry filter for this option chain
        option.SetFilter(lambda u: (u.Strikes(-2, +2)
                                     # Expiration method accepts TimeSpan objects or integer for days.
                                     # The following statements yield the same filtering criteria
                                     .Expiration(0, 180)))
                                     #.Expiration(TimeSpan.Zero, TimeSpan.FromDays(180))))

        # use the underlying equity as the benchmark
        self.SetBenchmark(equity.Symbol)

    def OnData(self,slice):
        if self.Portfolio.Invested or not self.IsMarketOpen(self.option_symbol): return

        chain = slice.OptionChains.GetValue(self.option_symbol)
        if chain is None:
            return

        # we sort the contracts to find at the money (ATM) contract with farthest expiration
        contracts = sorted(sorted(sorted(chain, \
            key = lambda x: abs(chain.Underlying.Price - x.Strike)), \
            key = lambda x: x.Expiry, reverse=True), \
            key = lambda x: x.Right, reverse=True)

        # if found, trade it
        if len(contracts) == 0 or not self.IsMarketOpen(contracts[0].Symbol): return
        symbol = contracts[0].Symbol
        self.MarketOrder(symbol, 1)
        self.MarketOnCloseOrder(symbol, -1)

    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))
