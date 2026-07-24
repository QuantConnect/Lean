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
### Regression algorithm for the winner path of a one-cancels-the-other (OCO) order group: buys SPY at
### market, then places a 2-leg OCO group (a take-profit limit leg 1% above the entry price and a stop-market
### leg 30% below it, which the January 2019 rally can never reach). The limit leg should fill and the group
### should cancel the stop leg in the same order-event batch
### </summary>
class OneCancelsTheOtherOrderRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm for the winner path of a one-cancels-the-other (OCO) order group'''

    def initialize(self) -> None:
        self.set_start_date(2019, 1, 1)
        self.set_end_date(2019, 1, 20)

        self._spy = self.add_equity("SPY", Resolution.HOUR).symbol
        self._tickets = None

        # tracks every order event this algorithm receives, relevant or not, so we can tell whether two
        # particular events were delivered back to back (same batch) or with something else in between
        self._order_event_count = 0

        self._winner_order_id = None
        self._winner_fill_utc_time = None
        self._winner_fill_event_count = None
        self._sibling_canceled_in_same_batch = False

    def on_data(self, data: Slice) -> None:
        # trade exactly once: once the winning leg closes the position, portfolio.invested goes back to
        # false and this would otherwise place a second, independent OCO group on top of the first
        if self._tickets is None and not self.portfolio.invested:
            self.market_order(self._spy, 100)

            # take profit +1% is reached by the January rally; the stop -30% can never fill
            self._tickets = self.one_cancels_the_other_order([
                LimitOrder(self._spy, -100, round(self.securities[self._spy].price * 1.01, 2), self.utc_time),
                StopMarketOrder(self._spy, -100, round(self.securities[self._spy].price * 0.70, 2), self.utc_time)
            ])

    def on_order_event(self, order_event: OrderEvent) -> None:
        self._order_event_count += 1

        if self._tickets is None or (order_event.order_id != self._tickets[0].order_id and order_event.order_id != self._tickets[1].order_id):
            # not one of our OCO legs (for example the entry market order)
            return

        if order_event.status == OrderStatus.FILLED:
            if self._winner_order_id is not None:
                raise RegressionTestException(
                    f"Order {order_event.order_id} filled after order {self._winner_order_id} had already won the OCO group. Only one leg should ever fill.")

            self._winner_order_id = order_event.order_id
            self._winner_fill_utc_time = order_event.utc_time
            self._winner_fill_event_count = self._order_event_count

        elif order_event.status == OrderStatus.CANCELED:
            if self._winner_order_id is None:
                raise RegressionTestException(f"Order {order_event.order_id} was canceled before any leg of the group had filled.")

            # the sibling cancel must land in the same order-event batch as the winning fill: same
            # timestamp, and delivered as the very next order event this algorithm receives after the fill
            if order_event.utc_time != self._winner_fill_utc_time or self._order_event_count != self._winner_fill_event_count + 1:
                raise RegressionTestException(
                    "Expected the losing leg's Canceled event to arrive in the same order-event batch as the winning Filled event.")

            self._sibling_canceled_in_same_batch = True

    def on_end_of_algorithm(self) -> None:
        if self._tickets is None or len(self._tickets) != 2:
            raise RegressionTestException("Expected the one-cancels-the-other order group to have been placed with 2 legs.")

        # limit leg won, stop leg was canceled by the group
        if self._tickets[0].status != OrderStatus.FILLED:
            raise RegressionTestException(f"Expected the take-profit limit order to be filled, but it was {self._tickets[0].status}.")

        if self._tickets[1].status != OrderStatus.CANCELED:
            raise RegressionTestException(f"Expected the stop-loss order to be canceled by the group, but it was {self._tickets[1].status}.")

        if self.portfolio.invested:
            raise RegressionTestException("Expected no open position at the end of the algorithm: the winning limit leg should have closed it.")

        if not self._sibling_canceled_in_same_batch:
            raise RegressionTestException("Expected the stop-loss leg's Canceled event to have arrived in the same order-event batch as the winning fill.")
