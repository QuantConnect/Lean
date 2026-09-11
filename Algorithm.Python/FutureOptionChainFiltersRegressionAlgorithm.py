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
### Regression algorithm using the strike, expiration and moneyness filters on future options: in the universe selection
### of the future and of its options, on the chains of the slice and on option_chain()
### </summary>
class FutureOptionChainFiltersRegressionAlgorithm(QCAlgorithm):
    MARCH_EXPIRY = datetime(2020, 3, 20)
    SELECTED_STRIKES = [3200, 3210, 3220, 3230, 3240, 3250]

    def initialize(self):
        self.set_start_date(2020, 1, 5)
        self.set_end_date(2020, 1, 6)
        self.set_cash(1000000)

        # The March 2020 future, by its expiration date
        es = self.add_future(Futures.Indices.SP_500_E_MINI, Resolution.MINUTE, Market.CME)
        es.set_filter(lambda universe: universe.expiration([self.MARCH_EXPIRY]))
        self._es = es.symbol

        # Its options: the out of the money contracts within three strikes of the future price
        self.add_future_option(self._es, lambda universe: universe.strikes(-3, 3).out_of_the_money())

        self._chain_seen = False
        self._traded = False

        # The option chain of the March future from the universe data: one expiration, the future at 3223.75
        chain = self.option_chain(Symbol.create_future(Futures.Indices.SP_500_E_MINI, Market.CME, self.MARCH_EXPIRY))
        if chain.count == 0 or chain.underlying.price != 3223.75 or chain.symbol.security_type != SecurityType.FUTURE_OPTION:
            raise AssertionError(f"Expected the March ES option chain at 3223.75 but got {chain.count} contracts at {chain.underlying.price}")
        # Strikes are 10 points apart around the money: three each side of 3223.75 are 3200 to 3250
        self._assert_strikes(chain.strikes(-3, 3).out_of_the_money().calls_only(), "strikes(-3, 3).out_of_the_money().calls_only()", [3230, 3240, 3250])
        self._assert_strikes(chain.strikes(-3, 3).out_of_the_money().puts_only(), "strikes(-3, 3).out_of_the_money().puts_only()", [3200, 3210, 3220])
        # Only the put is listed at 3310
        self._assert_strikes(chain.strikes_above(3300).strikes_below(3320), "strikes_above(3300).strikes_below(3320)", [3310])
        self._assert_strikes(chain.strikes_above(3300).strikes_below(3320).calls_only(), "strikes_above(3300).strikes_below(3320).calls_only()", [])
        self._assert_strikes(chain.at_the_money(5), "at_the_money(5)", [3220, 3220])
        if (chain.at_the_money().count != 0 or chain.expiration([self.MARCH_EXPIRY]).count != chain.count or chain.farthest_expiration().count != chain.count
                or chain.expiring_after(self.MARCH_EXPIRY).count != 0 or chain.zero_dte().count != 0
                or chain.standards_only().count != chain.count or chain.weeklys_only().count != 0):
            raise AssertionError("Expiration or contract type filters mismatch on the March ES option chain")

    def on_data(self, slice):
        # One chain per future contract, keyed by its canonical option symbol
        for chain in slice.option_chains.values():
            if chain.symbol.underlying.id.date != self.MARCH_EXPIRY:
                raise AssertionError(f"Unexpected option chain for {chain.symbol.underlying}")
            self._chain_seen = True

            # The universe selected the out of the money contracts within three strikes of the previous close: 3200 to 3250
            if chain.count == 0 or chain.strikes(self.SELECTED_STRIKES).count != chain.count or chain.expiration([self.MARCH_EXPIRY]).count != chain.count:
                raise AssertionError(f"The option chain disagrees with the universe filter: {[x.symbol.value for x in chain]}")

            # The moneyness filters partition the chain around the current future price, and match the strike bounds for a single right
            price = chain.underlying.price
            otm = chain.out_of_the_money()
            itm = chain.in_the_money()
            if (otm.count + itm.count + chain.strikes([price]).count != chain.count
                    or any((x.strike <= price if x.right == OptionRight.CALL else x.strike >= price) for x in otm)
                    or any((x.strike >= price if x.right == OptionRight.CALL else x.strike <= price) for x in itm)
                    or chain.calls_only().out_of_the_money().count != chain.calls_only().strikes_above(price).count
                    or chain.puts_only().out_of_the_money().count != chain.puts_only().strikes_below(price).count):
                raise AssertionError(f"Moneyness filters mismatch at {price}")

            # Buy the out of the money call closest to the future price
            if not self._traded:
                calls = sorted(otm.calls_only(), key=lambda x: x.strike)
                if calls:
                    self.market_order(calls[0].symbol, 1)
                    self._traded = True

    def on_end_of_algorithm(self):
        if not self._chain_seen or not self._traded:
            raise AssertionError(f"Expected the March ES option chain ({self._chain_seen}) and a trade ({self._traded})")

    def _assert_strikes(self, chain, filter_name, expected):
        actual = sorted(float(x.strike) for x in chain)
        if actual != sorted(float(x) for x in expected):
            raise AssertionError(f"{filter_name}: expected strikes {expected} but got {actual}")
