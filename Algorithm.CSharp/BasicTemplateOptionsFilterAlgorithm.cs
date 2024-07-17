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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm with new proposed option filter API using new options universe data (greeks, implied volatility, open interest, etc).
    /// </summary>
    public class BasicTemplateOptionsFilterAlgorithm : QCAlgorithm
    {
        private const string UnderlyingTicker = "GOOG";
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .Where(contract => true));

            // Filter by a single greek:
            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .Delta(0.64m, 0.65m));

            // Filter by multiple greeks:
            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .Delta(0.64m, 0.65m)
                .Gamma(0.0008m, 0.0010m)
                .Vega(7.5m, 10.5m)
                .Theta(-1.10m, -0.50m)
                .Rho(4m, 10m));

            // Some syntax sugar:
            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .D(0.64m, 0.65m)
                .G(0.0008m, 0.0010m)
                .V(7.5m, 10.5m)
                .T(-1.10m, -0.50m)
                .R(4m, 10m));

            // Filter by open interest and/or implied volatility
            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .OpenInterest(100, 1000)
                .ImpliedVolatility(0.10m, 0.20m));

            // Some syntax sugar:
            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .OI(100, 1000)
                .IV(0.10m, 0.20m));

            // Having delegate filters with the whole contract data.
            // We can reuse the OptionContract class for this. Might need some work on that side
            // (new constructors/factor methods, some abstraction to not rely on the option price mode, etc) but it's a good idea.

            // EXAMPLES:
            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .Contracts(contracts =>
                {
                    // Filter and select the contracts based on any criteria you define here based on the contracts and greeks data if needed
                    return contracts.Select(contract =>
                    {
                        // Can access the contract data here:
                        var greeks = contract.Greeks;
                        var iv = contract.ImpliedVolatility;
                        var openInterest = contract.OpenInterest;

                        return contract.Symbol;
                    });
                }));

            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .Select(contract =>
                {
                    // Can access the contract data here:
                    var greeks = contract.Greeks;
                    var iv = contract.ImpliedVolatility;
                    var openInterest = contract.OpenInterest;

                    // Filter and select the contracts based on any criteria you define here based on the contracts and greeks data if needed
                    return contract.Symbol;
                }));

            option.SetFilter(u => u
                .Strikes(-2, +2)
                .Expiration(0, 180)
                .Where(contract =>
                {
                    // Can access the contract data here:
                    var greeks = contract.Greeks;
                    var iv = contract.ImpliedVolatility;
                    var openInterest = contract.OpenInterest;

                    // Filter and select the contracts based on any criteria you define here based on the contracts and greeks data if needed
                    return true;
                }));
        }
    }
}
