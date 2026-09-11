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
### Regression algorithm using the expiration set and bound filters in the futures universe selection,
### the same ones the option universes and chains offer, and checking the selected chains in the slice
### </summary>
class FutureUniverseFiltersRegressionAlgorithm(QCAlgorithm):
    END_OF_2013 = datetime(2013, 12, 31)
    END_OF_NOVEMBER_2014 = datetime(2014, 11, 30)

    def initialize(self):
        self.set_start_date(2013, 10, 7)
        self.set_end_date(2013, 10, 9)
        self.set_cash(1000000)

        # The 2014 contracts up to September
        es = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE, Market.CME)
        es.set_filter(lambda universe: universe.expiring_after(self.END_OF_2013).expiring_before(self.END_OF_NOVEMBER_2014))
        self._es = es.symbol

        # The contracts expiring this year
        gc = self.add_future(Futures.Metals.GOLD, Resolution.MINUTE, Market.COMEX)
        gc.set_filter(lambda universe: universe.expiring_before(datetime(2014, 1, 1)))
        self._gc = gc.symbol

        self._es_chain_seen = False
        self._gc_chain_seen = False
        self._traded = False

        # The full chain from the universe data lists the December 2013 contract and the March to December 2014 ones
        chain = self.futures_chain(self._es)
        expiries = sorted(x.expiry for x in chain)
        if len(expiries) != 5 or expiries[0] > self.END_OF_2013 or any(x.year != 2014 for x in expiries[1:]):
            raise AssertionError(f"Unexpected ES chain expiries: {expiries}")

    def on_data(self, slice):
        es_chain = slice.futures_chains.get(self._es)
        if es_chain:
            self._es_chain_seen = True
            # March, June and September 2014
            if es_chain.count == 0 or es_chain.count > 3 or any(x.expiry <= self.END_OF_2013 or x.expiry >= self.END_OF_NOVEMBER_2014 for x in es_chain):
                raise AssertionError(f"The ES chain disagrees with the universe filter: {[x.expiry for x in es_chain]}")
            if not self._traded:
                self.market_order(min(es_chain, key=lambda x: x.expiry).symbol, 1)
                self._traded = True

        gc_chain = slice.futures_chains.get(self._gc)
        if gc_chain:
            self._gc_chain_seen = True
            # October, November and December 2013
            if gc_chain.count == 0 or gc_chain.count > 3 or any(x.expiry.year != 2013 for x in gc_chain):
                raise AssertionError(f"The GC chain disagrees with the universe filter: {[x.expiry for x in gc_chain]}")

    def on_end_of_algorithm(self):
        if not self._es_chain_seen or not self._gc_chain_seen or not self._traded:
            raise AssertionError(f"Expected the ES chain ({self._es_chain_seen}), the GC chain ({self._gc_chain_seen}) and a trade ({self._traded})")
