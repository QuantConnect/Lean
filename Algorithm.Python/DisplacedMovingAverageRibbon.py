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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QCAlgorithm import QCAlgorithm
import numpy as np
from datetime import datetime

### <summary>
### Constructs a displaced moving average ribbon and buys when all are lined up, liquidates when they all line down
### Ribbons are great for visualizing trends
### Signals are generated when they all line up in a paricular direction
### A buy signal is when the values of the indicators are increasing (from slowest to fastest).
### A sell signal is when the values of the indicators are decreasing (from slowest to fastest).
### </summary>
### <meta name="tag" content="charting" />
### <meta name="tag" content="plotting indicators" />
### <meta name="tag" content="indicators" />
### <meta name="tag" content="indicator classes" />
class DisplacedMovingAverageRibbon(QCAlgorithm):

    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def Initialize(self):
        self.SetStartDate(2009, 1, 1)  #Set Start Date
        self.SetEndDate(2015, 1, 1)    #Set End Date
        self.spy = self.AddEquity("SPY", Resolution.Daily).Symbol
        count = 6
        offset = 5
        period = 15
        self.ribbon = []
        # define our sma as the base of the ribbon
        self.sma = SimpleMovingAverage(period)
        
        for x in range(count):
            # define our offset to the zero sma, these various offsets will create our 'displaced' ribbon
            delay = Delay(offset*(x+1))
            # define an indicator that takes the output of the sma and pipes it into our delay indicator
            delayedSma = IndicatorExtensions.Of(delay, self.sma)
            # register our new 'delayedSma' for automaic updates on a daily resolution
            self.RegisterIndicator(self.spy, delayedSma, Resolution.Daily)
            self.ribbon.append(delayedSma)
        self.previous = datetime.min
        # plot indicators each time they update using the PlotIndicator function
        for i in self.ribbon:
            self.PlotIndicator("Ribbon", i) 

    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    def OnData(self, data):
        
        if data[self.spy] is None: return
        # wait for our entire ribbon to be ready
        if not all(x.IsReady for x in self.ribbon): return
        # only once per day
        if self.previous.date() == self.Time.date(): return
        self.Plot("Ribbon", "Price", data[self.spy].Price)

        # check for a buy signal
        values = [x.Current.Value for x in self.ribbon]
        holding = self.Portfolio[self.spy]
        if (holding.Quantity <= 0 and self.IsAscending(values)):
            self.SetHoldings(self.spy, 1.0)
        elif (holding.Quantity > 0 and self.IsDescending(values)):
            self.Liquidate(self.spy)
        self.previous = self.Time
    
    # Returns true if the specified values are in ascending order
    def IsAscending(self, values):
        last = None
        for val in values:
            if last is None:
                last = val
                continue
            if last < val:
                return False
            last = val
        return True
    
    # Returns true if the specified values are in Descending order
    def IsDescending(self, values):
        last = None
        for val in values:
            if last is None:
                last = val
                continue
            if last > val:
                return False
            last = val
        return True