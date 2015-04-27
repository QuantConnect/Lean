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
    public class WeightedMovingAverageTests
    {
        [Test]
        public void Wma4ComputesCorrectly()
        {
            const int period = 4;
            decimal[] values = { 1m, 2m, 3m, 4m };

            var wma4 = new QuantConnect.Indicators.WeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                wma4.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            current = ((4 * .4m) + (3 * .3m) + (2 * .2m) + (1 * .1m));
            Assert.AreEqual(current, wma4.Current.Value);
        }
        [Test]
        public void Wma1ComputesCorrectly()
        {
            const int period = 1;
            decimal[] values = { 1m };

            var wma4 = new QuantConnect.Indicators.WeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                wma4.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            current = 1m;
            Assert.AreEqual(current, wma4.Current.Value);
        }
        [Test]
        public void Wma2ComputesCorrectly()
        {
            const int period = 2;
            decimal[] values = { 1m, 2m };

            var wma4 = new QuantConnect.Indicators.WeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                wma4.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            current = ((2 * 2m) + (1 * 1m)) / 3;
            Assert.AreEqual(current, wma4.Current.Value);
        }
        [Test]
        public void Wma5ComputesCorrectly()
        {
            const int period = 5;
            decimal[] values = { 77m, 79m, 79m, 81m, 83m };

            var wma4 = new QuantConnect.Indicators.WeightedMovingAverage(period);

            decimal current = 0m;
            for (int i = 0; i < values.Length; i++)
            {
                wma4.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
                
            }
            current = 83*(5m/15) + 81 * (4m / 15) + 79 * (3m / 15) + 79 * (2m / 15) + 77 * (1m / 15);
            Assert.AreEqual(current, wma4.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            const int period = 4;
            decimal[] values = { 1m, 2m, 3m, 4m, 5m };

            var wma = new QuantConnect.Indicators.WeightedMovingAverage(period);

            
            for (int i = 0; i < values.Length; i++)
            {
                wma.Update(new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(i), values[i]));
            }
            Assert.IsTrue(wma.IsReady);
            Assert.AreNotEqual(0m, wma.Current.Value);
            Assert.AreNotEqual(0, wma.Samples);

            wma.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(wma);
        }

    }
}
