using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DetrendedPriceOscillatorTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override string TestColumnName => "DPO";

        protected override string TestFileName => "spy_dpo.csv";

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new DetrendedPriceOscillator(period: 21);
        }
    }
}