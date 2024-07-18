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
### Regression algorithm demonstrating the option universe filter by greeks and other options data feature
### </summary>
class OptionUniverseFilterGreeksRegressionAlgorithm(QCAlgorithm):

    def initialize(self):
        self.set_start_date(2015, 12, 24)
        self.set_end_date(2015, 12, 24)
        self.set_cash(100000)

        underlying_ticker = "GOOG"
        self.add_equity(underlying_ticker)
        option = self.add_option(underlying_ticker)
        self.option_symbol = option.symbol

        self.set_option_filter(option)

        self.option_chain_received = False

    def set_option_filter(self, security: Option) -> None:
        # Contracts can be filtered by greeks, implied volatility, open interest:
        security.set_filter(lambda u: u
                            .delta(0.5, 1.5)
                            .gamma(0.0001, 0.0006)
                            .vega(0.01, 1.5)
                            .theta(-2.0, -0.5)
                            .rho(0.5, 3.0)
                            .implied_volatility(1.0, 3.0)
                            .open_interest(100, 500))

        # Note: there are also shortcuts for these filter methods:
        '''
        security.set_filter(lambda u: u
                            .d(0.5, 1.5)
                            .g(0.0001, 0.0006)
                            .v(0.01, 1.5)
                            .t(-2.0, -0.5)
                            .r(0.5, 3.0)
                            .iv(1.0, 3.0)
                            .oi(100, 500))
        '''

    def on_data(self, slice: Slice) -> None:
        chain = slice.option_chains.get(self.option_symbol)
        if chain and len(chain.contracts) > 0:
            self.option_chain_received = True

    def on_end_of_algorithm(self) -> None:
        if not self.option_chain_received:
            raise RegressionTestException("Option chain was not received.")
