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
### Regression algorithm for the cancel path of a one-cancels-the-other (OCO) order group: the group is
### placed with both legs far from the market, so neither can fill inside the test window, then one of the
### two tickets is explicitly canceled. Asserts that canceling one leg cancels the whole group, not just
### the leg that was canceled
### </summary>
class OneCancelsTheOtherOrderCancelRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm for the cancel path of a one-cancels-the-other (OCO) order group'''

    def initialize(self) -> None:
        self.set_start_date(2019, 1, 1)
        self.set_end_date(2019, 1, 31)

        self._spy = self.add_equity("SPY", Resolution.HOUR).symbol
        self._tickets = None
        self._canceled = False

    def on_data(self, data: Slice) -> None:
        if not self.portfolio.invested:
            self.market_order(self._spy, 100)

            # both legs sit far from the market: limit sell +30% and stop sell -30% should never be
            # reachable in this test window, so only the explicit cancel below can close the group
            self._tickets = self.one_cancels_the_other_order([
                LimitOrder(self._spy, -100, round(self.securities[self._spy].price * 1.30, 2), self.utc_time),
                StopMarketOrder(self._spy, -100, round(self.securities[self._spy].price * 0.70, 2), self.utc_time)
            ])

        elif not self._canceled and self.time.day > 5:
            # cancel only one leg: the whole OCO group must cancel with it
            self._tickets[0].cancel()
            self._canceled = True

    def on_order_event(self, order_event: OrderEvent) -> None:
        if self._tickets is None or order_event.status != OrderStatus.FILLED:
            return

        # neither OCO leg's price should ever be reachable in this test window; a fill here means the
        # regression scenario itself is broken, not just the cancellation behavior being tested
        if any(ticket.order_id == order_event.order_id for ticket in self._tickets):
            raise RegressionTestException(
                f"Unexpected fill for OCO leg {order_event.order_id}: prices were set far from the market so the group should only close through the explicit cancel")

    def on_end_of_algorithm(self) -> None:
        if not self._canceled:
            raise RegressionTestException("Expected to have canceled one of the OCO legs before the end of the algorithm")

        if self._tickets is None or len(self._tickets) != 2:
            raise RegressionTestException("Expected the OCO group to have exactly 2 legs")

        for ticket in self._tickets:
            if ticket.status != OrderStatus.CANCELED:
                raise RegressionTestException(
                    f"Expected every OCO leg to be Canceled, including the leg that was not explicitly canceled. Leg {ticket.order_id} has status {ticket.status}")

        # canceling the OCO exit group must not touch the original market order fill
        if not self.portfolio.invested:
            raise RegressionTestException("Expected the algorithm to still be invested: the market order fill is independent from the canceled OCO group")
