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
    def initialize(self):
        self.set_start_date(2013, 10, 6)
        self.set_end_date(2013, 10, 7)
        self.set_cash(1000000)

        equity = self.add_equity("SPY", Resolution.TICK, Market.USA)
        quote_consolidator = self.consolidate(equity.symbol, Resolution.TICK, TickType.QUOTE, lambda tick : self.on_quote_tick(tick))
        self.there_is_at_least_one_quote_tick = False

        trade_consolidator = self.consolidate(equity.symbol, Resolution.TICK, TickType.TRADE, lambda tick : self.on_trade_tick(tick))
        self.there_is_at_least_one_trade_tick = False

        # Tick consolidators with max count
        self.consolidate(TradeBar, equity.symbol, 10, TickType.TRADE, lambda trade_bar: self.on_trade_tick_max_count(trade_bar))
        self._there_is_at_least_one_trade_bar = False

        self.consolidate(QuoteBar, equity.symbol, 10, TickType.QUOTE, lambda quote_bar: self.on_quote_tick_max_count(quote_bar))
        self._there_is_at_least_one_quote_bar = False

        self._consolidation_count = 0

    def on_trade_tick_max_count(self, trade_bar):
        self._there_is_at_least_one_trade_bar = True
        if type(trade_bar) != TradeBar:
            raise AssertionError(f"The type of the bar should be Trade, but was {type(trade_bar)}")

    def on_quote_tick_max_count(self, quote_bar):
        self._there_is_at_least_one_quote_bar = True
        if type(quote_bar) != QuoteBar:
            raise AssertionError(f"The type of the bar should be Quote, but was {type(quote_bar)}")

        self._consolidation_count += 1
        # Let's shortcut to reduce regression test duration: algorithms using tick data are too long
        if self._consolidation_count >= 1000:
            self.quit()

    def on_quote_tick(self, tick):
        self.there_is_at_least_one_quote_tick = True
        if tick.tick_type != TickType.QUOTE:
            raise AssertionError(f"The type of the tick should be Quote, but was {tick.tick_type}")

    def on_trade_tick(self, tick):
        self.there_is_at_least_one_trade_tick = True
        if tick.tick_type != TickType.TRADE:
            raise AssertionError(f"The type of the tick should be Trade, but was {tick.tick_type}")

    def on_end_of_algorithm(self):
        if not self.there_is_at_least_one_quote_tick:
            raise AssertionError(f"There should have been at least one tick in OnQuoteTick() method, but there wasn't")

        if not self.there_is_at_least_one_trade_tick:
            raise AssertionError(f"There should have been at least one tick in OnTradeTick() method, but there wasn't")

        if not self._there_is_at_least_one_trade_bar:
            raise AssertionError("There should have been at least one bar in OnTradeTickMaxCount() method, but there wasn't")

        if not self._there_is_at_least_one_quote_bar:
            raise AssertionError("There should have been at least one bar in OnQuoteTickMaxCount() method, but there wasn't")

