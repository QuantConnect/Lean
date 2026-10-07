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
### Regression algorithm asserting the behavior of the one_cancels_other_order helper method (OCO/OCA): a set of orders
### working at the same time where the first one to fill cancels the rest. We use it to exit an existing position,
### each time it's closed we open it again and submit a new set of exit orders.
### </summary>
class OneCancelsOtherOrderRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._tickets = None
        self._completed_sets = 0
        self._contingent_order_set_ids = set()

    def on_data(self, slice: Slice) -> None:
        if self._tickets is not None:
            if any(x.status in [OrderStatus.FILLED, OrderStatus.CANCELED, OrderStatus.INVALID] for x in self._tickets):
                self._assert_completed_set()
                self._tickets = None
            return

        if self.portfolio.invested or len(self.transactions.get_open_orders()) != 0:
            raise RegressionTestException("Expected no position nor open orders before submitting a new set of orders")

        self.market_order(self._symbol, 100)

        price = self.securities[self._symbol].price
        self._tickets = self.one_cancels_other_order([
            self.order_factory.limit_order(self._symbol, -100, price * 1.003, tag="Take Profit"),
            self.order_factory.stop_market_order(self._symbol, -100, price * 0.997, tag="Stop Loss"),
            self.order_factory.stop_limit_order(self._symbol, -100, price * 0.99, price * 0.98, tag="Far Stop Loss")
        ])

        if len(self._tickets) != 3:
            raise RegressionTestException(f"Expected 3 order tickets, but got {len(self._tickets)}")

        expected_contingency_id = self._tickets[0].contingency.links[0].id
        for ticket in self._tickets:
            contingencies = ticket.contingency.links
            if (ticket.contingency.is_waiting_for_trigger or ticket.status != OrderStatus.SUBMITTED or ticket.contingency.count != 3
                or len(contingencies) != 1 or contingencies[0].type != ContingencyType.ONE_CANCELS_OTHER
                or contingencies[0].role is not None or contingencies[0].id != expected_contingency_id):
                raise RegressionTestException(f"Unexpected order state: {ticket}")

        set_id = self._tickets[0].contingency.id
        if set_id in self._contingent_order_set_ids:
            raise RegressionTestException("Expected a new contingent order set id for each set of orders")
        self._contingent_order_set_ids.add(set_id)

        # at most one of them will fill
        open_quantity = self.transactions.get_open_orders_remaining_quantity(self._symbol)
        if open_quantity != -100:
            raise RegressionTestException(f"Expected the open orders remaining quantity to be -100 but was {open_quantity}")

    def _assert_completed_set(self) -> None:
        canceled = [x for x in self._tickets if x.status == OrderStatus.CANCELED]
        if len([x for x in self._tickets if x.status == OrderStatus.FILLED]) != 1 or len(canceled) != 2:
            raise RegressionTestException("Expected one order to fill and the others to be canceled")

        for ticket in canceled:
            cancel_event = next(x for x in ticket.order_events if x.status == OrderStatus.CANCELED)
            if "Contingent sibling order" not in cancel_event.message:
                raise RegressionTestException(f"Unexpected cancel event message: {cancel_event.message}")

        if self.portfolio.invested:
            raise RegressionTestException("Expected the position to be closed")
        self._completed_sets += 1

    def on_end_of_algorithm(self) -> None:
        if self._completed_sets < 2:
            raise RegressionTestException(f"Expected at least 2 completed sets of orders but got {self._completed_sets}")
