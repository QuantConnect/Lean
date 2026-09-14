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
### Regression algorithm using the futures chain filters, the same ones the futures universe selection offers,
### on futures_chain() and on the chains of the slice
### </summary>
class FuturesChainFiltersRegressionAlgorithm(QCAlgorithm):
    END_OF_2013 = datetime(2013, 12, 31)
    MAX_LONG = 2**62

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 9)
        self.set_cash(1000000)

        # The contracts expiring within a year
        es = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE, Market.CME)
        es.set_filter(lambda universe: universe.expiration(0, 365))
        self._es = es.symbol

        # The liquid contracts, by open interest
        gc = self.add_future(Futures.Metals.GOLD, Resolution.MINUTE, Market.COMEX)
        gc.set_filter(lambda universe: universe.open_interest(100000, self.MAX_LONG))
        self._gc = gc.symbol

        self._es_chain_seen = False
        self._gc_chain_seen = False
        self._traded = False

        # The full chain from the universe data: December 2013 and March, June, September and December 2014
        chain = self.futures_chain(self._es)
        if chain.count != 5:
            raise AssertionError(f"Expected 5 ES contracts but got {chain.count}")
        self._assert_expiries(chain.front_month(), "front_month()", [(2013, 12)])
        self._assert_expiries(chain.back_month(), "back_month()", [(2014, 3)])
        self._assert_expiries(chain.back_months(), "back_months()", [(2014, 3), (2014, 6), (2014, 9), (2014, 12)])
        self._assert_expiries(chain.farthest_expiration(), "farthest_expiration()", [(2014, 12)])
        self._assert_expiries(chain.expiration_cycle([3, 9]), "expiration_cycle([3, 9])", [(2014, 3), (2014, 9)])
        # ES contracts are named after their expiration month, so the contract month filters agree with the expiration ones
        self._assert_expiries(chain.contract_months([3, 9]), "contract_months([3, 9])", [(2014, 3), (2014, 9)])
        self._assert_expiries(chain.expiring_before(self.END_OF_2013), "expiring_before(2013-12-31)", [(2013, 12)])
        self._assert_expiries(chain.expiring_after(self.END_OF_2013).expiring_before(datetime(2014, 7, 1)), "expiring_after(2013-12-31).expiring_before(2014-07-01)", [(2014, 3), (2014, 6)])
        self._assert_expiries(chain.expiration([next(iter(chain.front_month())).expiry]), "expiration([front month expiry])", [(2013, 12)])
        if chain.zero_dte().count != 0 or chain.standards_only().count != chain.count or chain.weeklys_only().count != 0:
            raise AssertionError("Expected no contract expiring today and only standard contracts")

        # The liquidity filters read the universe data: only the front month has more than a million contracts open
        self._assert_expiries(chain.open_interest(1000000, self.MAX_LONG), "open_interest(1000000, max)", [(2013, 12)])
        if chain.oi(0, 1000000).count != chain.count - 1 or chain.volume(1, self.MAX_LONG).count != sum(1 for x in chain if x.volume >= 1):
            raise AssertionError("Open interest or volume filter mismatch")

    def on_data(self, slice):
        es_chain = slice.futures_chains.get(self._es)
        if es_chain:
            self._es_chain_seen = True
            # The universe selected the contracts expiring within a year, so the chain filters agree with it
            if (es_chain.count == 0 or es_chain.count > 4 or es_chain.expiration(0, 365).count != es_chain.count or es_chain.expiring_after(self.time).count != es_chain.count
                    or es_chain.zero_dte().count != 0 or es_chain.expiration_cycle([3, 6, 9, 12]).count != es_chain.count or es_chain.expiration_cycle([1, 2]).count != 0
                    or es_chain.standards_only().count != es_chain.count or es_chain.weeklys_only().count != 0):
                raise AssertionError("The ES slice chain disagrees with the universe filter")
            front_month = es_chain.front_month()
            farthest = es_chain.farthest_expiration()
            min_expiry = min(x.expiry for x in es_chain)
            max_expiry = max(x.expiry for x in es_chain)
            if (front_month.count == 0 or any(x.expiry != min_expiry for x in front_month) or any(x.expiry != max_expiry for x in farthest)
                    or es_chain.back_months().count != es_chain.count - front_month.count):
                raise AssertionError("Front month, back months or farthest expiration mismatch on the ES slice chain")
            if (es_chain.open_interest(1, self.MAX_LONG).count != sum(1 for x in es_chain if x.open_interest >= 1)
                    or es_chain.volume(1, self.MAX_LONG).count != sum(1 for x in es_chain if x.volume >= 1)):
                raise AssertionError("Open interest or volume filter mismatch on the ES slice chain")

            # Buy the front contract expiring at least 90 days out
            if not self._traded:
                contract = next(iter(es_chain.expiring_after(self.time + timedelta(days=90)).front_month()), None)
                if contract is not None:
                    self.market_order(contract.symbol, 1)
                    self._traded = True

        gc_chain = slice.futures_chains.get(self._gc)
        if gc_chain:
            self._gc_chain_seen = True
            # Only the December 2013 contract had more than a hundred thousand contracts open
            if gc_chain.count == 0 or any(x.expiry.year != 2013 or x.expiry.month != 12 for x in gc_chain) or gc_chain.front_month().count != gc_chain.count:
                raise AssertionError(f"The GC slice chain disagrees with the universe filter: {[x.expiry for x in gc_chain]}")

    def on_end_of_algorithm(self):
        if not self._es_chain_seen or not self._gc_chain_seen or not self._traded:
            raise AssertionError(f"Expected the ES chain ({self._es_chain_seen}), the GC chain ({self._gc_chain_seen}) and a trade ({self._traded})")

    def _assert_expiries(self, chain, filter_name, expected):
        actual = sorted((x.expiry.year, x.expiry.month) for x in chain)
        if actual != sorted(expected):
            raise AssertionError(f"{filter_name}: expected {expected} but got {actual}")
