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
        private IndicatorBase<IndicatorDataPoint> _maxMA;
        private IndicatorBase<IndicatorDataPoint> _minMA;
        private decimal _lastHigh;
        private decimal _lastLow;

        /// <summary>
        /// Initializes a new instance of the DeMarkerIndicator class with the specified period
        /// </summary>
        /// <param name="period">The period of the  DeMarker Indicator</param>
        /// <param name="type">The type of moving average to use in calculations</param>
        public DeMarkerIndicator(int period, MovingAverageType type = MovingAverageType.Simple)
            : this($"DeM({period},{type})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DeMarkerIndicator class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the  DeMarker Indicator</param>
        /// <param name="type">The type of moving average to use in calculations</param>
        public DeMarkerIndicator(string name, int period, MovingAverageType type = MovingAverageType.Simple)
            : base(name)
        {
            var _lastHigh = 0m;
            var _lastLow = 0m;
            WarmUpPeriod = period;
            _maxMA = type.AsIndicator(period);
            _minMA = type.AsIndicator(period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _maxMA.IsReady && _minMA.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _maxMA.Reset();
            _minMA.Reset();
            base.Reset();
        }


        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var deMax = 0m;
            var deMin = 0m;
            if (Samples > 1)
            {
                // By default, DeMin and DeMax must be 0m initially
                deMax = Math.Max(input.High - _lastHigh, 0);
                deMin = Math.Max(_lastLow - input.Low, 0);
            }

            _maxMA.Update(input.Time, deMax);
            _minMA.Update(input.Time, deMin);
            _lastHigh = input.High;
            _lastLow = input.Low;

            if (!IsReady)
            {
                return 0m;
            }

            var currentValue = _maxMA + _minMA;
            return currentValue > 0m ? _maxMA / currentValue : 0m;
        }
    }
}
