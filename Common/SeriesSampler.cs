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
using System.Linq;
using QuantConnect.Util;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QuantConnect
{
    /// <summary>
    /// A type capable of taking a chart and resampling using a linear interpolation strategy
    /// </summary>
    public class SeriesSampler
    {
        /// <summary>
        /// The desired sampling resolution
        /// </summary>
        protected TimeSpan Step { get; set; }

        /// <summary>
        /// True if sub sampling is enabled, if false only subsampling will happen
        /// </summary>
        public bool SubSample { get; set; } = true;

        /// <summary>
        /// Creates a new SeriesSampler to sample Series data on the specified resolution
        /// </summary>
        /// <param name="resolution">The desired sampling resolution</param>
        public SeriesSampler(TimeSpan resolution)
        {
            Step = resolution;
        }

        /// <summary>
        /// Samples the given series
        /// </summary>
        /// <param name="series">The series to be sampled</param>
        /// <param name="start">The date to start sampling, if before start of data then start of data will be used</param>
        /// <param name="stop">The date to stop sampling, if after stop of data, then stop of data will be used</param>
        /// <param name="truncateValues">True will truncate values to integers</param>
        /// <returns>The sampled series</returns>
        public virtual BaseSeries Sample(BaseSeries series, DateTime start, DateTime stop, bool truncateValues = false)
        {
            if (!SubSample && series.Values.Count > 1)
            {
                var dataDiff = series.Values[1].Time - series.Values[0].Time;
                if (dataDiff >= Step)
                {
                    // we don't want to subsample this case, directly return what we are given as long as is within the range
                    return GetIdentitySeries(series.Clone(empty: true), series, start, stop, truncateValues: false);
                }
            }

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
                sampledCharts[chart.Name] = SampleChart(chart, start, stop);
            }
            return sampledCharts;
        }

        /// <summary>
        /// Samples the given chart
        /// </summary>
        /// <param name="chart">The chart to be sampled</param>
        /// <param name="start">The date to start sampling</param>
        /// <param name="stop">The date to stop sampling</param>
        /// <returns>The sampled chart</returns>
        public Chart SampleChart(Chart chart, DateTime start, DateTime stop)
        {
            var sampledChart = chart.CloneEmpty();
            foreach (var series in chart.Series.Values)
            {
                var sampledSeries = Sample(series, start, stop);
                sampledChart.AddSeries(sampledSeries);
            }
            return sampledChart;
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
            var sampled = (Series)series.Clone(empty: true);

            var nextSampleTime = start;

            // we can't sample a single point and it doesn't make sense to sample scatter plots
            // in this case just copy the raw data
            if (series.Values.Count < 2 || series.SeriesType == SeriesType.Scatter || series.SeriesType == SeriesType.StackedArea)
            {
                return GetIdentitySeries(sampled, series, start, stop, truncateValues);
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
                // iterate until we pass where we want our next point
                while (nextSampleTime <= current.Time && nextSampleTime <= stop)
                {
                    ISeriesPoint sampledPoint;
                    if (series.SeriesType == SeriesType.Treemap)
                    {
                        // just carry along the values
                        sampledPoint = new ChartPoint(nextSampleTime, (nextSampleTime + Step) > current.Time ? current.Y : previous.Y);
                    }
                    else
                    {
                        sampledPoint = TruncateValue(Interpolate(previous, current, nextSampleTime, (decimal)Step.TotalSeconds), truncateValues, clone: false);
                    }

                    nextSampleTime += Step;
                    if (SubSample)
                    {
                        sampled.Values.Add(sampledPoint);
                    }
                    else
                    {
                        if (current.Time < nextSampleTime)
                        {
                            sampled.Values.Add(sampledPoint);
                        }
                    }
                }

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
            }
            // if we've passed our stop then we're finished sampling
            while (nextSampleTime <= stop);

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
            var sampledSeries = (CandlestickSeries)series.Clone(empty: true);

            var candlesticks = series.Values;
            var seriesSize = candlesticks.Count;

            // we can't sample a single point, so just copy the raw data
            if (seriesSize < 2)
            {
                return GetIdentitySeries(sampledSeries, series, start, stop, truncateValues);
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
            if (startIndex < 0)
            {
                // there's no value before the start, just return identity
                return GetIdentitySeries(sampledSeries, series, start, stop, truncateValues);
            }
            if (candlesticks[startIndex].Time == nextSampleTime && nextSampleTime <= stop)
            {
                sampledSeries.Values.Add(candlesticks[startIndex].Clone());
                nextSampleTime += Step;
                startIndex++;
            }

            // We iterate ignoring the last candlestick because we need to check the next candlestick on each iteration.
            for (var i = startIndex; i < seriesSize && nextSampleTime <= stop; i++)
            {
                var current = (Candlestick)candlesticks[i];
                Candlestick next = null;
                if (i + 1 < candlesticks.Count)
                {
                    next = (Candlestick)candlesticks[i + 1];
                }
                if (nextSampleTime > current.Time)
                {
                    // these bars will be aggregated
                    continue;
                }

                // Form the bar(s) between candlesticks at startIndex and i
                var aggregated = startIndex != i;
                var sampledCandlestick = AggregateCandlesticks(candlesticks, startIndex, i + 1, nextSampleTime, truncateValues);

                var first = (Candlestick)candlesticks[startIndex];
                var firstOpenTime = startIndex > 0
                    ? candlesticks[startIndex - 1].Time
                    : first.Time - (candlesticks[startIndex + 1].Time - candlesticks[startIndex].Time);
                Candlestick previous = null;
                var isNull = false;
                do
                {
                    var interpolated = Interpolate(sampledCandlestick, first, current, firstOpenTime, nextSampleTime, (decimal)Step.TotalSeconds);
                    nextSampleTime += Step;

                    if (SubSample)
                    {
                        if (previous != null)
                        {
                            interpolated.Open = previous.Close;
                        }
                        sampledSeries.Values.Add(interpolated);
                    }
                    else if (current.Time < nextSampleTime)
                    {
                        sampledSeries.Values.Add(interpolated);
                    }
                    previous = interpolated;

                    if (!aggregated)
                    {
                        // when subsampling, we build the high and low based on the open and close of the interpolated bar, not the bar we are sampling
                        interpolated.High = interpolated.Close;
                        interpolated.Low = interpolated.Close;
                        if (interpolated.Open.HasValue)
                        {
                            if (!interpolated.Close.HasValue || interpolated.Open > interpolated.Close.Value)
                            {
                                interpolated.High = interpolated.Open.Value;
                            }
                            if (!interpolated.Close.HasValue || interpolated.Open < interpolated.Close.Value)
                            {
                                interpolated.Low = interpolated.Open.Value;
                            }
                        }
                    }

                    if (next != null && (nextSampleTime + Step) < next.Time && interpolated.Open == null)
                    {
                        isNull = true;
                    }
                    else
                    {
                        isNull = false;
                    }
                }
                while ((nextSampleTime <= current.Time || isNull) && nextSampleTime <= stop);

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

            for (var j = start; j < end; j++)
            {
                var current = (Candlestick)candlesticks[j];
                aggregatedCandlestick.Update(current.Open);
                aggregatedCandlestick.Update(current.High);
                aggregatedCandlestick.Update(current.Low);
                aggregatedCandlestick.Update(current.Close);
            }

            return (Candlestick)TruncateValue(aggregatedCandlestick, truncateValues, clone: false);
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        protected static decimal? Interpolate(decimal x0, decimal? y0, decimal x1, decimal? y1, decimal xTarget, decimal step)
        {
            if (!y1.HasValue)
            {
                // if the next point isn't there we wont interpolate the value, it means it's the end, unless the target time is the current time or close
                if (xTarget - x0 <= step)
                {
                    return y0;
                }
                return null;
            }

            if (!y0.HasValue)
            {
                // if the previous value isn't there, return null unlesss we reach the target end time or close enough
                if (x1 - xTarget <= step)
                {
                    return y1;
                }
                return null;
            }

            //  y=mx+b
            return (y1 - y0) * (xTarget - x0) / (x1 - x0) + y0;
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        private static ChartPoint Interpolate(ChartPoint previous, ChartPoint current, DateTime targetTime, decimal step)
        {
            if (current.X == previous.X)
            {
                return (ChartPoint)current.Clone();
            }

            var targetUnixTime = Time.DateTimeToUnixTimeStamp(targetTime).SafeDecimalCast();

            return new ChartPoint(targetTime, Interpolate(previous.X, previous.Y, current.X, current.Y, targetUnixTime, step));
        }

        /// <summary>
        /// Linear interpolation used for sampling
        /// </summary>
        private static Candlestick Interpolate(Candlestick template, Candlestick first, Candlestick current,
            DateTime firstOpenTime, DateTime targetTime, decimal step)
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
            result.Close = Interpolate(firstOpenUnitTime, first.Open, current.LongTime, current.Close, targetUnixTime, step);

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
                chartPoint.y = SafeTruncate(chartPoint.y);
            }
            else if (truncatedPoint is Candlestick candlestick)
            {
                candlestick.Open = SafeTruncate(candlestick.Open);
                candlestick.High = SafeTruncate(candlestick.High);
                candlestick.Low = SafeTruncate(candlestick.Low);
                candlestick.Close = SafeTruncate(candlestick.Close);
            }

            return truncatedPoint;
        }

        /// <summary>
        /// Gets the identity series, this is the series with no sampling applied.
        /// </summary>
        protected static T GetIdentitySeries<T>(T sampled, T series, DateTime start, DateTime stop, bool truncateValues)
            where T : BaseSeries
        {
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

        private static decimal? SafeTruncate(decimal? value)
        {
            if (value.HasValue)
            {
                return Math.Truncate(value.Value);
            }
            return null;
        }
    }
}
