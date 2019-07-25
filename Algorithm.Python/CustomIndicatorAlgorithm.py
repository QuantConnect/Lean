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

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Algorithm")
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Algorithm import *
from collections import deque
from datetime import datetime, timedelta
from numpy import sum

### <summary>
### Demonstrates how to create a custom indicator and register it for automatic updated
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
### <meta name="tag" content="custom indicator" />
class CustomIndicatorAlgorithm(QCAlgorithm):
    def Initialize(self):
        self.SetStartDate(2013,10,7)
        self.SetEndDate(2013,10,11)
        self.AddEquity("SPY", Resolution.Second)

        # Create a QuantConnect indicator and a python custom indicator for comparison
        self.sma = self.SMA("SPY", 60, Resolution.Minute)
        self.custom = CustomSimpleMovingAverage('custom', 60)

        # The python custom class must inherit from PythonIndicator to enable Updated event handler
        self.custom.Updated += self.CustomUpdated

        self.customWindow = RollingWindow[IndicatorDataPoint](5)
        self.RegisterIndicator("SPY", self.custom, Resolution.Minute)
        self.PlotIndicator('cSMA', self.custom)

    def CustomUpdated(self, sender, updated):
        self.customWindow.Add(updated)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

        if self.Time.second == 0:
            self.Log(f"   sma -> IsReady: {self.sma.IsReady}. Value: {self.sma.Current.Value}")
            self.Log(f"custom -> IsReady: {self.custom.IsReady}. Value: {self.custom.Value}")

        # Regression test: test fails with an early quit
        diff = abs(self.custom.Value - self.sma.Current.Value)
        if diff > 1e-10:
            self.Quit(f"Quit: indicators difference is {diff}")

    def OnEndOfAlgorithm(self):
        for item in self.customWindow:
            self.Log(f'{item}')

# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA).
class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = sum(self.queue) / count
        return count == self.queue.maxlen