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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp.AltData
{
    public class BenzingaNewsAlgorithm : QCAlgorithm
    {
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
                .Take(10)
                .ToList();

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
                // Select the same Symbol we're getting a data point for
                // from the articles list so that we can get the sentiment of the article.
                // We use the underlying Symbol because the Symbols included in the `Symbols` property
                // are equity Symbols.
                var selectedSymbol = article.Symbols.SingleOrDefault(x => x.Symbol == article.Symbol.Underlying);
                if (selectedSymbol == null)
                {
                    throw new Exception($"Could not find current Symbol {article.Symbol.Underlying} even though it should exist");
                }

                // Sometimes sentiment is not included with the article by Benzinga.
                // We have to check for null values before using it.
                var sentimentScore = selectedSymbol.Sentiment;
                if (sentimentScore == null)
                {
                    continue;
                }

                // Set holdings equal to 1/10th of the sentiment score we get
                SetHoldings(article.Symbol.Underlying, sentimentScore.Value / 10);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var r in changes.RemovedSecurities)
            {
                // If removed from the universe, liquidate and remove the custom data from the algorithm
                Liquidate(r.Symbol);
                RemoveSecurity(QuantConnect.Symbol.CreateBase(typeof(BenzingaNews), r.Symbol, Market.USA));
            }
        }
    }
}
