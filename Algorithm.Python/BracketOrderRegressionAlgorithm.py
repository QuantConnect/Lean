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
### Regression algorithm exercising the engine-guaranteed OCO semantics of bracket orders:
### the entry fill places the protective legs, a leg fill cancels its sibling, an unrelated order
### closing the position cancels the remaining legs and a new bracket is refused while one is active.
### </summary>
### <meta name="tag" content="trading and orders" />
### <meta name="tag" content="placing orders" />
### <meta name="tag" content="bracket order"/>
class BracketOrderRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 11)
        self.set_cash(100000)

        self._symbol = self.add_equity("SPY", Resolution.MINUTE).symbol

        self._bracket1 = None
        self._bracket2 = None
        self._legs_verified = False
        self._refusal_verified = False
        self._phase1_verified = False
        self._manual_close_time = None
        self._manual_close_done = False
        self._phase2_verified = False
        self._take_profit_filled = False

    def on_data(self, slice: Slice):
        if abs(self.portfolio[self._symbol].quantity) > 10:
            raise AssertionError("The position must never exceed the bracket entry quantity.")

        price = self.securities[self._symbol].price

        # Phase 1: entry fill places the legs, then the take profit fill cancels the stop loss
        if self._bracket1 is None:
            self._bracket1 = self.bracket_order(self._symbol, 10,
                stop_loss_price=round(price * 0.975, 2),
                take_profit_price=round(price * 1.008, 2))
            return

        if not self._legs_verified and self._bracket1.stop_loss_ticket is not None:
            if self._bracket1.entry_ticket.status != OrderStatus.FILLED:
                raise AssertionError("The exit legs must not be placed before the entry order fills.")
            if self._bracket1.stop_loss_ticket.order_type != OrderType.STOP_MARKET or self._bracket1.stop_loss_ticket.quantity != -10:
                raise AssertionError("Expected a stop market leg for -10 units.")
            if (self._bracket1.take_profit_ticket is None
                    or self._bracket1.take_profit_ticket.order_type != OrderType.LIMIT
                    or self._bracket1.take_profit_ticket.quantity != -10):
                raise AssertionError("Expected a limit take profit leg for -10 units.")

            # a new bracket must be refused while this one is live instead of silently
            # overwriting it and stranding its legs
            refused = False
            try:
                self.bracket_order(self._symbol, 10, stop_loss_price=100, take_profit_price=200)
            except Exception as exception:
                if "already active" in str(exception):
                    refused = True
            if not refused:
                raise AssertionError("A second bracket order for the same symbol should have been refused.")
            self._refusal_verified = True
            self._legs_verified = True
            return

        # Phase 2: with a fresh bracket in place, manually closing the position cancels both legs
        if self._bracket2 is None:
            if self._legs_verified and not self._bracket1.is_active:
                if self._bracket1.take_profit_ticket.status != OrderStatus.FILLED:
                    raise AssertionError("Expected the take profit leg of the first bracket to fill.")
                if self._bracket1.stop_loss_ticket.status != OrderStatus.CANCELED:
                    raise AssertionError("Expected the stop loss leg to be canceled when its sibling filled.")
                if self.portfolio.invested:
                    raise AssertionError("Expected a flat position after the take profit filled.")
                if self.transactions.get_bracket_order_ticket(self._symbol) is not None:
                    raise AssertionError("Expected no active bracket after the first one completed.")
                self._phase1_verified = True

                # legs far away from the market so only the manual close can end this bracket
                self._bracket2 = self.bracket_order(self._symbol, 10,
                    stop_loss_price=round(price * 0.93, 2),
                    take_profit_price=round(price * 1.07, 2))
            return

        if self._manual_close_time is None and self._bracket2.stop_loss_ticket is not None:
            self._manual_close_time = self.time + timedelta(minutes=30)
            return

        if not self._manual_close_done and self._manual_close_time is not None and self.time >= self._manual_close_time:
            self.market_order(self._symbol, -10)
            self._manual_close_done = True
            return

        if self._manual_close_done and not self._phase2_verified:
            if (self._bracket2.stop_loss_ticket.status != OrderStatus.CANCELED
                    or self._bracket2.take_profit_ticket.status != OrderStatus.CANCELED):
                raise AssertionError("Expected both legs to be canceled after the position was closed manually.")
            if self.portfolio.invested or self._bracket2.is_active or self.transactions.get_bracket_order_ticket(self._symbol) is not None:
                raise AssertionError("Expected a flat position and no active bracket after the manual close.")
            self._phase2_verified = True

    def on_order_event(self, order_event: OrderEvent):
        if (self._bracket1 is not None and self._bracket1.take_profit_ticket is not None
                and order_event.order_id == self._bracket1.take_profit_ticket.order_id
                and order_event.status == OrderStatus.FILLED):
            self._take_profit_filled = True
        if (self._bracket1 is not None and self._bracket1.stop_loss_ticket is not None
                and order_event.order_id == self._bracket1.stop_loss_ticket.order_id
                and order_event.status == OrderStatus.CANCELED
                and not self._take_profit_filled):
            raise AssertionError("The stop loss must only be canceled after its sibling take profit filled.")

    def on_end_of_algorithm(self):
        if (not self._legs_verified or not self._refusal_verified or not self._phase1_verified
                or not self._manual_close_done or not self._phase2_verified):
            raise AssertionError(f"Not every phase completed: legs placed {self._legs_verified}, "
                f"re-entry refused {self._refusal_verified}, sibling canceled on fill {self._phase1_verified}, "
                f"manual close {self._manual_close_done}, legs canceled on position close {self._phase2_verified}")
        # entry, stop loss and take profit per bracket, plus the manual close
        if self.transactions.orders_count != 7:
            raise AssertionError(f"Expected 7 orders, found {self.transactions.orders_count}")
        if len(self.transactions.get_open_orders()) != 0:
            raise AssertionError("Expected no dangling open orders at the end of the algorithm.")
