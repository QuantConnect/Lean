namespace QuantConnect.Indicators
{
    /// <summary>
    ///     The Detrended Price Oscillator is an indicator designed to remove trend from price and make it easier to identify
    ///     cycles.
    ///     DPO does not extend to the last date because it is based on a displaced moving average.
    ///     Is estimated as Price {X/2 + 1} periods ago less the X-period simple moving average.
    ///     E.g.DPO(20) equals price 11 days ago less the 20-day SMA.
    /// </summary>
    /// <seealso cref="QuantConnect.Indicators.IndicatorBase{QuantConnect.Indicators.IndicatorDataPoint}" />
    public class DetrendedPriceOscillator : IndicatorBase<IndicatorDataPoint>
    {
        private readonly Delay _priceLag;
        private readonly SimpleMovingAverage _sma;


        /// <summary>
        ///     Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _sma.IsReady && _priceLag.IsReady;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DetrendedPriceOscillator" /> class.
        /// </summary>
        /// <param name="name">The name for the indicator.</param>
        /// <param name="period">The number of periods to calculate the DPO.</param>
        public DetrendedPriceOscillator(string name, int period) : base(name)
        {
            var lagPeriod = period / 2 + 1;
            _priceLag = new Delay(lagPeriod);
            _sma = new SimpleMovingAverage(period);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DetrendedPriceOscillator" /> class.
        /// </summary>
        /// <param name="period">The number of periods to calculate the DPO.</param>
        public DetrendedPriceOscillator(int period) : this("DPO" + period, period)
        { }

        /// <summary>
        ///     Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _priceLag.Reset();
            _sma.Reset();
        }

        /// <summary>
        ///     Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>
        ///     A new value for this indicator
        /// </returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _priceLag.Update(input);
            _sma.Update(input);
            return IsReady ? _priceLag - _sma : decimal.Zero;
        }
    }
}