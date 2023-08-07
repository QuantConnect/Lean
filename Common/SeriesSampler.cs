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
using static System.Net.Mime.MediaTypeNames;

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
            var nextSampleTime = start;
            if (start < candlesticks[0].Time)
            {
                nextSampleTime = candlesticks[0].Time;
            }

            // Find the first candlestick that is after the start time.
            // This variable will also be used to keep track of the first candlestick to be aggregated.
            var startIndex = candlesticks.FindIndex(x => x.Time > nextSampleTime) - 1;

            if (candlesticks[startIndex].Time == nextSampleTime)
            {
                sampledSeries.Values.Add(candlesticks[startIndex].Clone());
                nextSampleTime += _step;
                startIndex++;
            }

            // We iterate ignoring the last candlestick because we need to check the next candlestick on each iteration.
            for (var i = startIndex; i < seriesSize && nextSampleTime <= stop; i++)
            {
                var current = (Candlestick)candlesticks[i];
                if (nextSampleTime > current.Time)
                {
                    continue;
                }

                // Form the bar(s) between candlesticks at startIndex and i

                var sampledCandlestick = AggregateCandlesticks(candlesticks, startIndex, i + 1, nextSampleTime, truncateValues);

                var first = (Candlestick)candlesticks[startIndex];
                var firstOpenTime = startIndex > 0
                    ? candlesticks[startIndex - 1].Time
                    : first.Time - (candlesticks[startIndex + 1].Time - candlesticks[startIndex].Time);
                Candlestick previous = null;
                while (nextSampleTime <= current.Time && nextSampleTime <= stop)
                {
                    var interpolated = Interpolate(sampledCandlestick, first, current, firstOpenTime, nextSampleTime);

                    if (previous != null)
                    {
                        interpolated.Open = previous.Close;
                    }

                    sampledSeries.Values.Add(interpolated);
                    previous = interpolated;
                    nextSampleTime += _step;
                }

                // Update the start index
                startIndex = i + 1;
            }

            return sampledSeries;
        }

        /// <summary>
        /// Aggregates the candlesticks in the given range into a single candlestick,
        /// keeping the first open and last close and calculating highest high and lowest low
        /// </summary>
        private static Candlestick AggregateCandlesticks(List<ISeriesPoint> candlesticks, int start, int end, DateTime time, bool truncateValues)
        {
            var aggregatedCandlestick = new Candlestick
            {
                Time = time
            };

            // Set the open
            aggregatedCandlestick.Update(((Candlestick)candlesticks[start]).Open);

            // Set high and low
            for (var j = start; j < end; j++)
            {
                var current = (Candlestick)candlesticks[j];
                aggregatedCandlestick.Update(current.High);
                aggregatedCandlestick.Update(current.Low);
            }

            // Set the close
            aggregatedCandlestick.Update(((Candlestick)candlesticks[end - 1]).Close);

            return (Candlestick)TruncateValue(aggregatedCandlestick, truncateValues, clone: false);
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        private static decimal Interpolate(decimal x0, decimal y0, decimal x1, decimal y1, decimal x)
        {
            //  y=mx+b
            return (y1 - y0) * (x - x0) / (x1 - x0) + y0;
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        private static ChartPoint Interpolate(ChartPoint previous, ChartPoint current, DateTime targetTime)
        {
            if (current.X == previous.X)
            {
                return (ChartPoint)current.Clone();
            }

            var targetUnixTime = Time.DateTimeToUnixTimeStamp(targetTime).SafeDecimalCast();

            return new ChartPoint(targetTime, Interpolate(previous.X, previous.Y, current.X, current.Y, targetUnixTime));
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        private static Candlestick Interpolate(Candlestick template, Candlestick first, Candlestick current,
            DateTime firstOpenTime, DateTime targetTime)
        {
            Candlestick result;
            if (firstOpenTime == current.Time)
            {
                result = (Candlestick)current.Clone();
                result.Time = targetTime;
                return result;
            }

            result = (Candlestick)template.Clone();
            result.Time = targetTime;

            var targetUnixTime = Time.DateTimeToUnixTimeStamp(targetTime).SafeDecimalCast();
            var firstOpenUnitTime = Time.DateTimeToUnixTimeStamp(firstOpenTime).SafeDecimalCast();
            result.Close = Interpolate(firstOpenUnitTime, first.Open, current.LongTime, current.Close, targetUnixTime);

            return result;
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
