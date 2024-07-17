/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating the option universe filter feature that allows accessing the option universe data,
    /// including greeks, open interest and implied volatility, and filtering the contracts based on this data.
    /// </summary>
    public class OptionUniverseFilterOptionsDataRegressionAlgorithm : OptionUniverseFilterGreeksRegressionAlgorithm
    {
        protected override void SetOptionFilter(Option security)
        {
            // The filter used for the option security will be equivalent to the following commented one below,
            // but it is more flexible and allows for more complex filtering:

            //security.SetFilter(u => u
            //   .Strikes(-3, +3)
            //   .Expiration(0, 180)
            //   .Delta(0.64m, 0.65m)
            //   .Gamma(0.0008m, 0.0010m)
            //   .Vega(7.5m, 10.5m)
            //   .Theta(-1.10m, -0.50m)
            //   .Rho(4m, 10m)
            //   .ImpliedVolatility(0.10m, 0.20m)
            //   .OpenInterest(100, 1000));

            security.SetFilter(u => u
                .Strikes(-3, +3)
                .Expiration(0, 180)
                .Contracts(contracts =>
                {
                    // These contracts list will already be filtered by the strikes and expirations,
                    // since those filters where applied before this one.

                    return contracts
                        .Where(contract =>
                        {
                            // Can access the contract data here and do some filtering based on it is needed:
                            var greeks = contract.Greeks;
                            var iv = contract.ImpliedVolatility;
                            var openInterest = contract.OpenInterest;

                            // More complex math can be done here for filtering, but will be simple here for demonstration sake:
                            return greeks.Delta > 0.64m && greeks.Delta < 0.65m
                                && greeks.Gamma > 0.0008m && greeks.Gamma < 0.0010m
                                && greeks.Vega > 7.5m && greeks.Vega < 10.5m
                                && greeks.Theta > -1.10m && greeks.Theta < -0.50m
                                && greeks.Rho > 4m && greeks.Rho < 10m
                                && iv > 0.10m && iv < 0.20m
                                && openInterest > 100 && openInterest < 1000;
                        })
                        .Select(contract => contract.Symbol);
                }));
        }

    }
}
