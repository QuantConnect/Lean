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
### Regression algorithm asserting a default symbol is created using equity market when scheduling if none found
### </summary>
class DefaultSchedulingSymbolRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)

        # implicitly figured usa equity
        self.schedule.on(self.date_rules.tomorrow, self.time_rules.after_market_open("AAPL"), self._implicit_check)

        # picked up from cache
        self.add_index("SPX")
        self.schedule.on(self.date_rules.tomorrow, self.time_rules.before_market_close("SPX", extended_market_close = True), self._explicit_check)

    def _implicit_check(self):
        self._implicitChecked = True
        if self.time.time() != time(9, 30, 0):
            raise RegressionTestException(f"Unexpected time of day {self.time.time()}")

    def _explicit_check(self):
        self._explicitChecked = True
        if self.time.time() != time(16, 15, 0):
            raise RegressionTestException(f"Unexpected time of day {self.time.time()}")

    def on_end_of_algorithm(self):
        if not self._explicitChecked or not self._implicitChecked:
            raise RegressionTestException("Failed to run expected checks!")
