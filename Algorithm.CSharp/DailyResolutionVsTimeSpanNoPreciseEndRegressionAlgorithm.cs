using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests indicator behavior with "DailyPreciseEndTime" disabled, making indicator values equal at all times.
    /// </summary>
    public class DailyResolutionVsTimeSpanNoPreciseEndRegressionAlgorithm : DailyResolutionVsTimeSpanRegressionAlgorithm
    {
        /// <summary>
        /// Disables precise end time, considering the full day instead.
        /// </summary>
        protected override void SetDailyPreciseEndTime()
        {
            // By default, DailyPreciseEndTime is true
            Settings.DailyPreciseEndTime = false;
        }

        protected override void CompareValuesDuringMarketHours()
        {
            var value1 = RelativeStrengthIndex1.Current.Value;
            var value2 = RelativeStrengthIndex2.Current.Value;
            if (value1 != value2)
            {
                throw new RegressionTestException("The values must be equal during market hours");
            }
        }

        protected override void CompareValuesAfterMarketHours()
        {
            var value1 = RelativeStrengthIndex1.Current.Value;
            var value2 = RelativeStrengthIndex2.Current.Value;

            if (value1 != value2)
            {
                throw new RegressionTestException("The values must be equal after market hours");
            }
        }

    }
}
