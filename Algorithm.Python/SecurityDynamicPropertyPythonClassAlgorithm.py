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
    def Initialize(self):
        self.SetStartDate(2013, 10, 7)
        self.SetEndDate(2013, 10, 7)

        self.spy = self.AddEquity("SPY", Resolution.Minute)

        customSMA = CustomSimpleMovingAverage('custom', 60)
        self.spy.CustomSMA = customSMA
        customSMA.Security = self.spy

        self.RegisterIndicator(self.spy.Symbol, self.spy.CustomSMA,  Resolution.Minute)


    def OnWarmupFinished(self) -> None:
        if type(self.spy.CustomSMA) != CustomSimpleMovingAverage:
            raise Exception("spy.CustomSMA is not an instance of CustomSimpleMovingAverage")

        if self.spy.CustomSMA.Security is None:
            raise Exception("spy.CustomSMA.Security is None")
        else:
            self.Debug(f"spy.CustomSMA.Security.Symbol: {self.spy.CustomSMA.Security.Symbol}")

    def OnData(self, slice: Slice) -> None:
        if self.spy.CustomSMA.IsReady:
            self.Debug(f"CustomSMA: {self.spy.CustomSMA.Current.Value}")

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        super().__init__()
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen
