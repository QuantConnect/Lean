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
### Algorithm asserting that security dynamic properties keep Python references to the Python class they are instances of,
### specifically when this class is a subclass of a C# class.
### </summary>
class SecurityDynamicPropertyPythonClassAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)

        self.spy = self.add_equity("SPY", Resolution.MINUTE)

        customSMA = CustomSimpleMovingAverage('custom', 60)
        self.spy.customSMA = customSMA
        customSMA.security = self.spy

        self.register_indicator(self.spy.symbol, self.spy.customSMA,  Resolution.MINUTE)


    def on_warmup_finished(self) -> None:
        if type(self.spy.customSMA) != CustomSimpleMovingAverage:
            raise Exception("spy.customSMA is not an instance of CustomSimpleMovingAverage")

        if self.spy.customSMA.security is None:
            raise Exception("spy.customSMA.security is None")
        else:
            self.debug(f"spy.customSMA.security.symbol: {self.spy.customSMA.security.symbol}")

    def on_data(self, slice: Slice) -> None:
        if self.spy.customSMA.is_ready:
            self.debug(f"CustomSMA: {self.spy.customSMA.current.value}")

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        super().__init__()
        self.name = name
        self.value = 0
        self.queue = deque(maxlen=period)

    def update(self, input):
        self.queue.appendleft(input.value)
        count = len(self.queue)
        self.value = np.sum(self.queue) / count
        return count == self.queue.maxlen
