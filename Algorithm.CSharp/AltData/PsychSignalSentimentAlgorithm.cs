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
using QuantConnect.Data.Custom.PsychSignal;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Momentum based strategy that follows bullish rated stocks
    /// </summary>
    public class PsychSignalSentimentAlgorithm : QCAlgorithm
    {
        private DateTime _timeEntered = DateTime.MinValue;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 3, 1);
            SetEndDate(2018, 10, 1);
            SetCash(100000);

            AddUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseUniverse));

            // Request underlying equity data.
            var ibm = AddEquity("IBM", Resolution.Minute).Symbol;
            // Add news data for the underlying IBM asset
            var psy = AddData<PsychSignalSentiment>(ibm).Symbol;
            // Request 120 minutes of history with the PsychSignal IBM Custom Data Symbol.
            var history = History<PsychSignalSentiment>(psy, 120, Resolution.Minute);

            // Count the number of items we get from our history request
            Debug($"We got {history.Count()} items from our history request");
        }

        /// <summary>
        /// You can use custom data with a universe of assets
        /// </summary>
        public IEnumerable<Symbol> CoarseUniverse(IEnumerable<CoarseFundamental> coarse)
        {
            if (Time.Subtract(_timeEntered) <= TimeSpan.FromDays(10))
            {
                return Universe.Unchanged;
            }

            // Ask for the universe like normal and then filter it
            var symbols = coarse.Where(x => x.HasFundamentalData && x.DollarVolume > 50000000)
                .Select(x => x.Symbol)
                .Take(20);

            // Add the custom data to the underlying security
            foreach (var symbol in symbols)
            {
                AddData<PsychSignalSentiment>(symbol);
            }

            return symbols;
        }

        public override void OnData(Slice data)
        {
            // Scan our last time traded to prevent churn
            if (Time.Subtract(_timeEntered) <= TimeSpan.FromDays(10))
            {
                return;
            }

            // Fetch the PsychSignal data for the active securities and trade on any
            foreach (var security in ActiveSecurities.Values)
            {
                var tweets = security.Data.PsychSignalSentiment;

                foreach (var sentiment in tweets)
                {
                    if (sentiment.BullIntensity > 2.0m && sentiment.BullScoredMessages > 3m)
                    {
                        SetHoldings(sentiment.Symbol.Underlying, 0.05);
                        _timeEntered = Time;
                    }
                }
            }
        }

        /// <summary>
        /// When adding custom data from a universe we should also remove the data afterwards
        /// </summary>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var r in changes.RemovedSecurities)
            {
                Liquidate(r.Symbol);

                // Remove the custom data from our algorithm and collection
                RemoveSecurity(QuantConnect.Symbol.CreateBase(typeof(PsychSignalSentiment), r.Symbol, Market.USA));
            }
        }
    }
}
