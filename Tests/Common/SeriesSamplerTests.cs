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
            AssertCandlesticksValuesAreEqual(seriesValues[1], sampledValues[1]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[2].Open);
            Assert.AreEqual(Math.Max(seriesValues[2].High, seriesValues[3].High), sampledValues[2].High);
            Assert.AreEqual(Math.Min(seriesValues[2].Low, seriesValues[3].Low), sampledValues[2].Low);
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
            AssertCandlesticksValuesAreEqual(seriesValues[1], sampledValues[1]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[2].Open);
            Assert.AreEqual(Math.Max(seriesValues[2].High, seriesValues[3].High), sampledValues[2].High);
            Assert.AreEqual(Math.Min(seriesValues[2].Low, seriesValues[3].Low), sampledValues[2].Low);
            Assert.AreEqual(seriesValues[3].Close, sampledValues[2].Close);
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

            for (var i = 0; i < sampledValues.Count; i++)
            {
                Assert.AreEqual(seriesValues[0].Time.AddDays(i * 1.25), sampledValues[i].Time);
                AssertCandlesticksValuesAreEqual(seriesValues[i], sampledValues[i]);
            }
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
        public void HandlesCandlestickSeriesDuplicateTimes()
        {

            var series = new CandlestickSeries { Name = "name" };
            var reference = new DateTime(2023, 03, 03);
            series.AddPoint(reference, 1m, 2m, 1m, 2m);
            series.Values.Add(new Candlestick(reference, 2m, 3m, 2m, 3m));
            series.AddPoint(reference.AddDays(1), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var sampled = sampler.Sample(series, reference, reference.AddDays(1));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            // sampler will only produce one value at the time
            // it was also respect the latest value

            Assert.AreEqual(2, sampledValues.Count);

            for (var i = 0; i < sampledValues.Count; i++)
            {
                Assert.AreEqual(series.Values[i + 1].Time, sampledValues[i].Time);
                AssertCandlesticksValuesAreEqual(seriesValues[i + 1], sampledValues[i]);
            }
        }

        // TODO: Add tests for sample period < original series period

        private static void AssertCandlesticksValuesAreEqual(Candlestick first, Candlestick second)
        {
            Assert.AreEqual(first.Open, second.Open);
            Assert.AreEqual(first.High, second.High);
            Assert.AreEqual(first.Low, second.Low);
            Assert.AreEqual(first.Close, second.Close);
        }
    }
}
