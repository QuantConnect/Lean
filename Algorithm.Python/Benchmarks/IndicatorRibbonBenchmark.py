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
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Indicators")

from System import *
from QuantConnect import *
from QuantConnect.Indicators import *
from QuantConnect.Data import *
from QuantConnect.Data.Market import *
from QuantConnect.Algorithm import *
import numpy as np
from datetime import datetime


class IndicatorRibbonBenchmark(QCAlgorithm):

    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    def Initialize(self):
        self.SetStartDate(2010, 1, 1)  #Set Start Date
        self.SetEndDate(2018, 1, 1)    #Set End Date
        self.spy = self.AddEquity("SPY", Resolution.Minute).Symbol
        count = 50
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

    def OnData(self, data):
        # wait for our entire ribbon to be ready
        if not all(x.IsReady for x in self.ribbon): return
        for x in self.ribbon:
            value = x.Current.Value