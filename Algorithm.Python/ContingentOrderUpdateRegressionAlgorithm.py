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
### Regression algorithm asserting the behavior of updating contingent orders: orders held waiting for their parent to fill
### can be updated, as well as the parent and the orders already working. An order held with a marketable price does not fill
### until it's triggered, and once triggered it requires new data to fill, just like any other order.
### </summary>
class ContingentOrderUpdateRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 7)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._step = 0

    def on_data(self, slice: Slice) -> None:
        price = self.securities[self._symbol].price

        if self._step == 0:
            # the entry is far from the market price, won't fill
            tickets = self.bracket_order(self._symbol, 100, take_profit_price=round(price * 1.1, 2), stop_loss_price=round(price * 0.8, 2),
                limit_price=round(price * 0.9, 2))
            self._entry, self._take_profit, self._stop_loss = tickets

        elif self._step == 1:
            # update the held orders: the take profit gets a marketable price, below the market price, it would fill if it was working
            self._assert_success(self._take_profit.update_limit_price(round(price * 0.95, 2), "Updated take profit"))
            update_fields = UpdateOrderFields()
            update_fields.stop_price = round(price * 0.85, 2)
            update_fields.quantity = -100
            update_fields.tag = "Updated stop loss"
            self._assert_success(self._stop_loss.update(update_fields))

        elif self._step == 2 or self._step == 3:
            if (self._take_profit.status != OrderStatus.UPDATE_SUBMITTED or not self._take_profit.contingency.is_waiting_for_trigger
                or self._take_profit.quantity_filled != 0 or self._take_profit.tag != "Updated take profit"
                or self._take_profit.get(OrderField.LIMIT_PRICE) >= price
                or self._stop_loss.status != OrderStatus.UPDATE_SUBMITTED or not self._stop_loss.contingency.is_waiting_for_trigger
                or self._stop_loss.tag != "Updated stop loss"):
                raise RegressionTestException(f"Expected the held orders to be updated but not filled: {self._take_profit} | {self._stop_loss}")

            if self._step == 3:
                # update the entry so it fills
                self._assert_success(self._entry.update_limit_price(round(price * 1.01, 2), "Updated entry"))

        elif self._step == 4:
            # the updated entry filled right away triggering its children, which require new data to fill: just like any other order
            # they don't fill with the data from the time they start working. So the marketable take profit filled with the next data,
            # canceling the stop loss
            if self._take_profit.status != OrderStatus.FILLED or self._stop_loss.status != OrderStatus.CANCELED or self.portfolio.invested:
                raise RegressionTestException(f"Expected the take profit to be filled and the stop loss canceled: {self._take_profit} | {self._stop_loss}")

            entry_fill_time = next(x for x in self._entry.order_events if x.status == OrderStatus.FILLED).utc_time
            take_profit_fill_time = next(x for x in self._take_profit.order_events if x.status == OrderStatus.FILLED).utc_time
            if take_profit_fill_time != entry_fill_time + timedelta(minutes=1):
                raise RegressionTestException(f"Expected the take profit to fill the minute after the entry, entry: {entry_fill_time} take profit: {take_profit_fill_time}")

            # closed orders can't be updated
            if self._stop_loss.update_stop_price(1).is_success:
                raise RegressionTestException("Expected the update of a canceled order to fail")

        self._step += 1

    def _assert_success(self, response: OrderResponse) -> None:
        if not response.is_success:
            raise RegressionTestException(f"Expected the order request to succeed: {response}")

    def on_end_of_algorithm(self) -> None:
        if self._step < 5:
            raise RegressionTestException(f"Unexpected step count {self._step}")
