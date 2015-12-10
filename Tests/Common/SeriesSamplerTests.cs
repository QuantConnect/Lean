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
            Assert.AreEqual(1, sampled.Values.Count);
            Assert.AreEqual(series.Values[0].x, sampled.Values[0].x);
            Assert.AreEqual(series.Values[0].y, sampled.Values[0].y);
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
            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(series.Values[0].x, sampled.Values[0].x);
            Assert.AreEqual(series.Values[0].y, sampled.Values[0].y);

            Assert.AreEqual((series.Values[1].x + series.Values[2].x)/2, sampled.Values[1].x);
            Assert.AreEqual((series.Values[1].y + series.Values[2].y)/2, sampled.Values[1].y);

            Assert.AreEqual(series.Values[3].x, sampled.Values[2].x);
            Assert.AreEqual(series.Values[3].y, sampled.Values[2].y);
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
            Assert.AreEqual(2, sampled.Values.Count);

            Assert.AreEqual(series.Values[1].x, sampled.Values[0].x);
            Assert.AreEqual(series.Values[1].y, sampled.Values[0].y);

            Assert.AreEqual(series.Values[2].x, sampled.Values[1].x);
            Assert.AreEqual(series.Values[2].y, sampled.Values[1].y);
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
            Assert.AreEqual(3, sampled.Values.Count);

            Assert.AreEqual(series.Values[0].x, sampled.Values[0].x);
            Assert.AreEqual(series.Values[0].y, sampled.Values[0].y);

            Assert.AreEqual(series.Values[1].x, sampled.Values[1].x);
            Assert.AreEqual(series.Values[1].y, sampled.Values[1].y);

            Assert.AreEqual(series.Values[2].x, sampled.Values[2].x);
            Assert.AreEqual(series.Values[2].y, sampled.Values[2].y);
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
            foreach (var pair in series.Values.Skip(1).Zip<ChartPoint, ChartPoint, Tuple<ChartPoint, ChartPoint>>(sampled.Values, Tuple.Create))
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
            foreach (var pair in scatter.Values.Zip<ChartPoint, ChartPoint, Tuple<ChartPoint, ChartPoint>>(sampled.Values, Tuple.Create))
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
    }
}
