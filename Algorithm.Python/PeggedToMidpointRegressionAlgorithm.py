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
### Basic algorithm demonstrating how to place Pegged-to-Midpoint orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="pegged to midpoint order" />
class PeggedToMidpointRegressionAlgorithm(QCAlgorithm):

    _buy_order_ticket = None
    _sell_order_ticket = None

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)
        self._symbol = self.add_equity("SPY").symbol

    def on_data(self, data):
        if not data.contains_key(self._symbol):
            return

        if self._buy_order_ticket is None:
            self._buy_order_ticket = self.pegged_to_midpoint_order(self._symbol, 1, limit_price=0, limit_price_offset=0)
        elif self._sell_order_ticket is None and self.portfolio.invested:
            self._sell_order_ticket = self.pegged_to_midpoint_order(self._symbol, -1, limit_price=0, limit_price_offset=0)

    def on_order_event(self, order_event):
        if order_event.status != OrderStatus.FILLED:
            return

        order = self.transactions.get_order_by_id(order_event.order_id)
        if not isinstance(order, PeggedToMidpointOrder):
            raise AssertionError(f"Expected PeggedToMidpointOrder but got {type(order).__name__}")

    def on_end_of_algorithm(self):
        if self._sell_order_ticket is None or self._sell_order_ticket.status != OrderStatus.FILLED:
            raise AssertionError("Expected sell PeggedToMidpoint order to be filled by end of algorithm.")
