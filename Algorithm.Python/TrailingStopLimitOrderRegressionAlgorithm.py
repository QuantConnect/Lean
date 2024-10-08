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
### Basic algorithm demonstrating how to place trailing stop limit orders.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="trailing stop limit order"/>
class TrailingStopLimitOrderRegressionAlgorithm(QCAlgorithm):
    '''Basic algorithm demonstrating how to place trailing stop limit orders.'''

    tolerance = 0.001
    fast_period = 30
    slow_period = 60
    trailing_amount = 5
    limit_offset = 1

    def initialize(self):
        self.set_start_date(2013, 1, 1)
        self.set_end_date(2017, 1, 1)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.DAILY).symbol

        self._fast = self.ema(self._symbol, self.fast_period, Resolution.DAILY)
        self._slow = self.ema(self._symbol, self.slow_period, Resolution.DAILY)

        self._buy_order_ticket: OrderTicket = None
        self._sell_order_ticket: OrderTicket = None
        self._previous_slice: Slice = None

    def on_data(self, slice: Slice):
        if not slice.contains_key(self._symbol):
            return

        if not self.is_ready():
            return

        security = self.securities[self._symbol]
        
        if self._buy_order_ticket is None:
            if self.trend_is_up():
                self._buy_order_ticket = self.trailing_stop_limit_order(self._symbol, 100, security.price * 1.10, (security.price * 1.10) + self.limit_offset,
                                                               self.trailing_amount, False, self.limit_offset)
        elif self._buy_order_ticket.status != OrderStatus.FILLED:
            stop_price = self._buy_order_ticket.get(OrderField.STOP_PRICE)
            limit_price = self._buy_order_ticket.get(OrderField.LIMIT_PRICE)
            
            # Get the previous bar to compare to the stop and limit prices,
            # because stop and limit price update attempt with the current slice data happens after OnData.
            low = self._previous_slice.quote_bars[self._symbol].ask.low if self._previous_slice.quote_bars.contains_key(self._symbol) \
                else self._previous_slice.bars[self._symbol].low

            stop_price_to_market_price_distance = stop_price - low
            if stop_price_to_market_price_distance > self.trailing_amount:
                raise Exception(f"StopPrice {stop_price} should be within {self.trailing_amount} of the previous low price {low} at all times.")
            
            stop_price_to_limit_price_distance = limit_price - stop_price
            if stop_price_to_limit_price_distance != self.limit_offset:
                raise Exception(f"LimitPrice {limit_price} should be {self.limit_offset} from the stop price {stop_price} at all times.")
            
        elif self._sell_order_ticket is None:
            if self.trend_is_down():
                self._sell_order_ticket = self.trailing_stop_limit_order(self._symbol, -100, security.price * 0.99, (security.price * 0.99) - self.limit_offset,
                                                                         self.trailing_amount, False, self.limit_offset)
        elif self._sell_order_ticket.status != OrderStatus.FILLED:
            stop_price = self._sell_order_ticket.get(OrderField.STOP_PRICE)
            limit_price = self._sell_order_ticket.get(OrderField.LIMIT_PRICE)
            
            # Get the previous bar to compare to the stop and limit prices,
            # because stop and limit price update attempt with the current slice data happens after OnData.
            high = self._previous_slice.quote_bars[self._symbol].bid.high if self._previous_slice.quote_bars.contains_key(self._symbol) \
                    else self._previous_slice.bars[self._symbol].high
            
            stop_price_to_market_price_distance = high - stop_price
            if stop_price_to_market_price_distance > self.trailing_amount:
                raise Exception(f"StopPrice {stop_price} should be within {self.sell_trailing_amount} of the previous high price {high} at all times.")
            
            stop_price_to_limit_price_distance = stop_price - limit_price
            if stop_price_to_limit_price_distance != self.limit_offset:
                raise Exception(f"LimitPrice {limit_price} should be {self.limit_offset} from the stop price {stop_price} at all times.")
            
        self._previous_slice = slice
                
    def on_order_event(self, order_event: OrderEvent):
        if order_event.status == OrderStatus.FILLED:
            order: TrailingStopLimitOrder = self.transactions.get_order_by_id(order_event.order_id)
            if not order.stop_triggered:
                raise Exception("TrailingStopLimitOrder StopTriggered should haven been set if the order filled.")

            if order_event.direction == OrderDirection.BUY:
                limit_price = self._buy_order_ticket.get(OrderField.LIMIT_PRICE)
                if order_event.fill_price > limit_price:
                    raise Exception(f"Buy stop limit order should have filled with price less than or equal to the limit price {limit_price}. "
                                    f"Fill price: {order_event.fill_price}")
            else:
                limit_price = self._sell_order_ticket.get(OrderField.LIMIT_PRICE)
                if order_event.fill_price < limit_price:
                    raise Exception(f"Sell stop limit order should have filled with price greater than or equal to the limit price {limit_price}. "
                                    f"Fill price: {order_event.fill_price}")

    def is_ready(self):
        return self._fast.is_ready and self._slow.is_ready

    def trend_is_up(self):
        return self.is_ready() and self._fast.current.value > self._slow.current.value * (1 + self.tolerance)

    def trend_is_down(self):
        return self.is_ready() and self._fast.current.value < self._slow.current.value * (1 + self.tolerance)
