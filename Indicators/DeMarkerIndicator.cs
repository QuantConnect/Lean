using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// In the DeMarker strategy, for some period of size N, set:
    /// <para>
    /// DeMax = High - Previous High, and 
    /// DeMin = Previous Low - Low
    /// </para>
    /// where, in the prior, if either term is less than zero (DeMax or DeMin), set it to zero.
    /// We can now define the indicator itself, DeM, as:
    ///<para>
    /// DeM = MA(DeMax)/(MA(DeMax)+MA(DeMin))
    ///</para>
    /// where MA denotes a Moving Average of period N.
    /// 
    /// https://www.investopedia.com/terms/d/demarkerindicator.asp
    /// </summary>
    public class DeMarkerIndicator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private decimal LastHigh;
        private decimal LastLow;
        private decimal DeMin;
        private decimal DeMax;
        private IndicatorBase<IndicatorDataPoint> _MAmax;
        private IndicatorBase<IndicatorDataPoint> _MAmin;
        private int _period;


        /// <summary>
        /// Initializes a new instance of the DeMarkerIndicator class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the  DeMarker Indicator</param>
        /// <param name="type">The type of moving average to use in calculations</param>
        public DeMarkerIndicator(string name, int period, MovingAverageType type = MovingAverageType.Simple )
            : base(name)
        {
            LastHigh = 0m;
            LastLow = 0m;
            _period = period;
            _MAmax = type.AsIndicator(period);
            _MAmin = type.AsIndicator(period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _MAmax.IsReady && _MAmin.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            LastHigh = 0m;
            LastLow = 0m;
            DeMin = 0;
            DeMax = 0;
            _MAmax.Reset();
            _MAmin.Reset();
            base.Reset();
        }


        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (Samples > 1)
            {
                // By default, DeMin and DeMax must be 0m initially
                DeMax = Math.Max(input.High - LastHigh, 0);
                DeMin = Math.Max(LastLow - input.Low, 0);
            }

            _MAmax.Update(input.Time, DeMax);
            _MAmin.Update(input.Time, DeMin);
            LastHigh = input.High;
            LastLow = input.Low;
            if (IsReady)
            {
                var currentValue = _MAmax.Current.Value + _MAmin.Current.Value;
                var DeMark = currentValue > 0m ? _MAmax.Current.Value / currentValue : 0m;
                return DeMark;
            }

            return 0m;
        }
    }
}
