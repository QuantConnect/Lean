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
### Regression algorithm legging into multiple single-lot option strategy position groups with sequential
### market orders and then going through a margin call that requires a partial reduction of the groups.
### The margin call order quantity calculation probes degenerate (zero-quantity) trial groups, which used to
### crash the algorithm with "Sequence contains no matching element" in OptionStrategyPositionGroupBuyingPowerModel.
### </summary>
class LeggedInOptionStrategiesMarginCallRegressionAlgorithm(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(200000)

        equity = self.add_equity("GOOG", leverage=4)
        option = self.add_option(equity.symbol)
        self._option_symbol = option.symbol
        option.set_filter(lambda u: u.standards_only().strikes(-2, +2).expiration(0, 180))

        self._legged = False
        self._cash_dropped = False
        self._on_margin_call_count = 0

    def on_data(self, slice):
        if not self._legged:
            chain = slice.option_chains.get(self._option_symbol)
            if not self.is_market_open(self._option_symbol) or not chain:
                return

            contracts_by_expiry = {}
            for contract in chain:
                contracts_by_expiry.setdefault(contract.expiry, []).append(contract)
            expiries = sorted(contracts_by_expiry.keys())

            # A put spread at the nearest expiry: long the lowest strike put, short the next one
            puts = sorted([x for x in contracts_by_expiry[expiries[0]] if x.right == OptionRight.PUT],
                          key=lambda x: x.strike)
            long_put = puts[0]
            short_put = next(x for x in puts if x.strike > long_put.strike)

            # And a call spread at another expiry so two separate strategy groups are resolved
            calls = next(c for c in
                         (sorted([x for x in contracts_by_expiry[expiry] if x.right == OptionRight.CALL], key=lambda x: x.strike)
                          for expiry in expiries[1:])
                         if len(c) > 1)
            short_call = calls[0]
            long_call = next(x for x in calls if x.strike > short_call.strike)

            # Leg into the strategies with individual market orders instead of combo orders
            self.market_order(short_call.symbol, -1)
            self.market_order(long_call.symbol, +1)
            self.market_order(short_put.symbol, -1)
            self.market_order(long_put.symbol, +1)
            self._legged = True

            self.assert_option_strategy_is_present("Bear Call Spread")
            self.assert_option_strategy_is_present("Bull Put Spread")
            return

        if not self._cash_dropped and self.portfolio.invested:
            # Simulate a drawdown: equity drops below the margin used by the strategy groups so that the
            # margin call model requests a partial reduction of the single-lot position groups
            cash = self.portfolio.cash_book[Currencies.USD].amount
            self.portfolio.cash_book[Currencies.USD].set_amount(
                cash - self.portfolio.total_portfolio_value + 0.6 * self.portfolio.total_margin_used)
            self._cash_dropped = True

    def on_margin_call(self, requests):
        self._on_margin_call_count += 1

        for request in requests:
            holdings_quantity = self.securities[request.symbol].holdings.quantity
            if request.quantity != -holdings_quantity:
                raise Exception(f"Expected margin call order for {request.symbol} to fully liquidate the "
                                f"{holdings_quantity} holdings, but its quantity was {request.quantity}")

        return requests

    def on_end_of_algorithm(self):
        if self._on_margin_call_count != 1:
            raise Exception(f"OnMarginCall was called {self._on_margin_call_count} times, expected 1")

        orders = list(self.transactions.get_orders())
        if len(orders) <= 4:
            raise Exception(f"Expected margin call orders in addition to the 4 strategy leg entries, "
                            f"but found {len(orders)} orders in total")

        if any(x.status != OrderStatus.FILLED for x in orders):
            raise Exception("All orders should be filled")

    def assert_option_strategy_is_present(self, name):
        if sum(1 for group in self.portfolio.positions.groups
               if str(group.buying_power_model) == name) != 1:
            raise Exception(f"Option strategy: '{name}' was not found!")
