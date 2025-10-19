using System;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class CovarianceTests
    {
        [Test]
        public void BecomesReadyAfterPeriod()
        {
            var period = 5;
            var cov = new Covariance(period);

            var t0 = new DateTime(2020, 1, 1);
            for (int i = 0; i < period - 1; i++)
            {
                cov.Update(t0.AddDays(i), i + 1, 2*(i + 1));
                Assert.IsFalse(cov.IsReady, "Should not be ready before period samples");
            }

            cov.Update(t0.AddDays(period - 1), period, 2*period);
            Assert.IsTrue(cov.IsReady, "Should be ready at exactly 'period' samples");
        }

        [Test]
        public void MatchesExpectedSampleCovarianceForLinearRelation()
        {
            // x = 1,2,3,4,5; y = 2x
            // sample var(x) over 1..5 is 2.5; cov(x,y) = 2 * var(x) = 5.0
            var cov = new Covariance(5);

            var t0 = new DateTime(2020, 1, 1);
            for (int i = 0; i < 5; i++)
            {
                var x = i + 1;
                var y = 2 * x;
                cov.Update(t0.AddDays(i), x, y);
            }

            Assert.IsTrue(cov.IsReady);
            Assert.AreEqual(5.0m, Math.Round(cov.Value, 6));
        }

        [Test]
        public void ResetsProperly()
        {
            var cov = new Covariance(3);
            var t0 = new DateTime(2020, 1, 1);

            cov.Update(t0, 1, 2);
            cov.Update(t0.AddDays(1), 2, 4);
            cov.Update(t0.AddDays(2), 3, 6);

            Assert.IsTrue(cov.IsReady);
            Assert.AreNotEqual(0m, cov.Value);

            cov.Reset();

            Assert.IsFalse(cov.IsReady);
            Assert.AreEqual(0m, cov.Value);
            Assert.AreEqual(default(DateTime), cov.Time);
        }

        [Test]
        public void ThrowsOnInvalidPeriod()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Covariance(1));
            Assert.DoesNotThrow(() => new Covariance(2));
        }
    }
}
