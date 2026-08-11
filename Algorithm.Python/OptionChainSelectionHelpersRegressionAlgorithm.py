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
### Regression algorithm demonstrating the option chain selection helpers: select(), closest_expiry(),
### at(), at_the_money() and strikes, which replace the usual hand-rolled sorted-comprehension
### contract selection with a single call.
### </summary>
class OptionChainSelectionHelpersRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        goog = self.add_equity("GOOG").symbol
        chain = self.option_chain(goog)

        # One-line selection: the call at the expiry closest to 10 days out with the strike closest
        # to the underlying price (at the money is the default when no moneyness/delta is given)
        contract = chain.select(right=OptionRight.CALL, target_dte=10)
        if contract is None:
            raise AssertionError("select(right, target_dte) returned no contract")

        # The equivalent hand-rolled ceremony must select the very same contract
        spot = chain.underlying.price
        calls = [x for x in chain if x.right == OptionRight.CALL]
        ceremony_expiry = min({x.expiry for x in calls}, key=lambda expiry: abs((expiry - self.time).days - 10))
        ceremony_contract = min((x for x in calls if x.expiry == ceremony_expiry), key=lambda x: abs(x.strike - spot))
        if contract.symbol != ceremony_contract.symbol:
            raise AssertionError(f"select() mismatch: {contract.symbol.value} != ceremony {ceremony_contract.symbol.value}")
        # 2015-12-24: GOOG at 748.40, closest expiry to 10 days out is 2015-12-31, ATM strike is 747.50
        if contract.expiry != datetime(2015, 12, 31) or contract.strike != 747.5:
            raise AssertionError(f"Unexpected contract selected: {contract.symbol.value}")

        # Expiry selection with a DTE window: 2015-12-31 (7 days out) is excluded by min_dte,
        # so the closest expiry to 10 days out is 2016-01-08
        expiry = chain.closest_expiry(target_dte=10, min_dte=8, max_dte=40)
        if expiry != datetime(2016, 1, 8):
            raise AssertionError(f"closest_expiry() expected 2016-01-08 but got {expiry}")

        # Single-expiry view: composes with calls/puts, strikes and at_the_money
        at_expiry = chain.at(contract.expiry)
        if at_expiry.count == 0 or any(x.expiry != contract.expiry for x in at_expiry):
            raise AssertionError("at() returned contracts of other expiries")
        if len(at_expiry.calls) == 0 or len(at_expiry.puts) == 0:
            raise AssertionError("at().calls/.puts should not be empty")
        atm_put = at_expiry.at_the_money(OptionRight.PUT)
        if atm_put is None or atm_put.strike != 747.5 or atm_put.right != OptionRight.PUT:
            raise AssertionError(f"at_the_money(PUT) expected the 747.50 put but got {atm_put}")

        # Strikes helpers: strictly above/below and closest to the underlying price
        strikes = at_expiry.strikes
        if strikes.closest_to(spot) != 747.5 or strikes.first_above(spot) != 750 or strikes.first_below(spot) != 747.5:
            raise AssertionError(
                f"strikes helpers mismatch: {strikes.closest_to(spot)}/{strikes.first_above(spot)}/{strikes.first_below(spot)}")

        # Delta targeting: the put with |delta| closest to 0.35, using the universe pre-calculated greeks
        delta_put = chain.select(right=OptionRight.PUT, target_dte=7, target_delta=0.35)
        ceremony_delta_put = min(
            (x for x in chain if x.right == OptionRight.PUT and x.expiry == contract.expiry and x.greeks.delta != 0),
            key=lambda x: abs(abs(float(x.greeks.delta)) - 0.35))
        if delta_put is None or delta_put.symbol != ceremony_delta_put.symbol:
            raise AssertionError(f"select(target_delta) mismatch: {delta_put} != {ceremony_delta_put.symbol.value}")

        # Moneyness targeting: the put with the strike closest to 5% below the underlying price
        otm_put = chain.select(right=OptionRight.PUT, target_dte=7, moneyness=-0.05)
        ceremony_otm_put = min(
            (x for x in chain if x.right == OptionRight.PUT and x.expiry == contract.expiry),
            key=lambda x: abs(float(x.strike) - float(spot) * 0.95))
        if otm_put is None or otm_put.symbol != ceremony_otm_put.symbol:
            raise AssertionError(f"select(moneyness) mismatch: {otm_put} != {ceremony_otm_put.symbol.value}")

        # The helpers are None-safe: no match returns None instead of raising like min() would
        if (chain.select(right=OptionRight.CALL, min_dte=2000) is not None
                or chain.closest_expiry(min_dte=2000) is not None
                or chain.at(datetime(2050, 1, 1)).count != 0):
            raise AssertionError("Helpers should return None/empty when nothing matches")

        self._option_contract = self.add_option_contract(contract.symbol).symbol

    def on_data(self, slice):
        if not self.portfolio.invested:
            chain = slice.option_chains.get(self._option_contract.canonical)
            if chain:
                # Same one-liner against the slice option chain
                contract = chain.select(right=OptionRight.CALL, target_dte=7)
                if contract is not None:
                    self.market_order(contract.symbol, 1)

    def on_end_of_algorithm(self):
        if not self.portfolio.invested:
            raise AssertionError("Expected to select and buy a contract from the slice option chain")
