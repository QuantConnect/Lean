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
AddReference("QuantConnect.Indicators")
AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QCAlgorithm import QCAlgorithm
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
        self.RegisterIndicator("SPY", self.custom, Resolution.Minute)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)

        if self.Time.second == 0:
            self.Log("   sma -> IsReady: {0}. Time: {1}. Value: {2}".format(self.sma.IsReady, self.sma.Current.Time, self.sma.Current.Value))
            self.Log(str(self.custom))

        # Regression test: test fails with an early quit
        diff = abs(self.custom.Value - self.sma.Current.Value)
        if diff > 1e-25:
            self.Quit("Quit: indicators difference is {0}".format(diff))


# Python implementation of SimpleMovingAverage.
# Represents the traditional simple moving average indicator (SMA).
class CustomSimpleMovingAverage:
    def __init__(self, name, period):
        self.Name = name
        self.Time = datetime.min
        self.Value = 0
        self.IsReady = False
        self.queue = deque(maxlen=period)

    def __repr__(self):
        return "{0} -> IsReady: {1}. Time: {2}. Value: {3}".format(self.Name, self.IsReady, self.Time, self.Value)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Close)
        count = len(self.queue)
        self.Time = input.EndTime
        self.Value = sum(self.queue) / count
        self.IsReady = count == self.queue.maxlen