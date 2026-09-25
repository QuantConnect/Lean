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
### Regression algorithm asserting the behavior of canceling contingent orders:
###  - canceling a parent order cancels the orders it would of triggered, including the ones those would trigger in turn
###  - canceling a member of a one cancels other contingency cancels its siblings too, the contingency is canceled as a whole
###    like brokerages do, whether the members are working or still held waiting for their parent
### </summary>
class ContingentOrderCancelRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._step = 0
        self._tickets = None

    def on_data(self, slice: Slice) -> None:
        price = self.securities[self._symbol].price
        # far from the market price, won't fill
        entry_price = round(price * 0.9, 2)

        if self._step == 0:
            # a bracket whose take profit triggers another order in turn
            take_profit = self.order_factory.limit_order(self._symbol, -100, price * 1.1).triggers(self.order_factory.market_order(self._symbol, 10))
            stop_loss = self.order_factory.stop_market_order(self._symbol, -100, price * 0.8)
            entry = self.order_factory.limit_order(self._symbol, 100, entry_price).triggers(self.order_factory.one_cancels_other(take_profit, stop_loss))
            self._tickets = self.order(entry)
            if len(self._tickets) != 4 or any(not x.contingency.is_waiting_for_trigger for x in self._tickets[1:]):
                raise RegressionTestException("Unexpected order tickets")

        elif self._step == 1:
            # canceling the parent cancels all the orders it would trigger
            response = self._tickets[0].cancel("Canceling the parent")
            if not response.is_success:
                raise RegressionTestException(f"Expected the cancel request to succeed: {response}")

        elif self._step == 2:
            self._assert_canceled(self._tickets, self._tickets)
            # tickets are: entry, take profit, the order triggered by the take profit and the stop loss
            parent_id = self._tickets[0].order_id
            take_profit_id = self._tickets[1].order_id
            if (any(f"Contingent parent order {parent_id} was canceled" not in x.order_events[-1].message for x in [self._tickets[1], self._tickets[3]])
                or f"Contingent parent order {take_profit_id} was canceled" not in self._tickets[2].order_events[-1].message):
                raise RegressionTestException("Unexpected cancel event message")

            self._tickets = self.order(self.order_factory.limit_order(self._symbol, 100, entry_price).bracket(price * 1.1, price * 0.8))

        elif self._step == 3:
            # canceling a held take profit cancels its sibling stop loss too, the parent keeps working
            self._tickets[1].cancel("Canceling the held take profit")

        elif self._step == 4:
            self._assert_canceled(self._tickets, self._tickets[1:])
            if f"Contingent sibling order {self._tickets[1].order_id} was canceled" not in self._tickets[2].order_events[-1].message:
                raise RegressionTestException("Unexpected cancel event message for the sibling stop loss")
            self._tickets[0].cancel()

        elif self._step == 5:
            self._assert_canceled(self._tickets, self._tickets)

            self.market_order(self._symbol, 100)
            self._tickets = self.one_cancels_other_order([
                self.order_factory.limit_order(self._symbol, -100, round(price * 1.1, 2)),
                self.order_factory.stop_market_order(self._symbol, -100, round(price * 0.9, 2))
            ])

        elif self._step == 6:
            # canceling a member cancels its siblings
            self._tickets[0].cancel("Canceling a sibling")

        elif self._step == 7:
            self._assert_canceled(self._tickets, self._tickets)

            # liquidate
            self.liquidate()

        elif self._step == 8:
            self._assert_canceled(self._tickets, self._tickets)
            if self.portfolio.invested or len(self.transactions.get_open_orders()) != 0:
                raise RegressionTestException("Expected no position nor open orders")

        self._step += 1

    def _assert_canceled(self, tickets: list[OrderTicket], expected_canceled: list[OrderTicket]) -> None:
        canceled = [x.order_id for x in expected_canceled]
        for ticket in tickets:
            expected_status = OrderStatus.CANCELED if ticket.order_id in canceled else OrderStatus.SUBMITTED
            if ticket.status != expected_status:
                raise RegressionTestException(f"Expected order {ticket.order_id} status to be {expected_status} but was {ticket.status}")

    def on_end_of_algorithm(self) -> None:
        if self._step < 9:
            raise RegressionTestException(f"Unexpected step count {self._step}")
