using NUnit.Framework;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class SortinoRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new SortinoRatio("SORTINO", 10);
        }

        protected override string TestFileName => "spy_sortino.txt";

        protected override string TestColumnName => "SORTINO_10";

        [Test]
        public void TestTradeBarsWithSameValue()
        {
            // With the value not changing, the indicator should return default value 0m.
            var sr = new SortinoRatio("SORTINO", 10);

            // push the value 100000 into the indicator 20 times (sortinoRatioPeriod + movingAveragePeriod)
            for (int i = 0; i < 20; i++)
            {
                IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 100000m);
                sr.Update(point);
            }

            Assert.AreEqual(sr.Current.Value, 0m);
        }

        [Test]
        public void TestTradeBarsWithDifferingValue()
        {
            // With the value changing, the indicator should return a value that is not the default 0m.
            var sr = new SortinoRatio("SORTINO", 10);

            // push the value 100000 into the indicator 20 times (sortinoRatioPeriod + movingAveragePeriod)
            for (int i = 0; i < 20; i++)
            {
                IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 100000m + i);
                sr.Update(point);
            }

            Assert.AreNotEqual(sr.Current.Value, 0m);
        }

        [Test]
        public void TestDivByZero()
        {
            // With the value changing, the indicator should return a value that is not the default 0m.
            var sr = new SortinoRatio("SORTINO", 10);

            // push the value 100000 into the indicator 20 times (sortinoRatioPeriod + movingAveragePeriod)
            for (int i = 0; i < 20; i++)
            {
                IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 0);
                sr.Update(point);
            }

            Assert.AreEqual(sr.Current.Value, 0m);
        }
    }
}
