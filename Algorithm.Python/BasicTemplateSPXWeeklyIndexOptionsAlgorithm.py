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
### This example demonstrates how to add and trade SPX index weekly options
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="indexes" />
class BasicTemplateSPXWeeklyIndexOptionsAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2021, 1, 4)
        self.SetEndDate(2021, 1, 10)
        self.SetCash(1000000)

        self.spx = self.AddIndex("SPX").Symbol

        # regular option SPX contracts
        self.spxOptions = self.AddIndexOption(self.spx);
        self.spxOptions.SetFilter(lambda u: (u.Dynamic().Strikes(0, 1).Expiration(0, 30)))

        # weekly option SPX contracts
        spxw = self.AddIndexOption(self.spx, "SPXW")
        # set our strike/expiry filter for this option chain
        spxw.SetFilter(lambda u: (u.Dynamic()
                                     .Strikes(0, 1)
                                     # single week ahead since there are many SPXW contracts and we want to preserve performance
                                     .Expiration(0, 7)
                                     .IncludeWeeklys()))

        self.spxw_option = spxw.Symbol

    def OnData(self,slice):
        if self.Portfolio.Invested: return

        chain = slice.OptionChains.GetValue(self.spxw_option)
        if chain is None:
            return

        # we sort the contracts to find at the money (ATM) contract with closest expiration
        contracts = sorted(sorted(sorted(chain, \
            key = lambda x: x.Expiry), \
            key = lambda x: abs(chain.Underlying.Price - x.Strike)), \
            key = lambda x: x.Right, reverse=True)

        # if found, buy until it expires
        if len(contracts) == 0: return
        symbol = contracts[0].Symbol
        self.MarketOrder(symbol, 1)

    def OnOrderEvent(self, orderEvent):
        self.Debug(str(orderEvent))
