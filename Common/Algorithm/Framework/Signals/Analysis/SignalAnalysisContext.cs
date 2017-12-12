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

namespace QuantConnect.Algorithm.Framework.Signals.Analysis
{
    /// <summary>
    /// Defines a context for performing analysis on a single signal
    /// </summary>
    public class SignalAnalysisContext
    {
        private DateTime _previousEvaluationTimeUtc;
        private readonly DateTime _analysisEndTimeUtc;
        private readonly Dictionary<string, object> _contextStorage;

        /// <summary>
        /// Gets the symbol the signal is for
        /// </summary>
        public Symbol Symbol => Signal.Symbol;

        /// <summary>
        /// Gets the signal being analyzed
        /// </summary>
        public Signal Signal { get; }

        /// <summary>
        /// Gets the signal's current score
        /// </summary>
        public SignalScore Score => Signal.Score;

        /// <summary>
        /// Gets the period over which the signal will be analyzed
        /// </summary>
        public TimeSpan AnalysisPeriod { get; }

        /// <summary>
        /// Gets the initial values. These are values of price/volatility at the time the signal was generated
        /// </summary>
        public SecurityValues InitialValues { get; }

        /// <summary>
        /// Gets the current values. These are values of price/volatility as of the current algorithm time
        /// </summary>
        public SecurityValues CurrentValues { get; private set; }

        /// <summary>
        /// Flag indicating if the analysis period for the signal has closed. When this is true the scores will stop updating.
        /// </summary>
        public bool AnalysisPeriodIsClosed { get; private set; }

        /// <summary>
        /// Percentage through the analysis period
        /// </summary>
        public double NormalizedTime => Time.NormalizeInstantWithinRange(Signal.GeneratedTimeUtc, CurrentValues.TimeUtc, AnalysisPeriod);

        /// <summary>
        /// Percentage of the current time step w.r.t analysis period
        /// </summary>
        public double NormalizedTimeStep => Time.NormalizeTimeStep(AnalysisPeriod, CurrentValues.TimeUtc - _previousEvaluationTimeUtc);

        /// <summary>
        /// Gets a result object representing the current state/scores of the signal
        /// </summary>
        public SignalAnalysisResult CurrentResult => new SignalAnalysisResult
        {
            Signal = Signal,
            IsAnalysisClosed = AnalysisPeriodIsClosed,
            Score = new SignalScore(Score.Direction, Score.Magnitude, CurrentValues.TimeUtc)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalAnalysisContext"/> class
        /// </summary>
        /// <param name="signal">The signal to be analyzed</param>
        /// <param name="initialValues">The initial security values from when the signal was generated</param>
        /// <param name="analysisPeriod">The period over which to perform analysis of the signal. This should be
        /// greater than or equal to <see cref="Signals.Signal.Period"/>. Specify null for default, signal.Period</param>
        public SignalAnalysisContext(Signal signal, SecurityValues initialValues, TimeSpan analysisPeriod)
        {
            Signal = signal;
            AnalysisPeriod = analysisPeriod;
            _contextStorage = new Dictionary<string, object>();

            CurrentValues = InitialValues = initialValues;

            _previousEvaluationTimeUtc = CurrentValues.TimeUtc;
            _analysisEndTimeUtc = Signal.GeneratedTimeUtc + analysisPeriod;
        }

        /// <summary>
        /// Sets the <see cref="CurrentValues"/>
        /// </summary>
        public void SetCurrentValues(SecurityValues values)
        {
            _previousEvaluationTimeUtc = CurrentValues.TimeUtc;

            if (values.TimeUtc >= _analysisEndTimeUtc)
            {
                AnalysisPeriodIsClosed = true;
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
    }
}