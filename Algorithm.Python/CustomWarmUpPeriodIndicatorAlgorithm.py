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
### Demonstrates how to warm up a custom python indicator
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="custom indicator" />
class CustomWarmUpPeriodIndicatorAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.AddEquity("SPY", Resolution.Second)

        # Create two python indicators one defines Warm Up
        self.custom = CSMAWithoutWarmUp('custom', 60)
        self.customWarmUp = CSMAWithWarmUp('custom', 60)

        # The python custom class must inherit from PythonIndicator to enable Updated event handler
        self.customWarmUp.Updated += self.CustomUpdated

        self.customWindow = RollingWindow[IndicatorDataPoint](5)
        self.RegisterIndicator("SPY", self.customWarmUp, Resolution.Minute)

        self.SetWarmUp(60);

        # Warm Up custom indicator
        # The purpose of this custom indicator is only to warm up some data
        # not necessary the data from SPY symbol in the given resolution and period
        for i in range(60):
            self.custom.Update(i)
        if not self.custom.IsReady():
            raise "custom indicator was expected to be ready"

    def CustomUpdated(self, sender, updated):
        self.customWindow.Add(updated)

    def OnEndOfAlgorithm(self):
        if not self.customWarmUp.IsReady:
            raise "customWarmUp indicator was expected to warmed up already"

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) With Warm Up Period
class CSMAWithWarmUp(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)
        self.Period = period

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) without Warm Up Period
class CSMAWithoutWarmUp():
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)
        self.Period = period

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen
    # Check wheter the indicator is ready or not
    def IsReady(self):
        count = len(self.queue)
        return count == self.queue.maxlen
