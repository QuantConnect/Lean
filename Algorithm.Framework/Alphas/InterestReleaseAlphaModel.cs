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
using System.Globalization;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom.TradingEconomics;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that uses the Interest rate released by Fed to create insights.
    /// When Forecast Interest Rate is larger than Previous Interest Rate, we assume USD value goes up.
    /// </summary>
    public class InterestReleaseAlphaModel : AlphaModel
    {
        private TimeSpan _predictionInterval;
        private IEnumerable<Symbol> _pairs;
        private Symbol _calendar;

        /// <summary>
        /// Initializes a new instance of the InterestReleaseAlphaModel class
        /// </summary> 
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="period">The prediction interval period</param>
        /// <param name="resolution">The resolution of data</param>
        public InterestReleaseAlphaModel(QCAlgorithm algorithm, int period = 30, Resolution resolution = Resolution.Daily)
        {
            _predictionInterval = Time.Multiply(Extensions.ToTimeSpan(resolution), period);
            _calendar = algorithm.AddData<TradingEconomicsCalendar>(TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol;

            Name = $"{nameof(InterestReleaseAlphaModel)}({period}, {resolution})";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            if (!data.ContainsKey(_calendar))
            {
                return Enumerable.Empty<Insight>();
            }

            var insights = new List<Insight>();

            // Forecast Interest Rate
            var foreIR = System.Convert.ToDecimal(data[_calendar].Forecast.Replace("%", ""), CultureInfo.InvariantCulture);
            // Previous released actual Interest Rate
            var prevIR = System.Convert.ToDecimal(data[_calendar].Previous.Replace("%", ""), CultureInfo.InvariantCulture);
            // Judge whether USD value goes up
            var usdValueUp = foreIR >= prevIR;

            foreach (var pair in _pairs)
            {
                // when USD value goes up, the value of XXXUSD pairs would go down and USDXXX would go up
                var direction = pair.Value.StartsWith("USD") && usdValueUp ||
                                pair.Value.EndsWith("USD") && !usdValueUp
                    ? InsightDirection.Up
                    : InsightDirection.Down;

                insights.Add(Insight.Price(pair, _predictionInterval, direction));
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed.
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            _pairs = changes.AddedSecurities.Select(x => x.Symbol);
        }
    }
}