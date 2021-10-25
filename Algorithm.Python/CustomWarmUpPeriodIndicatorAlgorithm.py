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
### with indicators that do not set WarmUpPeriod.
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

        # Create two python indicators, one of them defines WarmUpPeriod parameter
        self.custom = CustomSMA('custom', 60)
        self.customWarmUp = CSMAWithWarmUp('customWarmUp', 60)

        # Register the daily data of "SPY" to automatically update both indicators
        self.RegisterIndicator("SPY", self.customWarmUp, Resolution.Minute)
        self.RegisterIndicator("SPY", self.custom, Resolution.Minute)

        # Warm up customWarmUp indicator
        self.WarmUpIndicator("SPY", self.customWarmUp, Resolution.Minute)

        # Check customWarmUp indicator has already been warmed up with the requested data
        assert(self.customWarmUp.IsReady), "customWarmUp indicator was expected to be ready"
        assert(self.customWarmUp.Samples == 60), "customWarmUp was expected to have processed 60 datapoints already"

        # Try to warm up custom indicator. It's expected from LEAN to skip the warm up process
        # because this indicator doesn't implement IIndicatorWarmUpPeriodProvider
        self.WarmUpIndicator("SPY", self.custom, Resolution.Minute)

        # Check custom indicator is not ready and is using the default WarmUpPeriod value
        assert(not self.custom.IsReady), "custom indicator wasn't expected to be warmed up"
        assert(self.custom.WarmUpPeriod == 0), "custom indicator WarmUpPeriod parameter was expected to be 0"

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

        if self.Time.second == 0:
            # Compute the difference between indicators values
            diff = abs(self.custom.Current.Value - self.customWarmUp.Current.Value)

            # Check self.custom indicator is ready when the number of samples is bigger than its WarmUpPeriod parameter
            assert(self.custom.IsReady == (self.custom.Samples >= 60)), "custom indicator was expected to be ready when the number of samples were bigger that its WarmUpPeriod parameter"

            # Check their values are the same when both are ready
            assert(diff <= 1e-10 or (not self.custom.IsReady) or (not self.customWarmUp.IsReady)), f"The values of the indicators are not the same. Indicators difference is {diff}"
            
# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) without Warm Up Period parameter defined
class CustomSMA(PythonIndicator):
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
class CSMAWithWarmUp(CustomSMA):
    def __init__(self, name, period):
        super().__init__(name, period)
        self.WarmUpPeriod = period
