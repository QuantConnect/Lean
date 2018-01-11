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

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Defines a context for performing analysis on a single alpha
    /// </summary>
    public class AlphaAnalysisContext
    {
        private DateTime _previousEvaluationTimeUtc;
        private readonly Dictionary<string, object> _contextStorage;
        private readonly TimeSpan _analysisPeriod;

        /// <summary>
        /// Gets the id of this context which is the same as the alpha's id
        /// </summary>
        public Guid Id => Alpha.Id;

        /// <summary>
        /// Gets the symbol the alpha is for
        /// </summary>
        public Symbol Symbol => Alpha.Symbol;

        /// <summary>
        /// Gets the alpha being analyzed
        /// </summary>
        public Alpha Alpha { get; }

        /// <summary>
        /// Gets the alpha's current score
        /// </summary>
        public AlphaScore Score => Alpha.Score;

        /// <summary>
        /// Gets ending time of the analysis period
        /// </summary>
        public DateTime AnalysisEndTimeUtc { get; }

        /// <summary>
        /// Gets ending time of the alpha, that is, the time it was generated at + the alpha period
        /// </summary>
        public DateTime AlphaPeriodEndTimeUtc { get; }

        /// <summary>
        /// Gets the initial values. These are values of price/volatility at the time the alpha was generated
        /// </summary>
        public SecurityValues InitialValues { get; }

        /// <summary>
        /// Gets whether or not this alpha's period has closed
        /// </summary>
        public bool AlphaPeriodClosed { get; private set; }

        /// <summary>
        /// Gets the current values. These are values of price/volatility as of the current algorithm time.
        /// NOTE: Once the scoring has been finalized these values will no longer be updated and will be the
        /// values as of the last scoring which may not be the same as the prediction end time
        /// </summary>
        public SecurityValues CurrentValues { get; private set; }

        /// <summary>
        /// Percentage through the analysis period
        /// </summary>
        public double NormalizedTime => Time.NormalizeInstantWithinRange(Alpha.GeneratedTimeUtc, CurrentValues.TimeUtc, _analysisPeriod);

        /// <summary>
        /// Percentage of the current time step w.r.t analysis period
        /// </summary>
        public double NormalizedTimeStep => Time.NormalizeTimeStep(_analysisPeriod, CurrentValues.TimeUtc - _previousEvaluationTimeUtc);

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaAnalysisContext"/> class
        /// </summary>
        /// <param name="alpha">The alpha to be analyzed</param>
        /// <param name="initialValues">The initial security values from when the alpha was generated</param>
        /// <param name="analysisPeriod">The period over which to perform analysis of the alpha. This should be
        /// greater than or equal to <see cref="Alphas.Alpha.Period"/>. Specify null for default, alpha.Period</param>
        public AlphaAnalysisContext(Alpha alpha, SecurityValues initialValues, TimeSpan analysisPeriod)
        {
            Alpha = alpha;
            _contextStorage = new Dictionary<string, object>();

            CurrentValues = InitialValues = initialValues;

            _previousEvaluationTimeUtc = CurrentValues.TimeUtc;

            AlphaPeriodEndTimeUtc = alpha.GeneratedTimeUtc + alpha.Period;

            var barSize = Time.Max(analysisPeriod.ToHigherResolutionEquivalent(false).ToTimeSpan(), Time.OneMinute);
            var barCount = (int)(alpha.Period.Ticks / barSize.Ticks);
            AnalysisEndTimeUtc = Time.GetEndTimeForTradeBars(initialValues.ExchangeHours, alpha.CloseTimeUtc, analysisPeriod.ToHigherResolutionEquivalent(false).ToTimeSpan(), barCount, false);
            _analysisPeriod = AnalysisEndTimeUtc - initialValues.TimeUtc;
        }

        /// <summary>
        /// Sets the <see cref="CurrentValues"/>
        /// </summary>
        internal void SetCurrentValues(SecurityValues values)
        {
            _previousEvaluationTimeUtc = CurrentValues.TimeUtc;

            if (values.TimeUtc >= AlphaPeriodEndTimeUtc)
            {
                AlphaPeriodClosed = true;
            }

            CurrentValues = values;
        }

        /// <summary>
        /// Gets a value from the context's generic storage.
        /// This is here to allow function to access contextual state without needing to track it themselves
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The value if in storage, otherwise default(T)</returns>
        public T Get<T>(string key)
        {
            object value;
            if (_contextStorage.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return default(T);
        }

        /// <summary>
        /// Sets the key/value in the context's generic storage
        /// </summary>
        /// <param name="key">The value's key</param>
        /// <param name="value">The value to be stored</param>
        public void Set(string key, object value)
        {
            _contextStorage[key] = value;
        }

        /// <summary>
        /// Determines whether or not this context/alpha can be analyzed for the specified score type
        /// </summary>
        /// <param name="scoreType">The type of alpha score</param>
        /// <returns>True to proceed with analyzing this alpha for the specified score type, false to skip analysis of the score type</returns>
        public bool ShouldAnalyze(AlphaScoreType scoreType)
        {
            if (scoreType == AlphaScoreType.Magnitude)
            {
                return Alpha.Magnitude.HasValue;
            }

            return true;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return $"{Alpha.Id}: {Alpha.GeneratedTimeUtc}/{Alpha.CloseTimeUtc} -- {Alpha.Score}";
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Id.Equals(((AlphaAnalysisContext)obj).Id);
        }
    }
}