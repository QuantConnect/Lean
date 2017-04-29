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
    public class HullMovingAverageTest : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new HullMovingAverage(16);
        }

        protected override string TestFileName
        {
            get { return "spy_hma.txt"; }
        }

        protected override string TestColumnName
        {
            get { return "HMA_16"; }
        }
    }
}
