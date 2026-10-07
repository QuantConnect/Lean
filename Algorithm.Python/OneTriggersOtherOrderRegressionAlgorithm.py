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
### Regression algorithm asserting the behavior of the one_triggers_other_order helper method (OTO): a parent order which once filled
### triggers multiple independent orders, for different symbols, one of which triggers another in turn (chain)
### </summary>
class OneTriggersOtherOrderRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self._spy = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._ibm = self.add_equity("IBM", Resolution.MINUTE).symbol
        self._bac = self.add_equity("BAC", Resolution.MINUTE).symbol
        self._parent = None
        self._tickets = None
        self._fill_order = []

    def on_data(self, slice: Slice) -> None:
        if self._parent is None:
            if not slice.contains_key(self._spy) or not slice.contains_key(self._ibm) or not slice.contains_key(self._bac):
                return

            price = self.securities[self._spy].price
            self._parent = self.order_factory.limit_order(self._spy, 100, round(price * 0.999, 2), tag="Parent")
            self._bac_grand_child = self.order_factory.market_order(self._bac, 20, tag="Grand child")
            self._ibm_child = self.order_factory.market_order(self._ibm, 10, tag="Child").triggers(self._bac_grand_child)
            self._limit_child = self.order_factory.limit_order(self._spy, -50, round(price * 1.05, 2), tag="Independent child")

            self._tickets = self.one_triggers_other_order(self._parent, [self._ibm_child, self._limit_child])

            expected_tickets = [self._ticket(self._parent), self._ticket(self._ibm_child), self._ticket(self._bac_grand_child), self._ticket(self._limit_child)]
            if ([x.order_id for x in self._tickets] != [x.order_id for x in expected_tickets]
                or any(x.contingency.count != 4 for x in self._tickets)):
                raise RegressionTestException("Unexpected order tickets")

            # the IBM order is the child of a contingency and the parent of another one
            contingencies = self._ticket(self._ibm_child).contingency.links
            child = [x for x in contingencies if x.role == ContingencyRole.CHILD]
            parent = [x for x in contingencies if x.role == ContingencyRole.PARENT]
            if (len(contingencies) != 2 or any(x.type != ContingencyType.ONE_TRIGGERS_OTHER for x in contingencies)
                or len(child) != 1 or child[0].id != self._ticket(self._parent).contingency.links[0].id
                or len(parent) != 1 or parent[0].id != self._ticket(self._bac_grand_child).contingency.links[0].id
                or self._ticket(self._limit_child).contingency.links[0].role != ContingencyRole.CHILD):
                raise RegressionTestException("Unexpected contingencies")

        tickets = self._tickets
        if self._ticket(self._parent).status != OrderStatus.FILLED:
            if any(not x.contingency.is_waiting_for_trigger or x.status != OrderStatus.SUBMITTED for x in tickets[1:]):
                raise RegressionTestException("Expected all the orders to be held waiting for the parent to fill")
        elif any(x.contingency.is_waiting_for_trigger for x in tickets):
            raise RegressionTestException("Expected all the orders to be triggered once the parent filled")

    def on_order_event(self, order_event: OrderEvent) -> None:
        if order_event.status == OrderStatus.FILLED:
            self._fill_order.append(order_event.order_id)
        elif order_event.status == OrderStatus.CANCELED:
            raise RegressionTestException(f"Unexpected canceled order event, the triggered orders are independent: {order_event}")

    def _get_fill_time(self, ticket: OrderTicket) -> datetime:
        return next(x for x in ticket.order_events if x.status == OrderStatus.FILLED).utc_time

    def _ticket(self, request: SubmitOrderRequest) -> OrderTicket:
        return self.transactions.get_order_ticket(request.order_id)

    def on_end_of_algorithm(self) -> None:
        expected_fill_order = [self._ticket(self._parent).order_id, self._ticket(self._ibm_child).order_id, self._ticket(self._bac_grand_child).order_id]
        if self._fill_order != expected_fill_order:
            raise RegressionTestException(f"Unexpected fill order: {self._fill_order}")

        # market orders fill right away once triggered
        parent_fill_time = self._get_fill_time(self._ticket(self._parent))
        if self._get_fill_time(self._ticket(self._ibm_child)) != parent_fill_time or self._get_fill_time(self._ticket(self._bac_grand_child)) != parent_fill_time:
            raise RegressionTestException("Expected the market orders to fill once triggered")

        if self.portfolio[self._spy].quantity != 100 or self.portfolio[self._ibm].quantity != 10 or self.portfolio[self._bac].quantity != 20:
            raise RegressionTestException("Unexpected holdings")

        # the independent limit order is still working
        open_orders = self.transactions.get_open_orders()
        if (len(open_orders) != 1 or open_orders[0].id != self._ticket(self._limit_child).order_id or self._ticket(self._limit_child).contingency.is_waiting_for_trigger
            or open_orders[0].status != OrderStatus.SUBMITTED):
            raise RegressionTestException("Expected the independent limit order to be still working")
