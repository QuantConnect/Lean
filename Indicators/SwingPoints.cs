/*
 * |Swing Points|
 * This indicator will return: 
 *   -> 1 for a swing up, -1 for a swing down.  This is the primary output value of the indicator
 *   -> Current swing high and swing
 *   -> The previous 3 swing high and swing lows 
 *
*/

using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator will determine whether a new swing low or high has occurred.  This is the primary output.
    /// The value of the swing high and swing low and the three previous high and low swing points is also available
    /// </summary>
    public class SwingPoints : TradeBarIndicator
    {

        /// <summary>
        /// The current TradeBar
        /// </summary>
        private TradeBar CurrentBar;

        /// <summary>
        /// The current swing direction
        /// </summary>
        private IndicatorBase<TradeBar> CurrentSwingDirection;

        /// <summary>
        /// The current swing high
        /// </summary>
        public decimal CurrentSwingHigh
        {
            get; private set;
        }

        /// <summary>
        /// The current swing low
        /// </summary>
        public decimal CurrentSwingLow
        {
            get; private set;
        }

        /// <summary>
        /// The list holds the last three swing highs
        /// </summary>
        private RollingWindow<decimal> SwingHighs;


        /// <summary>
        /// The list holds the last three swing lows
        /// </summary>
        private RollingWindow<decimal> SwingLows;

        /// <summary>
        /// The decimal holds the percent swing filter
        /// </summary>
        private decimal SwingFilterPercentage;

        /// <summary>
        /// First scan bool
        /// </summary>
        private bool FirstScan = true;

        /// <summary>
        /// Used to hold the current high 
        /// </summary>
        private decimal CurrentHigh;

        /// <summary>
        /// Used to hold the current lows 
        /// </summary>
        private decimal CurrentLow;

        /// <summary>
        /// Trend direction, -1 = up, 1 = down 
        /// </summary>
        private decimal _direction;


        /// <summary>
        /// Initializes a new instance of the <see cref="SwingPoints"/> class.
        /// </summary>
        public SwingPoints()
            : this(string.Format("Swing({0})", 0), 0m)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwingPoints"/> class.
        /// </summary>
        /// <param name="k">The swing filter percentage used in the calculation</param>
        public SwingPoints(decimal k)
            : this(string.Format("Swing({0})", k), k)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwingPoints"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="k">The swing filter percentage used in the calculation</param>
        public SwingPoints(string name, decimal k)
            : base(name)
        {
            //intialise
            SwingFilterPercentage = k;
            SwingHighs = new RollingWindow<decimal>(3);
            SwingLows = new RollingWindow<decimal>(3);

            CurrentSwingDirection = new FunctionalIndicator<TradeBar>(name + "_SwingBar",
                input => CalculateSwing(input),
                ready => true,
                () => CurrentSwingDirection.Reset()
                );
        }


        /// <summary>
        /// Calculates where has been a swing
        /// </summary>
        private decimal CalculateSwing(TradeBar input)
        {

            var _swingfilter = Decimal.Divide(SwingFilterPercentage, 100m);
            var _high = input.High;
            var _low = input.Low;
            var _close = input.Close;
            var _open = input.Open;

            if (_direction == 1)
            {
                if (_low < (CurrentHigh - Decimal.Multiply(_swingfilter, CurrentHigh)))
                {
                    _direction = -1m;
                    //Add to list of swing highs
                    SwingHighs.Add(CurrentSwingHigh);
                    //Update current swing
                    CurrentSwingHigh = CurrentHigh;
                    CurrentLow = _low;
                    //return swing direction
                    return -1m;
                }
                else if (_high > CurrentHigh)
                {
                    CurrentHigh = _high;
                    //return swing direction
                    return 0;
                }
            }
            ///Searches for a reversal then will search for a new low
            else if (_direction == -1)
            {

                if (_high > (CurrentLow - Decimal.Multiply(_swingfilter, CurrentLow)))
                {
                    _direction = 1m;
                    //Add to list of swing lows
                    SwingLows.Add(CurrentSwingLow);
                    //Update current swing
                    CurrentSwingLow = CurrentLow;
                    CurrentHigh = _high;
                    //return swing direction
                    return 1m;
                }
                else if (_low < CurrentLow)
                {
                    CurrentLow = _low;
                    //return swing direction
                    return 0;
                }
            }
            return 0;

        }

        /// <summary>
        /// Returns the decimal value of the nominated swing high
        /// </summary>
        public decimal GetSwingHighs(int index)
        {
            try
            {
                return SwingHighs[index];
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the decimal value of the nominated swing low
        /// </summary>
        public decimal GetSwingLows(int index)
        {
            try
            {
                return SwingLows[index];
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return CurrentSwingDirection.IsReady; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The TradeBar input given to the indicator</param>
        /// <returns>A new value for this indicator, which by convention is the mean value of the upper band and lower band.</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {

            CurrentBar = input;
            //Sets the initial values for the swing detection
            if (FirstScan)
            {
                CurrentHigh = CurrentBar.High;
                CurrentLow = CurrentBar.Low;
                CurrentSwingHigh = CurrentBar.High;
                CurrentSwingLow = CurrentBar.Low;
                _direction = (CurrentBar.Close > (CurrentBar.High + CurrentBar.Low) / 2) ? -1m : 1m;
                FirstScan = false;
            }
            CurrentSwingDirection.Update(input);
            return CurrentSwingDirection;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            CurrentSwingDirection.Reset();
            SwingHighs.Reset();
            SwingLows.Reset();
        }

    }
}
