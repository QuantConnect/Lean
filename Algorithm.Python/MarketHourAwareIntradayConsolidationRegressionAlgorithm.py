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
### Regression algorithm asserting that a MarketHourAwareConsolidator with an intraday period
### anchors each bar to the market open and never lets a bar extend past the market close.
### </summary>
class MarketHourAwareIntradayConsolidationRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 6)
        self.set_end_date(2013, 10, 11)

        self._period = timedelta(minutes=7)
        self._consolidated_bar_count = 0

        self._future = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE, extended_market_hours=True)
        self._hours = self._future.exchange.hours

        consolidator = MarketHourAwareConsolidator(False, self._period, TradeBar, TickType.TRADE, True)
        consolidator.data_consolidated += self._on_seven_minute_bar
        self.subscription_manager.add_consolidator(self._future.symbol, consolidator)

    def _on_seven_minute_bar(self, sender, consolidated):
        bar = consolidated
        market_open = self._hours.get_previous_market_open(bar.time + timedelta(microseconds=1), True)
        market_close = self._hours.get_next_market_close(market_open, True)

        # the bar must be anchored to the market open
        if (bar.time - market_open) % self._period != timedelta(0):
            raise RegressionTestException(f"Bar starting at {bar.time} is not anchored to the market open {market_open}")

        # the bar must not extend past the market close
        if bar.end_time > market_close:
            raise RegressionTestException(f"Bar ending at {bar.end_time} extends past the market close {market_close}")

        # bars span the full period unless the last one is clipped at the market close
        bar_period = bar.end_time - bar.time
        if bar_period != self._period and bar.end_time != market_close:
            raise RegressionTestException(f"Bar from {bar.time} to {bar.end_time} has period {bar_period} instead of {self._period}")

        self._consolidated_bar_count += 1

    def on_end_of_algorithm(self):
        if self._consolidated_bar_count == 0:
            raise RegressionTestException("The consolidator did not produce any bar")
