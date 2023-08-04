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
using System.Runtime.CompilerServices;
using QuantConnect.Util;

namespace QuantConnect
{
    /// <summary>
    /// A type capable of taking a chart and resampling using a linear interpolation strategy
    /// </summary>
    public class SeriesSampler
    {
        private readonly TimeSpan _step;

        /// <summary>
        /// Creates a new SeriesSampler to sample Series data on the specified resolution
        /// </summary>
        /// <param name="resolution">The desired sampling resolution</param>
        public SeriesSampler(TimeSpan resolution)
        {
            _step = resolution;
        }

        /// <summary>
        /// Samples the given series
        /// </summary>
        /// <param name="series">The series to be sampled</param>
        /// <param name="start">The date to start sampling, if before start of data then start of data will be used</param>
        /// <param name="stop">The date to stop sampling, if after stop of data, then stop of data will be used</param>
        /// <param name="truncateValues">True will truncate values to integers</param>
        /// <returns>The sampled series</returns>
        public BaseSeries Sample(BaseSeries series, DateTime start, DateTime stop, bool truncateValues = false)
        {
            if (series is Series seriesToSample)
            {
                return SampleSeries(seriesToSample, start, stop, truncateValues);
            }

            if (series is CandlestickSeries candlestickSeries)
            {
                return SampleCandlestickSeries(candlestickSeries, start, stop, truncateValues);
            }

            throw new ArgumentException($"SeriesSampler.Sample(): Sampling only supports {typeof(Series)} and {typeof(CandlestickSeries)}");
        }

        /// <summary>
        /// Samples the given charts
        /// </summary>
        /// <param name="charts">The charts to be sampled</param>
        /// <param name="start">The date to start sampling</param>
        /// <param name="stop">The date to stop sampling</param>
        /// <returns>The sampled charts</returns>
        public Dictionary<string, Chart> SampleCharts(IDictionary<string, Chart> charts, DateTime start, DateTime stop)
        {
            var sampledCharts = new Dictionary<string, Chart>();
            foreach (var chart in charts.Values)
            {
                var sampledChart = new Chart(chart.Name);
                sampledCharts.Add(sampledChart.Name, sampledChart);
                foreach (var series in chart.Series.Values)
                {
                    var sampledSeries = Sample(series, start, stop);
                    sampledChart.AddSeries(sampledSeries);
                }
            }
            return sampledCharts;
        }

        /// <summary>
        /// Samples the given series
        /// </summary>
        /// <param name="series">The series to be sampled</param>
        /// <param name="start">The date to start sampling, if before start of data then start of data will be used</param>
        /// <param name="stop">The date to stop sampling, if after stop of data, then stop of data will be used</param>
        /// <param name="truncateValues">True will truncate values to integers</param>
        /// <returns>The sampled series</returns>
        private Series SampleSeries(Series series, DateTime start, DateTime stop, bool truncateValues)
        {
            var sampled = series.Clone(empty: true);

            var nextSampleTime = start;

            // we can't sample a single point and it doesn't make sense to sample scatter plots
            // in this case just copy the raw data
            if (series.Values.Count < 2 || series.SeriesType == SeriesType.Scatter)
            {
                return GetIdentitySeries(series, start, stop, truncateValues);
            }

            var enumerator = series.Values.Cast<ChartPoint>().GetEnumerator();

            // initialize current/previous
            enumerator.MoveNext();
            var previous = enumerator.Current;
            enumerator.MoveNext();
            var current = enumerator.Current;

            // make sure we don't start sampling before the data begins
            if (nextSampleTime < previous.Time)
            {
                nextSampleTime = previous.Time;
            }

            // make sure to advance into the requested time frame before sampling
            while (current.Time < nextSampleTime && enumerator.MoveNext())
            {
                previous = current;
                current = enumerator.Current;
            }

            do
            {
                // advance our current/previous
                if (nextSampleTime > current.Time)
                {
                    if (enumerator.MoveNext())
                    {
                        previous = current;
                        current = enumerator.Current;
                    }
                    else
                    {
                        break;
                    }
                }

                // iterate until we pass where we want our next point
                while (nextSampleTime <= current.Time && nextSampleTime <= stop)
                {
                    var sampledPoint = TruncateValue(Interpolate(previous, current, nextSampleTime), truncateValues, clone: false);
                    sampled.Values.Add(sampledPoint);
                    nextSampleTime += _step;
                }

                // if we've passed our stop then we're finished sampling
                if (nextSampleTime > stop)
                {
                    break;
                }
            }
            while (true);

            enumerator.DisposeSafely();
            return sampled;
        }

        /// <summary>
        /// Samples the given candlestick series
        /// </summary>
        /// <param name="series">The series to be sampled</param>
        /// <param name="start">The date to start sampling, if before start of data then start of data will be used</param>
        /// <param name="stop">The date to stop sampling, if after stop of data, then stop of data will be used</param>
        /// <param name="truncateValues">True will truncate values to integers</param>
        /// <returns>The sampled series</returns>
        private CandlestickSeries SampleCandlestickSeries(CandlestickSeries series, DateTime start, DateTime stop, bool truncateValues)
        {
            var sampledSeries = series.Clone(empty: true);

            var candlesticks = series.Values;
            var seriesSize = candlesticks.Count;

            // we can't sample a single point, so just copy the raw data
            if (seriesSize < 2)
            {
                return GetIdentitySeries(series, start, stop, truncateValues);
            }

            // Make sure we don't start sampling before the data begins.
            var nextSampleTime = start < candlesticks[0].Time ? candlesticks[0].Time : start;

            // Find the first candlestick that is after the start time.
            // This variable will also be used to keep track of the first candlestick to be aggregated.
            var startIndex = candlesticks.FindIndex(x => x.Time > nextSampleTime) - 1;

            // We iterate ignoring the last candlestick because we need to check the next candlestick on each iteration.
            for (var i = startIndex; i < seriesSize - 1 && nextSampleTime <= stop; i++)
            {
                var next = (Candlestick)candlesticks[i + 1];
                if (nextSampleTime >= next.Time)
                {
                    continue;
                }

                var sampledCandlestick = AggregateCandlesticks(candlesticks, startIndex, i + 1, nextSampleTime, truncateValues);

                while (nextSampleTime < next.Time && nextSampleTime <= stop)
                {
                    var currentSampledCandlestick = sampledCandlestick.Clone();
                    currentSampledCandlestick.Time = nextSampleTime;
                    sampledSeries.Values.Add(currentSampledCandlestick);
                    nextSampleTime += _step;
                }

                startIndex = i + 1;
            }

            // Check the last candlestick to see if it should be aggregated.
            if (nextSampleTime <= stop && nextSampleTime == candlesticks[seriesSize - 1].Time)
            {
                var sampledCandlestick = AggregateCandlesticks(candlesticks, startIndex, seriesSize, nextSampleTime, truncateValues);
                sampledSeries.Values.Add(sampledCandlestick);
            }

            return sampledSeries;
        }

        /// <summary>
        /// Aggregates the candlesticks in the given range into a single candlestick,
        /// keeping the first open and last close and calculating highest high and lowest low
        /// </summary>
        private static Candlestick AggregateCandlesticks(List<ISeriesPoint> candlesticks, int start, int end, DateTime time, bool truncateValues)
        {
            var high = 0m;
            var low = decimal.MaxValue;
            for (var j = start; j < end; j++)
            {
                var point = (Candlestick)candlesticks[j];
                if (point.High > high)
                {
                    high = point.High;
                }
                if (point.Low < low)
                {
                    low = point.Low;
                }
            }

            var aggregatedCandlestick = new Candlestick
            {
                Time = time,
                Open = ((Candlestick)candlesticks[start]).Open,
                Close = ((Candlestick)candlesticks[end - 1]).Close,
                High = high,
                Low = low
            };
            aggregatedCandlestick = (Candlestick)TruncateValue(aggregatedCandlestick, truncateValues, clone: false);

            return aggregatedCandlestick;
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        private static ChartPoint Interpolate(ChartPoint previous, ChartPoint current, DateTime targetTime)
        {
            var deltaTicks = current.x - previous.x;
            // if they're at the same time return the current value
            if (deltaTicks == 0)
            {
                return (ChartPoint)current.Clone();
            }

            var targetUnitTime = Time.DateTimeToUnixTimeStamp(targetTime);
            double percentage = (targetUnitTime - previous.x) / deltaTicks;

            //  y=mx+b
            return new ChartPoint(targetTime, (current.y - previous.y) * percentage.SafeDecimalCast() + previous.y);
        }

        /// <summary>
        /// Truncates the value/values of the point after cloning it to avoid mutating the original point
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ISeriesPoint TruncateValue(ISeriesPoint point, bool truncate, bool clone = false)
        {
            if (!truncate)
            {
                return point;
            }

            var truncatedPoint = clone ? point.Clone() : point;

            if (truncatedPoint is ChartPoint chartPoint)
            {
                chartPoint.y = Math.Truncate(chartPoint.y);
            }
            else if (truncatedPoint is Candlestick candlestick)
            {
                candlestick.Open = Math.Truncate(candlestick.Open);
                candlestick.High = Math.Truncate(candlestick.High);
                candlestick.Low = Math.Truncate(candlestick.Low);
                candlestick.Close = Math.Truncate(candlestick.Close);
            }

            return truncatedPoint;
        }

        /// <summary>
        /// Gets the identity series, this is the series with no sampling applied.
        /// </summary>
        private static T GetIdentitySeries<T>(T series, DateTime start, DateTime stop, bool truncateValues)
            where T : BaseSeries
        {
            var sampled = (T)series.Clone(empty: true);
            // we can minimally verify we're within the start/stop interval
            foreach (var point in series.Values)
            {
                if (point.Time >= start && point.Time <= stop)
                {
                    sampled.Values.Add(TruncateValue(point, truncateValues, clone: true));
                }
            }
            return sampled;
        }
    }
}
