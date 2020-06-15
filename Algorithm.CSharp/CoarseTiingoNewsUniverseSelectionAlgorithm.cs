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

using System;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm of a custom universe selection using coarse data and adding TiingoNews
    /// If conditions are met will add the underlying and trade it
    /// </summary>
    public class CoarseTiingoNewsUniverseSelectionAlgorithm : QCAlgorithm
    {
        private const int NumberOfSymbols = 3;
        private List<Symbol> _symbols;

        public override void Initialize()
        {
            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 07);

            UniverseSettings.FillForward = false;

            AddUniverse(new CustomDataCoarseFundamentalUniverse(UniverseSettings, SecurityInitializer, CoarseSelectionFunction));

            _symbols = new List<Symbol>();
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            // sort descending by daily dollar volume
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);

            // take the top entries from our sorted collection
            var top = sortedByDollarVolume.Take(NumberOfSymbols);

            // we need to return only the symbol objects
            return top.Select(x => QuantConnect.Symbol.CreateBase(typeof(TiingoNews), x.Symbol, x.Symbol.ID.Market));
        }

        public override void OnData(Slice data)
        {
            var articles = data.Get<TiingoNews>();

            foreach (var kvp in articles)
            {
                var news = kvp.Value;
                if (news.Title.IndexOf("Stocks Drop", 0, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    if (!Securities.ContainsKey(kvp.Key.Underlying))
                    {
                        // add underlying we want to trade
                        AddSecurity(kvp.Key.Underlying);
                        _symbols.Add(kvp.Key.Underlying);
                    }
                }
            }

            foreach (var symbol in _symbols)
            {
                if (Securities[symbol].HasData)
                {
                    SetHoldings(symbol, 1m / _symbols.Count);
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            changes.FilterCustomSecurities = false;
            Log($"{Time} {changes}");
        }

        private class CustomDataCoarseFundamentalUniverse : CoarseFundamentalUniverse
        {
            public CustomDataCoarseFundamentalUniverse(UniverseSettings universeSettings, ISecurityInitializer securityInitializer, Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> selector)
                : base(universeSettings, securityInitializer, selector)
            { }

            public override IEnumerable<SubscriptionRequest> GetSubscriptionRequests(Security security, DateTime currentTimeUtc, DateTime maximumEndTimeUtc,
                ISubscriptionDataConfigService subscriptionService)
            {
                var config = subscriptionService.Add(
                    typeof(TiingoNews),
                    security.Symbol,
                    UniverseSettings.Resolution,
                    UniverseSettings.FillForward,
                    UniverseSettings.ExtendedMarketHours,
                    dataNormalizationMode: UniverseSettings.DataNormalizationMode);
                return new[]{new SubscriptionRequest(isUniverseSubscription: false,
                    universe: this,
                    security: security,
                    configuration: config,
                    startTimeUtc: currentTimeUtc,
                    endTimeUtc: maximumEndTimeUtc)};
            }
        }
    }
}
