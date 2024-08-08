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

        self._min_delta = 0.5
        self._max_delta = 1.5
        self._min_gamma = 0.0001
        self._max_gamma = 0.0006
        self._min_vega = 0.01
        self._max_vega = 1.5
        self._min_theta = -2.0
        self._max_theta = -0.5
        self._min_rho = 0.5
        self._max_rho = 3.0
        self._min_iv = 1.0
        self._max_iv = 3.0
        self._min_open_interest = 100
        self._max_open_interest = 500

        self.set_option_filter(option)

        self.option_chain_received = False

    def set_option_filter(self, security: Option) -> None:

        # Contracts can be filtered by greeks, implied volatility, open interest:
        security.set_filter(lambda u: u
                            .delta(self._min_delta, self._max_delta)
                            .gamma(self._min_gamma, self._max_gamma)
                            .vega(self._min_vega, self._max_vega)
                            .theta(self._min_theta, self._max_theta)
                            .rho(self._min_rho, self._max_rho)
                            .implied_volatility(self._min_iv, self._max_iv)
                            .open_interest(self._min_open_interest, self._max_open_interest))

        # Note: there are also shortcuts for these filter methods:
        '''
        security.set_filter(lambda u: u
                            .d(self._min_delta, self._max_delta)
                            .g(self._min_gamma, self._max_gamma)
                            .v(self._min_vega, self._max_vega)
                            .t(self._min_theta, self._max_theta)
                            .r(self._min_rho, self._max_rho)
                            .iv(self._min_iv, self._max_iv)
                            .oi(self._min_open_interest, self._max_open_interest))
        '''

    def on_data(self, slice: Slice) -> None:
        chain = slice.option_chains.get(self.option_symbol)
        if chain and len(chain.contracts) > 0:
            self.option_chain_received = True

            for contract in chain:
                if contract.Greeks.Delta < self._min_delta or contract.Greeks.Delta > self._max_delta:
                    raise RegressionTestException(f"Delta {contract.Greeks.Delta} is not within {self._min_delta} and {self._max_delta}")

                if contract.Greeks.Gamma < self._min_gamma or contract.Greeks.Gamma > self._max_gamma:
                    raise RegressionTestException(f"Gamma {contract.Greeks.Gamma} is not within {self._min_gamma} and {self._max_gamma}")

                if contract.Greeks.Vega < self._min_vega or contract.Greeks.Vega > self._max_vega:
                    raise RegressionTestException(f"Vega {contract.Greeks.Vega} is not within {self._min_vega} and {self._max_vega}")

                if contract.Greeks.Theta < self._min_theta or contract.Greeks.Theta > self._max_theta:
                    raise RegressionTestException(f"Theta {contract.Greeks.Theta} is not within {self._min_theta} and {self._max_theta}")

                if contract.Greeks.Rho < self._min_rho or contract.Greeks.Rho > self._max_rho:
                    raise RegressionTestException(f"Rho {contract.Greeks.Rho} is not within {self._min_rho} and {self._max_rho}")

                if contract.ImpliedVolatility < self._min_iv or contract.ImpliedVolatility > self._max_iv:
                    raise RegressionTestException(f"Implied volatility {contract.ImpliedVolatility} is not within {self._min_iv} and {self._max_iv}")

    def on_end_of_algorithm(self) -> None:
        if not self.option_chain_received:
            raise RegressionTestException("Option chain was not received.")
