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

from collections import deque
from math import isclose

class CustomIndicatorWithExtensionAlgorithm(QCAlgorithm):
    def initialize(self) -> None:
        self.set_start_date(2013, 10, 9)
        self.set_end_date(2013, 10, 9)

        self._spy = self.add_equity("SPY", Resolution.MINUTE).symbol

        self._sma_values = []
        self._period = 10

        self._sma = self.sma(self._spy, self._period, Resolution.MINUTE)
        self._sma.updated += self.on_sma_updated

        self._custom_sma = CustomSimpleMovingAverage("My SMA", self._period)
        self._ext = IndicatorExtensions.of(self._custom_sma, self._sma)
        self._ext.updated += self.on_indicator_extension_updated

        self._sma_minus_custom = IndicatorExtensions.minus(self._sma, self._custom_sma)
        self._sma_minus_custom.updated += self.on_minus_updated

        self._sma_was_updated = False
        self._custom_sma_was_updated = False
        self._sma_minus_custom_was_updated = False

    def on_sma_updated(self, sender: object, updated: IndicatorDataPoint) -> None:
        self._sma_was_updated = True

        if self._sma.is_ready:
            self._sma_values.append(self._sma.current.value)

    def on_indicator_extension_updated(self, sender: object, updated: IndicatorDataPoint) -> None:
        self._custom_sma_was_updated = True

        sma_last_values = self._sma_values[-self._period:]
        expected = sum(sma_last_values) / len(sma_last_values)

        if not isclose(expected, self._custom_sma.value):
            raise AssertionError(f"Expected the custom SMA to calculate the moving average of the last {self._period} values of the SMA. "
                            f"Current expected: {expected}. Actual {self._custom_sma.value}.")

        self.debug(f"{self._sma.current.value} :: {self._custom_sma.value} :: {updated}")

    def on_minus_updated(self, sender: object, updated: IndicatorDataPoint) -> None:
        self._sma_minus_custom_was_updated = True

        expected = self._sma.current.value - self._custom_sma.value

        if not isclose(expected, self._sma_minus_custom.current.value):
            raise AssertionError(f"Expected the composite minus indicator to calculate the difference between the SMA and custom SMA indicators. "
                            f"Expected: {expected}. Actual {self._sma_minus_custom.current.value}.")

    def on_end_of_algorithm(self) -> None:
        if not (self._sma_was_updated and self._custom_sma_was_updated and self._sma_minus_custom_was_updated):
            raise AssertionError("Expected all indicators to have been updated.")

# Custom indicator
class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name: str, period: int) -> None:
        self.name = name
        self.value = 0
        self.warm_up_period = period
        self._queue = deque(maxlen=period)

    def update(self, input: BaseData) -> bool:
        self._queue.appendleft(input.value)
        count = len(self._queue)
        self.value = sum(self._queue) / count

        return count == self._queue.maxlen
