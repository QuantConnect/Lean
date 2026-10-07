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
### Regression algorithm asserting the behavior of the bracket_order helper method (OTOCO): a market entry order
### which once filled triggers a take profit and a stop loss order, the first one to fill cancels the other
### </summary>
class BracketOrderRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._tickets = None
        self._order_events = []

    def on_data(self, slice: Slice) -> None:
        if self._tickets is not None:
            return

        price = self.securities[self._symbol].price
        self._tickets = self.bracket_order(self._symbol, 100, take_profit_price=price * 1.005, stop_loss_price=price * 0.995, tag="Bracket")

        if len(self._tickets) != 3:
            raise RegressionTestException(f"Expected 3 order tickets, but got {len(self._tickets)}")

        entry, take_profit, stop_loss = self._tickets
        if entry.order_type != OrderType.MARKET or entry.status != OrderStatus.FILLED:
            raise RegressionTestException(f"Expected the market entry order to be filled: {entry}")
        if (take_profit.order_type != OrderType.LIMIT or take_profit.quantity != -100
            or stop_loss.order_type != OrderType.STOP_MARKET or stop_loss.quantity != -100):
            raise RegressionTestException("Unexpected take profit and stop loss orders")

        order_ids = sorted([x.order_id for x in self._tickets])
        for ticket in self._tickets:
            contingency = ticket.contingency
            if contingency is None or contingency.count != 3 or sorted(contingency.order_ids) != order_ids:
                raise RegressionTestException(f"Unexpected contingency for order {ticket.order_id}")

        entry_contingencies = entry.contingency.links
        parent = entry_contingencies[0]
        if len(entry_contingencies) != 1 or parent.type != ContingencyType.ONE_TRIGGERS_OTHER or parent.role != ContingencyRole.PARENT:
            raise RegressionTestException("Unexpected entry contingencies")

        for child in [take_profit, stop_loss]:
            contingencies = child.contingency.links
            # the entry already filled so they should of been triggered and be working
            triggered = [c for c in contingencies if c.type == ContingencyType.ONE_TRIGGERS_OTHER and c.role == ContingencyRole.CHILD
                and c.id == parent.id and c.triggered and c.triggered_time == self.utc_time]
            member = [c for c in contingencies if c.type == ContingencyType.ONE_CANCELS_OTHER and c.role is None]
            if (child.contingency.is_waiting_for_trigger or child.status != OrderStatus.SUBMITTED or len(contingencies) != 2
                or len(triggered) != 1 or len(member) != 1):
                raise RegressionTestException(f"Unexpected child order state: {child}")

    def on_order_event(self, order_event: OrderEvent) -> None:
        self._order_events.append(order_event)

    def on_end_of_algorithm(self) -> None:
        if self._tickets is None:
            raise RegressionTestException("The bracket order was never submitted")

        exits = self._tickets[1:]
        filled = [x for x in exits if x.status == OrderStatus.FILLED]
        canceled = [x for x in exits if x.status == OrderStatus.CANCELED]
        if len(filled) != 1 or len(canceled) != 1:
            raise RegressionTestException("Expected one exit to fill and the other to be canceled")

        if self.portfolio.invested:
            raise RegressionTestException("Expected the position to be closed by the bracket exit")

        # the sibling is canceled right after the fill
        fill_index = next(i for i, x in enumerate(self._order_events) if x.order_id == filled[0].order_id and x.status == OrderStatus.FILLED)
        cancel_event = self._order_events[fill_index + 1]
        if (cancel_event.order_id != canceled[0].order_id or cancel_event.status != OrderStatus.CANCELED
            or cancel_event.utc_time != self._order_events[fill_index].utc_time):
            raise RegressionTestException(f"Expected the sibling to be canceled right after the fill, but was: {cancel_event}")

        if len(self.transactions.get_open_orders()) != 0:
            raise RegressionTestException("Unexpected open orders")
