using System;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Vortex Indicator (VI) measures the strength of a trend and its potential continuation or reversal.
    /// It is calculated using the highs, lows, and closing prices over a specified period.
    /// </summary>
    public class VortexIndicator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly RollingWindow<IBaseDataBar> _window;
        private IndicatorBase<IndicatorDataPoint> _positiveVortexIndicator;
        private IndicatorBase<IndicatorDataPoint> _negativeVortexIndicator;

        /// <summary>
        /// Indicates when the indicator is fully initialized.
        /// </summary>
        public override bool IsReady => _window.IsReady;

        /// <summary>
        /// Gets the Positive Vortex Indicator (+VI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> PositiveVortexIndicator => _positiveVortexIndicator;

        /// <summary>
        /// Gets the Negative Vortex Indicator (-VI).
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> NegativeVortexIndicator => _negativeVortexIndicator;

        /// <summary>
        /// The number of bars required for the indicator to be ready.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the VortexIndicator class.
        /// </summary>
        /// <param name="period">The lookback period over which the indicator will compute.</param>
        public VortexIndicator(int period)
            : this($"VI({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the VortexIndicator class with a name.
        /// </summary>
        /// <param name="name">The name of the indicator.</param>
        /// <param name="period">The lookback period over which the indicator will compute.</param>
        public VortexIndicator(string name, int period)
            : base(name)
        {
            _period = period;
            _window = new RollingWindow<IBaseDataBar>(period);

            _positiveVortexIndicator = new FunctionalIndicator<IndicatorDataPoint>(name + "_PositiveVortexIndicator",
                input => ComputeVortexIndicator(true),
                isReady => IsReady);

            _negativeVortexIndicator = new FunctionalIndicator<IndicatorDataPoint>(name + "_NegativeVortexIndicator",
                input => ComputeVortexIndicator(false),
                isReady => IsReady);
        }

        /// <summary>
        /// Calculates the Vortex Indicator for the given direction.
        /// </summary>
        /// <param name="isPositive">Determines whether to compute the positive or negative Vortex Indicator.</param>
        /// <returns>The computed Vortex Indicator value.</returns>
        private decimal ComputeVortexIndicator(bool isPositive)
        {
            if (_window.Count < _period) return 0;

            decimal sumVM = 0;
            decimal sumTR = 0;
            for (int i = 0; i < _period - 1; i++)
            {
                var currentBar = _window[i];
                var previousBar = _window[i + 1];
                if (isPositive)
                {
                    sumVM += Math.Abs(currentBar.High - previousBar.Low);
                }
                else
                {
                    sumVM += Math.Abs(currentBar.Low - previousBar.High);
                }
                sumTR += ComputeTrueRange(currentBar, previousBar);
            }

            return sumTR != 0 ? sumVM / sumTR : 0;
        }

        /// <summary>
        /// Computes the True Range.
        /// </summary>
        /// <param name="currentBar">The current bar data.</param>
        /// <param name="previousBar">The previous bar data.</param>
        /// <returns>The True Range value.</returns>
        private decimal ComputeTrueRange(IBaseDataBar currentBar, IBaseDataBar previousBar)
        {
            return Math.Max(currentBar.High - currentBar.Low, Math.Max(
                Math.Abs(currentBar.High - previousBar.Close),
                Math.Abs(currentBar.Low - previousBar.Close)));
        }

        /// <summary>
        /// Updates the indicators with the next data point.
        /// </summary>
        /// <param name="input">The input data for the indicators.</param>
        /// <returns>The indicator value after the update.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            _window.Add(input);

            if (!IsReady) return 0;

            _positiveVortexIndicator.Update(new IndicatorDataPoint(input.EndTime, ComputeVortexIndicator(true)));
            _negativeVortexIndicator.Update(new IndicatorDataPoint(input.EndTime, ComputeVortexIndicator(false)));

            return (_positiveVortexIndicator.Current.Value + _negativeVortexIndicator.Current.Value) / 2;
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators to their initial state.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _window.Reset();
            _positiveVortexIndicator.Reset();
            _negativeVortexIndicator.Reset();
        }
    }
}
