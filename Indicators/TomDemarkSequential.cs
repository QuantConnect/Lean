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
    ///  The TomDemark Sequential setup logic requires at least 6 bars to begin evaluating valid price flips and setups
    /// </summary>
    private const int RequiredSamples = 6;

    private static decimal Default => (decimal)TomDemarkSequentialPhase.None;
    private TomDemarkSequentialPhase _currentPhase;
    
    /// <summary>
    /// Gets the current resistance price calculated during a Tom Demark Sequential buy setup.
    /// This is the highest high of the 9-bar setup and can act as a resistance level.
    /// </summary>
    public decimal ResistancePrice { get; private set; }

    /// <summary>
    /// Gets the current support price calculated during a Tom Demark Sequential sell setup.
    /// This is the lowest low of the 9-bar setup and can act as a support level.
    /// </summary>
    public decimal SupportPrice { get; private set; }
    
    /// <summary>
    /// Gets the current Setup step count in the active Tom Demark Sequential (Buy/Sell) Setup phase 
    /// (e.g., 1 to 9 in Setup), 0 if not in setup phase.
    /// </summary>
    public int SetupPhaseStepCount { get; private set; }
    
    /// <summary>
    /// Gets the current Countdown step count in the active TomDemark Sequential (Buy/Sell) Countdown phase
    /// (e.g., 1 to 13 in Setup), 0 if bar is not in Countdown phase.
    /// </summary>
    public int CountdownPhaseStepCount { get; private set; }
    
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
    /// Returns true when at least 6 bars have been received, which is the minimum required
    /// for checking for pre-requisite price flips and for comparing the current close price
    /// to the close price 4 bars ago (used in the setup logic).
    /// </summary>
    public override bool IsReady => Samples >= RequiredSamples;

    /// <summary>
    /// Gets the number of data points (bars) required before the indicator is considered ready.
    /// The TomDemark Sequential setup logic requires at least 6 bars to begin evaluating valid setups.
    /// </summary>
    public override int WarmUpPeriod => RequiredSamples;

    /// <summary>
    /// Resets the indicator to its initial state by clearing all internal counters, flags,
    /// and historical bar data. This allows the indicator to be reused from a clean state.
    /// </summary>
    public override void Reset()
    {
        SetupPhaseStepCount = 0;
        CountdownPhaseStepCount = 0;
        SupportPrice = 0;
        ResistancePrice = 0;
        _currentPhase = TomDemarkSequentialPhase.None;
        base.Reset();
    }

    /// <summary>
    /// Computes the next value of the TD Sequential indicator based on the provided <see cref="TradeBar"/>.
    /// </summary>
    /// <param name="window">The window of data held in this indicator</param>
    /// <param name="current">The current input value to this indicator on this time step</param>
    /// <returns>The TomDemarkSequentialPhase state of the TD Sequential indicator for the current bar.</returns>
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
            var prevBar = window[1]; // previous to current bar
            var prevBar4Ago = window[5]; // 4 bars before prevBar
            return InitializeSetupPhase(current, bar4Ago, prevBar, prevBar4Ago);
        }

        // Buy Setup
        if (IsCurrentPhase(TomDemarkSequentialPhase.BuySetup))
        {
            return HandleBuySetupPhase(current, bar4Ago, bar2Ago, window);
        }

        // Sell Setup
        if (IsCurrentPhase(TomDemarkSequentialPhase.SellSetup))
        {
            return HandleSellSetupPhase(current, bar4Ago, bar2Ago, window);
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
            CountdownPhaseStepCount++;
            if (CountdownPhaseStepCount == MaxCountdownCount)
            {
                _currentPhase = TomDemarkSequentialPhase.None;
            }
            return (decimal)TomDemarkSequentialPhase.SellCountdown;
        }

        if (current.Close < SupportPrice)
        {
            _currentPhase = TomDemarkSequentialPhase.None;
        }

        return Default;
    }

    private decimal HandleBuyCountDown(IBaseDataBar current, IBaseDataBar bar2Ago)
    {
        if (current.Close <= bar2Ago.Low)
        {
            CountdownPhaseStepCount++;
            if (CountdownPhaseStepCount == MaxCountdownCount)
            {
                _currentPhase = TomDemarkSequentialPhase.None;
            }
            return (decimal)TomDemarkSequentialPhase.BuyCountdown;
        }

        if (current.Close > ResistancePrice)
        {
            _currentPhase = TomDemarkSequentialPhase.None;
        }

        return Default;
    }

    private decimal HandleSellSetupPhase(
        IBaseDataBar current,
        IBaseDataBar bar4Ago,
        IBaseDataBar bar2Ago,
        IReadOnlyWindow<IBaseDataBar> window
        )
    {
        if (current.Close > bar4Ago.Close)
        {
            SetupPhaseStepCount++;
            if (SetupPhaseStepCount == MaxSetupCount)
            {
                var isPerfect = IsSellSetupPerfect(window);
                _currentPhase = TomDemarkSequentialPhase.SellCountdown;
                //  Check if the close of bar 9 or current bar is greater than the high two bars earlier
                if (current.Close > bar2Ago.High)
                {
                    CountdownPhaseStepCount = 1;
                }
                SupportPrice = window.Skip(window.Count - MaxSetupCount).Take(MaxSetupCount).Min(b => b.Low);

                return
                    (decimal)(isPerfect
                        ? TomDemarkSequentialPhase.SellSetupPerfect
                        : TomDemarkSequentialPhase.SellSetup);
            }

            return (decimal)TomDemarkSequentialPhase.SellSetup;
        }

        _currentPhase = TomDemarkSequentialPhase.None;
        return Default;
    }

    private decimal HandleBuySetupPhase(
        IBaseDataBar current,
        IBaseDataBar bar4Ago,
        IBaseDataBar bar2Ago,
        IReadOnlyWindow<IBaseDataBar> window
        )
    {
        if (current.Close < bar4Ago.Close)
        {
            SetupPhaseStepCount++;
            if (SetupPhaseStepCount == MaxSetupCount)
            {
                var isPerfect = IsBuySetupPerfect(window);
                _currentPhase = TomDemarkSequentialPhase.BuyCountdown;
                ResistancePrice = window.Skip(window.Count - MaxSetupCount).Take(MaxSetupCount).Max(b => b.High);
                // check the close of bar 9 is less than the low two bars earlier.
                if (current.Close < bar2Ago.Low)
                {
                    CountdownPhaseStepCount = 1;
                }

                return 
                    (decimal)(isPerfect ? TomDemarkSequentialPhase.BuySetupPerfect : TomDemarkSequentialPhase.BuySetup);
            }

            return (decimal)TomDemarkSequentialPhase.BuySetup;
        }

        _currentPhase = TomDemarkSequentialPhase.None;
        return Default;
    }

    private decimal InitializeSetupPhase(
        IBaseDataBar current,
        IBaseDataBar bar4Ago,
        IBaseDataBar prevBar,
        IBaseDataBar prevBar4Ago
        )
    {
        // Bearish flip: prev close > previous's close[4 bars ago] and current close < close[4 bars ago]
        if (prevBar.Close > prevBar4Ago.Close && current.Close < bar4Ago.Close)
        {
            _currentPhase = TomDemarkSequentialPhase.BuySetup;
            SetupPhaseStepCount = 1;

            return (decimal)TomDemarkSequentialPhase.BuySetup;
        }

        // Bullish flip: prev close < previous's close[4 bars ago] and current close > close[4 bars ago]
        if (prevBar.Close < prevBar4Ago.Close && current.Close > bar4Ago.Close)
        {
            _currentPhase = TomDemarkSequentialPhase.SellSetup;
            SetupPhaseStepCount = 1;

            return (decimal)TomDemarkSequentialPhase.SellSetup;
        }

        return Default;
    }

    private static bool IsBuySetupPerfect(IReadOnlyWindow<IBaseDataBar> window)
    {
        var bar6 = window[3];
        var bar7 = window[2];
        var bar8 = window[1];
        var bar9 = window[0];

        return bar8.Low < bar6.Low && bar8.Low < bar7.Low ||
            bar9.Low < bar6.Low && bar9.Low < bar7.Low;
    }

    private static bool IsSellSetupPerfect(IReadOnlyWindow<IBaseDataBar> window)
    {
        var bar6 = window[3];
        var bar7 = window[2];
        var bar8 = window[1];
        var bar9 = window[0];

        return bar8.High > bar6.High && bar8.High > bar7.High ||
            bar9.High > bar6.High && bar9.High > bar7.High;
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
