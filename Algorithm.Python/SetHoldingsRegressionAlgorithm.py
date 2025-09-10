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
### Regression algorithm testing the SetHolding trading API precision
### </summary>
class SetHoldingsRegressionAlgorithm(QCAlgorithm):
    '''Basic template algorithm simply initializes the date range and cash'''

    asynchronous_orders = False

    def initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''

        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 8)
        self.add_equity("SPY", Resolution.MINUTE)

    def on_data(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.

        Arguments:
            data: Slice object keyed by symbol containing the stock data
        '''
        if not self.portfolio.invested:
            self.set_holdings("SPY", 0.1, asynchronous=self.asynchronous_orders)
            self.set_holdings("SPY", float(0.20), asynchronous=self.asynchronous_orders)
            self.set_holdings("SPY", np.float64(0.30), asynchronous=self.asynchronous_orders)
            self.set_holdings("SPY", 1, asynchronous=self.asynchronous_orders)

    def on_end_of_algorithm(self):
        for ticket in self.transactions.get_order_tickets():
            if ticket.submit_request.asynchronous != self.asynchronous_orders:
                raise AssertionError("Expected all orders to have the same asynchronous flag as the algorithm.")
