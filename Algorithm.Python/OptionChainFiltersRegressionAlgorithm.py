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
        option.set_filter(lambda universe: universe.calls_only().expiration(1, 10).strikes(-2, 2).out_of_the_money())

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

        # Moneyness filters split the strikes around the underlying price, ATM is the closest strike
        price = chain.underlying.price
        otm = chain.otm()
        itm = chain.itm()
        if (otm.count == 0 or itm.count == 0 or otm.count + itm.count + chain.strikes([price]).count != chain.count
                or any((x.strike <= price if x.right == OptionRight.CALL else x.strike >= price) for x in otm)
                or any((x.strike >= price if x.right == OptionRight.CALL else x.strike <= price) for x in itm)):
            raise AssertionError("Out/in the money filters mismatch")
        # By default the strikes on either side of the 748.54 close, 747.5 and 750, also reached within 2.5 points but not within 1;
        # a chain whose strikes start more than 2% above the close has no strike at the money
        atm = chain.atm()
        if (atm.count == 0 or atm.count != chain.strikes([747.5, 750]).count or any(x.strike != 747.5 and x.strike != 750 for x in atm)
                or chain.atm(2.5).count != atm.count or chain.atm(1).count != 0 or chain.atm(0).count != 0
                or chain.strikes_above(price + 20).atm().count != 0):
            raise AssertionError("Expected atm() to select the 747.5 and 750 strikes, atm(1) none")

        # Strike sets and bounds are absolute, unlike the relative strikes(min, max)
        strikes = chain.strikes([745, 750])
        if (strikes.count == 0 or any(x.strike != 745 and x.strike != 750 for x in strikes)
                or any(x.strike != 752.5 for x in chain.strikes_above(750).strikes_below(755))
                or chain.strikes_above(price).count + chain.strikes_below(price).count + chain.strikes([price]).count != chain.count):
            raise AssertionError("Strike set or bound filters mismatch")

        # Expiration sets and bounds, today's expiration and the farthest one
        front_month = datetime(2015, 12, 24)
        farthest = chain.farthest_expiration()
        max_expiry = max(x.expiry for x in chain)
        if (chain.expiration([front_month]).count != chain.front_month().count
                or chain.zero_dte().count != chain.expiration(0, 0).count
                or chain.expiring_after(front_month).count + chain.front_month().count != chain.count
                or chain.expiring_before(front_month).count != 0
                or farthest.count == 0 or any(x.expiry != max_expiry for x in farthest)):
            raise AssertionError("Expiration set, bound, zero_dte() or farthest_expiration() filters mismatch")

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

        # The universe only selected the out of the money calls expiring 1 to 10 days out, two strikes around the
        # previous close: 750 and 752.5 on 2015-12-31. The chain filters agree with it
        if (chain.calls_only().expiration(1, 10).count != chain.count or chain.puts_only().count != 0
                or chain.strikes([750, 752.5]).count != chain.count or chain.expiration([datetime(2015, 12, 31)]).count != chain.count):
            raise AssertionError("Slice chain filters disagree with the universe filter")

        # On a calls only chain the moneyness filters are the strike bounds around the current price
        price = chain.underlying.price
        if (chain.out_of_the_money().count != chain.strikes_above(price).count or chain.in_the_money().count != chain.strikes_below(price).count
                or chain.out_of_the_money().count + chain.in_the_money().count + chain.strikes([price]).count != chain.count):
            raise AssertionError("Slice chain moneyness filters mismatch")
        if (chain.expiring_after(self.time).count != chain.count or chain.expiring_before(self.time).count != 0 or chain.zero_dte().count != 0
                or chain.farthest_expiration().count != chain.count):
            raise AssertionError("Slice chain expiration filters mismatch")

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
