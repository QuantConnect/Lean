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
### Regression algorithm asserting the flat order surface shortcuts: the numeric OrderFee
### surface (order_fee.amount, arithmetic and comparison operators, order_event.order_fee_amount),
### the flat combo group ids (order.group_order_manager_id, order_event.group_id, ComboOrderTicket),
### the tag-argument tolerance of market_order/liquidate and order_target_notional
### </summary>
class OrderSurfaceShortcutsRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(200000)

        equity = self.add_equity("GOOG", leverage=4, fill_forward=True)
        self._equity_symbol = equity.symbol
        option = self.add_option(equity.symbol, fill_forward=True)
        self._option_symbol = option.symbol

        option.set_filter(lambda u: u.standards_only().strikes(-2, 2).expiration(0, 180))

        self._tagged_ticket = None
        self._notional_ticket = None
        self._combo_ticket = None
        self._combo_fill_group_ids = set()
        self._combo_fill_events_count = 0

    def on_data(self, slice):
        if self._tagged_ticket is None and self.is_market_open(self._equity_symbol):
            # the tag in the third positional slot must be accepted as the tag argument
            self._tagged_ticket = self.market_order(self._equity_symbol, 1, "tagged entry")

            # target an absolute notional value instead of a portfolio percentage
            self._notional_ticket = self.order_target_notional(self._equity_symbol, 10000)
            if self._notional_ticket is None:
                raise AssertionError("order_target_notional was expected to place an order")

            # a tag slipped into the symbol slot must fail pointing to the tag parameter
            liquidate_failed = False
            try:
                self.liquidate("EOD close")
            except Exception as exception:
                liquidate_failed = True
                if "tag" not in str(exception):
                    raise AssertionError("liquidate() with an unknown ticker was expected to point to the "
                                         f"tag parameter but the error was: {exception}")
            if not liquidate_failed:
                raise AssertionError("liquidate() with an unknown ticker was expected to fail")

        if self._combo_ticket is None and self.is_market_open(self._option_symbol):
            chain = slice.option_chains.get(self._option_symbol)
            if chain is None:
                return
            call_contracts = [contract for contract in chain if contract.right == OptionRight.CALL]
            if not call_contracts:
                return
            first_expiry = min(contract.expiry for contract in call_contracts)
            call_contracts = sorted((contract for contract in call_contracts if contract.expiry == first_expiry),
                                    key=lambda contract: contract.strike)
            if len(call_contracts) < 3:
                return

            legs = [
                Leg.create(call_contracts[0].symbol, 1),
                Leg.create(call_contracts[1].symbol, -2),
                Leg.create(call_contracts[2].symbol, 1),
            ]
            self._combo_ticket = self.combo_market_order(legs, 10)

            if len(self._combo_ticket) != len(legs) or len(self._combo_ticket.tickets) != len(legs):
                raise AssertionError(f"Expected {len(legs)} leg tickets, found {len(self._combo_ticket)}")
            if self._combo_ticket.group_order_manager_id is None:
                raise AssertionError("The combo order ticket was expected to have a group order manager id")

    def on_order_event(self, order_event):
        if order_event.status != OrderStatus.FILLED:
            return

        order = self.transactions.get_order_by_id(order_event.order_id)

        # the fee amount shortcuts and operators must match the two-level value.amount
        fee_amount = order_event.order_fee.value.amount
        if order_event.order_fee_amount != fee_amount or order_event.order_fee.amount != fee_amount:
            raise AssertionError(f"Order fee amount shortcuts do not match the fee amount {fee_amount}")
        fee = order_event.order_fee
        if fee + fee != 2 * fee_amount or sum([fee, fee], 0) != 2 * fee_amount or (fee_amount != 0 and not fee > 0):
            raise AssertionError(f"Order fee operators do not match the fee amount {fee_amount}")

        if order.type == OrderType.COMBO_MARKET:
            # Note: these fill events are received while the synchronous combo_market_order() call is still
            # in flight, so the combo ticket is checked against them in on_end_of_algorithm
            self._combo_fill_events_count += 1
            if order.group_order_manager_id is None:
                raise AssertionError("Combo orders were expected to have a group order manager id")
            if order_event.group_id != order.group_order_manager_id:
                raise AssertionError(f"Expected order event group id {order.group_order_manager_id}, "
                                     f"found {order_event.group_id}")
            self._combo_fill_group_ids.add(order.group_order_manager_id)
        elif order.group_order_manager_id is not None or order_event.group_id is not None:
            raise AssertionError("Non-combo orders were expected to have null group ids")

    def on_end_of_algorithm(self):
        if self._tagged_ticket is None or self._tagged_ticket.tag != "tagged entry":
            raise AssertionError("The market order tag was not set from the tag argument")
        if self._notional_ticket.status != OrderStatus.FILLED:
            raise AssertionError("The notional target order was expected to be filled")
        if self._combo_ticket is None or self._combo_fill_events_count != len(self._combo_ticket):
            raise AssertionError("The combo order was expected to be placed and filled")
        if not self._combo_ticket.filled:
            raise AssertionError("The combo order ticket was expected to aggregate the leg fills")
        if self._combo_fill_group_ids != {self._combo_ticket.group_order_manager_id}:
            raise AssertionError(f"Expected all combo fills to have group id {self._combo_ticket.group_order_manager_id}, "
                                 f"found {self._combo_fill_group_ids}")
