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
### Algorithm asserting that consolidated bars are of type `QuoteBar` when `QCAlgorithm.Consolidate()` is called with `tickType=TickType.Quote`
### </summary>
class CorrectConsolidatedBarTypeForTickTypesAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 7)

        symbol = self.AddEquity("SPY", Resolution.Tick).Symbol

        self.Consolidate(symbol, timedelta(minutes=1), TickType.Quote, self.quote_tick_consolidation_handler)
        self.Consolidate(symbol, timedelta(minutes=1), TickType.Trade, self.trade_tick_consolidation_handler)

        self.quote_tick_consolidation_handler_called = False
        self.trade_tick_consolidation_handler_called = False

    def OnData(self, slice: Slice) -> None:
        if self.Time.hour > 9:
            self.Quit("Early quit to save time")

    def OnEndOfAlgorithm(self):
        if not self.quote_tick_consolidation_handler_called:
            raise Exception("quote_tick_consolidation_handler was not called")

        if not self.trade_tick_consolidation_handler_called:
            raise Exception("trade_tick_consolidation_handler was not called")

    def quote_tick_consolidation_handler(self, consolidated_bar: QuoteBar) -> None:
        if type(consolidated_bar) != QuoteBar:
            raise Exception(f"Expected the consolidated bar to be of type {QuoteBar} but was {type(consolidated_bar)}")

        self.quote_tick_consolidation_handler_called = True

    def trade_tick_consolidation_handler(self, consolidated_bar: TradeBar) -> None:
        if type(consolidated_bar) != TradeBar:
            raise Exception(f"Expected the consolidated bar to be of type {TradeBar} but was {type(consolidated_bar)}")

        self.trade_tick_consolidation_handler_called = True
