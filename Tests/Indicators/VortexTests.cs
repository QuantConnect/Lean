using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class VortexIndicatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new VortexIndicator(14);  // Assuming a 14-day period for the Vortex calculations
        }

        protected override string TestFileName => "spy_with_vtx.csv";

        protected override string TestColumnName => "plus_vtx";

        [Test]
        public override void ComparesAgainstExternalData()
        {
            var vortex = CreateIndicator();
            const double epsilon = .0001;

            // Test positive Vortex Indicator values (+VI)
            TestHelper.TestIndicator(vortex, TestFileName, "plus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((VortexIndicator)ind).PositiveVortexIndicator.Current.Value,epsilon)
            );

            // Reset indicator to ensure clean state for next test
            vortex.Reset();

            // Test negative Vortex Indicator values (-VI)
            TestHelper.TestIndicator(vortex, TestFileName, "minus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((VortexIndicator)ind).NegativeVortexIndicator.Current.Value,epsilon)
            );
        }
    }
}
    