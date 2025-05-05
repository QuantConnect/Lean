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
### Algorithm asserting that closed orders can be updated with a new tag
### </summary>
class CompleteOrderTagUpdateAlgorithm(QCAlgorithm):

    _tag_after_fill = "This is the tag set after order was filled."
    _tag_after_canceled = "This is the tag set after order was canceled."

    def initialize(self) -> None:
        self.set_start_date(2013,10, 7)
        self.set_end_date(2013,10,11)
        self.set_cash(100000)

        self._spy = self.add_equity("SPY", Resolution.MINUTE).symbol

        self._market_order_ticket = None
        self._limit_order_ticket = None

        self._quantity = 100

    def on_data(self, data: Slice) -> None:
        if not self.portfolio.invested:
            if self._limit_order_ticket is None:
                # a limit order to test the tag update after order was canceled.

                # low price, we don't want it to fill since we are canceling it
                self._limit_order_ticket = self.limit_order(self._spy, 100, self.securities[self._spy].price * 0.1)
                self._limit_order_ticket.cancel()
            else:
                # a market order to test the tag update after order was filled.
                self.buy(self._spy, self._quantity)

    def on_order_event(self, order_event: OrderEvent) -> None:
        if order_event.status == OrderStatus.CANCELED:
            if not self._limit_order_ticket or order_event.order_id != self._limit_order_ticket.order_id:
                raise AssertionError("The only canceled order should have been the limit order.")

            # update canceled order tag
            self.update_order_tag(self._limit_order_ticket, self._tag_after_canceled, "Error updating order tag after canceled")
        elif order_event.status == OrderStatus.FILLED:
            self._market_order_ticket = list(self.transactions.get_order_tickets(lambda x: x.order_type == OrderType.MARKET))[0]
            if not self._market_order_ticket or order_event.order_id != self._market_order_ticket.order_id:
                raise AssertionError("The only filled order should have been the market order.")

            # update filled order tag
            self.update_order_tag(self._market_order_ticket, self._tag_after_fill, "Error updating order tag after fill")

    def on_end_of_algorithm(self) -> None:
        # check the filled order
        self.assert_order_tag_update(self._market_order_ticket, self._tag_after_fill, "filled")
        if self._market_order_ticket.quantity != self._quantity or self._market_order_ticket.quantity_filled != self._quantity:
            raise AssertionError("The market order quantity should not have been updated.")

        # check the canceled order
        self.assert_order_tag_update(self._limit_order_ticket, self._tag_after_canceled, "canceled")

    def assert_order_tag_update(self, ticket: OrderTicket, expected_tag: str, order_action: str) -> None:
        if ticket is None:
            raise AssertionError(f"The order ticket was not set for the {order_action} order")

        if ticket.tag != expected_tag:
            raise AssertionError(f"Order ticket tag was not updated after order was {order_action}")

        order = self.transactions.get_order_by_id(ticket.order_id)
        if order.tag != expected_tag:
            raise AssertionError(f"Order tag was not updated after order was {order_action}")

    def update_order_tag(self, ticket: OrderTicket, tag: str, error_message_prefix: str) -> None:
        update_fields = UpdateOrderFields()
        update_fields.tag = tag
        response = ticket.update(update_fields)

        if response.is_error:
            raise AssertionError(f"{error_message_prefix}: {response.error_message}")
