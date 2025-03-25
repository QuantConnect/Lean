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
### Regression algorithm to test we can specify a custom brokerage model, and override some of its methods
### </summary>
class CustomBrokerageModelRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2013,10,7)
        self.set_end_date(2013,10,11)
        self.set_brokerage_model(CustomBrokerageModel())
        self.add_equity("SPY", Resolution.DAILY)
        self.add_equity("AIG", Resolution.DAILY)
        self.update_request_submitted = False

        if self.brokerage_model.default_markets[SecurityType.EQUITY] != Market.USA:
            raise AssertionError(f"The default market for Equity should be {Market.USA}")
        if self.brokerage_model.default_markets[SecurityType.CRYPTO] != Market.BINANCE:
            raise AssertionError(f"The default market for Crypto should be {Market.BINANCE}")

    def on_data(self, slice):
        if not self.portfolio.invested:
            self.market_order("SPY", 100.0)
            self.aig_ticket = self.market_order("AIG", 100.0)

    def on_order_event(self, order_event):
        spy_ticket = self.transactions.get_order_ticket(order_event.order_id)
        if self.update_request_submitted == False:
            update_order_fields = UpdateOrderFields()
            update_order_fields.quantity = spy_ticket.quantity + 10
            spy_ticket.update(update_order_fields)
            self.spy_ticket = spy_ticket
            self.update_request_submitted = True

    def on_end_of_algorithm(self):
        submit_expected_message = "BrokerageModel declared unable to submit order: [2] Information - Code:  - Symbol AIG can not be submitted"
        if self.aig_ticket.submit_request.response.error_message != submit_expected_message:
            raise AssertionError(f"Order with ID: {self.aig_ticket.order_id} should not have submitted symbol AIG")
        update_expected_message = "OrderID: 1 Information - Code:  - This order can not be updated"
        if self.spy_ticket.update_requests[0].response.error_message != update_expected_message:
            raise AssertionError(f"Order with ID: {self.spy_ticket.order_id} should have been updated")

class CustomBrokerageModel(DefaultBrokerageModel):
    default_markets = { SecurityType.EQUITY: Market.USA, SecurityType.CRYPTO : Market.BINANCE  }

    def can_submit_order(self, security: SecurityType, order: Order, message: BrokerageMessageEvent):
        if security.symbol.value == "AIG":
            message = BrokerageMessageEvent(BrokerageMessageType.INFORMATION, "", "Symbol AIG can not be submitted")
            return False, message
        return True, None

    def can_update_order(self, security: SecurityType, order: Order, request: UpdateOrderRequest, message: BrokerageMessageEvent):
        message = BrokerageMessageEvent(BrokerageMessageType.INFORMATION, "", "This order can not be updated")
        return False, message
