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
        private readonly DateTime _reference = new(2023, 2, 2);

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        public void ReturnsIdentityFutureOrPastCandlestick(int dataPoints, bool futureOrPast)
        {
            var series = new CandlestickSeries { Name = "name" };
            for (var i = 0; i < dataPoints; i++)
            {
                series.AddPoint(new Candlestick(_reference.AddDays(futureOrPast ? (i + 1000) : i), 1, 1, 1, 1));
            }

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var sampled = sampler.Sample(series, _reference.AddDays(10), _reference.AddDays(11));

            // empty
            Assert.AreEqual(0, sampled.Values.Count);
        }

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        public void ReturnsIdentityFutureOrPastPoints(int dataPoints, bool futureOrPast)
        {
            var series = new Series { Name = "name" };
            for (var i = 0; i < dataPoints; i++)
            {
                series.AddPoint(new ChartPoint(_reference.AddDays(futureOrPast ? (i + 1000) : i), 1));
            }

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var sampled = sampler.Sample(series, _reference.AddDays(10), _reference.AddDays(11));

            // empty
            Assert.AreEqual(0, sampled.Values.Count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReturnsIdentityOnSinglePoint(bool isNullValue)
        {
            var series = new Series {Name = "name"};
            series.AddPoint(new ChartPoint(_reference, isNullValue ? null: 1));

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, _reference.AddSeconds(-1), _reference.AddSeconds(1));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(1, sampled.Values.Count);
            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[0].y, sampledValues[0].y);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamplesNullFirstValue(bool subSample)
        {
            var series = new Series { Name = "name" };
            series.AddPoint(new ChartPoint(_reference, null));
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(_reference.AddDays(2), 3m);
            series.AddPoint(_reference.AddDays(3), 4m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(3));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[0].y);

            Assert.AreEqual((seriesValues[1].x + seriesValues[2].x) / 2, sampledValues[1].x);
            Assert.AreEqual((seriesValues[1].y + seriesValues[2].y) / 2, sampledValues[1].y);

            Assert.AreEqual(seriesValues[3].x, sampledValues[2].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[2].y);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamples(bool subSample)
        {
            var series = new Series {Name = "name"};
            series.AddPoint(_reference, 1m);
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(_reference.AddDays(2), 3m);
            series.AddPoint(_reference.AddDays(3), 4m);
            series.AddPoint(new ChartPoint(_reference.AddDays(4), null));
            series.AddPoint(_reference.AddDays(5), 5m);
            series.AddPoint(_reference.AddDays(6), 6m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(6));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(5, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[0].y, sampledValues[0].y);

            Assert.AreEqual((seriesValues[1].x + seriesValues[2].x)/2, sampledValues[1].x);
            Assert.AreEqual((seriesValues[1].y + seriesValues[2].y)/2, sampledValues[1].y);

            Assert.AreEqual(seriesValues[3].x, sampledValues[2].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[2].y);

            Assert.AreEqual((seriesValues[4].x + seriesValues[5].x) / 2, sampledValues[3].x);
            Assert.AreEqual(seriesValues[5].y, sampledValues[3].y);

            Assert.AreEqual(seriesValues[6].x, sampledValues[4].x);
            Assert.AreEqual(seriesValues[6].y, sampledValues[4].y);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamplesWithTimeJump(bool subSample)
        {
            var series = new Series { Name = "name" };
            series.AddPoint(_reference, 1m);
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(_reference.AddDays(2), 3m);
            series.AddPoint(_reference.AddDays(3), 4m);
            series.AddPoint(new ChartPoint(_reference.AddDays(4), null));
            // time jump
            series.AddPoint(_reference.AddDays(20), 5m);
            series.AddPoint(_reference.AddDays(21), 6m);

            var span = TimeSpan.FromDays(1.5);
            var sampler = new SeriesSampler(span) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(24));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(subSample ? 15 : 5, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[0].y, sampledValues[0].y);

            Assert.AreEqual((seriesValues[1].x + seriesValues[2].x) / 2, sampledValues[1].x);
            Assert.AreEqual((seriesValues[1].y + seriesValues[2].y) / 2, sampledValues[1].y);

            Assert.AreEqual(seriesValues[3].x, sampledValues[2].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[2].y);

            var expectedTime = seriesValues[3].x;
            for (var i = 3; i < 13; i++)
            {
                expectedTime += (long)span.TotalSeconds;

                if (subSample)
                {
                    Assert.AreEqual(expectedTime, sampledValues[i].x);
                    // all nulls
                    Assert.AreEqual(null, sampledValues[i].y);
                }
            }

            var expectedIndex = subSample ? 13 : 3;

            Assert.AreEqual(expectedTime + (long)span.TotalSeconds, sampledValues[expectedIndex].x);
            Assert.AreEqual(seriesValues[5].y, sampledValues[expectedIndex].y);

            expectedIndex++;
            Assert.AreEqual(seriesValues[6].x, sampledValues[expectedIndex].x);
            Assert.AreEqual(seriesValues[6].y, sampledValues[expectedIndex].y);
        }

        [Test]
        public void SubSamples()
        {
            var series = new Series {Name = "name"};
            series.AddPoint(_reference, 1m);
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(new ChartPoint(_reference.AddDays(2), null));
            series.AddPoint(_reference.AddDays(3), 4m);
            series.AddPoint(_reference.AddDays(4), 5m);
            series.AddPoint(_reference.AddDays(5), 6m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(0.5));

            var sampled = sampler.Sample(series, _reference.AddDays(1), _reference.AddDays(3));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(5, sampled.Values.Count);

            Assert.AreEqual(seriesValues[1].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[0].y);

            Assert.AreEqual((seriesValues[1].x + seriesValues[2].x) / 2, sampledValues[1].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[1].y);
            Assert.AreEqual(seriesValues[2].x, sampledValues[2].x);
            Assert.AreEqual(null, sampledValues[2].y);

            Assert.AreEqual((seriesValues[3].x + seriesValues[2].x) / 2, sampledValues[3].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[3].y);
            Assert.AreEqual(seriesValues[3].x, sampledValues[4].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[4].y);
        }

        [Test]
        public void SubSamplesDisabled()
        {
            var series = new Series { Name = "name" };
            series.AddPoint(_reference, 1m);
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(new ChartPoint(_reference.AddDays(2), null));
            // even if the data doesn't fit exactly the expected bar span we expect it to pass through
            series.AddPoint(_reference.AddDays(2.8), 4m);
            series.AddPoint(_reference.AddDays(4), 5m);
            series.AddPoint(_reference.AddDays(5), 6m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(0.5)) { SubSample = false };

            var sampled = sampler.Sample(series, _reference.AddDays(1), _reference.AddDays(3));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[1].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[0].y);

            Assert.AreEqual(seriesValues[2].x, sampledValues[1].x);
            Assert.AreEqual(seriesValues[2].y, sampledValues[1].y);

            Assert.AreEqual(seriesValues[3].x, sampledValues[2].x);
            Assert.AreEqual(seriesValues[3].y, sampledValues[2].y);
        }

        [Test]
        public void SubSamplesWithTimeJump()
        {
            var series = new Series { Name = "name" };
            series.AddPoint(_reference, 1m);
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(new ChartPoint(_reference.AddDays(2), null));
            series.AddPoint(_reference.AddDays(10), 4m);
            series.AddPoint(_reference.AddDays(11), 5m);
            series.AddPoint(_reference.AddDays(12), 6m);

            var span = TimeSpan.FromDays(0.5);
            var sampler = new SeriesSampler(span);

            var sampled = sampler.Sample(series, _reference.AddDays(1), _reference.AddDays(12));

            var seriesValues = series.Values.Cast<ChartPoint>().ToList();
            var sampledValues = sampled.Values.Cast<ChartPoint>().ToList();

            Assert.AreEqual(23, sampled.Values.Count);

            Assert.AreEqual(seriesValues[1].x, sampledValues[0].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[0].y);

            var expectedTime = seriesValues[1].x;
            expectedTime += (long)span.TotalSeconds;

            Assert.AreEqual(expectedTime, sampledValues[1].x);
            Assert.AreEqual(seriesValues[1].y, sampledValues[1].y);

            for (var i = 2; i < 17; i++)
            {
                expectedTime += (long)span.TotalSeconds;

                Assert.AreEqual(expectedTime, sampledValues[i].x);
                Assert.AreEqual(null, sampledValues[i].y);
            }

            Assert.AreEqual(seriesValues[4].x, sampledValues[20].x);
            Assert.AreEqual(seriesValues[4].y, sampledValues[20].y);

            Assert.AreEqual(seriesValues[4].x + (long)span.TotalSeconds, sampledValues[21].x);
            Assert.AreEqual((seriesValues[4].y + seriesValues[5].y) / 2, sampledValues[21].y);

            Assert.AreEqual(seriesValues[5].x, sampledValues[22].x);
            Assert.AreEqual(seriesValues[5].y, sampledValues[22].y);
        }

        [Test]
        public void DoesNotSampleBeforeStart()
        {
            var series = new Series { Name = "name" };
            series.AddPoint(_reference, 1m);
            series.AddPoint(_reference.AddDays(1), 2m);
            series.AddPoint(_reference.AddDays(2), 3m);
            series.AddPoint(_reference.AddDays(3), 4m);
            series.AddPoint(_reference.AddDays(4), 5m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, _reference.AddDays(-1), _reference.AddDays(2));

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
            series.Values.Add(new ChartPoint(_reference, 1m));
            series.Values.Add(new ChartPoint(_reference, 2m));
            series.Values.Add(new ChartPoint(_reference.AddDays(1), 3m));

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var sampled = sampler.Sample(series, _reference, _reference.AddDays(1));

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
            scatter.AddPoint(_reference, 1m);
            scatter.AddPoint(_reference, 3m);
            scatter.AddPoint(_reference.AddSeconds(1), 1.5m);
            scatter.AddPoint(_reference.AddSeconds(0.5), 1.5m);

            var sampler = new SeriesSampler(TimeSpan.FromMilliseconds(1));
            var sampled = sampler.Sample(scatter, _reference, _reference.AddDays(1));
            foreach (var pair in scatter.Values.Cast<ChartPoint>().Zip(sampled.Values.Cast<ChartPoint>(), Tuple.Create))
            {
                Assert.AreEqual(pair.Item1.x, pair.Item2.x);
                Assert.AreEqual(pair.Item1.y, pair.Item2.y);
            }
        }

        [Test]
        public void SubSampleTreemapPlots()
        {
            var treeMap = new Series("Treemap", SeriesType.Treemap, 0, "$");
            treeMap.AddPoint(_reference, 1m);
            treeMap.AddPoint(_reference.AddSeconds(0.5), 1.5m);
            treeMap.AddPoint(_reference.AddMinutes(1), 2m);

            var sampler = new SeriesSampler(TimeSpan.FromMilliseconds(1));
            var sampled = sampler.Sample(treeMap, _reference, _reference.AddDays(1));

            Assert.AreEqual(1000 * 60 + 1, sampled.Values.Count);
            foreach (var newValues in sampled.Values.Cast<ChartPoint>())
            {
                var expected = (ChartPoint)treeMap.Values[0];
                if (newValues.Time >= treeMap.Values[1].Time)
                {
                    expected = (ChartPoint)treeMap.Values[1];
                }
                if (newValues.Time >= treeMap.Values[2].Time)
                {
                    expected = (ChartPoint)treeMap.Values[2];
                }
                Assert.AreEqual(expected.y, newValues.y);
            }
        }

        [Test]
        public void DownSampleTreemapPlots()
        {
            var treeMap = new Series("Treemap", SeriesType.Treemap, 0, "$");
            treeMap.AddPoint(_reference, 1m);
            treeMap.AddPoint(_reference, 3m);
            treeMap.AddPoint(_reference.AddSeconds(0.5), 1.5m);
            treeMap.AddPoint(_reference.AddSeconds(1), 1.5m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));
            var sampled = sampler.Sample(treeMap, _reference, _reference.AddDays(1));

            // the last value
            Assert.AreEqual(1, sampled.Values.Count);
            Assert.AreEqual(_reference, sampled.Values[0].Time);
            Assert.AreEqual(((ChartPoint)treeMap.Values.Last()).Y, ((ChartPoint)sampled.Values[0]).Y);
        }

        [Test]
        public void EmitsEmptySeriesWithSinglePointOutsideOfStartStop()
        {
            var series = new Series { Name = "name" };
            series.AddPoint(_reference.AddSeconds(-1), 1m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, _reference, _reference);
            Assert.AreEqual(0, sampled.Values.Count);
        }

        [Test]
        public void ReturnsIdentityOnSinglePointCandlestickSeries()
        {
            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, _reference.AddSeconds(-1), _reference.AddSeconds(1));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(1, sampledValues.Count);
            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            Assert.AreEqual(seriesValues[0].Open, sampledValues[0].Open);
            Assert.AreEqual(seriesValues[0].High, sampledValues[0].High);
            Assert.AreEqual(seriesValues[0].Low, sampledValues[0].Low);
            Assert.AreEqual(seriesValues[0].Close, sampledValues[0].Close);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DoesNotSampleCandlestickSeriesBeforeStart(bool subSample)
        {
            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(_reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(_reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference.AddDays(-1), _reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            for (var i = 0; i < sampledValues.Count; i++)
            {
                Assert.AreEqual(seriesValues[i].Time, sampledValues[i].Time);
                AssertCandlesticksValuesAreEqual(seriesValues[i], sampledValues[i]);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamplesCandlestickSeriesWithStopSameAsLastPoint(bool subSample)
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 1.5 day intervals
            //      Original series: |---------|---------|---------|
            //      Sampled series:  |--------------|--------------|

            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(_reference.AddDays(3), 2m, 2m, 1m, 1m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(3));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(1.5), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(Math.Max(seriesValues[1].High.Value, seriesValues[2].High.Value), sampledValues[1].High);
            Assert.AreEqual(Math.Min(seriesValues[1].Low.Value, seriesValues[2].Low.Value), sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[2], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[3].Open, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[3].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[3].Low, sampledValues[2].Low);
            Assert.AreEqual(seriesValues[3].Close, sampledValues[2].Close);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamplesCandlestickSeriesWithStopAfterLastPoint(bool subSample)
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 1.5 day intervals
            //      Original series: |---------|---------|---------|---------|
            //      Sampled series:  |--------------|--------------|--------------

            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(_reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(_reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.5)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(4));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(1.5), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(Math.Max(seriesValues[1].High.Value, seriesValues[2].High.Value), sampledValues[1].High);
            Assert.AreEqual(Math.Min(seriesValues[1].Low.Value, seriesValues[2].Low.Value), sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[2], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[3].Open, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[3].High, sampledValues[2].High);
            Assert.AreEqual(seriesValues[3].Low, sampledValues[2].Low);
            Assert.AreEqual(seriesValues[3].Close, sampledValues[2].Close);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamplesCandlestickSeriesWithResamplePeriodSpanningMultipleCandlesticks(bool subSample)
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 2.5 day intervals
            //      Original series: |---------|---------|---------|---------|---------|---------|---------|
            //      Sampled series:  |------------------------|------------------------|-----------------------

            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(_reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(_reference.AddDays(4), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(5), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(6), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(7), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(2.5)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(5));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2.5), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(new[] { seriesValues[1].High, seriesValues[2].High, seriesValues[3].High }.Max(), sampledValues[1].High);
            Assert.AreEqual(new[] { seriesValues[1].Low, seriesValues[2].Low, seriesValues[3].Low }.Min(), sampledValues[1].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[3], TimeSpan.FromDays(1), sampledValues[1].LongTime), (double)sampledValues[1].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 2.5), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[4].Open, sampledValues[2].Open);
            Assert.AreEqual(Math.Max(seriesValues[4].High.Value, seriesValues[5].High.Value), sampledValues[2].High);
            Assert.AreEqual(Math.Min(seriesValues[4].Low.Value, seriesValues[5].Low.Value), sampledValues[2].Low);
            Assert.AreEqual(seriesValues[5].Close, sampledValues[2].Close);
        }

        private static decimal InterpolateClose(Candlestick prev, Candlestick next, TimeSpan candleSpan, long time)
        {
            var prevOpenUnitTime = Time.DateTimeToUnixTimeStamp(prev.Time - candleSpan).SafeDecimalCast();
            return (next.Close.Value - prev.Open.Value) * (time - prevOpenUnitTime) / (next.LongTime - prevOpenUnitTime) + prev.Open.Value;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DownSamplesCandlestickSeriesWithStopBeforeLastPoint(bool subSample)
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 1.25 day intervals
            //      Original series: |-----------|-----------|-----------|-----------|
            //      Sampled series:  |--------------|--------------|--------------|

            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(_reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(_reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1.25)) { SubSample = subSample };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(4));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(4, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(1.25), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(Math.Max(seriesValues[1].High.Value, seriesValues[2].High.Value), sampledValues[1].High);
            Assert.AreEqual(Math.Min(seriesValues[1].Low.Value, seriesValues[2].Low.Value), sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[2], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 1.25), sampledValues[2].Time);
            Assert.AreEqual(seriesValues[3].Open, sampledValues[2].Open);
            Assert.AreEqual(seriesValues[3].High, sampledValues[2].High);
            Assert.AreEqual(1.5m, sampledValues[2].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[3], seriesValues[3], TimeSpan.FromDays(1), sampledValues[2].LongTime), sampledValues[2].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(3 * 1.25), sampledValues[3].Time);
            Assert.AreEqual(seriesValues[4].Open, sampledValues[3].Open);
            Assert.AreEqual(2.75m, sampledValues[3].High);
            Assert.AreEqual(seriesValues[4].Low, sampledValues[3].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[4], seriesValues[4], TimeSpan.FromDays(1), sampledValues[3].LongTime), sampledValues[3].Close);
        }

        [Test]
        public void SubSamplesCandlestickSeries()
        {
            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(_reference.AddDays(3), 2m, 2m, 1m, 1m);
            series.AddPoint(_reference.AddDays(4), 2m, 3m, 2m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, _reference.AddDays(1), _reference.AddDays(2));

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
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(0.25));

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(9, sampled.Values.Count);

            // First half:

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddDays(0.25), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(2.25, sampledValues[1].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[1].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[1].LongTime), sampledValues[1].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(2 * 0.25), sampledValues[2].Time);
            Assert.AreEqual(sampledValues[1].Close, sampledValues[2].Open);
            Assert.AreEqual(2.5, sampledValues[2].High);
            Assert.AreEqual(2.25, sampledValues[2].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[2].LongTime), sampledValues[2].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(3 * 0.25), sampledValues[3].Time);
            Assert.AreEqual(sampledValues[2].Close, sampledValues[3].Open);
            Assert.AreEqual(2.75, sampledValues[3].High);
            Assert.AreEqual(2.5, sampledValues[3].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[3].LongTime), sampledValues[3].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(4 * 0.25), sampledValues[4].Time);
            Assert.AreEqual(sampledValues[3].Close, sampledValues[4].Open);
            Assert.AreEqual(seriesValues[1].High, sampledValues[4].High);
            Assert.AreEqual(2.75, sampledValues[4].Low);
            Assert.AreEqual(seriesValues[1].Close, sampledValues[4].Close);

            // Second half:

            Assert.AreEqual(seriesValues[0].Time.AddDays(5 * 0.25), sampledValues[5].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[5].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[5].High);
            Assert.AreEqual(3.75, sampledValues[5].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[5].LongTime), sampledValues[5].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(6 * 0.25), sampledValues[6].Time);
            Assert.AreEqual(sampledValues[5].Close, sampledValues[6].Open);
            Assert.AreEqual(3.75m, sampledValues[6].High);
            Assert.AreEqual(3.5m, sampledValues[6].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[6].LongTime), sampledValues[6].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(7 * 0.25), sampledValues[7].Time);
            Assert.AreEqual(sampledValues[6].Close, sampledValues[7].Open);
            Assert.AreEqual(3.5m, sampledValues[7].High);
            Assert.AreEqual(3.25m, sampledValues[7].Low);
            Assert.AreEqual(InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[7].LongTime), sampledValues[7].Close);

            Assert.AreEqual(seriesValues[0].Time.AddDays(8 * 0.25), sampledValues[8].Time);
            Assert.AreEqual(sampledValues[7].Close, sampledValues[8].Open);
            Assert.AreEqual(3.25m, sampledValues[8].High);
            Assert.AreEqual(seriesValues[2].Low, sampledValues[8].Low);
            Assert.AreEqual(seriesValues[2].Close, sampledValues[8].Close);
        }

        [Test]
        public void SamplesCandlestickSeriesWithHigherSamplingResolutionDisabled()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 0.25 day intervals
            //      Original series: |-------------------|-------------------|
            //      Sampled series:  |-------------------|-------------------|

            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            // even if the data doesn't fit exactly the expected bar span we expect it to pass through
            series.AddPoint(_reference.AddDays(1.6), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(0.25)) { SubSample = false };

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[1].Time, sampledValues[1].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[1], sampledValues[1]);

            Assert.AreEqual(seriesValues[2].Time, sampledValues[2].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[2], sampledValues[2]);
        }

        [Test]
        public void SamplesCandlestickSeriesWithLowerSamplingResolutionNullValues()
        {
            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(new Candlestick(_reference.AddDays(3), null, null, null, null));
            // Time jump
            series.AddPoint(_reference.AddDays(11), 5m, 5m, 5m, 5m);

            var barSpan = TimeSpan.FromDays(1.5);
            var sampler = new SeriesSampler(barSpan);

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(12));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(8, sampled.Values.Count);

            var expectedTime = series.Values[3].Time;
            for (var i = 2; i < 7; i++)
            {
                Assert.AreEqual(expectedTime, sampledValues[i].Time);
                Assert.AreEqual(null, sampledValues[i].Open);
                Assert.AreEqual(null, sampledValues[i].High);
                Assert.AreEqual(null, sampledValues[i].Low);
                Assert.AreEqual(null, sampledValues[i].Close);
                expectedTime += barSpan;
            }

            Assert.AreEqual(expectedTime, sampledValues[7].Time);
            Assert.AreEqual(seriesValues[4].Open, sampledValues[7].Open);
            Assert.AreEqual(seriesValues[4].High, sampledValues[7].High);
            Assert.AreEqual(seriesValues[4].Low, sampledValues[7].Low);
            Assert.AreEqual(seriesValues[4].Close, sampledValues[7].Close);
        }

        [Test]
        public void SamplesCandlestickSeriesWithHigherSamplingResolutionNullValues()
        {
            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);
            series.AddPoint(new Candlestick(_reference.AddDays(3), null, null, null, null));
            // Time jump
            series.AddPoint(_reference.AddDays(10), 5m, 5m, 5m, 5m);

            var barSpan = TimeSpan.FromDays(0.5);
            var sampler = new SeriesSampler(barSpan);

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(10));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(21, sampled.Values.Count);

            var expectedTime = series.Values[2].Time;
            for (var i = 5; i < 19; i++)
            {
                expectedTime += barSpan;
                Assert.AreEqual(expectedTime, sampledValues[i].Time);
                Assert.AreEqual(null, sampledValues[i].Open);
                Assert.AreEqual(null, sampledValues[i].High);
                Assert.AreEqual(null, sampledValues[i].Low);
                Assert.AreEqual(null, sampledValues[i].Close);
            }

            expectedTime += barSpan;
            Assert.AreEqual(expectedTime, sampledValues[19].Time);
            Assert.AreEqual(seriesValues[4].Open, sampledValues[19].Open);
            Assert.AreEqual(seriesValues[4].High, sampledValues[19].High);
            Assert.AreEqual(seriesValues[4].Low, sampledValues[19].Low);
            Assert.AreEqual(seriesValues[4].Close, sampledValues[19].Close);

            expectedTime += barSpan;
            Assert.AreEqual(expectedTime, sampledValues[20].Time);
            Assert.AreEqual(seriesValues[4].Open, sampledValues[20].Open);
            Assert.AreEqual(seriesValues[4].High, sampledValues[20].High);
            Assert.AreEqual(seriesValues[4].Low, sampledValues[20].Low);
            Assert.AreEqual(seriesValues[4].Close, sampledValues[20].Close);
        }

        [Test]
        public void SamplesCandlestickSeriesWithHigherUnevenSamplingResolutionAndStopAfterLastPoint()
        {
            // Original series is sampled at 1 day intervals
            // Sampled series is sampled at 10 hours intervals
            //      Original series: |-------------------|-------------------|
            //      Sampled series:  |--------|--------|--------|--------|--------

            var series = new CandlestickSeries { Name = "name" };
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromHours(10));

            var sampled = sampler.Sample(series, _reference, _reference.AddDays(2));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(5, sampled.Values.Count);

            // First half:

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddHours(1 * 10), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(2.416667m, sampledValues[1].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[1].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[1].LongTime), (double)sampledValues[1].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(2 * 10), sampledValues[2].Time);
            Assert.AreEqual(sampledValues[1].Close, sampledValues[2].Open);
            Assert.AreEqual(2.833333m, sampledValues[2].High);
            Assert.AreEqual(2.416667m, sampledValues[2].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[2].LongTime), (double)sampledValues[2].Close, delta: 1e-3);

            // Second half:

            Assert.AreEqual(seriesValues[0].Time.AddHours(3 * 10), sampledValues[3].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[3].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[3].High);
            Assert.AreEqual(3.75m, sampledValues[3].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[3].LongTime), (double)sampledValues[3].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(4 * 10), sampledValues[4].Time);
            Assert.AreEqual(sampledValues[3].Close, sampledValues[4].Open);
            Assert.AreEqual(3.75m, sampledValues[4].High);
            Assert.AreEqual(3.333333m, sampledValues[4].Low);
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
            series.AddPoint(_reference, 1m, 2m, 1m, 2m);
            series.AddPoint(_reference.AddDays(1), 2m, 3m, 2m, 3m);
            series.AddPoint(_reference.AddDays(2), 4m, 4m, 3m, 3m);

            var sampler = new SeriesSampler(TimeSpan.FromHours(10));

            var sampled = sampler.Sample(series, _reference, _reference.AddHours(40));

            var seriesValues = series.Values.Cast<Candlestick>().ToList();
            var sampledValues = sampled.Values.Cast<Candlestick>().ToList();

            Assert.AreEqual(5, sampled.Values.Count);

            Assert.AreEqual(5, sampled.Values.Count);

            // First half:

            Assert.AreEqual(seriesValues[0].Time, sampledValues[0].Time);
            AssertCandlesticksValuesAreEqual(seriesValues[0], sampledValues[0]);

            Assert.AreEqual(seriesValues[0].Time.AddHours(1 * 10), sampledValues[1].Time);
            Assert.AreEqual(seriesValues[1].Open, sampledValues[1].Open);
            Assert.AreEqual(2.416667m, sampledValues[1].High);
            Assert.AreEqual(seriesValues[1].Low, sampledValues[1].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[1].LongTime), (double)sampledValues[1].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(2 * 10), sampledValues[2].Time);
            Assert.AreEqual(sampledValues[1].Close, sampledValues[2].Open);
            Assert.AreEqual(2.833333m, sampledValues[2].High);
            Assert.AreEqual(2.416667m, sampledValues[2].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[1], seriesValues[1], TimeSpan.FromDays(1), sampledValues[2].LongTime), (double)sampledValues[2].Close, delta: 1e-3);

            // Second half:

            Assert.AreEqual(seriesValues[0].Time.AddHours(3 * 10), sampledValues[3].Time);
            Assert.AreEqual(seriesValues[2].Open, sampledValues[3].Open);
            Assert.AreEqual(seriesValues[2].High, sampledValues[3].High);
            Assert.AreEqual(3.75m, sampledValues[3].Low);
            Assert.AreEqual((double)InterpolateClose(seriesValues[2], seriesValues[2], TimeSpan.FromDays(1), sampledValues[3].LongTime), (double)sampledValues[3].Close, delta: 1e-3);

            Assert.AreEqual(seriesValues[0].Time.AddHours(4 * 10), sampledValues[4].Time);
            Assert.AreEqual(sampledValues[3].Close, sampledValues[4].Open);
            Assert.AreEqual(3.75m, sampledValues[4].High);
            Assert.AreEqual(3.333333m, sampledValues[4].Low);
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
