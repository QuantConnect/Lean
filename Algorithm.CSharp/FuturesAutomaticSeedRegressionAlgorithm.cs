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

using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that futures and future option contracts added via universe selection
    /// get automatically seeded by default
    /// </summary>
    public class FuturesAutomaticSeedRegressionAlgorithm : AutomaticSeedBaseRegressionAlgorithm
    {
        private bool _futureContractsAdded;
        private bool _fopsContractsAdded;

        protected override bool ShouldHaveTradeData => true;
        protected override bool ShouldHaveQuoteData => false;
        protected override bool ShouldHaveOpenInterestData => true;

        public override void Initialize()
        {
            SetStartDate(2020, 01, 07);
            SetEndDate(2020, 01, 07);
            SetCash(100000);

            Settings.SeedInitialPrices = true;

            var futures = AddFuture(Futures.Indices.SP500EMini);
            futures.SetFilter(0, 365);

            AddFutureOption(futures.Symbol, universe => universe.Strikes(-5, +5));
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            base.OnSecuritiesChanged(changes);

            if (!_futureContractsAdded || !_fopsContractsAdded)
            {
                foreach (var addedSecurity in changes.AddedSecurities)
                {
                    // Just making sure we had the data to select and seed futures and future options
                    _futureContractsAdded |= addedSecurity.Symbol.SecurityType == SecurityType.Future;
                    _fopsContractsAdded |= addedSecurity.Symbol.SecurityType == SecurityType.FutureOption;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_futureContractsAdded)
            {
                throw new RegressionTestException("No option contracts were added");
            }

            if (!_fopsContractsAdded)
            {
                throw new RegressionTestException("No future option contracts were added");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 448;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 453;
    }
}
