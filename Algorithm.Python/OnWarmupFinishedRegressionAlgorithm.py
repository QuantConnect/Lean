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

### <summary>
### Regression algorithm asserting 'OnWarmupFinished' is being called
### </summary>
class OnWarmupFinishedRegressionAlgorithm(QCAlgorithm):
    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.SetStartDate(2013,10, 8)  #Set Start Date
        self.SetEndDate(2013,10,11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash

        self.AddEquity("SPY", Resolution.Minute)
        self.SetWarmup(timedelta(days = 1))
        self._onWarmupFinished = 0
    
    def OnWarmupFinished(self):
        self._onWarmupFinished += 1
    
    def OnEndOfAlgorithm(self):
        if self._onWarmupFinished != 1:
            raise Exception(f"Unexpected OnWarmupFinished call count {self._onWarmupFinished}")
