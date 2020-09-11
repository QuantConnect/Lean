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
from datetime import timedelta

### <summary>
### This regression algorithm is for testing a custom Python filter for options
### that returns a OptionFilterUniverse.
### </summary>
### <meta name="tag" content="options" />
### <meta name="tag" content="filter selection" />
### <meta name="tag" content="regression test" />
class FilterUniverseRegressionAlgorithm(QCAlgorithm):
    UnderlyingTicker = "GOOG"

    def Initialize(self):
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)
        self.SetCash(100000)

        equity = self.AddEquity(self.UnderlyingTicker)
        option = self.AddOption(self.UnderlyingTicker)
        self.OptionSymbol = option.Symbol

        # Set our custom universe filter
        option.SetFilter(self.FilterFunction)

        # use the underlying equity as the benchmark
        self.SetBenchmark(equity.Symbol)

    def FilterFunction(self, universe):
        universe = universe.WeeklysOnly().Strikes(-5, +5).CallsOnly().Expiration(0, 1)
        return universe

    def OnData(self,slice):
        if self.Portfolio.Invested: return

        for kvp in slice.OptionChains:
            
            if kvp.Key != self.OptionSymbol: continue

            chain = kvp.Value
            contracts = [option for option in sorted(chain, key = lambda x:x.Strike, reverse = True)]
            
            if contracts:
                self.MarketOrder(contracts[0].Symbol, 1)

