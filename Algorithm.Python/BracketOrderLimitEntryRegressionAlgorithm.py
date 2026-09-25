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
### Regression algorithm asserting the behavior of a bracket order (OTOCO) built through the generic OrderFactory api (an entry which triggers a one cancels other) using
### a limit entry order: the take profit and the stop loss are held, they can't fill, until the entry order fills
### </summary>
class BracketOrderLimitEntryRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._entry = None
        self._take_profit = None
        self._stop_loss = None
        self._entry_fill_time = None

    def on_data(self, slice: Slice) -> None:
        if self._entry is None:
            price = self.securities[self._symbol].price

            # the take profit and stop loss are held until the entry fills
            self._entry = self.order_factory.limit_order(self._symbol, 100, round(price * 0.999, 2), tag="Entry")
            self._take_profit = self.order_factory.limit_order(self._symbol, -100, round(price * 1.004, 2), tag="Take profit")
            self._stop_loss = self.order_factory.stop_market_order(self._symbol, -100, round(price * 0.99, 2), tag="Stop loss")
            self._entry.triggers(self.order_factory.one_cancels_other(self._take_profit, self._stop_loss))

            # composed but not submitted yet: the contingency is already set, the set id is not
            entry_links = self._entry.contingency.links
            stop_loss_links = self._stop_loss.contingency.links
            if (self._entry.order_id > 0 or self._ticket(self._entry) is not None or self._entry.contingency.id != 0 or entry_links[0].role != ContingencyRole.PARENT
                or len(self._take_profit.contingency.links) != 2 or len(stop_loss_links) != 2 or stop_loss_links[1].type != ContingencyType.ONE_CANCELS_OTHER):
                raise RegressionTestException("Unexpected order request state before being submitted")

            tickets = self.order(self._entry)

            if (len(tickets) != 3 or tickets[0].order_id != self._entry.order_id or tickets[1].order_id != self._take_profit.order_id
                or tickets[2].order_id != self._stop_loss.order_id or self._entry.order_id <= 0
                or next(x for x in tickets[1].contingency.links if x.role is None).type != ContingencyType.ONE_CANCELS_OTHER):
                raise RegressionTestException("Unexpected order tickets")

            # an order request can only be submitted once
            try:
                self.order(self._entry)
                raise RegressionTestException("Expected an exception when submitting an order request twice")
            except ArgumentException:
                pass

        if self._ticket(self._entry).status != OrderStatus.FILLED:
            for child in [self._ticket(self._take_profit), self._ticket(self._stop_loss)]:
                if not child.contingency.is_waiting_for_trigger or child.status != OrderStatus.SUBMITTED or child.quantity_filled != 0:
                    raise RegressionTestException(f"Expected the child order to be held waiting for the entry to fill: {child}")

            # held orders are not accounted as open quantity
            open_quantity = self.transactions.get_open_orders_remaining_quantity(self._symbol)
            if open_quantity != 100:
                raise RegressionTestException(f"Expected the open orders remaining quantity to be 100 but was {open_quantity}")

    def _get_triggered_time(self, ticket: OrderTicket) -> datetime:
        return next(x for x in ticket.contingency.links if x.role == ContingencyRole.CHILD).triggered_time

    def on_order_event(self, order_event: OrderEvent) -> None:
        if order_event.status != OrderStatus.FILLED:
            return

        if order_event.order_id == self._ticket(self._entry).order_id:
            self._entry_fill_time = order_event.utc_time
        else:
            triggered_time = self._get_triggered_time(order_event.ticket)
            if self._entry_fill_time is None or triggered_time != self._entry_fill_time or order_event.utc_time <= triggered_time:
                raise RegressionTestException(f"Expected the exit order to fill after being triggered by the entry fill at {self._entry_fill_time}: {order_event}")

    def _ticket(self, request: SubmitOrderRequest) -> OrderTicket:
        return self.transactions.get_order_ticket(request.order_id)

    def on_end_of_algorithm(self) -> None:
        if self._entry_fill_time is None:
            raise RegressionTestException("Expected the entry order to be filled")

        exits = [self._ticket(self._take_profit), self._ticket(self._stop_loss)]
        if len([x for x in exits if x.status == OrderStatus.FILLED]) != 1 or len([x for x in exits if x.status == OrderStatus.CANCELED]) != 1:
            raise RegressionTestException("Expected one exit to fill and the other to be canceled")

        if any(x.contingency.is_waiting_for_trigger or self._get_triggered_time(x) != self._entry_fill_time for x in exits):
            raise RegressionTestException("Expected both exits to be triggered at the entry fill time")

        if self.portfolio.invested or len(self.transactions.get_open_orders()) != 0:
            raise RegressionTestException("Expected the position to be closed and no open orders")

        # the orders keep their contingencies
        order = self.transactions.get_order_by_id(self._ticket(self._stop_loss).order_id)
        if order.contingency is None or order.contingency.count != 3 or len(order.contingency.links) != 2:
            raise RegressionTestException("Unexpected order contingencies")
