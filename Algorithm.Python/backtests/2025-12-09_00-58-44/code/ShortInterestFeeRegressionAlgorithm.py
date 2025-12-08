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
### Example algorithm showing and asserting the usage of the "ShortMarginInterestRateModel"
### paired with a "IShortableProvider" instance, for example "InteractiveBrokersShortableProvider"
### </summary>
class ShortInterestFeeRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2013,10, 7)
        self.set_end_date(2013,10,11)

        self._short = self.add_equity("SPY", Resolution.HOUR)
        self._long = self.add_equity("AAPL", Resolution.HOUR)

        for security in [ self._short, self._long]:
            security.set_shortable_provider(LocalDiskShortableProvider("testbrokerage"))
            security.margin_interest_rate_model = ShortMarginInterestRateModel()

    def on_data(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.portfolio.invested:
            self.set_holdings("SPY", -0.5)
            self.set_holdings("AAPL", 0.5)

    def on_end_of_algorithm(self):
        if self._short.margin_interest_rate_model.amount >= 0:
            raise RegressionTestException("Expected short fee interest rate to be charged")

        if self._long.margin_interest_rate_model.amount <= 0:
            raise RegressionTestException("Expected short fee interest rate to be earned")
