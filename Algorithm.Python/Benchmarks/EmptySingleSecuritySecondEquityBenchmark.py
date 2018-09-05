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
from QuantConnect.Data import *
from QCAlgorithm import QCAlgorithm


### <summary>
### Benchmark Algorithm: Pure processing of 1 equity second resolution with the same benchmark.
### </summary>
### <remarks>
### This should eliminate the synchronization part of LEAN and focus on measuring the performance of a single datafeed and event handling system.
### </remarks>
class EmptySingleSecuritySecondEquityBenchmark(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2008, 1, 1)
        self.SetEndDate(2009, 1, 1)    
        self.SetBenchmark(lambda x: 1)
        self.AddEquity("SPY", Resolution.Second)
    
    def OnData(self, data):
        pass