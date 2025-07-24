/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace QuantConnect.Indicators;

using System.Linq;
using QuantConnect.Data.Market;

/// <summary>
/// Represents the Tom Demark Sequential indicator, which is used to identify potential trend exhaustion points.
/// This implementation tracks both the setup and countdown phases, and can be extended to handle bullish and bearish setups,
/// as well as qualifiers such as Perfect Setups and Countdown completions.
/// 
/// - **Setup Phase**: Detects a trend by counting 9 consecutive bars where the close is less than (Buy Setup)
///   or greater than (Sell Setup) the close 4 bars earlier.
/// - **TDST Support/Resistance**: After a valid 9-bar setup completes, the indicator records the **lowest low** (for Sell Setup)
///   or **highest high** (for Buy Setup) among the 9 bars. These are referred to as Support Price(for Buy Setup) and
///   Resistance Price (for Sell Setup), and serve as critical thresholds that validate the continuation of the countdown phase.
/// - **Countdown Phase**: Once a valid setup is completed, the indicator attempts to count 13 qualifying bars (not necessarily consecutive)
///   where the close is less than (Buy Countdown) or greater than (Sell Countdown) the low/high 2 bars earlier.
///   During this phase, the TDST Support/Resistance levels are checked — if the price breaks these levels, the countdown phase is reset,
///   as the trend reversal condition is considered invalidated.
/// </summary>
/// <remarks>
/// This implementation uses a rolling window of 10 bars and begins logic when at least 5 bars are available.
/// </remarks>
/// <seealso cref="IBaseDataBar"/>
/// <seealso href="https://demark.com/sequential-indicator/"/>
/// <seealso href="https://practicaltechnicalanalysis.blogspot.com/2013/01/tom-demark-sequential.html"/>
/// <seealso href="https://medium.com/traderlands-blog/tds-td-sequential-indicator-2023-f8675bc5d14"/>

public class TomDemarkSequential : WindowIndicator<IBaseDataBar>
{
    /// <summary>
    /// The fixed number of bars used in the setup phase, as defined by the Tom DeMark Sequential (TD Sequential) indicator.
    /// According to the TomDemark Sequential rules, a setup consists of exactly 9 consecutive qualifying bars.
    /// </summary>
    private const int MaxSetupCount = 9;

    /// <summary>
    /// The fixed number of bars used in the countdown phase, per the Tom Demark Sequential methodology.
    /// A countdown is completed after exactly 13 qualifying bars.
    /// These values are not configurable as they are fundamental to the algorithm's definition.
    /// </summary>
    private const int MaxCountdownCount = 13;
    
    /// <summary>
    /// The windows of size 10 bars are enough and begin logic when at least 5 bars are available.
    /// </summary>
    private const int WindowSize = 10;

    /// <summary>
    ///  The TomDemark Sequential setup logic requires at least 5 bars to begin evaluating valid setups.
    /// </summary>
    private const int RequiredSamples = 5;

    private static decimal Default => EncodeState(TomDemarkSequentialPhase.None, 0);

    private int _stepCount;

    private TomDemarkSequentialPhase _currentPhase;
    
    private decimal _resistancePrice; // highest high of the 9-bar Tom Demark Sequential buy setup (indicates resistance)
    private decimal _supportPrice; // lowest low of the 9-bar Tom Demark Sequential sell setup (indicates support)

    /// <summary>
    /// Initializes a new instance of the <see cref="TomDemarkSequential"/> indicator.
    /// </summary>
    /// <param name="name">The name of the indicator.</param>
    public TomDemarkSequential(string name = "TomDemarkSequential")
        : base(name, WindowSize)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the indicator is ready and fully initialized.
    /// Returns true when at least 5 bars have been received, which is the minimum required
    /// for comparing the current close price to the close price 4 bars ago (used in the setup logic).
    /// </summary>
    public override bool IsReady => Samples >= RequiredSamples;

    /// <summary>
    /// Gets the number of data points (bars) required before the indicator is considered ready.
    /// The TomDemark Sequential setup logic requires at least 5 bars to begin evaluating valid setups.
    /// </summary>
    public override int WarmUpPeriod => RequiredSamples;

    /// <summary>
    /// Resets the indicator to its initial state by clearing all internal counters, flags,
    /// and historical bar data. This allows the indicator to be reused from a clean state.
    /// </summary>
    public override void Reset()
    {
        ResetStepAndPhase();
        base.Reset();
    }

    /// <summary>
    /// Computes the next value of the TD Sequential indicator based on the provided <see cref="TradeBar"/>.
    /// </summary>
    /// <param name="window">The window of data held in this indicator</param>
    /// <param name="current">The current input value to this indicator on this time step</param>
    /// <returns>The encoded state of the TD Sequential indicator for the current bar.</returns>
    protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar current)
    {
        if (!IsReady)
        {
            return Default;
        }

        var bar4Ago = window[4];
        var bar2Ago = window[2];

        // Initialize setup if nothing is active
        if (IsCurrentPhase(TomDemarkSequentialPhase.None))
        {
            return InitializeSetupPhase(current, bar4Ago);
        }

        // Buy Setup
        if (IsCurrentPhase(TomDemarkSequentialPhase.BuySetup))
        {
            return HandleBuySetupPhase(current, bar4Ago, window);
        }

        // Sell Setup
        if (IsCurrentPhase(TomDemarkSequentialPhase.SellSetup))
        {
            return HandleSellSetupPhase(current, bar4Ago, window);
        }

        // Buy Countdown
        if (IsCurrentPhase(TomDemarkSequentialPhase.BuyCountdown))
        {
            return HandleBuyCountDown(current, bar2Ago);
        }

        // Sell Countdown
        if (IsCurrentPhase(TomDemarkSequentialPhase.SellCountdown))
        {
            return HandleSellCountDown(current, bar2Ago);
        }

        return Default;
    }

    private bool IsCurrentPhase(TomDemarkSequentialPhase phase)
    {
        return _currentPhase == phase;
    }

    private decimal HandleSellCountDown(IBaseDataBar current, IBaseDataBar bar2Ago)
    {
        if (current.Close >= bar2Ago.High)
        {
            _stepCount++;
            if (_stepCount == MaxCountdownCount)
            {
                ResetStepAndPhase();
                return EncodeState(TomDemarkSequentialPhase.SellCountdown, MaxCountdownCount);
            }
            return EncodeState(TomDemarkSequentialPhase.SellCountdown, _stepCount);
        }

        if (current.Close < _supportPrice)
        {
            ResetStepAndPhase();
        }

        return Default;
    }

    private decimal HandleBuyCountDown(IBaseDataBar current, IBaseDataBar bar2Ago)
    {
        if (current.Close <= bar2Ago.Low)
        {
            _stepCount++;
            if (_stepCount == MaxCountdownCount)
            {
                ResetStepAndPhase();
                return EncodeState(TomDemarkSequentialPhase.BuyCountdown, MaxCountdownCount);
            }
            return EncodeState(TomDemarkSequentialPhase.BuyCountdown, _stepCount);
        }

        if (current.Close > _resistancePrice)
        {
            ResetStepAndPhase();
        }

        return Default;
    }

  
    private decimal HandleSellSetupPhase(
        IBaseDataBar current,
        IBaseDataBar bar4Ago,
        IReadOnlyWindow<IBaseDataBar> window
        )
    {
        if (current.Close > bar4Ago.Close)
        {
            _stepCount++;
            if (_stepCount == MaxSetupCount)
            {
                var isPerfect = IsSellSetupPerfect(window);
                _currentPhase = TomDemarkSequentialPhase.SellCountdown;
                _supportPrice = window.Skip(window.Count - MaxSetupCount).Take(MaxSetupCount).Min(b => b.Low);
                _stepCount = 0;

                return EncodeState(
                    isPerfect ? TomDemarkSequentialPhase.SellSetupPerfect : TomDemarkSequentialPhase.SellSetup, MaxSetupCount);
            }

            return EncodeState(TomDemarkSequentialPhase.SellSetup, _stepCount);
        }
        
        ResetStepAndPhase();
        return Default;
    }

    private decimal HandleBuySetupPhase(
        IBaseDataBar current,
        IBaseDataBar bar4Ago,
        IReadOnlyWindow<IBaseDataBar> window
        )
    {
        if (current.Close < bar4Ago.Close)
        {
            _stepCount++;
            if (_stepCount == MaxSetupCount)
            {
                var isPerfect = IsBuySetupPerfect(window);
                _currentPhase = TomDemarkSequentialPhase.BuyCountdown;
                _resistancePrice = window.Skip(window.Count - MaxSetupCount).Take(MaxSetupCount).Max(b => b.High);
                _stepCount = 0;

                return EncodeState(
                    isPerfect ? TomDemarkSequentialPhase.BuySetupPerfect : TomDemarkSequentialPhase.BuySetup, MaxSetupCount);
            }

            return EncodeState(TomDemarkSequentialPhase.BuySetup, _stepCount);
        }

        ResetStepAndPhase();
        return Default;
    }

    private decimal InitializeSetupPhase(IBaseDataBar current, IBaseDataBar bar4Ago)
    {
        if (current.Close < bar4Ago.Close)
        {
            _currentPhase = TomDemarkSequentialPhase.BuySetup;
            _stepCount = 1;

            return EncodeState(TomDemarkSequentialPhase.BuySetup, _stepCount);
        }

        if (current.Close > bar4Ago.Close)
        {
            _currentPhase = TomDemarkSequentialPhase.SellSetup;
            _stepCount = 1;

            return EncodeState(TomDemarkSequentialPhase.SellSetup, _stepCount);
        }

        return Default;
    }

    private void ResetStepAndPhase()
    {
        _stepCount = 0;
        _currentPhase = TomDemarkSequentialPhase.None;
    }
    private static bool IsBuySetupPerfect(IReadOnlyWindow<IBaseDataBar> window)
    {
        var bar6 = window[3];
        var bar7 = window[2];
        var bar8 = window[1];
        var bar9 = window[0];

        return bar8.Low <= bar6.Low && bar8.Low <= bar7.Low ||
            bar9.Low <= bar6.Low && bar9.Low <= bar7.Low;
    }

    private static bool IsSellSetupPerfect(IReadOnlyWindow<IBaseDataBar> window)
    {
        var bar6 = window[3];
        var bar7 = window[2];
        var bar8 = window[1];
        var bar9 = window[0];

        return bar8.High >= bar6.High && bar8.High >= bar7.High ||
            bar9.High >= bar6.High && bar9.High >= bar7.High;
    }

    private static decimal EncodeState(TomDemarkSequentialPhase phase, int step)
    {
        return (decimal)phase + (step / 100m);
    }
}
/// <summary>
/// Represents the different phases of the TomDemark Sequential indicator.
/// </summary>
public enum TomDemarkSequentialPhase
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
