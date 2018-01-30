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
    public class CoppockCurveTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator() { return new CoppockCurve(); }

        protected override string TestFileName => "spy_coppock_curve.csv";
        protected override string TestColumnName => "CoppockCurve";
    }
}
