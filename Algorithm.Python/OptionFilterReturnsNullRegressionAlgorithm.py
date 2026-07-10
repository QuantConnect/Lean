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
### Regression algorithm asserting that the option filter function is allowed to return None (null in C#):
### the filter methods modify the universe in place, so returning it is only necessary for chaining.
### </summary>
class OptionFilterReturnsNullRegressionAlgorithm(QCAlgorithm):
    underlying_ticker = "GOOG"

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        equity = self.add_equity(self.underlying_ticker)
        option = self.add_option(self.underlying_ticker)
        self.option_symbol = option.symbol
        self._option_chain_received = False

        # set our strike/expiry filter for this option chain without returning the universe:
        # it is modified in place by the filter methods, returning it is only necessary for chaining
        option.set_filter(self._option_filter)

        # use the underlying equity as the benchmark
        self.set_benchmark(equity.symbol)

    def _option_filter(self, universe: OptionFilterUniverse) -> None:
        universe.standards_only().strikes(-2, +2).expiration(0, 180)

    def on_data(self, slice):
        if self.portfolio.invested or not self.is_market_open(self.option_symbol):
            return

        chain = slice.option_chains.get(self.option_symbol)
        if not chain:
            return

        self._option_chain_received = True

        # we find at the money (ATM) put contract with farthest expiration
        contracts = sorted(sorted(sorted(chain, \
            key = lambda x: abs(chain.underlying.price - x.strike)), \
            key = lambda x: x.expiry, reverse=True), \
            key = lambda x: x.right, reverse=True)

        # if found, trade it
        if len(contracts) == 0: return
        symbol = contracts[0].symbol
        self.market_order(symbol, 1)
        self.market_on_close_order(symbol, -1)

    def on_end_of_algorithm(self):
        if not self._option_chain_received:
            raise Exception("The option filter did not select any contracts")
