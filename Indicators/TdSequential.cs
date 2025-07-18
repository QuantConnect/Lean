using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Indicators
{    
    /// <summary>
    /// Represents the TD Sequential indicator, which is used to identify potential trend exhaustion points.
    /// This implementation tracks the setup count and can be extended to handle bullish and bearish setups.
    /// sources:
    /// https://demark.com/sequential-indicator/
    /// https://practicaltechnicalanalysis.blogspot.com/2013/01/tom-demark-sequential.html
    /// https://medium.com/traderlands-blog/tds-td-sequential-indicator-2023-f8675bc5d14
    /// </summary>
    /// <remarks>
    /// The TD Sequential indicator is a popular technical analysis tool that helps traders identify potential reversal points
    /// in the market. It consists of two main components: the setup phase and the countdown phase. The setup phase counts the number of consecutive bars that meet specific criteria, while the countdown phase
    /// counts the number of bars that follow the setup phase to confirm a potential reversal.
    /// This implementation focuses on the setup phase, counting the number of consecutive bars that meet the criteria for a bullish or bearish setup.
    /// </remarks>
    /// <seealso cref="IndicatorBase{T}"/>
    /// <seealso cref="TradeBar"/>
    public class TdSequential : IndicatorBase<TradeBar>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The Max Step count in Setup phase
        /// </summary>
        public const int MaxSetupCount = 9;
        
        /// <summary>
        /// Max Step count in Countdown phase
        /// </summary>
        public const int MaxCountdownCount = 13;
        private decimal Default => EncodeState(TdSequentialPhase.None, 0);

        private readonly List<TradeBar> _bars = [];

        private int _setupCount;
        private int _countdownCount;

        private bool _inBuySetup;
        private bool _inSellSetup;
        private bool _inBuyCountdown;
        private bool _inSellCountdown;

        private decimal _tdstResistance; // higeest high of the 9-bar TD Sequential buy setup (indicates resistance)
        private decimal _tdstSupport; // lowest low of the 9-bar TD Sequential sell setup (indicates support)

        /// <summary>
        /// Creates a new instance of <see cref="TdSequential"/> indicator
        /// </summary>
        /// <param name="name"></param>
        public TdSequential(string name) : base(name) { }

        /// <inheritdoc />
        public override bool IsReady => _bars.Count >= 5;

        /// <inheritdoc />
        public int WarmUpPeriod => 5;

        /// <inheritdoc />
        public override void Reset()
        {
            _bars.Clear();
            _setupCount = 0;
            _countdownCount = 0;
            _inBuySetup = false;
            _inSellSetup = false;
            _inBuyCountdown = false;
            _inSellCountdown = false;
            base.Reset();
        }

        /// <summary>
        /// Computes the next value of the TD Sequential indicator based on the provided <see cref="TradeBar"/>.
        /// </summary>
        /// <param name="input">The current trade bar input.</param>
        /// <returns>The encoded state of the TD Sequential indicator for the current bar.</returns>
        protected override decimal ComputeNextValue(TradeBar input)
        {
            _bars.Add(input);

            if (_bars.Count < 5)
                return Default;

            if (_bars.Count > 10)
                _bars.RemoveAt(0);
            
            var current = _bars[^1];
            var bar4Ago = _bars[^5];
            var bar2Ago = _bars[^3];

            // Initialize setup if nothing is active
            if (!_inBuySetup && !_inSellSetup && !_inBuyCountdown && !_inSellCountdown)
            {
                return InitializeSetupPhase(current, bar4Ago);
            }

            // Buy Setup
            if (_inBuySetup)
            {
                return HandleBuySetupPhase(current, bar4Ago);
            }

            // Sell Setup
            if (_inSellSetup)
            {
                return HandleSellSetupPhase(current, bar4Ago);
            }

            // Buy Countdown
            if (_inBuyCountdown && _bars.Count >= 3)
            {
                return HandleBuyCountDown(current, bar2Ago);
            }

            // Sell Countdown
            if (_inSellCountdown && _bars.Count >= 3)
            {
                return HandleSellCountDown(current, bar2Ago);
            }

            return Default;
        }

        private decimal HandleSellCountDown(TradeBar current, TradeBar bar2Ago)
        {
            if (current.Close >= bar2Ago.High)
            {
                _countdownCount++;
                if (_countdownCount == MaxCountdownCount)
                {
                    _inSellCountdown = false;
                    _countdownCount = 0;

                    return EncodeState(TdSequentialPhase.SellCountdown, MaxCountdownCount);
                }

                return EncodeState(TdSequentialPhase.SellCountdown, _countdownCount);
            }

            if (current.Close < _tdstSupport)
            {
                _inSellCountdown = false;
                _countdownCount = 0;
            }
                
            return Default;
        }

        private decimal HandleBuyCountDown(TradeBar current, TradeBar bar2Ago)
        {
            if (current.Close <= bar2Ago.Low)
            {
                _countdownCount++;
                if (_countdownCount == MaxCountdownCount)
                {
                    _inBuyCountdown = false;
                    _countdownCount = 0;

                    return EncodeState(TdSequentialPhase.BuyCountdown, MaxCountdownCount);
                }

                return EncodeState(TdSequentialPhase.BuyCountdown, _countdownCount);
            }

            if (current.Close > _tdstResistance)
            {
                _inBuyCountdown = false;
                _countdownCount = 0;
            }
            return Default;
        }

        private decimal HandleSellSetupPhase(TradeBar current, TradeBar bar4Ago)
        {
            if (current.Close > bar4Ago.Close)
            {
                _setupCount++;
                if (_setupCount == MaxSetupCount)
                {
                    var isPerfect = IsSellSetupPerfect();
                    _inSellSetup = false;
                    _inSellCountdown = true;
                    _tdstSupport = _bars.Skip(_bars.Count - MaxSetupCount).Take(MaxSetupCount).Min(b => b.Low);
                    _setupCount = 0;

                    return EncodeState(isPerfect ? TdSequentialPhase.SellSetupPerfect : TdSequentialPhase.SellSetup, 9);
                }

                return EncodeState(TdSequentialPhase.SellSetup, _setupCount);
            }

            _inSellSetup = false;
            _setupCount = 0;
            
            return Default;
        }

        private decimal HandleBuySetupPhase(TradeBar current, TradeBar bar4Ago)
        {
            if (current.Close < bar4Ago.Close)
            {
                _setupCount++;
                if (_setupCount == MaxSetupCount)
                {
                    var isPerfect = IsBuySetupPerfect();
                    _inBuySetup = false;
                    _inBuyCountdown = true;
                    _tdstResistance = _bars.Skip(_bars.Count - MaxSetupCount).Take(MaxSetupCount).Max(b => b.High);
                    _setupCount = 0;

                    return EncodeState(isPerfect ? TdSequentialPhase.BuySetupPerfect : TdSequentialPhase.BuySetup, 9);
                }

                return EncodeState(TdSequentialPhase.BuySetup, _setupCount);
            }

            _inBuySetup = false;
            _setupCount = 0;
            
            return Default;
        }

        private decimal InitializeSetupPhase(TradeBar current, TradeBar bar4Ago)
        {
            // Start a new setup based on the current bar compared to the bar 4 days ago
            if (current.Close < bar4Ago.Close)
            {
                _inBuySetup = true;
                _setupCount = 1;

                return EncodeState(TdSequentialPhase.BuySetup, _setupCount);
            }

            if (current.Close > bar4Ago.Close)
            {
                _inSellSetup = true;
                _setupCount = 1;

                return EncodeState(TdSequentialPhase.SellSetup, _setupCount);
            }
            
            return Default;
        }

        private bool IsBuySetupPerfect()
        {
            if (_bars.Count < MaxSetupCount) return false;
            var bar6 = _bars[^4];
            var bar7 = _bars[^3];
            var bar8 = _bars[^2];
            var bar9 = _bars[^1];

            return bar8.Low <= bar6.Low && bar8.Low <= bar7.Low ||
                   bar9.Low <= bar6.Low && bar9.Low <= bar7.Low;
        }

        private bool IsSellSetupPerfect()
        {
            if (_bars.Count < MaxSetupCount) return false;
            var bar6 = _bars[^4];
            var bar7 = _bars[^3];
            var bar8 = _bars[^2];
            var bar9 = _bars[^1];

            return bar8.High >= bar6.High && bar8.High >= bar7.High ||
                   bar9.High >= bar6.High && bar9.High >= bar7.High;
        }

        private decimal EncodeState(TdSequentialPhase phase, int step)
        {
            return (decimal)phase + (step / 100m);
        }

    }

    /// <summary>
    /// Represents the different phases of the TD Sequential indicator.
    /// </summary>
    public enum TdSequentialPhase
    {
        /// <summary>
        /// No active phase.
        /// </summary>
        None = 0,
        /// <summary>
        /// Buy setup phase.
        /// </summary>
        BuySetup = 1,
        /// <summary>
        /// Sell setup phase.
        /// </summary>
        SellSetup = 2,
        /// <summary>
        /// Buy countdown phase.
        /// </summary>
        BuyCountdown = 3,
        /// <summary>
        /// Sell countdown phase.
        /// </summary>
        SellCountdown = 4,
        /// <summary>
        /// Perfect buy setup phase.
        /// </summary>
        BuySetupPerfect = 5,
        /// <summary>
        /// Perfect sell setup phase.
        /// </summary>
        SellSetupPerfect = 6
    }
}
