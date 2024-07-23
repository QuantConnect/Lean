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

from OptionUniverseFilterGreeksRegressionAlgorithm import OptionUniverseFilterGreeksRegressionAlgorithm

### <summary>
### Regression algorithm demonstrating the option universe filter feature that allows accessing the option universe data,
### including greeks, open interest and implied volatility, and filtering the contracts based on this data.
### </summary>
class OptionUniverseFilterOptionsDataRegressionAlgorithm(OptionUniverseFilterGreeksRegressionAlgorithm):

    def set_option_filter(self, security: Option) -> None:
        # The filter used for the option security will be equivalent to the following commented one below,
        # but it is more flexible and allows for more complex filtering:

        '''
        security.set_filter(lambda u: u
                            .delta(0.5, 1.5)
                            .gamma(0.0001, 0.0006)
                            .vega(0.01, 1.5)
                            .theta(-2.0, -0.5)
                            .rho(0.5, 3.0)
                            .implied_volatility(1.0, 3.0)
                            .open_interest(100, 500))
        '''

        security.set_filter(
            lambda u: u
            # These contracts list will already be filtered by the strikes and expirations,
            # since those filters where applied before this one.
            .contracts(lambda contracts: [
                    contract for contract in contracts
                    # Can access the contract data here and do some filtering based on it is needed.
                    # More complex math can be done here for filtering, but will be simple here for demonstration sake:
                    if (contract.Greeks.Delta > 0.5 and contract.Greeks.Delta < 1.5 and
                        contract.Greeks.Gamma > 0.0001 and contract.Greeks.Gamma < 0.0006 and
                        contract.Greeks.Vega > 0.01 and contract.Greeks.Vega < 1.5 and
                        contract.Greeks.Theta > -2.0 and contract.Greeks.Theta < -0.5 and
                        contract.Greeks.Rho > 0.5 and contract.Greeks.Rho < 3.0 and
                        contract.ImpliedVolatility > 1.0 and contract.ImpliedVolatility < 3.0 and
                        contract.OpenInterest > 100 and contract.OpenInterest < 500)
                    ]))
