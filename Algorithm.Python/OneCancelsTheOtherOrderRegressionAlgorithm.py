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
from enum import Enum

### <summary>
### Regression algorithm for one-cancels-the-other (OCO) order groups. It shows that both leg types can win.
###
### Buy 100 SPY, then place two groups one after the other:
### - sell 200: the limit leg wins, so we go from long 100 to short 100
### - buy 100: the stop leg wins, so we end flat
###
### Holdings go 0 -> 100 -> -100 -> 0. In each group the losing leg must be canceled in the same event batch
### as the winning fill. The second group matters because stop legs are checked before limit legs, so a
### winning stop leg takes a different path than a winning limit leg
### </summary>
class OneCancelsTheOtherOrderRegressionAlgorithm(QCAlgorithm):
    '''Regression algorithm for one-cancels-the-other (OCO) order groups: both leg types can win'''

    def initialize(self) -> None:
        self.set_start_date(2019, 1, 1)
        self.set_end_date(2019, 1, 20)

        self._spy = self.add_equity("SPY", Resolution.HOUR).symbol

        # counts every order event we get, so we can tell if two events arrived one after the other
        self._order_event_count = 0

        self._position_opened = False
        self._reversal_group = None
        self._cover_group = None

    def on_data(self, data: Slice) -> None:
        if not data.contains_key(self._spy):
            return

        # open the position on its own bar, so the groups below start from a position that is already there
        if not self._position_opened:
            self.market_order(self._spy, 100)
            self._position_opened = True
            return

        # no rounding here: Lean rounds order prices to the brokerage's precision before it sends them
        price = self.securities[self._spy].price

        if self._reversal_group is None:
            # sell 200. The January rally reaches the limit +1%, the stop -30% never fills, so the limit wins
            self._reversal_group = OrderGroupTracker(self.one_cancels_the_other_order(self._spy, -200,
                limit_price=price * 1.01,
                stop_price=price * 0.70))
            return

        if self._cover_group is None and self._reversal_group.has_winner:
            # now short 100, so buy it back with the prices swapped: the rally reaches the stop +1% and the
            # limit -30% never fills, so this time the stop wins. We wait for the first group to have a
            # winner instead of checking portfolio.invested, which is also false while an order is working
            self._cover_group = OrderGroupTracker(self.one_cancels_the_other_order(self._spy, 100,
                limit_price=price * 0.70,
                stop_price=price * 1.01))

    def on_order_event(self, order_event: OrderEvent) -> None:
        self._order_event_count += 1

        # events that belong to no group are skipped, for example the opening market order
        group = self._find_group(order_event.order_id)
        if group is not None:
            group.track(order_event, self._order_event_count)

    def _find_group(self, order_id: int):
        if self._reversal_group is not None and self._reversal_group.contains(order_id):
            return self._reversal_group

        if self._cover_group is not None and self._cover_group.contains(order_id):
            return self._cover_group

        return None

    def on_end_of_algorithm(self) -> None:
        self._assert_group_resolved(self._reversal_group, GroupRole.REVERSAL, OrderType.LIMIT)
        self._assert_group_resolved(self._cover_group, GroupRole.COVER, OrderType.STOP_MARKET)

        # bought 100, sold 200, bought 100 back, so we end with nothing
        holdings = self.portfolio[self._spy].quantity
        if holdings != 0:
            raise RegressionTestException(
                f"Expected to end flat after the cover group's stop leg bought the short back, but SPY holdings are {holdings}.")

    def _assert_group_resolved(self, group, role, winning_order_type) -> None:
        '''Checks one group: the leg of the given type filled, the other leg was canceled, and the cancel came
        in the same event batch as the fill'''
        if group is None or len(group.tickets) != 2:
            raise RegressionTestException(
                f"Expected the {role.name} one-cancels-the-other group to have been placed with 2 legs.")

        winner = next(ticket for ticket in group.tickets if ticket.order_type == winning_order_type)
        if winner.status != OrderStatus.FILLED:
            raise RegressionTestException(
                f"Expected the {role.name} group's {winner.order_type} leg to be filled, but it was {winner.status}.")

        loser = next(ticket for ticket in group.tickets if ticket.order_type != winning_order_type)
        if loser.status != OrderStatus.CANCELED:
            raise RegressionTestException(
                f"Expected the {role.name} group's {loser.order_type} leg to be canceled by the group, but it was {loser.status}.")

        if not group.sibling_canceled_in_same_batch:
            raise RegressionTestException(
                f"Expected the {role.name} group's losing leg Canceled event to have arrived in the same order-event batch as the winning fill.")


class GroupRole(Enum):
    '''What each order group is for'''

    # sells 200, so the winning leg turns long 100 into short 100
    REVERSAL = 0

    # buys 100 back, so the winning leg leaves us flat
    COVER = 1


class OrderGroupTracker:
    '''Watches one group: only one leg may fill, and the other leg must be canceled in the same event batch'''

    def __init__(self, tickets) -> None:
        self.tickets = tickets
        self.sibling_canceled_in_same_batch = False

        self._winner_order_id = None
        self._winner_fill_utc_time = None
        self._winner_fill_event_count = None

    @property
    def has_winner(self) -> bool:
        return self._winner_order_id is not None

    def contains(self, order_id: int) -> bool:
        return any(ticket.order_id == order_id for ticket in self.tickets)

    def track(self, order_event: OrderEvent, order_event_count: int) -> None:
        if order_event.status == OrderStatus.FILLED:
            if self._winner_order_id is not None:
                raise RegressionTestException(
                    f"Order {order_event.order_id} filled after order {self._winner_order_id} had already won the group. "
                    "Only one leg of a one-cancels-the-other group should ever fill.")

            self._winner_order_id = order_event.order_id
            self._winner_fill_utc_time = order_event.utc_time
            self._winner_fill_event_count = order_event_count

        elif order_event.status == OrderStatus.CANCELED:
            if self._winner_order_id is None:
                raise RegressionTestException(
                    f"Order {order_event.order_id} was canceled before any leg of the group had filled.")

            # same batch means same timestamp, and the very next event we get after the fill
            if order_event.utc_time != self._winner_fill_utc_time or order_event_count != self._winner_fill_event_count + 1:
                raise RegressionTestException(
                    "Expected the losing leg's Canceled event to arrive in the same order-event batch as the winning Filled event.")

            self.sibling_canceled_in_same_batch = True
