using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator computes the n-period downside population standard deviation.
    /// </summary>
    public class StandardDownsideDeviation : StandardDeviation
    {
        /// <summary>
        /// Maximum acceptable return (MAR) for standard downside deviation calculation
        /// </summary>
        private readonly decimal _mar;

        /// <summary>
        /// Initializes a new instance of the StandardDownsideDeviation class with the specified period.
        ///
        /// Evaluates the standard downside deviation of samples in the look-back period. 
        /// On a data set of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="period">The sample size of the standard downside deviation</param>
        /// <param name="mar">Maximum acceptable return (MAR) for standard downside deviation calculation</param>
        public StandardDownsideDeviation(int period, decimal mar = 0.0m)
            : this($"STDD({period})", period, mar)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StandardDownsideDeviation class with the specified name and period.
        /// 
        /// Evaluates the standard downside deviation of samples in the look-back period.
        /// On a data set of size N will use an N normalizer and would thus be biased if applied to a subset.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The sample size of the standard downside deviation</param>
        /// <param name="mar">Maximum acceptable return (MAR) for standard downside deviation calculation</param>
        public StandardDownsideDeviation(string name, int period, decimal mar = 0.0m)
            : base(name, period)
        {
            _mar = mar;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <param name="window">The window for the input history</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            var n = Math.Min(Period, Samples);

            //Only consider values who's valueDelta - _mar < 0. Only take downside values
            var valueDelta = n > Period ? (input.Value - window[0].Value) / window[0].Value : Decimal.MaxValue;
            return valueDelta - _mar < 0 ? base.ComputeNextValue(window, input) : 0m;
        }
    }
}
