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
### Asserts that indicators registered with a consolidator are automatically warmed up when
### 'settings.automatic_indicator_warm_up' is enabled, without requiring a manual history replay
### </summary>
class AutomaticIndicatorWarmupConsolidatorRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 8)
        self.set_end_date(2013, 10, 9)

        self.settings.automatic_indicator_warm_up = True
        self._spy = self.add_equity("SPY", Resolution.MINUTE).symbol

        # Test case 1: bar indicator registered with a consolidator, previously required a manual history replay
        self._atr = AverageTrueRange(10)
        self.register_indicator(self._spy, self._atr, TradeBarConsolidator(timedelta(minutes=30)))
        self.assert_is_ready(self._atr, True)

        # Test case 2: data point indicator registered with a consolidator and a selector
        self._rsi = RelativeStrengthIndex(14)
        self.register_indicator(self._spy, self._rsi, TradeBarConsolidator(timedelta(minutes=30)), lambda bar: bar.close)
        self.assert_is_ready(self._rsi, True)

        # Test case 3: non time based consolidators cannot be automatically warmed up, the indicator is
        # registered but left cold
        renko_rsi = RelativeStrengthIndex(14)
        self.register_indicator(self._spy, renko_rsi, RenkoConsolidator(1), lambda bar: bar.close)
        self.assert_is_ready(renko_rsi, False)

        # Test case 4: with the setting disabled nothing is warmed up
        self.settings.automatic_indicator_warm_up = False
        not_warmed = AverageTrueRange(10)
        self.register_indicator(self._spy, not_warmed, TradeBarConsolidator(timedelta(minutes=30)))
        self.assert_is_ready(not_warmed, False)
        self.settings.automatic_indicator_warm_up = True

    def assert_is_ready(self, indicator, expected):
        if indicator.is_ready != expected:
            raise Exception(f"Expected {indicator.name} is_ready to be {expected} but was {indicator.is_ready}")

    def on_data(self, data):
        if not self.portfolio.invested:
            self.set_holdings(self._spy, 1)
