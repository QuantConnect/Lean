using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class LinearWeightedMovingAverageTests
    {
        [Test]
        public void Lwma4ComputesCorrectly()
        {
            const int period = 4;
            decimal[] values = { 1m, 2m, 3m, 4m };

            var lwma = new QuantConnect.Indicators.LinearWeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                lwma.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            current = ((4 * .4m) + (3 * .3m) + (2 * .2m) + (1 * .1m));
            Assert.AreEqual(current, lwma.Current.Value);
        }
        [Test]
        public void Lwma1ComputesCorrectly()
        {
            const int period = 1;
            decimal[] values = { 1m };

            var lwma = new QuantConnect.Indicators.LinearWeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                lwma.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            current = 1m;
            Assert.AreEqual(current, lwma.Current.Value);
        }
        [Test]
        public void Lwma2ComputesCorrectly()
        {
            const int period = 2;
            decimal[] values = { 1m, 2m };

            var lwma = new QuantConnect.Indicators.LinearWeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                lwma.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            current = ((2 * 2m) + (1 * 1m)) / 3;
            Assert.AreEqual(current, lwma.Current.Value);
        }
        [Test]
        // See http://en.wikipedia.org/wiki/Moving_average
        // for the formula and the numbers in this test.
        public void Lwma5ComputesCorrectly()
        {
            const int period = 5;
            decimal[] values = { 77m, 79m, 79m, 81m, 83m };

            var lwma = new QuantConnect.Indicators.LinearWeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                lwma.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));

            }
            current = 83 * (5m / 15) + 81 * (4m / 15) + 79 * (3m / 15) + 79 * (2m / 15) + 77 * (1m / 15);
            Assert.AreEqual(current, lwma.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            const int period = 4;
            decimal[] values = { 1m, 2m, 3m, 4m, 5m };

            var lwma = new QuantConnect.Indicators.LinearWeightedMovingAverage(period);


            for (int i = 0; i < values.Length; i++)
            {
                lwma.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            Assert.IsTrue(lwma.IsReady);
            Assert.AreNotEqual(0m, lwma.Current.Value);
            Assert.AreNotEqual(0, lwma.Samples);

            lwma.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(lwma);
        }

    }
}
