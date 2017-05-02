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
    public class ArnaudLegouxMovingAverageTest : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new ArnaudLegouxMovingAverage(9, 6);
        }

        protected override string TestFileName
        {
            get { return "spy_alma.txt"; }
        }

        protected override string TestColumnName
        {
            get { return "ALMA"; }
        }
    }
}
