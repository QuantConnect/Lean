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
### Regression algorithm asserting the behavior of a trailing stop order triggered by another order, through the generic OrderFactory api:
### its stop price is set once it's triggered, from the market price at that time, from where it starts trailing
### </summary>
class ContingentTrailingStopOrderRegressionAlgorithm(QCAlgorithm):

    _trailing_percentage = 0.005

    def initialize(self) -> None:
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol
        self._entry = None
        self._trailing_stop = None
        self._asserted_triggered_stop_price = False

    def on_data(self, slice: Slice) -> None:
        price = self.securities[self._symbol].price
        if self._entry is None:
            self._trailing_stop = self.order_factory.trailing_stop_order(self._symbol, -100, self._trailing_percentage, True, tag="Trailing stop")
            self._entry = self.order_factory.limit_order(self._symbol, 100, round(price * 0.999, 2), tag="Entry").triggers(self._trailing_stop)
            self.order(self._entry)

        stop_price = self._ticket(self._trailing_stop).get(OrderField.STOP_PRICE)
        if self._ticket(self._trailing_stop).contingency.is_waiting_for_trigger:
            if stop_price != 0:
                raise RegressionTestException(f"Expected the stop price of the held trailing stop order not to be set yet but was {stop_price}")
        elif not self._asserted_triggered_stop_price:
            self._asserted_triggered_stop_price = True

            # it was just triggered, the stop price is set from the current market price
            expected_stop_price = price * (1 - self._trailing_percentage)
            if self._ticket(self._entry).status != OrderStatus.FILLED or abs(stop_price - expected_stop_price) > 0.01:
                raise RegressionTestException(f"Expected the stop price to be {expected_stop_price} but was {stop_price}")

    def _ticket(self, request: SubmitOrderRequest) -> OrderTicket:
        return self.transactions.get_order_ticket(request.order_id)

    def on_end_of_algorithm(self) -> None:
        if not self._asserted_triggered_stop_price or self._ticket(self._trailing_stop).status != OrderStatus.FILLED or self.portfolio.invested:
            raise RegressionTestException(f"Expected the trailing stop order to be triggered and filled: {self._ticket(self._trailing_stop)}")

        # it trailed the market price up before filling
        entry_fill_price = self._ticket(self._entry).average_fill_price
        if self._ticket(self._trailing_stop).get(OrderField.STOP_PRICE) <= entry_fill_price * (1 - self._trailing_percentage):
            raise RegressionTestException("Expected the stop price to trail the market price")
