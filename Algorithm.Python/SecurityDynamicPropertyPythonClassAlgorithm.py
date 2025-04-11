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

import numpy as np

### <summary>
### Algorithm asserting that security dynamic properties keep Python references to the Python class they are instances of,
### specifically when this class is a subclass of a C# class.
### </summary>
class SecurityDynamicPropertyPythonClassAlgorithm(QCAlgorithm):
    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)

        self._spy = self.add_equity("SPY", Resolution.MINUTE)

        custom_sma = CustomSimpleMovingAverage('custom', 60)
        self._spy.custom_sma = custom_sma
        custom_sma.security = self._spy

        self.register_indicator(self._spy.symbol, self._spy.custom_sma, Resolution.MINUTE)

    def on_warmup_finished(self) -> None:
        if type(self._spy.custom_sma) != CustomSimpleMovingAverage:
            raise AssertionError("spy.custom_sma is not an instance of CustomSimpleMovingAverage")

        if not self._spy.custom_sma.security:
            raise AssertionError("spy.custom_sma.security is None")
        else:
            self.debug(f"spy.custom_sma.security.symbol: {self._spy.custom_sma.security.symbol}")

    def on_data(self, slice: Slice) -> None:
        if self._spy.custom_sma.is_ready:
            self.debug(f"CustomSMA: {self._spy.custom_sma.current.value}")

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name: str, period: int) -> None:
        super().__init__()
        self.name = name
        self.value = 0
        self._queue = deque(maxlen=period)

    def update(self, input: IndicatorDataPoint) -> bool:
        self._queue.appendleft(input.value)
        count = len(self._queue)
        self.value = np.sum(self._queue) / count
        return count == self._queue.maxlen
