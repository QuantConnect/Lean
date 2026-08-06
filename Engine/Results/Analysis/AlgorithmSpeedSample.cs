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

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// A point-in-time sample of the engine's cumulative speed counters, fed by the result handler
    /// into the <see cref="AlgorithmSpeedTracker"/> on each in-run analysis run.
    /// </summary>
    public readonly struct AlgorithmSpeedSample
    {
        /// <summary>
        /// The wall-clock time elapsed since the backtest started.
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// The cumulative data points processed by the main algorithm loop.
        /// </summary>
        public long DataPoints { get; }

        /// <summary>
        /// The cumulative data points served by the history provider.
        /// </summary>
        public long HistoryDataPoints { get; }

        /// <summary>
        /// The calendar days the backtest has processed so far.
        /// </summary>
        public int ProcessedDays { get; }

        /// <summary>
        /// The total calendar days the backtest will run.
        /// </summary>
        public int TotalDays { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmSpeedSample"/> struct.
        /// </summary>
        /// <param name="elapsed">Wall-clock time elapsed since the backtest started.</param>
        /// <param name="dataPoints">Cumulative data points processed by the main algorithm loop.</param>
        /// <param name="historyDataPoints">Cumulative data points served by the history provider.</param>
        /// <param name="processedDays">Calendar days the backtest has processed so far.</param>
        /// <param name="totalDays">Total calendar days the backtest will run.</param>
        public AlgorithmSpeedSample(TimeSpan elapsed, long dataPoints, long historyDataPoints, int processedDays, int totalDays)
        {
            Elapsed = elapsed;
            DataPoints = dataPoints;
            HistoryDataPoints = historyDataPoints;
            ProcessedDays = processedDays;
            TotalDays = totalDays;
        }
    }
}
