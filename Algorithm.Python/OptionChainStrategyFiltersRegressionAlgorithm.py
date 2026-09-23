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
### Regression algorithm demonstrating that the option strategy filters of the universe selection, like straddle()
### or iron_condor(), select the strategy legs straight from an option chain too
### </summary>
class OptionChainStrategyFiltersRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        option = self.add_option("GOOG")
        self._option = option.symbol
        # The universe selects the straddle legs, the same filter picks them again from the slice chain below
        option.set_filter(lambda universe: universe.straddle(7))

        chain = self.option_chain(self._option)
        expiry = datetime(2015, 12, 31)

        # GOOG closed at 748.54 on 2015-12-23, the first expiry at least 7 days out is 2015-12-31 and the ATM strike is 747.50
        self._assert_legs(chain.straddle(7), expiry, [(OptionRight.CALL, 747.5), (OptionRight.PUT, 747.5)])

        # Iron condor: near legs 5 away from the spot, far legs 10 away
        self._assert_legs(chain.iron_condor(7, 5, 10), expiry,
            [(OptionRight.CALL, 752.5), (OptionRight.CALL, 757.5), (OptionRight.PUT, 737.5), (OptionRight.PUT, 742.5)])

        # Single contract and vertical spread pickers
        self._assert_legs(chain.naked_put(7, -5), expiry, [(OptionRight.PUT, 742.5)])
        self._assert_legs(chain.call_spread(7, 5), expiry, [(OptionRight.CALL, 742.5), (OptionRight.CALL, 752.5)])

        # Calendar spread: same strike, expiries at least 7 and 14 days out
        calendar = chain.call_calendar_spread(0, 7, 14)
        if (calendar.count != 2 or any(x.right != OptionRight.CALL or x.strike != 747.5 for x in calendar)
                or sorted(x.expiry for x in calendar) != [expiry, datetime(2016, 1, 8)]):
            raise AssertionError(f"Unexpected calendar spread legs: {[x.symbol.value for x in calendar]}")

        # No match selects nothing instead of raising
        if chain.straddle(1000).count != 0:
            raise AssertionError("Expected no legs for an expiry out of the chain")

        # Invalid arguments are rejected like the universe filters do
        try:
            chain.strangle(7, -5, 5)
            raise AssertionError("Expected strangle() to reject a negative call strike distance")
        except ArgumentException:
            pass

        self._traded = False

    def on_data(self, slice):
        if self._traded:
            return
        chain = slice.option_chains.get(self._option)
        if not chain:
            return

        # The same filter that selected the universe picks the legs from the slice chain
        legs = chain.straddle(7)
        if legs.count == 2:
            leg = next(iter(legs))
            self.buy(OptionStrategies.straddle(self._option, leg.strike, leg.expiry), 1)
            self._traded = True

    def on_end_of_algorithm(self):
        if not self._traded:
            raise AssertionError("Expected to trade the straddle selected from the slice option chain")

    def _assert_legs(self, legs, expiry, expected):
        actual = sorted((x.right, x.strike) for x in legs)
        if any(x.expiry != expiry for x in legs) or actual != sorted(expected):
            raise AssertionError(f"Unexpected legs: {[x.symbol.value for x in legs]}")
