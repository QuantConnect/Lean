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
### Regression algorithm asserting that register_indicator accepts calendar periods, like self.consolidate does.
### </summary>
class RegisterIndicatorWithCalendarRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2014, 5, 5)
        self.set_end_date(2014, 5, 12)

        eurusd = self.add_forex("EURUSD", Resolution.MINUTE, Market.OANDA).symbol

        self._weekly_rsi = RelativeStrengthIndex(2)
        self.register_indicator(eurusd, self._weekly_rsi, Calendar.WEEKLY)

    def on_end_of_algorithm(self):
        if self._weekly_rsi.samples == 0:
            raise AssertionError("Expected the weekly RSI to be updated when the week boundary was crossed")
