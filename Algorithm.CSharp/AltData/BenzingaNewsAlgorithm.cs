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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class BenzingaNewsAlgorithm : QCAlgorithm
    {
        // Predefine a dictionary of words with scores to scan for in the description
        // of the news article
        private readonly Dictionary<string, double> _words = new Dictionary<string, double>()
        {
            {"bad", -0.5}, {"good", 0.5},
            {"negative", -0.5}, {"great", 0.5},
            {"growth", 0.5}, {"fail", -0.5},
            {"failed", -0.5}, {"success", 0.5},
            {"nailed", 0.5}, {"beat", 0.5},
            {"missed", -0.5}, {"slipped", -0.5},
            {"outperforming", 0.5}, {"underperforming", -0.5},
            {"outperform", 0.5}, {"underperform", -0.5}
        };

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 10, 12);
            SetEndDate(2018, 11, 25);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));
        }

        public IEnumerable<Symbol> CoarseSelector(IEnumerable<CoarseFundamental> coarse)
        {
            // Add Benzinga news data from the filtered coarse selection
            var symbols = coarse.Where(x => x.HasFundamentalData && x.DollarVolume > 50000000)
                .Select(x => x.Symbol)
                .Take(10);

            foreach (var symbol in symbols)
            {
                AddData<BenzingaNews>(symbol);
            }

            return symbols;
        }

        public override void OnData(Slice data)
        {
            // Get all Benzinga data and loop over it
            foreach (var article in data.Get<BenzingaNews>().Values)
            {
                // Get the list of matching words we have sentiment definitions for
                var intersection = article.Contents.ToLowerInvariant()
                    .Split(' ')
                    .Intersect(_words.Keys);

                // Get the sentiment score
                var sentimentScore = intersection.Select(x => _words[x]).Sum();

                // Set holdings equal to 1/10th of the sentiment score we get
                SetHoldings(article.Symbol.Underlying, sentimentScore / 10);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var r in changes.RemovedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Equity))
            {
                // If removed from the universe, liquidate and remove the custom data from the algorithm
                Liquidate(r.Symbol);
                RemoveSecurity(QuantConnect.Symbol.CreateBase(typeof(BenzingaNews), r.Symbol, Market.USA));
            }
        }
    }
}
