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
### Regression algorithm demonstrating that option chains can be filtered with the same filters used for
### option universe selection, both on chains from QCAlgorithm.option_chain() and on the chains delivered in the slice
### </summary>
class OptionChainFiltersRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        option = self.add_option("GOOG")
        self._option = option.symbol
        # The same words select the universe and, below, narrow down the chains
        option.set_filter(lambda universe: universe.calls_only().expiration(1, 10).strikes(-2, 2))

        chain = self.option_chain(self._option)
        if chain.count == 0:
            raise AssertionError("Expected a non empty option chain")
        # The relative strikes filter needs the underlying price, chains built from universe data must carry it
        if chain.underlying.price == 0:
            raise AssertionError("Expected the chain to carry the underlying price")

        total_contracts = chain.count
        filtered = chain.calls_only().expiration(1, 10).strikes(-2, 2)
        # GOOG closed at 748.54 on 2015-12-23 and the only expiration 1 to 10 days out is 2015-12-31,
        # so the two strikes below the spot and the two at or above it are 745, 747.5, 750 and 752.5
        self._assert_contracts(filtered, OptionRight.CALL, datetime(2015, 12, 31), [745, 747.5, 750, 752.5])
        if chain.count != total_contracts:
            raise AssertionError("Filters must not modify the source chain")

        # Front month is the nearest expiration, 2015-12-24 itself
        self._assert_contracts(chain.puts_only().front_month(), OptionRight.PUT, datetime(2015, 12, 24))

        # Standard contracts expire on the third Friday, weeklys do not
        standards = chain.standards_only().front_month()
        if standards.count == 0 or any(x.expiry != datetime(2016, 1, 15) for x in standards):
            raise AssertionError("Expected the standard front month to expire on 2016-01-15")
        weeklys = chain.weeklys_only()
        if weeklys.count == 0 or any(OptionSymbol.is_standard(x.symbol) for x in weeklys):
            raise AssertionError("Expected only weekly contracts")

        # Greeks filters use the greeks the chain carries
        deltas = chain.delta(0.5, 0.6)
        expected_deltas = sum(1 for x in chain if 0.5 <= x.greeks.delta <= 0.6)
        if deltas.count == 0 or deltas.count != expected_deltas or any(not 0.5 <= x.greeks.delta <= 0.6 for x in deltas):
            raise AssertionError("Delta filter mismatch")

        # where() takes a predicate, like the universe filter does
        high_open_interest = chain.where(lambda x: x.open_interest > 1000)
        if high_open_interest.count == 0 or high_open_interest.count != sum(1 for x in chain if x.open_interest > 1000):
            raise AssertionError("where() filter mismatch")

        self._traded = False

    def on_data(self, slice):
        if self._traded:
            return
        chain = slice.option_chains.get(self._option)
        if not chain:
            return

        # The universe only selected calls expiring 1 to 10 days out, so the chain filters agree with it
        if chain.calls_only().expiration(1, 10).count != chain.count or chain.puts_only().count != 0:
            raise AssertionError("Slice chain filters disagree with the universe filter")

        # Buy the call at the first strike at or above the underlying price
        contract = next(iter(chain.strikes(0, 0)), None)
        if contract is not None:
            self.market_order(contract.symbol, 1)
            self._traded = True

    def on_end_of_algorithm(self):
        if not self._traded:
            raise AssertionError("Expected to trade a contract selected from the slice option chain")

    def _assert_contracts(self, chain, right, expiry, strikes=None):
        if chain.count == 0 or any(x.right != right or x.expiry != expiry for x in chain):
            raise AssertionError(f"Expected only {right} contracts expiring on {expiry:%Y-%m-%d}")
        if strikes is not None and sorted(x.strike for x in chain) != strikes:
            raise AssertionError(f"Unexpected strikes: {[x.strike for x in chain]}")
