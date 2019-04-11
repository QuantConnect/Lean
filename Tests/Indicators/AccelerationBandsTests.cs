using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AccelerationBandsTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new AccelerationBands(period: 20, width: 4m);
        }

        protected override string TestFileName => "spy_acceleration_bands_20_4.txt";

        protected override string TestColumnName => "MiddleBand";

        [Test]
        public void ComparesWithExternalDataLowerBand()
        {
            var abands = CreateIndicator();
            TestHelper.TestIndicator(
                abands,
                "spy_acceleration_bands_20_4.txt",
                "LowerBand",
                (ind, expected) => Assert.AreEqual(expected, (double) ((AccelerationBands) ind).LowerBand.Current.Value,
                    delta: 1e-4, message: "Lower band test fail.")
            );
        }

        [Test]
        public void ComparesWithExternalDataUpperBand()
        {
            var abands = CreateIndicator();
            TestHelper.TestIndicator(
                abands,
                "spy_acceleration_bands_20_4.txt",
                "UpperBand",
                (ind, expected) => Assert.AreEqual(expected, (double) ((AccelerationBands) ind).UpperBand.Current.Value,
                    delta: 1e-4, message: "Upper band test fail.")
            );
        }
    }
}