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
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SeriesSamplerTests
    {
        [Test]
        public void ReturnsIdentityOnSinglePoint()
        {
            var series = new Series {Name = "name"};
            var reference = DateTime.Now.ToUniversalTime();
            series.AddPoint(reference, 1m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference.AddSeconds(-1), reference.AddSeconds(1));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(1, sampled.Values.Count);
            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[0].y, sampledValues[0].y);
        }

        [Test]
        public void DownSamples()
        {
            var series = new Series {Name = "name"};
            var reference = DateTime.UtcNow.Date;
            series.AddPoint(reference, 1m);
            series.AddPoint(reference.AddDays(1), 2m);
            series.AddPoint(reference.AddDays(2), 3m);
            series.AddPoint(reference.AddDays(3), 4m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5));

            var sampled = sampler.Sample(series, reference, reference.AddDays(3));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[0].y, sampledValues[0].y);

            Assert.AreEqual((seriesValues[1].x + seriesValues[2].x)/2, sampledValues[1].x);
            Assert.AreEqual((seriesValues[1].y + seriesValues[2].y)/2, sampledValues[1].y);

            Assert.AreEqual(seriesValues[3].x, sampledValues[2].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[2].y);
        }

        [Test]
        public void SubSamples()
        {
            var series = new Series {Name = "name"};
            var reference = DateTime.UtcNow.Date;
            series.AddPoint(reference, 1m);
            series.AddPoint(reference.AddDays(1), 2m);
            series.AddPoint(reference.AddDays(2), 3m);
            series.AddPoint(reference.AddDays(3), 4m);
            series.AddPoint(reference.AddDays(4), 5m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference.AddDays(1), reference.AddDays(2));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(2, sampled.Values.Count);

            Assert.AreEqual(seriesValues[1].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[0].y);

            Assert.AreEqual(seriesValues[2].x, sampledValues[1].x);
            Assert.AreEqual(seriesValues[2].y, sampledValues[1].y);
        }

        [Test]
        public void DoesNotSampleBeforeStart()
        {
            var series = new Series { Name = "name" };
            var reference = DateTime.UtcNow.Date;
            series.AddPoint(reference, 1m);
            series.AddPoint(reference.AddDays(1), 2m);
            series.AddPoint(reference.AddDays(2), 3m);
            series.AddPoint(reference.AddDays(3), 4m);
            series.AddPoint(reference.AddDays(4), 5m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference.AddDays(-1), reference.AddDays(2));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[0].y, sampledValues[0].y);

            Assert.AreEqual(seriesValues[1].x, sampledValues[1].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[1].y);

            Assert.AreEqual(seriesValues[2].x, sampledValues[2].x);
            Assert.AreEqual(seriesValues[2].y, sampledValues[2].y);
        }

        [Test]
        public void HandlesDuplicateTimes()
        {
            var series = new Series();
            series.Values.Add(new ChartPoint(DateTime.Today, 1m));
            series.Values.Add(new ChartPoint(DateTime.Today, 2m));
            series.Values.Add(new ChartPoint(DateTime.Today.AddDays(1), 3m));

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var sampled = sampler.Sample(series, DateTime.Today, DateTime.Today.AddDays(1));

            // sampler will only produce one value at the time
            // it was also respect the latest value

            Assert.AreEqual(2, sampled.Values.Count);
            foreach (var pair in series.Values.Skip(1).Cast<ChartPoint>().Zip(sampled.Values.Cast<ChartPoint>(), Tuple.Create))
            {
                Assert.AreEqual(pair.Item1.x, pair.Item2.x);
                Assert.AreEqual(pair.Item1.y, pair.Item2.y);
            }
        }

        [Test]
        public void DoesNotSampleScatterPlots()
        {
            var scatter = new Series("scatter", SeriesType.Scatter, 0, "$");
            scatter.AddPoint(DateTime.Today, 1m);
            scatter.AddPoint(DateTime.Today, 3m);
            scatter.AddPoint(DateTime.Today.AddSeconds(1), 1.5m);
            scatter.AddPoint(DateTime.Today.AddSeconds(0.5), 1.5m);

            var sampler = new SeriesSampler(TimeSpan.FromMilliseconds(1));
            var sampled = sampler.Sample(scatter, DateTime.Today, DateTime.Today.AddDays(1));
            foreach (var pair in scatter.Values.Cast<ChartPoint>().Zip(sampled.Values.Cast<ChartPoint>(), Tuple.Create))
            {
                Assert.AreEqual(pair.Item1.x, pair.Item2.x);
                Assert.AreEqual(pair.Item1.y, pair.Item2.y);
            }
        }

        [Test]
        public void EmitsEmptySeriesWithSinglePointOutsideOfStartStop()
        {
            var series = new Series { Name = "name" };
            var reference = DateTime.Now;
            series.AddPoint(reference.AddSeconds(-1), 1m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference, reference);
            Assert.AreEqual(0, sampled.Values.Count);
        }

        [Test]
        public void ReturnsIdentityOnSinglePointCandlestickSeries()
        {
            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference.AddSeconds(-1), reference.AddSeconds(1));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(1, sampledValues.Count);
            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            Assert.AreEqual(seriesValues[0].Open, sampledValues[0].Open);
            Assert.AreEqual(seriesValues[0].High, sampledValues[0].High);
            Assert.AreEqual(seriesValues[0].Low, sampledValues[0].Low);
            Assert.AreEqual(seriesValues[0].Close, sampledValues[0].Close);
        }

        [Test]
        public void DoesNotSampleCandlestickSeriesBeforeStart()
        {
            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference.AddDays(-1), reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            for (var i = 0; i < sampledValues.Count; i++)
            {
                Assert.AreEqual(seriesValues[i].Time, sampledValues[i].Time);
                AssertCandlesticksValuesAreEqual(seriesValues[i], sampledValues[i]);
            }
        }

        [Test]
        public void DownSamplesCandlestickSeriesWithStopSameAsLastPoint()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 1.5 day intervals
            //      Original series: |---------|---------|---------|
            //      Sampled series:  |--------------|--------------|

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(reference.AddDays(3), 2m, 2m, 1m, 1m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5));

            var sampled = sampler.Sample(series, reference, reference.AddDays(3));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(1.5), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(Math.Max(seriesValues[1].High, seriesValues[2].High), sampledValues[1].High);
            Assert.AreEqual(Math.Min(seriesValues[1].Low, seriesValues[2].Low), sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[2], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[3].Open, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[3].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[3].Low, sampledValues[2].Low);
            Assert.AreEqual(seriesValues[3].Close, sampledValues[2].Close);
        }

        [Test]
        public void DownSamplesCandlestickSeriesWithStopAfterLastPoint()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 1.5 day intervals
            //      Original series: |---------|---------|---------|---------|
            //      Sampled series:  |--------------|--------------|--------------

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5));

            var sampled = sampler.Sample(series, reference, reference.AddDays(4));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(1.5), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(Math.Max(seriesValues[1].High, seriesValues[2].High), sampledValues[1].High);
            Assert.AreEqual(Math.Min(seriesValues[1].Low, seriesValues[2].Low), sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[2], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[3].Open, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[3].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[3].Low, sampledValues[2].Low);
            Assert.AreEqual(seriesValues[3].Close, sampledValues[2].Close);
        }

        private static decimal InterpolateClose(Candlestick prev, Candlestick next, TimeSpan candleSpan, long time)
        {
            var prevOpenUnitTime = Time.DateTimeToUnixTimeStamp(prev.Time - candleSpan).SafeDecimalCast();
            return (next.Close - prev.Open) * (time - prevOpenUnitTime) / (next.LongTime - prevOpenUnitTime) + prev.Open;
        }

        [Test]
        public void DownSamplesCandlestickSeriesWithStopBeforeLastPoint()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 1.25 day intervals
            //      Original series: |-----------|-----------|-----------|-----------|
            //      Sampled series:  |--------------|--------------|--------------|

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.25));

            var sampled = sampler.Sample(series, reference, reference.AddDays(4));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(4, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(1.25), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(Math.Max(seriesValues[1].High, seriesValues[2].High), sampledValues[1].High);
            Assert.AreEqual(Math.Min(seriesValues[1].Low, seriesValues[2].Low), sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[2], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.25), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[3].Open, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[3].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[3].Low, sampledValues[2].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[3], seriesValues[3], TimeSpan.FromDays(1), sampledValues[2].LongTime), sampledValues[2].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(3 * 1.25), sampledValues[3].Time);
            Assert.AreEqual(seriesValues[4].Open, sampledValues[3].Open);
            Assert.AreEqual(seriesValues[4].High, sampledValues[3].High);
            Assert.AreEqual(seriesValues[4].Low, sampledValues[3].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[4], seriesValues[4], TimeSpan.FromDays(1), sampledValues[3].LongTime), sampledValues[3].Close);
        }

        [Test]
        public void SubSamplesCandlestickSeries()
        {
            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference.AddDays(1), reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(2, sampled.Values.Count);

            for (var i = 0; i < sampledValues.Count; i++)
            {
                Assert.AreEqual(seriesValues[i + 1].Time, sampledValues[i].Time);
                AssertCandlesticksValuesAreEqual(seriesValues[i + 1], sampledValues[i]);
            }
        }

        [Test]
        public void SamplesCandlestickSeriesWithHigherSamplingResolution()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 0.25 day intervals
            //      Original series: |-------------------|-------------------|
            //      Sampled series:  |----|----|----|----|----|----|----|----|

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(0.25));

            var sampled = sampler.Sample(series, reference, reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(9, sampled.Values.Count);

            // First half:

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(0.25), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[1].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 0.25), sampledValues[2].Time);
            Assert.AreEqual(sampledValues[1].Close, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[2].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[2].LongTime), sampledValues[2].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(3 * 0.25), sampledValues[3].Time);
            Assert.AreEqual(sampledValues[2].Close, sampledValues[3].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[3].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[3].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[3].LongTime), sampledValues[3].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(4 * 0.25), sampledValues[4].Time);
            Assert.AreEqual(sampledValues[3].Close, sampledValues[4].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[4].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[4].Low);
            Assert.AreEqual(seriesValues[1].Close, sampledValues[4].Close);

            // Second half:

            Assert.AreEqual(seriesValues[0].Time.AddDays(5 * 0.25), sampledValues[5].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[5].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[5].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[5].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[5].LongTime), sampledValues[5].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(6 * 0.25), sampledValues[6].Time);
            Assert.AreEqual(sampledValues[5].Close, sampledValues[6].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[6].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[6].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[6].LongTime), sampledValues[6].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(7 * 0.25), sampledValues[7].Time);
            Assert.AreEqual(sampledValues[6].Close, sampledValues[7].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[7].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[7].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[7].LongTime), sampledValues[7].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(8 * 0.25), sampledValues[8].Time);
            Assert.AreEqual(sampledValues[7].Close, sampledValues[8].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[8].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[8].Low);
            Assert.AreEqual(seriesValues[2].Close, sampledValues[8].Close);
        }

        [Test]
        public void SamplesCandlestickSeriesWithHigherUnevenSamplingResolutionAndStopAfterLastPoint()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 10 hours intervals
            //      Original series: |-------------------|-------------------|
            //      Sampled series:  |--------|--------|--------|--------|--------

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromHours(10));

            var sampled = sampler.Sample(series, reference, reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(5, sampled.Values.Count);

            // First half:

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddHours(1 * 10), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[1].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[1].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[1].LongTime), (double)sampledValues[1].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(2 * 10), sampledValues[2].Time);
            Assert.AreEqual(sampledValues[1].Close, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[2].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[2].LongTime), (double)sampledValues[2].Close, delta: 1e-3);

            // Second half:

            Assert.AreEqual(seriesValues[0].Time.AddHours(3 * 10), sampledValues[3].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[3].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[3].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[3].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[3].LongTime), (double)sampledValues[3].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(4 * 10), sampledValues[4].Time);
            Assert.AreEqual(sampledValues[3].Close, sampledValues[4].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[4].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[4].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[4].LongTime), (double)sampledValues[4].Close, delta: 1e-3);
        }

        [Test]
        public void SamplesCandlestickSeriesWithHigherUnevenSamplingResolutionAndStopBeforeLastPoint()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 10 hours intervals
            //      Original series: |-------------------|-------------------|
            //      Sampled series:  |--------|--------|--------|--------|

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.AddPoint(reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(reference.AddDays(2), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromHours(10));

            var sampled = sampler.Sample(series, reference, reference.AddHours(40));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(5, sampled.Values.Count);

            Assert.AreEqual(5, sampled.Values.Count);

            // First half:

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddHours(1 * 10), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[1].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[1].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[1].LongTime), (double)sampledValues[1].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(2 * 10), sampledValues[2].Time);
            Assert.AreEqual(sampledValues[1].Close, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[2].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[2].LongTime), (double)sampledValues[2].Close, delta: 1e-3);

            // Second half:

            Assert.AreEqual(seriesValues[0].Time.AddHours(3 * 10), sampledValues[3].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[3].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[3].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[3].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[3].LongTime), (double)sampledValues[3].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(4 * 10), sampledValues[4].Time);
            Assert.AreEqual(sampledValues[3].Close, sampledValues[4].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[4].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[4].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[4].LongTime), (double)sampledValues[4].Close, delta: 1e-3);
        }

        private static void AssertCandlesticksValuesAreEqual(Candlestick first, Candlestick second)
        {
            Assert.AreEqual(first.Open, second.Open);
            Assert.AreEqual(first.High, second.High);
            Assert.AreEqual(first.Low, second.Low);
            Assert.AreEqual(first.Close, second.Close);
        }
    }
}
