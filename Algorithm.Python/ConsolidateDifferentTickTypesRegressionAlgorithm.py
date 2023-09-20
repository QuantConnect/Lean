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
### This algorithm asserts we can consolidate Tick data with different tick types
### </summary>
class ConsolidateDifferentTickTypesRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 6)
        self.SetEndDate(2013, 10, 7)
        self.SetCash(1000000)

        equity = self.AddEquity("SPY", Resolution.Tick, Market.USA)
        quoteConsolidator = self.Consolidate(equity.Symbol, Resolution.Tick, TickType.Quote, lambda tick : self.OnQuoteTick(tick))
        self.thereIsAtLeastOneQuoteTick = False

        tradeConsolidator = self.Consolidate(equity.Symbol, Resolution.Tick, TickType.Trade, lambda tick : self.OnTradeTick(tick))
        self.thereIsAtLeastOneTradeTick = False

    def OnQuoteTick(self, tick):
        self.thereIsAtLeastOneQuoteTick = True
        if tick.TickType != TickType.Quote:
            raise Exception(f"The type of the tick should be Quote, but was {tick.TickType}")

    def OnTradeTick(self, tick):
        self.thereIsAtLeastOneTradeTick = True
        if tick.TickType != TickType.Trade:
            raise Exception(f"The type of the tick should be Trade, but was {tick.TickType}")

    def OnEndOfAlgorithm(self):
        if not self.thereIsAtLeastOneQuoteTick:
            raise Exception(f"There should have been at least one tick in OnQuoteTick() method, but there wasn't")

        if not self.thereIsAtLeastOneTradeTick:
            raise Exception(f"There should have been at least one tick in OnTradeTick() method, but there wasn't")

