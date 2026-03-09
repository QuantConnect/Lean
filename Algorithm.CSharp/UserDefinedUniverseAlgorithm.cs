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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm shows how you can handle universe selection in anyway you like,
    /// at any time you like. This algorithm has a list of 10 stocks that it rotates
    /// through every hour.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="custom universes" />
    public class UserDefinedUniverseAlgorithm : QCAlgorithm
    {
        private static readonly IReadOnlyList<string> Symbols = new List<string>
        {
            "SPY", "GOOG", "IBM", "AAPL", "MSFT", "CSCO", "ADBE", "WMT",
        };

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Hour;

            SetStartDate(2015, 01, 01);
            SetEndDate(2015, 12, 01);

            AddUniverse("my-universe-name", Resolution.Hour, time =>
            {
                var hour = time.Hour;
                var index = hour%Symbols.Count;
                return new List<string> {Symbols[index]};
            });
        }

        public override void OnData(Slice slice)
        {
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var removed in changes.RemovedSecurities)
            {
                if (removed.Invested)
                {
                    Liquidate(removed.Symbol);
                }
            }

            foreach (var added in changes.AddedSecurities)
            {
                SetHoldings(added.Symbol, 1/(decimal)changes.AddedSecurities.Count);
            }
        }
    }
}
