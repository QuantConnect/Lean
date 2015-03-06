using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Indicators {


    public class Sum : WindowIndicator<IndicatorDataPoint> {
        /// <summary>The sum for the given period</summary>
        private decimal _sum;

        /// <summary>
        ///     Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady {
            get { return Samples >= Period; }
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset() {
            _sum = 0.0m;
            base.Reset();
        }

        /// <summary>
        ///     Initializes a new instance of the Sum class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the SMA</param>
        public Sum(string name, int period)
            : base(name, period)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the Sum class with the default name and period
        /// </summary>
        /// <param name="period">The period of the SMA</param>
        public Sum(int period)
            : this("SUM" + period, period)
        {
        }

        /// <summary>
        ///     Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input) {
            _sum += input.Value;
            if (window.IsReady) {
                _sum -= window.MostRecentlyRemoved.Value;
            }
            return _sum;
        }
    }
}
