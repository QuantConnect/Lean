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
    /// Defines a context for performing analysis on a single insight
    /// </summary>
    public class InsightAnalysisContext
    {
        private readonly Lazy<int> _lazyHashCode;
        private DateTime _previousEvaluationTimeUtc;
        private readonly Dictionary<string, object> _contextStorage;
        private readonly TimeSpan _analysisPeriod;

        /// <summary>
        /// Gets the id of this context which is the same as the insight's id
        /// </summary>
        public Guid Id => Insight.Id;

        /// <summary>
        /// Gets the symbol the insight is for
        /// </summary>
        public Symbol Symbol => Insight.Symbol;

        /// <summary>
        /// Gets the insight being analyzed
        /// </summary>
        public Insight Insight { get; }

        /// <summary>
        /// Gets the insight's current score
        /// </summary>
        public InsightScore Score => Insight.Score;

        /// <summary>
        /// Gets ending time of the analysis period
        /// </summary>
        public DateTime AnalysisEndTimeUtc { get; private set; }

        /// <summary>
        /// Gets the initial values. These are values of price/volatility at the time the insight was generated
        /// </summary>
        public SecurityValues InitialValues { get; }

        /// <summary>
        /// Gets whether or not this insight's period has closed
        /// </summary>
        public bool InsightPeriodClosed { get; private set; }

        /// <summary>
        /// Gets the current values. These are values of price/volatility as of the current algorithm time.
        /// NOTE: Once the scoring has been finalized these values will no longer be updated and will be the
        /// values as of the last scoring which may not be the same as the prediction end time
        /// </summary>
        public SecurityValues CurrentValues { get; private set; }

        /// <summary>
        /// Percentage through the analysis period
        /// </summary>
        public double NormalizedTime => Time.NormalizeInstantWithinRange(Insight.GeneratedTimeUtc, CurrentValues.TimeUtc, _analysisPeriod);

        /// <summary>
        /// Percentage of the current time step w.r.t analysis period
        /// </summary>
        public double NormalizedTimeStep => Time.NormalizeTimeStep(_analysisPeriod, CurrentValues.TimeUtc - _previousEvaluationTimeUtc);

        /// <summary>
        /// Initializes a new instance of the <see cref="InsightAnalysisContext"/> class
        /// </summary>
        /// <param name="insight">The insight to be analyzed</param>
        /// <param name="initialValues">The initial security values from when the insight was generated</param>
        /// <param name="analysisPeriod">The period over which to perform analysis of the insight. This should be
        /// greater than or equal to <see cref="Alphas.Insight.Period"/>. Specify null for default, insight.Period</param>
        public InsightAnalysisContext(Insight insight, SecurityValues initialValues, TimeSpan analysisPeriod)
        {
            Insight = insight;
            _contextStorage = new Dictionary<string, object>();

            CurrentValues = InitialValues = initialValues;

            _previousEvaluationTimeUtc = CurrentValues.TimeUtc;

            // this will always be equal when the InsightManager is initialized with extraAnalysisPeriodRatio == 0
            // this is the way LEAN run in the cloud and locally, but support for non-zero ratios are left in for posterity
            // by short-circuiting this here, we guarantee that analysis end time and close time are identical
            if (analysisPeriod == insight.Period)
            {
                AnalysisEndTimeUtc = insight.CloseTimeUtc;
            }
            else
            {
                var barSize = Time.Max(analysisPeriod.ToHigherResolutionEquivalent(false).ToTimeSpan(), Time.OneMinute);
                var barCount = (int)(insight.Period.Ticks / barSize.Ticks);
                AnalysisEndTimeUtc = Time.GetEndTimeForTradeBars(initialValues.ExchangeHours, insight.CloseTimeUtc, analysisPeriod.ToHigherResolutionEquivalent(false).ToTimeSpan(), barCount, false);
            }

            _analysisPeriod = AnalysisEndTimeUtc - initialValues.TimeUtc;
            _lazyHashCode = new Lazy<int>(() => Id.GetHashCode());
        }

        /// <summary>
        /// Sets the <see cref="CurrentValues"/>
        /// </summary>
        internal void SetCurrentValues(SecurityValues values)
        {
            _previousEvaluationTimeUtc = CurrentValues.TimeUtc;

            if (values.TimeUtc >= Insight.CloseTimeUtc)
            {
                InsightPeriodClosed = true;
                if (Insight.Period == Time.EndOfTimeTimeSpan)
                {
                    // Special case, see OrderBasedInsightGenerator
                    AnalysisEndTimeUtc = Insight.CloseTimeUtc;
                    Insight.Period = Insight.CloseTimeUtc - Insight.GeneratedTimeUtc;
                }
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
        /// Determines whether or not this context/insight can be analyzed for the specified score type
        /// </summary>
        /// <param name="scoreType">The type of insight score</param>
        /// <returns>True to proceed with analyzing this insight for the specified score type, false to skip analysis of the score type</returns>
        public bool ShouldAnalyze(InsightScoreType scoreType)
        {
            if (Insight.Direction == InsightDirection.Flat)
            {
                return false;
            }
            else if (scoreType == InsightScoreType.Magnitude)
            {
                return Insight.Magnitude.HasValue;
            }

            return true;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return $"{Insight.Id}: {Insight.GeneratedTimeUtc}/{Insight.CloseTimeUtc} -- {Insight.Score}";
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return _lazyHashCode.Value;
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
            return Id.Equals(((InsightAnalysisContext)obj).Id);
        }
    }
}