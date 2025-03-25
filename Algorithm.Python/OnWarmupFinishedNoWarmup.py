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
###  Regression algorithm asserting "OnWarmupFinished" is called even if no warmup period is set
### </summary>
class OnWarmupFinishedNoWarmup(QCAlgorithm):
    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2013,10, 7)
        self.set_end_date(2013,10,11)
        self.add_equity("SPY", Resolution.MINUTE)
        self._on_warmup_finished = 0

    def on_warmup_finished(self):
        self._on_warmup_finished += 1

    def on_end_of_algorithm(self):
        if self._on_warmup_finished != 1:
            raise AssertionError(f"Unexpected OnWarmupFinished call count {self._on_warmup_finished}")
