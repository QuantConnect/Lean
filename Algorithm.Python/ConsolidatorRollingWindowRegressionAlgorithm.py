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
### Regression algorithm asserting that consolidators expose a built-in rolling window
### </summary>
class ConsolidatorRollingWindowRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        self.add_equity("SPY", Resolution.MINUTE)

        self._consolidation_count = 0
        self._consolidator = TradeBarConsolidator(timedelta(minutes=10))
        self._consolidator.data_consolidated += self._on_data_consolidated
        self.subscription_manager.add_consolidator("SPY", self._consolidator)

    def _on_data_consolidated(self, sender, bar):
        self._consolidation_count += 1

        if self._consolidator.current != self._consolidator[0]:
            raise AssertionError("Expected current to be the same as window[0]")

        # consolidator[0] must always match the bar just fired
        currentBar = self._consolidator[0]
        if currentBar.time != bar.time:
            raise AssertionError(f"Expected consolidator[0].time == {bar.time} but was {currentBar.time}")
        if currentBar.value != bar.close:
            raise AssertionError(f"Expected consolidator[0].value == {bar.close} but was {currentBar.value}")

        # After the second consolidation the previous bar must be at index 1
        if self._consolidator.window.count >= 2:
            previous = self._consolidator[1]
            if self._consolidator.previous != self._consolidator[1]:
                raise AssertionError("Expected previous to be the same as window[1]")
            if previous.time >= bar.time:
                raise AssertionError(
                    f"consolidator[1].time ({previous.time}) should be earlier "
                    f"than consolidator[0].time ({bar.time})"
                )
            if previous.value <= 0:
                raise AssertionError("consolidator[1].value should be greater than zero")

    def on_data(self, data):
        pass

    def on_end_of_algorithm(self):
        if self._consolidation_count == 0:
            raise AssertionError("Expected at least one consolidation but got zero")

        # Default window size is 2, it must be full
        if self._consolidator.window.count != 2:
            raise AssertionError(f"Expected window count of 2 but was {self._consolidator.window.count}")
