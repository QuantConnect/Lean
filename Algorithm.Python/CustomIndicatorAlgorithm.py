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

### <summary>
### Demonstrates how to create a custom indicator and register it for automatic updated
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="custom indicator" />
class CustomIndicatorAlgorithm(QCAlgorithm):
    def initialize(self) -> None:
        self.set_start_date(2013,10,7)
        self.set_end_date(2013,10,11)
        self.add_equity("SPY", Resolution.SECOND)

        # Create a QuantConnect indicator and a python custom indicator for comparison
        self._sma = self.sma("SPY", 60, Resolution.MINUTE)
        self._custom = CustomSimpleMovingAverage('custom', 60)

        # The python custom class must inherit from PythonIndicator to enable Updated event handler
        self._custom.updated += self.custom_updated

        self._custom_window = RollingWindow[IndicatorDataPoint](5)
        self.register_indicator("SPY", self._custom, Resolution.MINUTE)
        self.plot_indicator('CSMA', self._custom)

    def custom_updated(self, sender: object, updated: IndicatorDataPoint) -> None:
        self._custom_window.add(updated)

    def on_data(self, data: Slice) -> None:
        if not self.portfolio.invested:
            self.set_holdings("SPY", 1)

        if self.time.second == 0:
            self.log(f"   sma -> IsReady: {self._sma.is_ready}. Value: {self._sma.current.value}")
            self.log(f"custom -> IsReady: {self._custom.is_ready}. Value: {self._custom.value}")

        # Regression test: test fails with an early quit
        diff = abs(self._custom.value - self._sma.current.value)
        if diff > 1e-10:
            self.quit(f"Quit: indicators difference is {diff}")

    def on_end_of_algorithm(self) -> None:
        for item in self._custom_window:
            self.log(f'{item}')

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA).
class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name: str, period: int) -> None:
        super().__init__()
        self.name = name
        self.value = 0
        self._queue = deque(maxlen=period)

    # Update method is mandatory
    def update(self, input: IndicatorDataPoint) -> bool:
        self._queue.appendleft(input.value)
        count = len(self._queue)
        self.value = np.sum(self._queue) / count
        return count == self._queue.maxlen
