using System;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// A type capable of taking a chart and resampling using a linear interpolation strategy
    /// </summary>
    public class SeriesSampler
    {
        private readonly double _seconds;

        /// <summary>
        /// Creates a new SeriesSampler to sample Series data on the specified resolution
        /// </summary>
        /// <param name="resolution">The desired sampling resolution</param>
        public SeriesSampler(TimeSpan resolution)
        {
            _seconds = resolution.TotalSeconds;
        }

        /// <summary>
        /// Samples the given series
        /// </summary>
        /// <param name="series">The series to be sampled</param>
        /// <param name="start">The date to start sampling, if before start of data then start of data will be used</param>
        /// <param name="stop">The date to stop sampling, if after stop of data, then stop of data will be used</param>
        /// <returns>The sampled series</returns>
        public Series Sample(Series series, DateTime start, DateTime stop)
        {
            var sampled = new Series(series.Name, series.SeriesType);

            if (series.Values.Count < 2)
            {
                // return new instance, but keep the same data
                sampled.Values.AddRange(series.Values);
                return sampled;
            }

            double nextSample = Time.DateTimeToUnixTimeStamp(start);
            double unixStopDate = Time.DateTimeToUnixTimeStamp(stop);

            var enumerator = series.Values.GetEnumerator();

            // initialize current/previous
            enumerator.MoveNext();
            ChartPoint previous = enumerator.Current;
            enumerator.MoveNext();
            ChartPoint current = enumerator.Current;

            // make sure we don't start sampling before the data begins
            if (nextSample < previous.x)
            {
                nextSample = previous.x;
            }

            // make sure to advance into the requestd time frame before sampling
            while (current.x < nextSample && enumerator.MoveNext())
            {
                previous = current;
                current = enumerator.Current;
            }

            do
            {
                // advance our current/previous
                if (nextSample > current.x)
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
                while (nextSample <= current.x && nextSample <= unixStopDate)
                {
                    var value = Interpolate(previous, current, (long) nextSample);
                    sampled.Values.Add(new ChartPoint {x = (long) nextSample, y = value});
                    nextSample += _seconds;
                }

                // if we've passed our stop then we're finished sampling
                if (nextSample > unixStopDate)
                {
                    break;
                }
            }
            while (true);

            return sampled;
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
                var sampledChart = new Chart(chart.Name, chart.ChartType);
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
        /// Linear interpolation used for sampling
        /// </summary>
        private static decimal Interpolate(ChartPoint previous, ChartPoint current, long target)
        {
            var deltaTicks = current.x - previous.x;
            double percentage = (target - previous.x) / (double)deltaTicks;

            //  y=mx+b
            return (current.y - previous.y) * (decimal)percentage + previous.y;
        }
    }
}
