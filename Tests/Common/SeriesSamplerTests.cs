using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var reference = DateTime.Now;
            series.AddPoint(reference, 1m);

            var sampler = new SeriesSampler(TimeSpan.FromDays(1));

            var sampled = sampler.Sample(series, reference, reference);
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
    }
}
