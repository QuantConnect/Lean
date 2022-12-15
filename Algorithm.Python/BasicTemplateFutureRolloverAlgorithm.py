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
### Example algorithm for trading continuous future
### </summary>
class BasicTemplateFutureRolloverAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetCash(1000000)
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2014, 10, 10)

        # Requesting data
        self.continuous_contract = self.AddFuture(Futures.Indices.SP500EMini,
            resolution = Resolution.Daily,
            dataNormalizationMode = DataNormalizationMode.BackwardsRatio,
            dataMappingMode = DataMappingMode.OpenInterest,
            contractDepthOffset = 0
        )
        self.symbol = self.continuous_contract.Symbol
        self.old_symbol = None

    def OnData(self, slice):
        # Accessing data
        for changedEvent in slice.SymbolChangedEvents.Values:
            if changedEvent.Symbol == self.symbol:
                self.old_symbol = changedEvent.OldSymbol
                self.Log(f"Symbol changed at {self.Time}: {self.old_symbol} -> {changedEvent.NewSymbol}")

        mapped_symbol = self.continuous_contract.Mapped

        if not slice.Bars.ContainsKey(self.symbol) or not mapped_symbol:
            return

        # Rolling over: to liquidate any position of the old mapped contract and switch to the newly mapped contract
        if self.old_symbol:
            self.Liquidate(self.old_symbol)
            self.MarketOrder(mapped_symbol, 1)
            self.old_symbol = None