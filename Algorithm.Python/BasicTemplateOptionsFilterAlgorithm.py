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
### Regression algorithm with new proposed option filter API using new options universe data (greeks, implied volatility, open interest, etc).
### </summary>
class BasicTemplateOptionsFilterAlgorithm(QCAlgorithm):
    underlying_ticker = "GOOG"

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        equity = self.add_equity(self.underlying_ticker)
        option = self.add_option(self.underlying_ticker)
        self.option_symbol = option.symbol

        # set our strike/expiry filter for this option chain
        option.set_filter(lambda u: u.strikes(-2, +2).expiration(0, 180))

        # Filter by a single greek:
        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .delta(0.64, 0.65))

        # Filter by multiple greeks:
        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .delta(0.64, 0.65)
                          .gamma(0.0008, 0.0010)
                          .vega(7.5, 10.5)
                          .theta(-1.10, -0.50)
                          .rho(4, 10))

        # Some syntax sugar:
        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .d(0.64, 0.65)
                          .g(0.0008, 0.0010)
                          .v(7.5, 10.5)
                          .t(-1.10, -0.50)
                          .r(4, 10))

        # Filter by open interest and/or implied volatility:
        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .open_interest(100, 1000)
                          .implied_volatility(0.10, 0.20))

        # Some syntax sugar:
        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .oi(100, 1000)
                          .iv(0.10, 0.20))

        # Having delegate filters with the whole contract data.
        # We can reuse the OptionContract class for this. Might need some work on that side
        # (new constructors/factor methods, some abstraction to not rely on the option price mode, etc) but it's a good idea.

        # EXAMPLES:
        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .contracts(self.contracts_filter)) # def contracts_filter(self, contracts: list[OptionContract]) -> list[Symbol]:

        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .select(self.select_contract)) # def select_contract(self, contract: OptionContract) -> Symbol:

        option.set_filter(lambda u: u
                          .strikes(-2, +2)
                          .expiration(0, 180)
                          .where(self.where_contract)) # def where_contract(self, contract: OptionContract) -> bool:

    def contracts_filter(self, contracts: list[OptionUniverse]) -> list[Symbol]:
        for contract in contracts:
            # Can access the contract data here:
            greeks = contract.greeks
            iv = contract.implied_volatility
            open_interest = contract.open_interest

            yield contract.symbol

    def select_contract(self, contract: OptionUniverse) -> Symbol:
        # Can access the contract data here:
        greeks = contract.greeks
        iv = contract.implied_volatility
        open_interest = contract.open_interest

        return contract.symbol

    def where_contract(self, contract: OptionUniverse) -> bool:
        # Can access the contract data here:
        greeks = contract.greeks
        iv = contract.implied_volatility
        open_interest = contract.open_interest

        return True
