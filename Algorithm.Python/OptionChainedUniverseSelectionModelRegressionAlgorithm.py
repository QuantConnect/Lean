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
### Regression algorithm to test the OptionChainedUniverseSelectionModel class
### </summary>
class OptionChainedUniverseSelectionModelRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.UniverseSettings.Resolution = Resolution.Minute
        self.SetStartDate(2014, 6, 6)
        self.SetEndDate(2014, 6, 6)
        self.SetCash(100000)
        
        universe = self.AddUniverse("my-minute-universe-name", lambda time: [ "AAPL", "TWX" ])
        self.AddUniverseSelection(
            OptionChainedUniverseSelectionModel(
                universe,
                lambda u: (u.Strikes(-2, +2)
                                     # Expiration method accepts TimeSpan objects or integer for days.
                                     # The following statements yield the same filtering criteria
                                     .Expiration(0, 180))
            )
        )
        
    def OnData(self, slice):
        if self.Portfolio.Invested or not (self.IsMarketOpen("AAPL") and self.IsMarketOpen("AAPL")): return
        values = list(map(lambda x: x.Value, filter(lambda x: x.Key == "?AAPL" or x.Key == "?TWX",  slice.OptionChains)))
        for chain in values:
            # we sort the contracts to find at the money (ATM) contract with farthest expiration
            contracts = sorted(sorted(sorted(chain, \
                key = lambda x: abs(chain.Underlying.Price - x.ScaledStrike)), \
                key = lambda x: x.Expiry, reverse=True), \
                key = lambda x: x.Right, reverse=True)

            # if found, trade it
            if len(contracts) == 0: return
            symbol = contracts[0].Symbol
            self.MarketOrder(symbol, 1)
            self.MarketOnCloseOrder(symbol, -1)
            
