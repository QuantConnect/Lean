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

using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that option contracts added via universe selection get automatically seeded by default
    /// </summary>
    public class OptionsAutomaticSeedRegressionAlgorithm : AutomaticSeedBaseRegressionAlgorithm
    {
        private bool _contractsAdded;

        protected override bool ShouldHaveTradeData => true;
        protected override bool ShouldHaveQuoteData => true;
        protected override bool ShouldHaveOpenInterestData => true;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 28);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            Settings.SeedInitialPrices = true;
            UniverseSettings.Resolution = Resolution.Minute;

            var equity = AddEquity("GOOG");

            // This security should haven been seeded right away
            if (!equity.HasData || equity.Price == 0)
            {
                throw new RegressionTestException("Equity security was not seeded");
            }

            var option = AddOption(equity.Symbol);

            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (Time.TimeOfDay.Hours > 12)
            {
                var anotherEquity = AddEquity("SPY", Resolution.Daily);

                // This security should haven been seeded right away
                if (!anotherEquity.HasData || anotherEquity.Price == 0)
                {
                    throw new RegressionTestException("Equity security was not seeded");
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            base.OnSecuritiesChanged(changes);

            if (!_contractsAdded)
            {
                foreach (var addedSecurity in changes.AddedSecurities)
                {
                    // Just making sure we had the data to select and seed options
                    _contractsAdded |= addedSecurity.Symbol.SecurityType == SecurityType.Option;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_contractsAdded)
            {
                throw new RegressionTestException("No option contracts were added");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 4044;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 218;
    }
}
