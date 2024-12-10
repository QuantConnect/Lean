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
from Orders.Fees.InteractiveBrokersTieredFeeModel import InteractiveBrokersTieredFeeModel

### <summary>
### Test algorithm using "InteractiveBrokersTieredFeeModel"
### </summary>
class InteractiveBrokersTieredFeeModelAlgorithm(QCAlgorithm):
    fee_model = InteractiveBrokersTieredFeeModel()

    def initialize(self):
        ''' Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        self.set_start_date(2013, 10, 7)        #Set Start Date
        self.set_end_date(2013, 10, 10)         #Set End Date
        self.set_cash(1000000000)               #Set Strategy Cash

        # Set the fee model to be shared by all securities to accurately track the volume/value traded to select the correct tiered fee structure.
        self.set_security_initializer(lambda security: security.set_fee_model(self.fee_model))
        
        self.spy = self.add_equity("SPY", Resolution.MINUTE, extended_market_hours=True).symbol
        self.aig = self.add_equity("AIG", Resolution.MINUTE, extended_market_hours=True).symbol
        self.bac = self.add_equity("BAC", Resolution.MINUTE, extended_market_hours=True).symbol

    def on_data(self, slice: Slice) -> None:
        # Order at different time for various order type to elicit different fee structure.
        if slice.time.hour == 9 and slice.time.minute == 0:
            self.market_on_open_order(self.spy, 30000)
            self.market_on_open_order(self.aig, 30000)
            self.market_on_open_order(self.bac, 30000)
        elif slice.time.hour == 10 and slice.time.minute == 0:
            self.market_order(self.spy, 30000)
            self.market_order(self.aig, 30000)
            self.market_order(self.bac, 30000)
        elif slice.time.hour == 15 and slice.time.minute == 30:
            self.market_on_close_order(self.spy, -60000)
            self.market_on_close_order(self.aig, -60000)
            self.market_on_close_order(self.bac, -60000)
