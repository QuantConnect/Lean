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

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Accumulates periodic samples of the engine's speed counters (data points processed, history data points,
    /// backtest days processed) while a backtest runs, and computes the throughput and progress metrics consumed
    /// by the in-run <see cref="Analyses.AlgorithmSpeedAnalysis"/>.
    /// All rates are computed between samples, so setup time before the first sample is excluded.
    /// </summary>
    public class AlgorithmSpeedTracker
    {
        /// <summary>
        /// The number of trailing samples that make up the "recent" window used by the windowed rates.
        /// At the ~30 second in-run analysis cadence this spans roughly the last two minutes.
        /// </summary>
        public const int RecentWindowSamples = 5;

        private readonly List<AlgorithmSpeedSample> _samples = new();

        /// <summary>
        /// The number of samples recorded so far.
        /// </summary>
        public int SampleCount => _samples.Count;

        /// <summary>
        /// The total number of calendar days the backtest will run.
        /// </summary>
        public int TotalDays => _samples.Count > 0 ? _samples[^1].TotalDays : 0;

        /// <summary>
        /// The number of calendar days the backtest has processed as of the latest sample.
        /// </summary>
        public int ProcessedDays => _samples.Count > 0 ? _samples[^1].ProcessedDays : 0;

        /// <summary>
        /// The backtest progress as of the latest sample, in the [0, 1] range.
        /// </summary>
        public decimal Progress => TotalDays > 0 ? Math.Min((decimal)ProcessedDays / TotalDays, 1m) : 0m;

        /// <summary>
        /// The wall-clock time elapsed since the backtest started, as of the latest sample.
        /// </summary>
        public TimeSpan Elapsed => _samples.Count > 0 ? _samples[^1].Elapsed : TimeSpan.Zero;

        /// <summary>
        /// The wall-clock time between the first and the latest sample, that is,
        /// the period the rates are measured over.
        /// </summary>
        public TimeSpan SampledSpan => _samples.Count > 1 ? _samples[^1].Elapsed - _samples[0].Elapsed : TimeSpan.Zero;

        /// <summary>
        /// Whether the main loop data point counter is being fed. When the counter is not wired in
        /// (it reads zero), data-point-based rates are not meaningful and should not be used.
        /// </summary>
        public bool HasDataPointCounts => _samples.Count > 0 && _samples[^1].DataPoints > 0;

        /// <summary>
        /// Records a sample of the cumulative speed counters. Samples with a non-increasing
        /// elapsed time are ignored so rates are always computed over positive time deltas.
        /// </summary>
        /// <param name="sample">The sample of the cumulative speed counters.</param>
        public void AddSample(AlgorithmSpeedSample sample)
        {
            if (_samples.Count > 0 && sample.Elapsed <= _samples[^1].Elapsed)
            {
                return;
            }
            _samples.Add(sample);
        }

        /// <summary>
        /// The average data points processed per second over the whole sampled span, including
        /// history data points to match the speed the engine reports on completion.
        /// Null when there are not enough samples to measure.
        /// </summary>
        public double? DataPointsPerSecond => RateBetween(0, _samples.Count - 1, TotalDataPoints);

        /// <summary>
        /// The average data points processed per second over the first <see cref="RecentWindowSamples"/> samples,
        /// used as the early-run baseline for degradation detection. Null when there are not enough samples to measure.
        /// </summary>
        public double? InitialDataPointsPerSecond => RateBetween(0, Math.Min(RecentWindowSamples, _samples.Count) - 1, TotalDataPoints);

        /// <summary>
        /// The average data points processed per second over the recent window, including history data points.
        /// </summary>
        /// <param name="skipLast">Number of trailing samples to skip, to evaluate the window as of a previous run.</param>
        /// <returns>The windowed rate, or null when there are not enough samples to measure.</returns>
        public double? RecentDataPointsPerSecond(int skipLast = 0)
        {
            var (start, end) = RecentWindow(skipLast);
            return RateBetween(start, end, TotalDataPoints);
        }

        /// <summary>
        /// The average backtest calendar days processed per wall-clock second over the recent window.
        /// </summary>
        /// <param name="skipLast">Number of trailing samples to skip, to evaluate the window as of a previous run.</param>
        /// <returns>The windowed rate, or null when there are not enough samples to measure.</returns>
        public double? RecentDaysPerSecond(int skipLast = 0)
        {
            var (start, end) = RecentWindow(skipLast);
            return RateBetween(start, end, sample => sample.ProcessedDays);
        }

        /// <summary>
        /// The number of history data points served over the recent window.
        /// </summary>
        /// <param name="skipLast">Number of trailing samples to skip, to evaluate the window as of a previous run.</param>
        public long RecentHistoryDataPoints(int skipLast = 0)
        {
            var (start, end) = RecentWindow(skipLast);
            return end > start ? _samples[end].HistoryDataPoints - _samples[start].HistoryDataPoints : 0;
        }

        /// <summary>
        /// The share of the data points processed over the recent window that were served by the history
        /// provider, in the [0, 1] range.
        /// </summary>
        /// <param name="skipLast">Number of trailing samples to skip, to evaluate the window as of a previous run.</param>
        /// <returns>The share, or null when there are not enough samples or no data points were processed in the window.</returns>
        public double? RecentHistoryDataPointsShare(int skipLast = 0)
        {
            var (start, end) = RecentWindow(skipLast);
            if (end <= start)
            {
                return null;
            }
            var totalDelta = TotalDataPoints(_samples[end]) - TotalDataPoints(_samples[start]);
            if (totalDelta <= 0)
            {
                return null;
            }
            return (_samples[end].HistoryDataPoints - _samples[start].HistoryDataPoints) / totalDelta;
        }

        /// <summary>
        /// The estimated wall-clock time left for the backtest to complete, projecting the recent
        /// calendar-days-per-second pace over the remaining backtest days.
        /// </summary>
        /// <param name="skipLast">Number of trailing samples to skip, to evaluate the projection as of a previous run.</param>
        /// <returns>The estimate, zero when the backtest already reached its end date, or null when the recent pace
        /// is zero or there are not enough samples to measure.</returns>
        public TimeSpan? EstimatedRemainingTime(int skipLast = 0)
        {
            var end = _samples.Count - 1 - skipLast;
            if (end < 0 || TotalDays <= 0)
            {
                return null;
            }
            var remainingDays = TotalDays - _samples[end].ProcessedDays;
            if (remainingDays <= 0)
            {
                return TimeSpan.Zero;
            }
            var daysPerSecond = RecentDaysPerSecond(skipLast);
            if (daysPerSecond is null or <= 0)
            {
                return null;
            }
            return TimeSpan.FromSeconds(remainingDays / daysPerSecond.Value);
        }

        private (int Start, int End) RecentWindow(int skipLast)
        {
            var end = _samples.Count - 1 - skipLast;
            var start = Math.Max(0, end - RecentWindowSamples + 1);
            return (start, end);
        }

        private double? RateBetween(int startIndex, int endIndex, Func<AlgorithmSpeedSample, double> selector)
        {
            if (startIndex < 0 || endIndex <= startIndex || endIndex >= _samples.Count)
            {
                return null;
            }
            var seconds = (_samples[endIndex].Elapsed - _samples[startIndex].Elapsed).TotalSeconds;
            if (seconds <= 0)
            {
                return null;
            }
            return (selector(_samples[endIndex]) - selector(_samples[startIndex])) / seconds;
        }

        private static double TotalDataPoints(AlgorithmSpeedSample sample) => sample.DataPoints + sample.HistoryDataPoints;
    }
}
