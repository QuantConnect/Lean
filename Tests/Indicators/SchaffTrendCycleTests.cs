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

        /// <summary>
        /// Returns a custom assertion function, parameters are the indicator and the expected value from the file
        /// This overwrites the virtual function to allow a +/- 1 variance since this indicator is highly sensitive with
        /// a chain of EMAs and Stochastics calculation.
        /// </summary>
        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double)indicator.Current.Value, 1); }
        }
    }
}
