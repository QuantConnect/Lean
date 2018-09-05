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
from QuantConnect.Parameters import *
from QCAlgorithm import QCAlgorithm
import decimal as d

### <summary>
### Demonstration of the parameter system of QuantConnect. Using parameters you can pass the values required into C# algorithms for optimization.
### </summary>
### <meta name="tag" content="optimization" />
### <meta name="tag" content="using quantconnect" />
class ParameterizedAlgorithm(QCAlgorithm):

    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013, 10, 7)   #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)             #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddEquity("SPY")

        # Receive parameters from the Job
        ema_fast = self.GetParameter("ema-fast")
        ema_slow = self.GetParameter("ema-slow")

        # The values 100 and 200 are just default values that only used if the parameters do not exist
        fast_period = 100 if ema_fast is None else int(ema_fast)
        slow_period = 200 if ema_slow is None else int(ema_slow)

        self.fast = self.EMA("SPY", fast_period)
        self.slow = self.EMA("SPY", slow_period)


    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.'''

        # wait for our indicators to ready
        if not self.fast.IsReady or not self.slow.IsReady:
            return

        fast = self.fast.Current.Value
        slow = self.slow.Current.Value

        if fast > slow * d.Decimal(1.001):
            self.SetHoldings("SPY", 1)
        elif fast < slow * d.Decimal(0.999):
            self.Liquidate("SPY")