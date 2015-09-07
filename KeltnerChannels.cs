using System;

namespace QuantConnect.Indicators
{

    /// <summary> 
    /// This indicator creates a moving average (middle band) with an upper band and lower band
    /// fixed at k average true range multiples away from the middle band.  
    /// </summary>
    public class KeltnerChannels : TradeBarIndicator
    {
        /// Set up of variables
        public IndicatorBase<IndicatorDataPoint> MiddleBand { get; private set; }
        public IndicatorBase<TradeBar> UpperBand { get; private set; }
        public IndicatorBase<TradeBar> LowerBand { get; private set; }
        public IndicatorBase<TradeBar> ATR { get; private set; }


        /// <summary>
        /// Initializes a new instance of the KeltnerChannels class
        /// </summary>
        /// <param name="period">The period of the average true range and moving average (middle band)</param>
        /// <param name="k">The number of multiplies specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public KeltnerChannels(int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this(string.Format("KC({0},{1})", period, k), period, k, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the KeltnerChannels class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the average true range and moving average (middle band)</param>
        /// <param name="k">The number of multiples specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public KeltnerChannels(string name, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple)
        : base(name)
        {
            ///Initialise ATR and SMA
            ATR = new AverageTrueRange(name + "_AverageTrueRange", period, movingAverageType);
            MiddleBand = new SimpleMovingAverage(name + "_SimpleMovingAverage", period);

            ///Compute Lower Band
            LowerBand = new FunctionalIndicator<TradeBar>(name + "_LowerBand",
                input => ComputeLowerBand(k, period, input),
                fastStoch => MiddleBand.IsReady,
                () => MiddleBand.Reset()
                );

            ///Compute Upper Band
            UpperBand = new FunctionalIndicator<TradeBar>(name + "_UpperBand",
                input => ComputeUpperBand(k, period, input),
                fastStoch => MiddleBand.IsReady,
                () => MiddleBand.Reset()
                );
        }


        /// <summary>
        /// calculates the lower band
        /// </summary>
        private decimal ComputeLowerBand(decimal k, int period, TradeBar input)
        {
            var lowerBand = MiddleBand.Samples >= period ? MiddleBand - Decimal.Multiply(ATR, k) : new decimal(0.0);

            return lowerBand;
        }

        /// <summary>
        /// calculates the upper band
        /// </summary>
        private decimal ComputeUpperBand(decimal k, int period, TradeBar input)
        {
            var upperBand = MiddleBand.Samples >= period ? MiddleBand + Decimal.Multiply(ATR, k) : new decimal(0.0);

            return upperBand;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return MiddleBand.IsReady && UpperBand.IsReady && LowerBand.IsReady; }
        }
 
        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The TradeBar to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            ATR.Update(input);
            MiddleBand.Update(input.Time, input.Close);
            LowerBand.Update(input);
            UpperBand.Update(input);
            return MiddleBand;
        }

        public override void Reset()
        {
            ATR.Reset();
            MiddleBand.Reset();
            UpperBand.Reset();
            LowerBand.Reset();
            base.Reset();
        }

    }

}
