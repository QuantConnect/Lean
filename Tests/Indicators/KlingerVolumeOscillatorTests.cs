using System;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]

    /// <summary>
    /// Tests for the Klinger Volume Oscillator (KVO) indicator
    /// </summary>
    public class KlingerVolumeOscillatorTests : CommonIndicatorTests<TradeBar>
    {
        /// <summary>
        /// Generated Klinger Volume Oscillator test data from talipp
        /// </summary>
        protected override string TestFileName => "spy_with_kvo.csv";

        /// <summary>
        /// Generated column for KVO(5,10) from talipp
        /// </summary>
        protected override string TestColumnName => "KVO5_10";

        /// <summary>
        /// Required by CommonIndicatorTests: return a fresh instance of your indicator.
        /// </summary>
        protected override IndicatorBase<TradeBar> CreateIndicator()
        {
            RenkoBarSize = 1m;

            // match generated data from talipp
            return new KlingerVolumeOscillator(fastPeriod: 5, slowPeriod: 10);
        }

        /// <summary>
        /// This indicator doesn't accept Renko Bars as input. Skip this test.
        /// </summary>
        public override void AcceptsRenkoBarsAsInput()
        {
        }
    }
}
