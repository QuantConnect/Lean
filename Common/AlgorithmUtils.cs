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
*/

using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect
{
    /// <summary>
    /// Provides utility methods for or related to algorithms
    /// </summary>
    public static class AlgorithmUtils
    {
        /// <summary>
        /// Seeds the provided securities with their last known prices from the algorithm
        /// </summary>
        /// <param name="securities">The securities to seed</param>
        /// <param name="algorithm">The algorithm instance</param>
        public static void SeedSecurities(IReadOnlyCollection<Security> securities, IAlgorithm algorithm)
        {
            var securitiesToSeed = securities.Where(x => x.Price == 0);
            var data = algorithm.GetLastKnownPrices(securitiesToSeed.Select(x => x.Symbol));

            foreach (var security in securitiesToSeed)
            {
                if (data.TryGetValue(security.Symbol, out var seedData))
                {
                    foreach (var datum in seedData)
                    {
                        security.SetMarketPrice(datum);
                    }
                }
            }
        }

        /// <summary>
        /// Seeds an initial conversion rate for the cashbook currencies that don't have one yet, so they are
        /// non-zero right away instead of waiting for the first conversion pair bar to arrive
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="currenciesToUpdateWhiteList">
        /// If passed, only the currencies in the CashBook contained in this list will be updated.
        /// By default, if not passed (null), all currencies in the cashbook without a properly set up currency conversion will be updated.
        /// </param>
        public static void SeedCurrencyConversionRates(IAlgorithm algorithm, IReadOnlyCollection<string> currenciesToUpdateWhiteList = null)
        {
            Func<Cash, bool> cashToUpdateFilter = currenciesToUpdateWhiteList == null
                ? (x) => x.CurrencyConversion != null && x.ConversionRate == 0
                : (x) => currenciesToUpdateWhiteList.Contains(x.Symbol);
            var cashToUpdate = algorithm.Portfolio.CashBook.Values.Where(cashToUpdateFilter).ToList();

            if (cashToUpdate.Count == 0)
            {
                return;
            }

            var securitiesToUpdate = cashToUpdate
                .SelectMany(x => x.CurrencyConversion.ConversionRateSecurities)
                .Distinct()
                .ToList();

            SeedSecurities(securitiesToUpdate, algorithm);

            foreach (var cash in cashToUpdate)
            {
                cash.Update();
            }
        }
    }
}
