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
from QuantConnect.Data import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *

### <summary>
### Demonstration algorthm for the Warm Up feature with basic indicators.
### </summary>
### <meta name="tag" content="indicators" />
### <meta name="tag" content="warm up" />
### <meta name="tag" content="history and warm up" />
### <meta name="tag" content="using data" />
class WarmupAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10,8)   #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY", Resolution.Second)

        fast_period = 60
        slow_period = 3600

        self.fast = self.EMA("SPY", fast_period)
        self.slow = self.EMA("SPY", slow_period)

        self.SetWarmup(slow_period)
        self.first = True


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''
        if self.first and not self.IsWarmingUp:
            self.first = False
            self.Log("Fast: {0}".format(self.fast.Samples))
            self.Log("Slow: {0}".format(self.slow.Samples))

        if self.fast.Current.Value > self.slow.Current.Value:
            self.SetHoldings("SPY", 1)
        else:
            self.SetHoldings("SPY", -1)