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
using QuantConnect.Data;
using QuantConnect.Data.Custom.Benzinga;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp.AltData
{
    /// <summary>
    /// Benzinga is a provider of news data. Their news is made in-house
    /// and covers stock related news such as corporate events.
    /// </summary>
    public class BenzingaNewsAlgorithm : QCAlgorithm
    {
        // Predefine a dictionary of words with scores to scan for in the description
        // of the Benzinga news article
        private readonly Dictionary<string, double> _words = new Dictionary<string, double>()
        {
            {"bad", -0.5}, {"good", 0.5},
            {"negative", -0.5}, {"great", 0.5},
            {"growth", 0.5}, {"fail", -0.5},
            {"failed", -0.5}, {"success", 0.5},
            {"nailed", 0.5}, {"beat", 0.5},
            {"missed", -0.5}
        };

        // Trade only every 5 days
        private DateTime _lastTrade = DateTime.MinValue;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 6, 5);
            SetEndDate(2018, 8, 4);
            SetCash(100000);

            var aapl = AddEquity("AAPL", Resolution.Hour).Symbol;
            var ibm = AddEquity("IBM", Resolution.Hour).Symbol;

            AddData<BenzingaNews>(aapl);
            AddData<BenzingaNews>(ibm);
        }

        public override void OnData(Slice data)
        {
            if ((Time - _lastTrade) < TimeSpan.FromDays(5))
            {
                return;
            }

            // Get rid of our holdings after 5 days, and start fresh
            Liquidate();

            // Get all Benzinga data and loop over it
            foreach (var article in data.Get<BenzingaNews>().Values)
            {
                // Select the same Symbol we're getting a data point for
                // from the articles list so that we can get the sentiment of the article.
                // We use the underlying Symbol because the Symbols included in the `Symbols` property
                // are equity Symbols.
                var selectedSymbol = article.Symbols.SingleOrDefault(s => s == article.Symbol.Underlying);
                if (selectedSymbol == null)
                {
                    throw new Exception($"Could not find current Symbol {article.Symbol.Underlying} even though it should exist");
                }

                // The intersection of the article contents and the pre-defined words are the words that are included in both collections
                var intersection = article.Contents.ToLowerInvariant().Split(' ').Intersect(_words.Keys);
                // Get the words, then get the aggregate sentiment
                var sentimentSum = intersection.Select(x => _words[x]).Sum();

                if (sentimentSum >= 0.5)
                {
                    Log($"Longing {article.Symbol.Underlying} with sentiment score of {sentimentSum}");
                    SetHoldings(article.Symbol.Underlying, sentimentSum / 5);

                    _lastTrade = Time;
                }
                if (sentimentSum <= -0.5)
                {
                    Log($"Shorting {article.Symbol.Underlying} with sentiment score of {sentimentSum}");
                    SetHoldings(article.Symbol.Underlying, sentimentSum / 5);

                    _lastTrade = Time;
                }
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
