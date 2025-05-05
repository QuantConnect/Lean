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
    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.add_equity("SPY", Resolution.SECOND)

        # Create three python indicators
        # - custom_not_warm_up does not define WarmUpPeriod parameter
        # - custom_warm_up defines WarmUpPeriod parameter
        # - custom_not_inherit defines WarmUpPeriod parameter but does not inherit from PythonIndicator class
        # - csharp_indicator defines WarmUpPeriod parameter and represents the traditional LEAN C# indicator
        self._custom_not_warm_up = CSMANotWarmUp('custom_not_warm_up', 60)
        self._custom_warm_up = CSMAWithWarmUp('custom_warm_up', 60)
        self._custom_not_inherit = CustomSMA('custom_not_inherit', 60)
        self._csharp_indicator = SimpleMovingAverage('csharp_indicator', 60)

        # Register the daily data of "SPY" to automatically update the indicators
        self.register_indicator("SPY", self._custom_warm_up, Resolution.MINUTE)
        self.register_indicator("SPY", self._custom_not_warm_up, Resolution.MINUTE)
        self.register_indicator("SPY", self._custom_not_inherit, Resolution.MINUTE)
        self.register_indicator("SPY", self._csharp_indicator, Resolution.MINUTE)

        # Warm up custom_warm_up indicator
        self.warm_up_indicator("SPY", self._custom_warm_up, Resolution.MINUTE)

        # Check custom_warm_up indicator has already been warmed up with the requested data
        assert(self._custom_warm_up.is_ready), "custom_warm_up indicator was expected to be ready"
        assert(self._custom_warm_up.samples == 60), "custom_warm_up indicator was expected to have processed 60 datapoints already"

        # Try to warm up custom_not_warm_up indicator. It's expected from LEAN to skip the warm up process
        # because this indicator doesn't define WarmUpPeriod parameter
        self.warm_up_indicator("SPY", self._custom_not_warm_up, Resolution.MINUTE)

        # Check custom_not_warm_up indicator is not ready and is using the default WarmUpPeriod value
        assert(not self._custom_not_warm_up.is_ready), "custom_not_warm_up indicator wasn't expected to be warmed up"
        assert(self._custom_not_warm_up.warm_up_period == 0), "custom_not_warm_up indicator WarmUpPeriod parameter was expected to be 0"

        # Warm up custom_not_inherit indicator. Though it does not inherit from PythonIndicator class,
        # it defines WarmUpPeriod parameter so it's expected to be warmed up from LEAN
        self.warm_up_indicator("SPY", self._custom_not_inherit, Resolution.MINUTE)

        # Check custom_not_inherit indicator has already been warmed up with the requested data
        assert(self._custom_not_inherit.is_ready), "custom_not_inherit indicator was expected to be ready"
        assert(self._custom_not_inherit.samples == 60), "custom_not_inherit indicator was expected to have processed 60 datapoints already"

        # Warm up csharp_indicator
        self.warm_up_indicator("SPY", self._csharp_indicator, Resolution.MINUTE)

        # Check csharp_indicator indicator has already been warmed up with the requested data
        assert(self._csharp_indicator.is_ready), "csharp_indicator indicator was expected to be ready"
        assert(self._csharp_indicator.samples == 60), "csharp_indicator indicator was expected to have processed 60 datapoints already"

    def on_data(self, data: Slice) -> None:
        if not self.portfolio.invested:
            self.set_holdings("SPY", 1)

        if self.time.second == 0:
            # Compute the difference between indicators values
            diff = abs(self._custom_not_warm_up.current.value - self._custom_warm_up.current.value)
            diff += abs(self._custom_not_inherit.value - self._custom_not_warm_up.current.value)
            diff += abs(self._custom_not_inherit.value - self._custom_warm_up.current.value)
            diff += abs(self._csharp_indicator.current.value - self._custom_warm_up.current.value)
            diff += abs(self._csharp_indicator.current.value - self._custom_not_warm_up.current.value)
            diff += abs(self._csharp_indicator.current.value - self._custom_not_inherit.value)

            # Check custom_not_warm_up indicator is ready when the number of samples is bigger than its WarmUpPeriod parameter
            assert(self._custom_not_warm_up.is_ready == (self._custom_not_warm_up.samples >= 60)), "custom_not_warm_up indicator was expected to be ready when the number of samples were bigger that its WarmUpPeriod parameter"

            # Check their values are the same. We only need to check if custom_not_warm_up indicator is ready because the other ones has already been asserted to be ready
            assert(diff <= 1e-10 or (not self._custom_not_warm_up.is_ready)), f"The values of the indicators are not the same. Indicators difference is {diff}"
            
# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) without Warm Up Period parameter defined
class CSMANotWarmUp(PythonIndicator):
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

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA) With Warm Up Period parameter defined
class CSMAWithWarmUp(CSMANotWarmUp):
    def __init__(self, name: str, period: int) -> None:
        super().__init__(name, period)
        self.warm_up_period = period

# Custom python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA)
class CustomSMA():
    def __init__(self, name: str, period: int) -> None:
        self.name = name
        self.value = 0
        self._queue = deque(maxlen=period)
        self.warm_up_period = period
        self.is_ready = False
        self.samples = 0

    # Update method is mandatory
    def update(self, input: IndicatorDataPoint) -> bool:
        self.samples += 1
        self._queue.appendleft(input.value)
        count = len(self._queue)
        self.value = np.sum(self._queue) / count
        if count == self._queue.maxlen:
            self.is_ready = True
        return self.is_ready
