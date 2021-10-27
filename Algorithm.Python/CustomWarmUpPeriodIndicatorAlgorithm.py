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
### Regression test to check python indicator is keeping backwards compatibility 
### with indicators that do not set WarmUpPeriod or do not inherit from PythonIndicator class.
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="custom indicator" />
### <meta name="tag" content="regression test" />
class CustomWarmUpPeriodIndicatorAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.AddEquity("SPY", Resolution.Second)

        # Create three python indicators
        # - customNotWarmUp does not define WarmUpPeriod parameter
        # - customWarmUp defines WarmUpPeriod parameter
        # - customNotInherit defines WarmUpPeriod parameter but does not inherit from PythonIndicator class
        # - csharpIndicator defines WarmUpPeriod parameter and represents the traditional LEAN C# indicator
        self.customNotWarmUp = CSMANotWarmUp('customNotWarmUp', 60)
        self.customWarmUp = CSMAWithWarmUp('customWarmUp', 60)
        self.customNotInherit = CustomSMA('customNotInherit', 60)
        self.csharpIndicator = SimpleMovingAverage('csharpIndicator', 60)

        # Register the daily data of "SPY" to automatically update the indicators
        self.RegisterIndicator("SPY", self.customWarmUp, Resolution.Minute)
        self.RegisterIndicator("SPY", self.customNotWarmUp, Resolution.Minute)
        self.RegisterIndicator("SPY", self.customNotInherit, Resolution.Minute)
        self.RegisterIndicator("SPY", self.csharpIndicator, Resolution.Minute)

        # Warm up customWarmUp indicator
        self.WarmUpIndicator("SPY", self.customWarmUp, Resolution.Minute)

        # Check customWarmUp indicator has already been warmed up with the requested data
        assert(self.customWarmUp.IsReady), "customWarmUp indicator was expected to be ready"
        assert(self.customWarmUp.Samples == 60), "customWarmUp indicator was expected to have processed 60 datapoints already"

        # Try to warm up customNotWarmUp indicator. It's expected from LEAN to skip the warm up process
        # because this indicator doesn't define WarmUpPeriod parameter
        self.WarmUpIndicator("SPY", self.customNotWarmUp, Resolution.Minute)

        # Check customNotWarmUp indicator is not ready and is using the default WarmUpPeriod value
        assert(not self.customNotWarmUp.IsReady), "customNotWarmUp indicator wasn't expected to be warmed up"
        assert(self.customNotWarmUp.WarmUpPeriod == 0), "customNotWarmUp indicator WarmUpPeriod parameter was expected to be 0"

        # Warm up customNotInherit indicator. Though it does not inherit from PythonIndicator class,
        # it defines WarmUpPeriod parameter so it's expected to be warmed up from LEAN
        self.WarmUpIndicator("SPY", self.customNotInherit, Resolution.Minute)

        # Check customNotInherit indicator has already been warmed up with the requested data
        assert(self.customNotInherit.IsReady), "customNotInherit indicator was expected to be ready"
        assert(self.customNotInherit.Samples == 60), "customNotInherit indicator was expected to have processed 60 datapoints already"

        # Warm up csharpIndicator
        self.WarmUpIndicator("SPY", self.csharpIndicator, Resolution.Minute)

        # Check csharpIndicator indicator has already been warmed up with the requested data
        assert(self.csharpIndicator.IsReady), "csharpIndicator indicator was expected to be ready"
        assert(self.csharpIndicator.Samples == 60), "csharpIndicator indicator was expected to have processed 60 datapoints already"

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

        if self.Time.second == 0:
            # Compute the difference between indicators values
            diff = abs(self.customNotWarmUp.Current.Value - self.customWarmUp.Current.Value)
            diff += abs(self.customNotInherit.Value - self.customNotWarmUp.Current.Value)
            diff += abs(self.customNotInherit.Value - self.customWarmUp.Current.Value)
            diff += abs(self.csharpIndicator.Current.Value - self.customWarmUp.Current.Value)
            diff += abs(self.csharpIndicator.Current.Value - self.customNotWarmUp.Current.Value)
            diff += abs(self.csharpIndicator.Current.Value - self.customNotInherit.Value)

            # Check customNotWarmUp indicator is ready when the number of samples is bigger than its WarmUpPeriod parameter
            assert(self.customNotWarmUp.IsReady == (self.customNotWarmUp.Samples >= 60)), "customNotWarmUp indicator was expected to be ready when the number of samples were bigger that its WarmUpPeriod parameter"

            # Check their values are the same. We only need to check if customNotWarmUp indicator is ready because the other ones has already been asserted to be ready
            assert(diff <= 1e-10 or (not self.customNotWarmUp.IsReady)), f"The values of the indicators are not the same. Indicators difference is {diff}"
            
# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) without Warm Up Period parameter defined
class CSMANotWarmUp(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) With Warm Up Period parameter defined
class CSMAWithWarmUp(CSMANotWarmUp):
    def __init__(self, name, period):
        super().__init__(name, period)
        self.WarmUpPeriod = period

# Custom python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA)
class CustomSMA():
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)
        self.WarmUpPeriod = period
        self.IsReady = False
        self.Samples = 0

    # Update method is mandatory
    def Update(self, input):
        self.Samples += 1
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        if count == self.queue.maxlen:
            self.IsReady = True
        return self.IsReady
