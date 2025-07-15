using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the TD Sequential indicator, which is used to identify potential trend exhaustion points.
    /// This implementation tracks the setup count and can be extended to handle bullish and bearish setups.
    /// soourceS: 
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
    
    public class TdSequential : IndicatorBase<TradeBar>
    {
        private readonly List<TradeBar> _window = [];
        private int _setupCount;
        private bool _isBullish = true; // You can later parameterize this

        public TdSequential(string name)
            : base(name)
        {
        }

        public override bool IsReady => _window.Count >= 4;

        public int SetupCount => _setupCount;

        public override void Reset()
        {
            _window.Clear();
            _setupCount = 0;
            base.Reset();
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            _window.Add(input);
            if (_window.Count > 4)
            {
                var current = _window[^1];
                var previous = _window[^5];

                if (_isBullish)
                {
                    if (current.Close > previous.Close)
                    {
                        _setupCount++;
                        if (_setupCount == 9)
                        {
                            // Setup complete
                            OnSetupComplete(input, bullish: true);
                        }
                    }
                    else
                    {
                        _setupCount = 0;
                    }
                }
                else
                {
                    if (current.Close < previous.Close)
                    {
                        _setupCount++;
                        if (_setupCount == 9)
                        {
                            OnSetupComplete(input, bullish: false);
                        }
                    }
                    else
                    {
                        _setupCount = 0;
                    }
                }

                if (_window.Count > 5)
                    _window.RemoveAt(0);
            }

            return _setupCount;
        }

        protected virtual void OnSetupComplete(TradeBar input, bool bullish)
        {
            // This method can be overridden to handle the setup completion event
            // For example, you can log or trigger an event here
            // Giri: determine if the setup is bullish or bearish
            Logging.Log.Trace($"TD Sequential setup completed at {input.Time} with count {_setupCount}. Bullish: {bullish}");
        }
    }
}