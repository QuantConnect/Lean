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
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Securities.Option import OptionStrategies
from datetime import datetime, timedelta

### <summary>
### This algorithm demonstrate how to use Option Strategies (e.g. OptionStrategies.Straddle) helper classes to batch send orders for common strategies.
### It also shows how you can prefilter contracts easily based on strikes and expirations, and how you can inspect the
### option chain to pick a specific option contract to trade.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="options" />
### <meta name="tag" content="option strategies" />
### <meta name="tag" content="filter selection" />
class BasicTemplateOptionStrategyAlgorithm(QCAlgorithm):

    def Initialize(self):
        # Set the cash we'd like to use for our backtest
        self.SetCash(1000000)

        # Start and end dates for the backtest.
        self.SetStartDate(2015,12,24)
        self.SetEndDate(2015,12,24)

        # Add assets you'd like to see
        option = self.AddOption("GOOG")
        self.option_symbol = option.Symbol

        # set our strike/expiry filter for this option chain
        option.SetFilter(-2, +2, timedelta(0), timedelta(180))

        # use the underlying equity as the benchmark
        self.SetBenchmark("GOOG")

    def OnData(self,slice):
        if not self.Portfolio.Invested:
            for kvp in slice.OptionChains:
                chain = kvp.Value
                contracts = sorted(sorted(chain, key = lambda x: abs(chain.Underlying.Price - x.Strike)),
                                                 key = lambda x: x.Expiry, reverse=False)

                if len(contracts) == 0: continue
                atmStraddle = contracts[0]
                if atmStraddle != None:
                    self.Sell(OptionStrategies.Straddle(self.option_symbol, atmStraddle.Strike, atmStraddle.Expiry), 2)
        else:
            self.Liquidate()

    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent))