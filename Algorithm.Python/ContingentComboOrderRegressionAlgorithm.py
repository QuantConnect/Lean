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
### Regression algorithm asserting the behavior of contingent combo orders: a combo market order which once all its legs fill
### triggers two combo limit orders related through a one cancels other contingency. Each combo order is handled as a single unit:
### when one of the combo limit orders fills all the legs of the other one are canceled.
### </summary>
class ContingentComboOrderRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        equity = self.add_equity("GOOG", leverage=4, fill_forward=True)
        option = self.add_option(equity.symbol, fill_forward=True)
        self._option_symbol = option.symbol

        option.set_filter(lambda u: u.standards_only().strikes(-2, 2).expiration(0, 180))

        self._parent = None
        self._step = 0

    def on_data(self, slice: Slice) -> None:
        if self._parent is None:
            chain = slice.option_chains.get(self._option_symbol)
            if not self.is_market_open(self._option_symbol) or chain is None:
                return

            calls = [contract for contract in chain if contract.right == OptionRight.CALL]
            if not calls:
                return
            expiry = min(contract.expiry for contract in calls)
            call_contracts = sorted([contract for contract in calls if contract.expiry == expiry], key=lambda contract: contract.strike)
            if len(call_contracts) < 3:
                return

            legs = [
                Leg.create(call_contracts[0].symbol, 1),
                Leg.create(call_contracts[1].symbol, -2),
                Leg.create(call_contracts[2].symbol, 1),
            ]
            current_price = sum(leg.quantity * self.securities[leg.symbol].close for leg in legs)

            # selling the combo: the first one is too expensive so it won't fill, the second one is marketable
            self._far_exit = self.order_factory.combo_limit_order(legs, -2, current_price + 3, tag="Far exit")
            self._marketable_exit = self.order_factory.combo_limit_order(legs, -2, current_price - 1.5, tag="Marketable exit")
            self._parent = self.order_factory.combo_market_order(legs, 2, tag="Parent")
            # the legs of a combo order are a single unit, they trigger together
            tickets = self.one_triggers_other_order(self._parent, self.order_factory.one_cancels_other(self._far_exit + self._marketable_exit))

            self._parent_tickets = [self._ticket(leg) for leg in self._parent]
            self._far_exit_tickets = [self._ticket(leg) for leg in self._far_exit]
            self._marketable_exit_tickets = [self._ticket(leg) for leg in self._marketable_exit]

            if (len(tickets) != 9 or [x.order_id for x in tickets] != [x.order_id for x in self._parent_tickets + self._far_exit_tickets + self._marketable_exit_tickets]
                or any(leg.contingency.count != 9 for leg in self._parent)
                or any(x.contingency.count != 9 for x in tickets)
                or len({x.submit_request.group_order_manager.id for x in tickets}) != 3):
                raise RegressionTestException("Unexpected order tickets")

            # the combo market order filled, all its legs, so the exits were triggered
            if (any(x.status != OrderStatus.FILLED for x in self._parent_tickets)
                or any(x.contingency.is_waiting_for_trigger or x.status != OrderStatus.SUBMITTED for x in self._far_exit_tickets + self._marketable_exit_tickets)):
                raise RegressionTestException("Expected the parent combo order to be filled and the exits to be triggered")

            # each leg holds the contingencies of its combo order
            if any(len(x.contingency.links) != 1 or x.contingency.links[0].role != ContingencyRole.PARENT for x in self._parent_tickets):
                raise RegressionTestException("Unexpected contingencies")
            for ticket in self._far_exit_tickets + self._marketable_exit_tickets:
                links = ticket.contingency.links
                if (len(links) != 2
                    or sum(1 for link in links if link.role == ContingencyRole.CHILD and link.triggered) != 1
                    or sum(1 for link in links if link.role is None and link.type == ContingencyType.ONE_CANCELS_OTHER) != 1):
                    raise RegressionTestException("Unexpected contingencies")
            return

        self._step += 1
        if self._step == 2:
            # the marketable combo filled, all its legs, so all the legs of the other combo were canceled
            if (any(x.status != OrderStatus.FILLED for x in self._marketable_exit_tickets)
                or any(x.status != OrderStatus.CANCELED for x in self._far_exit_tickets)):
                raise RegressionTestException("Expected the marketable exit to be filled and the far exit to be canceled")

            if self.portfolio.invested or len(self.transactions.get_open_orders()) != 0:
                raise RegressionTestException("Expected no position nor open orders")

    def _ticket(self, request: SubmitOrderRequest) -> OrderTicket:
        return self.transactions.get_order_ticket(request.order_id)

    def on_end_of_algorithm(self) -> None:
        if self._step < 2:
            raise RegressionTestException("Expected the contingent combo orders to be submitted and asserted")
