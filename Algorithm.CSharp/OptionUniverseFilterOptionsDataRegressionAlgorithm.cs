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
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating the option universe filter feature that allows accessing the option universe data,
    /// including greeks, open interest and implied volatility, and filtering the contracts based on this data.
    /// </summary>
    public class OptionUniverseFilterOptionsDataRegressionAlgorithm : OptionUniverseFilterGreeksRegressionAlgorithm
    {
        protected override OptionFilterUniverse OptionFilter(OptionFilterUniverse universe)
        {
            // The filter used for the option security will be equivalent to the following commented one below,
            // but it is more flexible and allows for more complex filtering:

            //return universe
            //    .Delta(MinDelta, MaxDelta)
            //    .Gamma(MinGamma, MaxGamma)
            //    .Vega(MinVega, MaxVega)
            //    .Theta(MinTheta, MaxTheta)
            //    .Rho(MinRho, MaxRho)
            //    .ImpliedVolatility(MinIv, MaxIv)
            //    .OpenInterest(MinOpenInterest, MaxOpenInterest);

            return universe.Contracts(contracts =>
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
                        return greeks.Delta > MinDelta && greeks.Delta < MaxDelta &&
                            greeks.Gamma > MinGamma && greeks.Gamma < MaxGamma &&
                            greeks.Vega > MinVega && greeks.Vega < MaxVega &&
                            greeks.Theta > MinTheta && greeks.Theta < MaxTheta &&
                            greeks.Rho > MinRho && greeks.Rho < MaxRho &&
                            iv > MinIv && iv < MaxIv &&
                            openInterest > MinOpenInterest && openInterest < MaxOpenInterest;
                    })
                    .Select(contract => contract.Symbol);
            });
        }

    }
}
