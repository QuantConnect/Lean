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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm shows some of the various helper methods available when defining universes
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    public class UniverseSelectionDefinitionsAlgorithm : QCAlgorithm
    {
        private SecurityChanges _changes = SecurityChanges.None;

        public override void Initialize()
        {
            // subscriptions added via universe selection will have this resolution
            UniverseSettings.Resolution = Resolution.Hour;
            // force securities to remain in the universe for a minimm of 30 minutes
            UniverseSettings.MinimumTimeInUniverse = TimeSpan.FromMinutes(30);

            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100*1000);

            // add universe for the top 50 stocks by dollar volume
            AddUniverse(Universe.DollarVolume.Top(50));

            // add universe for the bottom 50 stocks by dollar volume
            AddUniverse(Universe.DollarVolume.Bottom(50));

            // add universe for the 90th dollar volume percentile
            AddUniverse(Universe.DollarVolume.Percentile(90));

            // add universe for stocks between the 70th and 80th dollar volume percentile
            AddUniverse(Universe.DollarVolume.Percentile(70, 80));
        }

        public void OnData(TradeBars data)
        {
            if (_changes == SecurityChanges.None) return;

            // liquidate securities that fell out of our universe
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // invest in securities just added to our universe
            foreach (var security in _changes.AddedSecurities)
            {
                if (!security.Invested)
                {
                    MarketOrder(security.Symbol, 10);
                }
            }

            _changes = SecurityChanges.None;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
        }
    }
}
