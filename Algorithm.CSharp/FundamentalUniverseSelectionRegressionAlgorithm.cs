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

using System.Linq;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to define a universe using the fundamental data
    /// </summary>
    public class FundamentalUniverseSelectionRegressionAlgorithm : FundamentalRegressionAlgorithm
    {
        private const int NumberOfSymbolsFundamental = 2;

        private SecurityChanges _changes = SecurityChanges.None;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 26);
            SetEndDate(2014, 04, 07);

            AddEquity("SPY");
            AddEquity("AAPL");

            SetUniverseSelection(new FundamentalUniverseSelectionModelTest());
        }

        private class FundamentalUniverseSelectionModelTest : FundamentalUniverseSelectionModel
        {
            public override IEnumerable<Symbol> Select(QCAlgorithm algorithm, IEnumerable<Fundamental> fundamental)
            {
                // select only symbols with fundamental data and sort descending by daily dollar volume
                var sortedByDollarVolume = fundamental
                    .Where(x => x.Price > 1)
                    .OrderByDescending(x => x.DollarVolume);

                // sort descending by P/E ratio
                var sortedByPeRatio = sortedByDollarVolume.OrderByDescending(x => x.ValuationRatios.PERatio);

                // take the top entries from our sorted collection
                var topFine = sortedByPeRatio.Take(NumberOfSymbolsFundamental);

                // we need to return only the symbol objects
                return topFine.Select(x => x.Symbol);
            }
        }

        public override void OnData(Slice slice)
        {
            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None) return;

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // we want allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.02m);
            }

            _changes = SecurityChanges.None;
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
        }

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;
    }
}
