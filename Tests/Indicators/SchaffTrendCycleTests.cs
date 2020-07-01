using System;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    class SchaffTrendCycleTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new SchaffTrendCycle();
        }

        protected override string TestFileName
        {
            get { return "spy_stc.txt"; }
        }

        protected override string TestColumnName
        {
            get { return "STC"; }
        }

        [Test]
        public virtual void RunningTest()
        {
            var indicator = CreateIndicator();
            var startDate = new DateTime(2019, 1, 1);

            for (var i = 0; i < 30; i++)
            {
                var input = new IndicatorDataPoint(startDate.AddDays(i), 100m + i);
                indicator.Update(input);
            }
        }
    }
}
