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
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using QuantConnect.Logging;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Class in charge of calculating the Kelly Criterion values.
    /// Will use the sample values of the last year.
    /// </summary>
    /// <remarks>See https://www.quantconnect.com/forum/discussion/6194/insight-scoring-metric/p1 </remarks>
    public class KellyCriterionManager
    {
        private bool _requiresRecalculation;
        private double _average;
        private readonly Normal _normalDistribution = new Normal();
        /// <summary>
        /// We keep both the value and the corresponding time in separate collections for performance
        /// this way we can directly calculate the Mean() and Variance() on the values collection
        /// with no need to select or create another temporary collection
        /// </summary>
        private readonly List<double> _insightValues = new List<double>();
        private readonly List<DateTime> _insightTime = new List<DateTime>();

        /// <summary>
        /// Score of the strategy's insights predictive power
        /// </summary>
        public decimal KellyCriterionEstimate { get; set; }

        /// <summary>
        /// The p-value or probability value of the <see cref="KellyCriterionEstimate"/>
        /// </summary>
        public decimal KellyCriterionProbabilityValue { get; set; }

        /// <summary>
        /// Adds a new value to the population.
        /// Will remove values older than an year compared with the provided time.
        /// For performance, will update the continuous average calculation
        /// </summary>
        /// <param name="newValue">The new value to add</param>
        /// <param name="time">The new values time</param>
        public void AddNewValue(decimal newValue, DateTime time)
        {
            _requiresRecalculation = true;

            // calculate new average, adding new value
            _average = (_insightValues.Count * _average + (double)newValue)
                    / (_insightValues.Count + 1);

            _insightValues.Add((double)newValue);
            _insightTime.Add(time);

            // clean up values older than a year
            var firstTime = _insightTime[0];
            while ((time - firstTime) >= Time.OneYear)
            {
                // calculate new average, removing a value
                _average = (_insightValues.Count * _average - _insightValues[0])
                           / (_insightValues.Count - 1);

                _insightValues.RemoveAt(0);
                _insightTime.RemoveAt(0);
                // there will always be at least 1 item, the one we just added
                firstTime = _insightTime[0];
            }
        }

        /// <summary>
        /// Updates the Kelly Criterion values
        /// </summary>
        public void UpdateScores()
        {
            try
            {
                // need at least 2 samples
                if (_requiresRecalculation && _insightValues.Count > 1)
                {
                    _requiresRecalculation = false;

                    var averagePowered = Math.Pow(_average, 2);

                    var variance = _insightValues.Variance();
                    var denominator = averagePowered + variance;
                    var kellyCriterionEstimate = denominator.IsNaNOrZero() ? 0 : _average / denominator;

                    KellyCriterionEstimate = kellyCriterionEstimate.SafeDecimalCast();

                    var variancePowered = Math.Pow(variance, 2);
                    var kellyCriterionStandardDeviation = Math.Sqrt(
                        (1 / variance + 2 * averagePowered / variancePowered)
                        / _insightValues.Count - 1);

                    KellyCriterionProbabilityValue =
                        1 - _normalDistribution.CumulativeDistribution(kellyCriterionEstimate / kellyCriterionStandardDeviation)
                            .SafeDecimalCast();
                }
            }
            catch (Exception exception)
            {
                // just in case...
                Log.Error(exception);
            }
        }
    }
}
