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
### Regression algorithm asserting consolidator registration ergonomics on a quote-only feed (forex):
### trade bar consolidators and indicators are fed collapsed quote bars instead of being rejected,
### register_indicator accepts calendar periods, and consolidator periods smaller than the subscription
### period are rejected at registration time instead of when the first data point arrives.
### </summary>
class ConsolidatorAutoAdaptationRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2014, 5, 5)
        self.set_end_date(2014, 5, 12)

        eurusd = self.add_forex("EURUSD", Resolution.MINUTE, Market.OANDA).symbol
        self._adapted_trade_bars = 0

        # a trade bar consolidator on a quote-only feed: the engine feeds it quote bars collapsed
        # into mid-point trade bars with zero volume
        self._consolidator = TradeBarConsolidator(timedelta(hours=1))
        self._consolidator.data_consolidated += self._on_trade_bar
        self.subscription_manager.add_consolidator(eurusd, self._consolidator)

        # a trade bar indicator on a quote-only feed is fed collapsed trade bars as well
        self._obv = self.obv(eurusd, Resolution.HOUR)

        # register_indicator accepts calendar periods, like self.consolidate does
        self._weekly_rsi = RelativeStrengthIndex(2)
        self.register_indicator(eurusd, self._weekly_rsi, Calendar.WEEKLY)

        # a consolidator period smaller than the subscription period is rejected at registration time,
        # instead of when the first data point arrives
        rejected = False
        try:
            self.subscription_manager.add_consolidator(eurusd, TradeBarConsolidator(timedelta(seconds=10)))
        except:
            # expected, all the required information is available at registration time
            rejected = True
        if not rejected:
            raise AssertionError("Expected an error for a consolidator period smaller than the subscription period")

    def _on_trade_bar(self, sender, bar):
        self._adapted_trade_bars += 1
        if bar.volume != 0:
            raise AssertionError("Expected trade bars collapsed from quote bars to have zero volume")

    def on_end_of_algorithm(self):
        if self._adapted_trade_bars == 0:
            raise AssertionError("Expected the adapted trade bar consolidator to receive data")
        if self._obv.samples == 0:
            raise AssertionError("Expected the OnBalanceVolume indicator to be updated with collapsed trade bars")
        if self._weekly_rsi.samples == 0:
            raise AssertionError("Expected the weekly RSI to be updated when the week boundary was crossed")
