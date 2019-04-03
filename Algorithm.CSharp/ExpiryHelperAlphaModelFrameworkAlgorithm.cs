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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Expiry Helper algorithm uses <see cref="Expiry"/> helper class in an Alpha Model
    /// </summary>
    public class ExpiryHelperAlphaModelFrameworkAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Hour;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2014, 1, 1);      //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // set algorithm framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ExpiryHelperAlphaModel());
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));

            InsightsGenerated += (s, e) =>
            {
                foreach (var insight in e.Insights)
                {
                    Log($"{e.DateTimeUtc.DayOfWeek}: Close Time {insight.CloseTimeUtc} {insight.CloseTimeUtc.DayOfWeek}");
                }
            };
        }

        /// <summary>
        /// <see cref="ExpiryHelperAlphaModel"/> shows how we can use the <see cref="Expiry"/> helper class
        /// to set an insight with a calendar expiry.
        /// </summary>
        private class ExpiryHelperAlphaModel : AlphaModel
        {
            private const InsightDirection _direction = InsightDirection.Up;
            private DateTime _nextUpdate = DateTime.MinValue;

            public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                if (_nextUpdate > algorithm.Time)
                {
                    yield break;
                }

                var expiry = Expiry.EndOfDay;

                // Use the Expiry helper to calculate a date/time in the future
                _nextUpdate = expiry(algorithm.Time);

                foreach (var symbol in data.Bars.Keys)
                {
                    switch (algorithm.Time.DayOfWeek)
                    {
                        // Expected CloseTime: next month on the same day and time
                        case DayOfWeek.Monday:
                            yield return Insight.Price(symbol, Expiry.OneMonth, _direction);
                            break;
                        // Expected CloseTime: next month on the 1st at market open time
                        case DayOfWeek.Tuesday:
                            yield return Insight.Price(symbol, Expiry.EndOfMonth, _direction);
                            break;
                        // Expected CloseTime: next Monday at market open time
                        case DayOfWeek.Wednesday:
                            yield return Insight.Price(symbol, Expiry.EndOfWeek, _direction);
                            break;
                        // Expected CloseTime: next day (Friday) at market open time
                        case DayOfWeek.Thursday:
                            yield return Insight.Price(symbol, Expiry.EndOfDay, _direction);
                            break;
                        default:
                            yield break;
                    }
                }
            }
        }
    }
}