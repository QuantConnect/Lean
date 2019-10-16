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
using QuantConnect.Data;
using QuantConnect.Data.Custom.Tiingo;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Look for positive and negative words in the news article description
    /// and trade based on the sum of the sentiment
    /// </summary>
    public class TiingoNewsAlgorithm : QCAlgorithm
    {
        private Symbol _tiingoSymbol;

        // Predefine a dictionary of words with scores to scan for in the description
        // of the Tiingo news article
        private readonly Dictionary<string, double> _words = new Dictionary<string, double>()
        {
            {"bad", -0.5}, {"good", 0.5},
            { "negative", -0.5}, {"great", 0.5},
            {"growth", 0.5}, {"fail", -0.5},
            {"failed", -0.5}, {"success", 0.5},
            {"nailed", 0.5}, {"beat", 0.5},
            {"missed", -0.5}
        };

        public override void Initialize()
        {
            SetStartDate(2019, 6, 10);
            SetEndDate(2019, 10, 3);
            SetCash(100000);

            var aapl = AddEquity("AAPL", Resolution.Hour).Symbol;
            _tiingoSymbol = AddData<TiingoNews>(aapl).Symbol;

            // Request underlying equity data
            var ibm = AddEquity("IBM", Resolution.Minute).Symbol;
            // Add news data for the underlying IBM asset
            var news = AddData<TiingoNews>(ibm).Symbol;
            // Request 60 days of history with the TiingoNews IBM Custom Data Symbol.
            var history = History<TiingoNews>(news, 60, Resolution.Daily);

            // Count the number of items we get from our history request
            Debug($"We got {history.Count()} items from our history request");
        }

        public override void OnData(Slice data)
        {
            //Confirm that the data is in the collection
            if (!data.ContainsKey(_tiingoSymbol)) return;

            // Gets the first piece of data from the Slice
            var article = data.Get<TiingoNews>(_tiingoSymbol);

            // Article descriptions come in all caps. Lower and split by word
            var descriptionWords = article.Description.ToLowerInvariant().Split(' ');

            // Take the intersection of predefined words and the words in the
            // description to get a list of matching words
            var intersection = _words.Keys.Intersect(descriptionWords);

            // Get the sum of the article's sentiment, and go long or short
            // depending if it's a positive or negative description
            var sentiment = intersection.Select(x => _words[x]).Sum();

            SetHoldings(article.Symbol.Underlying, sentiment);
        }
    }
}