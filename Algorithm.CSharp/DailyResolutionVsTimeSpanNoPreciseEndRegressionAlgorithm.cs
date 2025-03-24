using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests indicator behavior when "DailyPreciseEndTime" is disabled,  
    /// ensuring updates occur at midnight and values remain consistent.  
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

        protected override void SetupFirstIndicatorUpdatedHandler()
        {
            RelativeStrengthIndex1.Updated += (sender, data) =>
            {
                var updatedTime = Time;

                // RSI1 should update at midnight when precise end time is disabled
                if (!(updatedTime.Hour == 0 && updatedTime.Minute == 0 && updatedTime.Second == 0))
                {
                    throw new RegressionTestException($"{RelativeStrengthIndex1.Name} must have updated at midnight, but it was updated at {updatedTime}");
                }

                // Since RSI1 updates before RSI2, it should have exactly one extra sample
                if (RelativeStrengthIndex1.Samples - 1 != RelativeStrengthIndex2.Samples)
                {
                    throw new RegressionTestException("RSI1 must have 1 extra sample");
                }

                // RSI1's previous value should match RSI2's current value, ensuring consistency
                if (RelativeStrengthIndex1.Previous.Value != RelativeStrengthIndex2.Current.Value)
                {
                    throw new RegressionTestException("RSI1 and RSI2 must have same value");
                }
            };
        }
    }
}
