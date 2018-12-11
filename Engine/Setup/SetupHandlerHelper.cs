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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Helper class that provides shared code
    /// for <see cref="ISetupHandler"/> implementations
    /// </summary>
    public class SetupHandlerHelper
    {
        private readonly UniverseSelection _universeSelection;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="universeSelection">Will be used to
        /// <see cref="UniverseSelection.EnsureCurrencyDataFeeds"/></param>
        public SetupHandlerHelper(UniverseSelection universeSelection)
        {
            _universeSelection = universeSelection;
        }

        /// <summary>
        /// Will first check and add all the required conversion rate securities
        /// and later will seed an initial value to them.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public void InitializeCashConversionRates(IAlgorithm algorithm)
        {
            // this is needed to have non-zero currency conversion rates during warmup
            // will also set the Cash.ConversionRateSecurity
            _universeSelection.EnsureCurrencyDataFeeds(SecurityChanges.None);

            // now set conversion rates
            var cashToUpdate = algorithm.Portfolio.CashBook.Values
                .Where(x => x.ConversionRateSecurity != null && x.ConversionRate == 0)
                .ToList();

            var slices = algorithm.History(
                cashToUpdate.Select(x => x.ConversionRateSecurity.Symbol), 1);
            slices.PushThrough(data =>
            {
                foreach (var cash in cashToUpdate
                    .Where(x => x.ConversionRateSecurity.Symbol == data.Symbol))
                {
                    cash.Update(data);
                }
            });
        }
    }
}
