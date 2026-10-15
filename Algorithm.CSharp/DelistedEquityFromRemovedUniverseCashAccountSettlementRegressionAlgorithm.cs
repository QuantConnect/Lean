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

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the proceeds of the automatic delisting liquidation of an equity held on a cash account settle
    /// when its only universe was removed before the delisting, keeping it as a pending removal because of its holdings
    /// </summary>
    public class DelistedEquityFromRemovedUniverseCashAccountSettlementRegressionAlgorithm : DelistedEquityCashAccountSettlementRegressionAlgorithm
    {
        /// <summary>
        /// Adds the equity that gets delisted, only selected by a framework universe which is removed before the delisting
        /// </summary>
        protected override Symbol AddDelistedEquity()
        {
            var symbol = QuantConnect.Symbol.Create("AAA.1", SecurityType.Equity, Market.USA);
            UniverseSettings.Resolution = Resolution.Daily;
            SetUniverseSelection(new RemovedBeforeDelistingUniverseSelectionModel(symbol, new DateTime(2007, 05, 17)));
            return symbol;
        }

        public override void OnData(Slice slice)
        {
            foreach (var delisting in slice.Delistings.Values.Where(x => x.Type == DelistingType.Delisted))
            {
                if (UniverseManager.ActiveSecurities.ContainsKey(delisting.Symbol)
                    || UniverseManager.Values.Any(universe => universe.ContainsMember(delisting.Symbol)))
                {
                    throw new RegressionTestException("Expected the delisted equity universe to be removed before the delisting");
                }
            }

            base.OnData(slice);
        }

        /// <summary>
        /// Selects the given symbol until the removal date, after which its universe is disposed and removed
        /// </summary>
        private class RemovedBeforeDelistingUniverseSelectionModel : UniverseSelectionModel
        {
            private readonly ManualUniverseSelectionModel _universeSelectionModel;
            private readonly DateTime _removalDate;
            private DateTime _nextRefreshTimeUtc;

            public RemovedBeforeDelistingUniverseSelectionModel(Symbol symbol, DateTime removalDate)
            {
                _universeSelectionModel = new ManualUniverseSelectionModel(symbol);
                _removalDate = removalDate;
            }

            public override DateTime GetNextRefreshTimeUtc()
            {
                return _nextRefreshTimeUtc;
            }

            public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
            {
                _nextRefreshTimeUtc = algorithm.UtcTime.Date.AddDays(1);
                if (algorithm.UtcTime.Date >= _removalDate)
                {
                    return Enumerable.Empty<Universe>();
                }
                return _universeSelectionModel.CreateUniverses(algorithm);
            }
        }
    }
}
