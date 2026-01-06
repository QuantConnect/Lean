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
### Regression algorithm testing manual updates for custom indicators.
### Ensures the indicator updates correctly and fires its Updated event.
### </summary>
class CustomIndicatorManualUpdatesRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 8)
        self.set_cash(100000)
        
        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._sma = CustomIndicator(10)
        self._sma.updated += self._on_indicator_updated
        self._event_count = 0
    
    def _on_indicator_updated(self, sender, updated) -> None:
        self._event_count += 1

    def on_data(self, data) -> None:
        bar = data.bars.get(self._symbol)
        if bar:
            # Update the indicator manually
            self._sma.update(bar)
    
    def on_end_of_algorithm(self) -> None:
        if not self._sma.is_ready:
            raise RegressionTestException("CustomIndicator should be ready")
        
        if self._sma.samples != self._event_count:
            raise RegressionTestException("Samples and triggered events should be equal.")

class CustomIndicator(PythonIndicator):
    
    def __init__(self, period):
        super().__init__(self) # Must pass self to the base class for proper initialization
        self.name = "CustomSMA"
        self.value = 0.0
        self._is_ready = False
        self._queue = deque(maxlen=period)
        self._period = period
        self._sum = 0.0
        self.warm_up_period = period

    @property
    def is_ready(self):
        return self._is_ready

    def compute_next_value(self, input):
        self._queue.append(input.value)
        self._sum += input.value

        if len(self._queue) > self._period:
            self._sum -= self._queue.popleft()

        if not self._is_ready and len(self._queue) == self._period:
            self._is_ready = True

        return (self._sum / len(self._queue)) if self._is_ready else 0.0
        
    