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
### Regression algorithm using the strike, expiration and moneyness filters on index options: in the universe selection
### of standard and weekly contracts, on the chains of the slice and on option_chain()
### </summary>
class IndexOptionChainFiltersRegressionAlgorithm(QCAlgorithm):
    FIRST_DAY = datetime(2021, 1, 4)
    STANDARD_EXPIRY = datetime(2021, 1, 15)

    def initialize(self):
        self.set_start_date(2021, 1, 4)
        self.set_end_date(2021, 1, 8)
        self.set_cash(1000000)

        # Standard SPX contracts: the out of the money ones with strikes below 4000
        spx = self.add_index_option("SPX")
        spx.set_filter(lambda universe: universe.out_of_the_money().strikes_below(4000))
        self._spx = spx.symbol

        # Weekly SPXW contracts: the 3700 strike of the expirations after the first day
        spxw = self.add_index_option("SPX", "SPXW")
        spxw.set_filter(lambda universe: universe.strikes([3700]).expiring_after(self.FIRST_DAY))
        self._spxw = spxw.symbol

        self._spx_chain_seen = False
        self._zero_dte_seen = False
        self._traded = False

        # The latest universe data, from 2020-12-31, lists the 3200, 3700, 3800 and 4250 calls and the 3200 and 4200 puts
        # expiring on 2021-01-15, with the index at 3766.63: the same filters narrow the chain down
        chain = self.option_chain(self._spx)
        if chain.count != 6 or chain.underlying.price != 3766.63:
            raise AssertionError(f"Expected the 6 SPX contracts at 3766.63 but got {chain.count} at {chain.underlying.price}")
        self._assert_contracts(chain.out_of_the_money(), "out_of_the_money()", [(3800, OptionRight.CALL), (4250, OptionRight.CALL), (3200, OptionRight.PUT)])
        self._assert_contracts(chain.in_the_money(), "in_the_money()", [(3200, OptionRight.CALL), (3700, OptionRight.CALL), (4200, OptionRight.PUT)])
        # 2% of the index, 75 points, reaches the 3700 and 3800 strikes; 50 points only 3800, 25 points none
        self._assert_contracts(chain.at_the_money(), "at_the_money()", [(3700, OptionRight.CALL), (3800, OptionRight.CALL)])
        self._assert_contracts(chain.at_the_money(50), "at_the_money(50)", [(3800, OptionRight.CALL)])
        self._assert_contracts(chain.at_the_money(25), "at_the_money(25)", [])
        self._assert_contracts(chain.at_the_money(0), "at_the_money(0)", [])
        self._assert_contracts(chain.strikes_above(3700).strikes_below(4250), "strikes_above(3700).strikes_below(4250)", [(3800, OptionRight.CALL), (4200, OptionRight.PUT)])
        self._assert_contracts(chain.strikes([3200, 4250]), "strikes([3200, 4250])", [(3200, OptionRight.CALL), (4250, OptionRight.CALL), (3200, OptionRight.PUT)])
        self._assert_contracts(chain.out_of_the_money().strikes_below(4000), "the SPX universe filter", [(3800, OptionRight.CALL), (3200, OptionRight.PUT)])
        if (chain.expiration([self.STANDARD_EXPIRY]).count != chain.count or chain.farthest_expiration().count != chain.count
                or chain.expiring_after(self.STANDARD_EXPIRY).count != 0 or chain.expiring_before(self.STANDARD_EXPIRY).count != 0
                or chain.zero_dte().count != 0):
            raise AssertionError("Expected every SPX contract to expire on 2021-01-15")

    def on_data(self, slice):
        spx_chain = slice.option_chains.get(self._spx)
        if spx_chain:
            self._spx_chain_seen = True
            # The universe selected the out of the money contracts below 4000: the 3800 call and the 3200 put.
            # The index stays between those strikes, so the chain filter agrees with the universe filter
            self._assert_contracts(spx_chain, "the SPX slice chain", [(3800, OptionRight.CALL), (3200, OptionRight.PUT)])
            if spx_chain.out_of_the_money().count != spx_chain.count:
                raise AssertionError("Expected the SPX slice chain to be out of the money")
            self._assert_moneyness(spx_chain)

        chain = slice.option_chains.get(self._spxw)
        if not chain:
            return

        # The universe selected the 3700 strike of the expirations after the first day: 2021-01-06 and 2021-01-08
        if (chain.count == 0 or chain.strikes([3700]).count != chain.count or chain.expiring_after(self.FIRST_DAY).count != chain.count
                or chain.expiring_before(datetime(2021, 1, 9)).count != chain.count):
            raise AssertionError("The SPXW slice chain disagrees with the universe filter")
        self._assert_moneyness(chain)

        zero_dte = chain.zero_dte()
        if any(x.expiry.date() != self.time.date() for x in zero_dte):
            raise AssertionError("zero_dte() selected contracts not expiring today")
        self._zero_dte_seen |= zero_dte.count > 0

        farthest = chain.farthest_expiration()
        max_expiry = max(x.expiry for x in chain)
        if farthest.count == 0 or any(x.expiry != max_expiry for x in farthest):
            raise AssertionError("farthest_expiration() mismatch")

        # Buy the 3700 call of the nearest expiration after today
        if not self._traded:
            contract = next(iter(chain.calls_only().expiring_after(self.time).front_month()), None)
            if contract is not None:
                self.market_order(contract.symbol, 1)
                self._traded = True

    def on_end_of_algorithm(self):
        if not self._spx_chain_seen or not self._zero_dte_seen or not self._traded:
            raise AssertionError(f"Expected the SPX chain ({self._spx_chain_seen}), a 0DTE SPXW contract ({self._zero_dte_seen}) and a trade ({self._traded})")

    def _assert_moneyness(self, chain):
        '''The moneyness filters partition the chain around the current index price, and match the strike bounds for a single right'''
        price = chain.underlying.price
        otm = chain.out_of_the_money()
        itm = chain.in_the_money()
        if (otm.count + itm.count + chain.strikes([price]).count != chain.count
                or any((x.strike <= price if x.right == OptionRight.CALL else x.strike >= price) for x in otm)
                or any((x.strike >= price if x.right == OptionRight.CALL else x.strike <= price) for x in itm)
                or chain.calls_only().out_of_the_money().count != chain.calls_only().strikes_above(price).count
                or chain.puts_only().out_of_the_money().count != chain.puts_only().strikes_below(price).count):
            raise AssertionError(f"Moneyness filters mismatch at {price}")

    def _assert_contracts(self, chain, filter_name, expected):
        key = lambda contract: (float(contract[0]), contract[1] == OptionRight.PUT)
        actual = sorted(((x.strike, x.right) for x in chain), key=key)
        expected = sorted(expected, key=key)
        if [key(x) for x in actual] != [key(x) for x in expected]:
            raise AssertionError(f"{filter_name}: expected {expected} but got {actual}")
