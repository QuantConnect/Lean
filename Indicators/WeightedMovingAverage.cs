using System;
using MathNet.Numerics;
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the traditional Weighted Moving Average indicator.  The weight are
    /// distributed according to the number of periods in the indicator. 
    /// 
    /// For example, a 4 period indicator will have a numerator of (4 * window[0]) + (3 * window[1]) + (2 * window[2]) + window[3]
    /// and a denominator of 4 + 3 + 2 + 1 = 10
    /// 
    /// During the warm up period, IsReady will return false, but the WMA will still be computed correctly because
    /// the denominator will be the minimum of Samples factorial or Size factorial and 
    /// the computation iterates over that minimum value.
    /// 
    /// The RollingWindow of inputs is created when the indicator is created.
    /// A RollingWindow of WMAs is not saved.  That is up to the caller.
    /// </summary>
    public class WeightedMovingAverage : WindowIndicator<IndicatorDataPoint>
    {
        /// <summary>
        ///     Initializes a new instance of the WeightedMovingAverage class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the WMA</param>
        public WeightedMovingAverage(string name, int period)
            : base(name, period)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the WeightedMovingAverage class with the default name and period
        /// </summary>
        /// <param name="period">The period of the WMA</param>
        public WeightedMovingAverage(int period)
            : this("WMA" + period, period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            decimal smooth = 0m;

            long denominator = 0;
            for (int i = 0; i <= window.Size; i++)
            {
                denominator += i;
            }

            // our first data point just return identity
            if (window.Size == 1)
            {
                return input.Value;
            }
            long index = window.Size;
            long minSizeSamples = (long)Math.Min(window.Size, window.Samples);
            for (long i = 0; i < minSizeSamples; i++)
            {
                decimal x = (index--*window[(int) i]);
                smooth += x;
            }
            //System.Diagnostics.Debug.WriteLine(string.Format("WMA = {0}", (smooth/denominator)));
            return smooth / denominator; 
        }


    }
}
