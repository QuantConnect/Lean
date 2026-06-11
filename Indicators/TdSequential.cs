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
/// Represents the Tom DeMark (TD) Sequential indicator, a technical analysis tool used to identify
/// potential price exhaustion points and trend reversals. The indicator operates in two phases:
/// Setup and Countdown.
/// 
/// <para>
/// <b>Setup Phase:</b> Counts 9 consecutive bars where the close is less than (Buy Setup)
/// or greater than (Sell Setup) the close 4 bars ago. When a setup completes, a support
/// or resistance level is established and the indicator transitions to the countdown phase.
/// </para>
/// 
/// <para>
/// <b>Countdown Phase:</b> After a valid setup, counts 13 bars (not necessarily consecutive)
/// where the close is compared to the low (Buy Countdown) or high (Sell Countdown)
/// from 2 bars ago. When the countdown completes, it signals potential trend exhaustion.
/// </para>
/// 
/// <para>
/// This implementation uses a rolling window of 10 bars and requires at least 6 data points
/// before producing valid signals. The window size (10) is sufficient because the indicator
/// needs to reference bars up to 4 periods back (for setup comparison) and 2 periods back
/// (for countdown comparison).
/// </para>
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>Setup requires exactly 9 consecutive qualifying bars</item>
///   <item>Countdown requires exactly 13 qualifying bars (not necessarily consecutive)</item>
///   <item>Support/Resistance levels are calculated from the 9-bar setup period</item>
///   <item>Perfect setups occur when specific price relationships exist among bars 6-9</item>
/// </list>
/// </remarks>
/// <seealso cref="IBaseDataBar"/>
/// <seealso href="https://demark.com/sequential-indicator/"/>
/// <seealso href="https://oxfordstrat.com/trading-strategies/td-sequential-1/"/>
public class TdSequential : WindowIndicator<IBaseDataBar>
{
    /// <summary>
    /// The number of consecutive bars required to complete a Setup phase.
    /// Per the Tom DeMark Sequential methodology, a setup consists of exactly 9 consecutive qualifying bars.
    /// </summary>
    private const int MaxSetupCount = 9;

    /// <summary>
    /// The number of qualifying bars required to complete a Countdown phase.
    /// Per the Tom DeMark Sequential methodology, a countdown requires exactly 13 qualifying bars.
    /// These are not required to be consecutive.
    /// </summary>
    private const int MaxCountdownCount = 13;

    /// <summary>
    /// The fixed window size used by this indicator. A window of 10 bars is sufficient
    /// because the algorithm needs to compare the current bar against bars 4 and 2 periods ago,
    /// and the setup initialization logic requires bar 5 ago as well.
    /// </summary>
    private const int WindowSize = 10;

    /// <summary>
    /// The minimum number of samples required before the indicator begins computing values.
    /// The Tom DeMark Sequential setup logic requires at least 6 bars to begin evaluating
    /// valid price flips and setups (bar 0 for current, bar 1 for previous, bar 4 for comparison,
    /// and bar 5 for previous bar's comparison).
    /// </summary>
    private const int RequiredSamples = 6;

    /// <summary>
    /// The default value to return when no phase is active.
    /// </summary>
    private static readonly decimal DefaultPhaseValue = (decimal)TdSequentialPhase.None;

    /// <summary>
    /// The current phase of the TD Sequential indicator.
    /// </summary>
    private TdSequentialPhase _currentPhase;

    /// <summary>
    /// Gets the current count of consecutive qualifying bars in the Setup phase.
    /// Valid range: 1-9 during an active setup phase, 0 otherwise.
    /// When this reaches 9, <see cref="IsSetupComplete"/> becomes <c>true</c>.
    /// </summary>
    public int SetupCount { get; private set; }

    /// <summary>
    /// Gets the current count of qualifying bars in the Countdown phase.
    /// Valid range: 1-13 during an active countdown phase, 0 otherwise.
    /// When this reaches 13, <see cref="IsCountdownComplete"/> becomes <c>true</c>.
    /// </summary>
    public int CountdownCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the 9-bar Setup phase has completed.
    /// This is <c>true</c> when <see cref="SetupCount"/> equals <see cref="MaxSetupCount"/> (9).
    /// </summary>
    public bool IsSetupComplete => SetupCount == MaxSetupCount;

    /// <summary>
    /// Gets a value indicating whether the 13-bar Countdown phase has completed.
    /// This is <c>true</c> when <see cref="CountdownCount"/> equals <see cref="MaxCountdownCount"/> (13).
    /// A completed countdown signals potential trend exhaustion and a possible reversal.
    /// </summary>
    public bool IsCountdownComplete => CountdownCount == MaxCountdownCount;

    /// <summary>
    /// Gets the current trading signal produced by the indicator.
    /// </summary>
    /// <value>
    /// A <see cref="TdSequentialSignal"/> value indicating the current signal.
    /// <see cref="TdSequentialSignal.Buy"/> when a buy setup or buy countdown is active,
    /// <see cref="TdSequentialSignal.Sell"/> when a sell setup or sell countdown is active,
    /// and <see cref="TdSequentialSignal.None"/> when no signal is active.
    /// </value>
    public TdSequentialSignal Signal { get; private set; }

    /// <summary>
    /// Gets the support price level calculated during a Sell Setup.
    /// This is the lowest low among the 9 bars of the completed setup period
    /// and serves as a critical threshold during the countdown phase.
    /// If the price falls below this level during a Sell Countdown, the countdown is invalidated.
    /// </summary>
    public decimal SupportPrice { get; private set; }

    /// <summary>
    /// Gets the resistance price level calculated during a Buy Setup.
    /// This is the highest high among the 9 bars of the completed setup period
    /// and serves as a critical threshold during the countdown phase.
    /// If the price rises above this level during a Buy Countdown, the countdown is invalidated.
    /// </summary>
    public decimal ResistancePrice { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TdSequential"/> indicator with the specified name.
    /// </summary>
    /// <param name="name">The name of the indicator. Defaults to "TdSequential".</param>
    public TdSequential(string name = "TdSequential")
        : base(name, WindowSize)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the indicator is ready and fully initialized.
    /// Returns <c>true</c> when at least <see cref="RequiredSamples"/> bars have been received,
    /// which is the minimum required for checking prerequisite price flips and for comparing
    /// the current close price to the close price 4 bars ago (used in the setup logic).
    /// </summary>
    public override bool IsReady => Samples >= RequiredSamples;

    /// <summary>
    /// Gets the number of data points (bars) required before the indicator is considered ready.
    /// The Tom DeMark Sequential setup logic requires at least 6 bars to begin evaluating valid setups.
    /// </summary>
    public override int WarmUpPeriod => RequiredSamples;

    /// <summary>
    /// Resets the indicator to its initial state by clearing all internal counters, flags,
    /// and historical bar data. This allows the indicator to be reused from a clean state.
    /// </summary>
    public override void Reset()
    {
        SetupCount = 0;
        CountdownCount = 0;
        Signal = TdSequentialSignal.None;
        SupportPrice = 0m;
        ResistancePrice = 0m;
        _currentPhase = TdSequentialPhase.None;
        base.Reset();
    }

    /// <summary>
    /// Computes the next value of the TD Sequential indicator based on the provided data window
    /// and the current bar.
    /// </summary>
    /// <param name="window">The window of data held in this indicator. Index 0 is the most recent bar.</param>
    /// <param name="current">The current input bar being processed.</param>
    /// <returns>
    /// A <c>decimal</c> representing the <see cref="TdSequentialPhase"/> of the current bar.
    /// </returns>
    protected override decimal ComputeNextValue(IReadOnlyWindow<IBaseDataBar> window, IBaseDataBar current)
    {
        if (!IsReady)
        {
            return DefaultPhaseValue;
        }

        // Indexing: window[0] = current bar (same as 'current'),
        // window[1] = 1 bar ago, window[2] = 2 bars ago, window[4] = 4 bars ago, window[5] = 5 bars ago
        var bar4Ago = window[4];
        var bar2Ago = window[2];

        return _currentPhase switch
        {
            TdSequentialPhase.None => InitializeSetupPhase(current, bar4Ago, window[1], window[5]),
            TdSequentialPhase.BuySetup => HandleBuySetupPhase(current, bar4Ago, bar2Ago, window),
            TdSequentialPhase.SellSetup => HandleSellSetupPhase(current, bar4Ago, bar2Ago, window),
            TdSequentialPhase.BuyCountdown => HandleBuyCountDown(current, bar2Ago),
            TdSequentialPhase.SellCountdown => HandleSellCountDown(current, bar2Ago),
            _ => DefaultPhaseValue
        };
    }

    /// <summary>
    /// Attempts to initialize a new setup phase by detecting a price flip.
    /// A Bullish flip occurs when the previous bar's close was above its 4-bars-ago close
    /// and the current bar's close is below its 4-bars-ago close (initiates Buy Setup).
    /// A Bearish flip occurs when the previous bar's close was below its 4-bars-ago close
    /// and the current bar's close is above its 4-bars-ago close (initiates Sell Setup).
    /// </summary>
    private decimal InitializeSetupPhase(IBaseDataBar current, IBaseDataBar bar4Ago, IBaseDataBar prevBar, IBaseDataBar prevBar4Ago)
    {
        // Bearish flip: initiates a Buy Setup
        if (prevBar.Close > prevBar4Ago.Close && current.Close < bar4Ago.Close)
        {
            _currentPhase = TdSequentialPhase.BuySetup;
            SetupCount = 1;
            Signal = TdSequentialSignal.Buy;
            return (decimal)TdSequentialPhase.BuySetup;
        }

        // Bullish flip: initiates a Sell Setup
        if (prevBar.Close < prevBar4Ago.Close && current.Close > bar4Ago.Close)
        {
            _currentPhase = TdSequentialPhase.SellSetup;
            SetupCount = 1;
            Signal = TdSequentialSignal.Sell;
            return (decimal)TdSequentialPhase.SellSetup;
        }

        Signal = TdSequentialSignal.None;
        return DefaultPhaseValue;
    }

    /// <summary>
    /// Handles the Buy Setup phase, counting consecutive bars where the close is less than
    /// the close 4 bars ago. When 9 consecutive qualifying bars are reached, the setup is marked
    /// as complete (possibly perfect) and the indicator transitions to the Buy Countdown phase.
    /// </summary>
    private decimal HandleBuySetupPhase(IBaseDataBar current, IBaseDataBar bar4Ago, IBaseDataBar bar2Ago, IReadOnlyWindow<IBaseDataBar> window)
    {
        if (current.Close < bar4Ago.Close)
        {
            SetupCount++;
            if (SetupCount == MaxSetupCount)
            {
                // Setup complete — transition to countdown
                var isPerfect = IsBuySetupPerfect(window);
                _currentPhase = TdSequentialPhase.BuyCountdown;
                Signal = TdSequentialSignal.Buy;

                // Calculate resistance: highest high among the 9 setup bars
                ResistancePrice = window.Skip(window.Count - MaxSetupCount).Take(MaxSetupCount).Max(b => b.High);

                // Check if bar 9 qualifies as countdown bar 1
                // (close is less than low 2 bars earlier)
                if (current.Close < bar2Ago.Low)
                {
                    CountdownCount = 1;
                }

                return (decimal)(isPerfect ? TdSequentialPhase.BuySetupPerfect : TdSequentialPhase.BuySetup);
            }

            return (decimal)TdSequentialPhase.BuySetup;
        }

        // Setup broken — reset
        _currentPhase = TdSequentialPhase.None;
        Signal = TdSequentialSignal.None;
        return DefaultPhaseValue;
    }

    /// <summary>
    /// Handles the Sell Setup phase, counting consecutive bars where the close is greater than
    /// the close 4 bars ago. When 9 consecutive qualifying bars are reached, the setup is marked
    /// as complete (possibly perfect) and the indicator transitions to the Sell Countdown phase.
    /// </summary>
    private decimal HandleSellSetupPhase(IBaseDataBar current, IBaseDataBar bar4Ago, IBaseDataBar bar2Ago, IReadOnlyWindow<IBaseDataBar> window)
    {
        if (current.Close > bar4Ago.Close)
        {
            SetupCount++;
            if (SetupCount == MaxSetupCount)
            {
                // Setup complete — transition to countdown
                var isPerfect = IsSellSetupPerfect(window);
                _currentPhase = TdSequentialPhase.SellCountdown;
                Signal = TdSequentialSignal.Sell;

                // Calculate support: lowest low among the 9 setup bars
                SupportPrice = window.Skip(window.Count - MaxSetupCount).Take(MaxSetupCount).Min(b => b.Low);

                // Check if bar 9 qualifies as countdown bar 1
                // (close is greater than high 2 bars earlier)
                if (current.Close > bar2Ago.High)
                {
                    CountdownCount = 1;
                }

                return (decimal)(isPerfect ? TdSequentialPhase.SellSetupPerfect : TdSequentialPhase.SellSetup);
            }

            return (decimal)TdSequentialPhase.SellSetup;
        }

        // Setup broken — reset
        _currentPhase = TdSequentialPhase.None;
        Signal = TdSequentialSignal.None;
        return DefaultPhaseValue;
    }

    /// <summary>
    /// Handles the Buy Countdown phase. In this phase, we count bars where the close
    /// is less than or equal to the low of 2 bars ago. The countdown completes after 13
    /// qualifying bars. If the price breaks above the established Resistance level,
    /// the countdown is invalidated.
    /// </summary>
    private decimal HandleBuyCountDown(IBaseDataBar current, IBaseDataBar bar2Ago)
    {
        if (current.Close <= bar2Ago.Low)
        {
            CountdownCount++;
            if (CountdownCount == MaxCountdownCount)
            {
                // Countdown complete — trend exhaustion signal, reset phase
                _currentPhase = TdSequentialPhase.None;
                Signal = TdSequentialSignal.None;
            }

            return (decimal)TdSequentialPhase.BuyCountdown;
        }

        // Check if resistance level is broken — invalidates countdown
        if (current.Close > ResistancePrice)
        {
            _currentPhase = TdSequentialPhase.None;
            Signal = TdSequentialSignal.None;
        }

        return DefaultPhaseValue;
    }

    /// <summary>
    /// Handles the Sell Countdown phase. In this phase, we count bars where the close
    /// is greater than or equal to the high of 2 bars ago. The countdown completes after 13
    /// qualifying bars. If the price breaks below the established Support level,
    /// the countdown is invalidated.
    /// </summary>
    private decimal HandleSellCountDown(IBaseDataBar current, IBaseDataBar bar2Ago)
    {
        if (current.Close >= bar2Ago.High)
        {
            CountdownCount++;
            if (CountdownCount == MaxCountdownCount)
            {
                // Countdown complete — trend exhaustion signal, reset phase
                _currentPhase = TdSequentialPhase.None;
                Signal = TdSequentialSignal.None;
            }

            return (decimal)TdSequentialPhase.SellCountdown;
        }

        // Check if support level is broken — invalidates countdown
        if (current.Close < SupportPrice)
        {
            _currentPhase = TdSequentialPhase.None;
            Signal = TdSequentialSignal.None;
        }

        return DefaultPhaseValue;
    }

    /// <summary>
    /// Determines whether the current 9-bar window represents a Perfect Buy Setup.
    /// A Perfect Buy Setup occurs when bar 8 has a lower low than both bars 6 and 7,
    /// or when bar 9 has a lower low than both bars 6 and 7.
    /// </summary>
    /// <param name="window">The 10-bar rolling window. Index 0 = current (bar 9 of setup).</param>
    /// <returns><c>true</c> if the setup is perfect; otherwise, <c>false</c>.</returns>
    private static bool IsBuySetupPerfect(IReadOnlyWindow<IBaseDataBar> window)
    {
        // In the 10-bar window (index 0-9), bars 6-9 of the setup are at:
        // Bar 6 = window[3], Bar 7 = window[2], Bar 8 = window[1], Bar 9 = window[0]
        var bar6 = window[3];
        var bar7 = window[2];
        var bar8 = window[1];
        var bar9 = window[0];

        return (bar8.Low < bar6.Low && bar8.Low < bar7.Low)
            || (bar9.Low < bar6.Low && bar9.Low < bar7.Low);
    }

    /// <summary>
    /// Determines whether the current 9-bar window represents a Perfect Sell Setup.
    /// A Perfect Sell Setup occurs when bar 8 has a higher high than both bars 6 and 7,
    /// or when bar 9 has a higher high than both bars 6 and 7.
    /// </summary>
    /// <param name="window">The 10-bar rolling window. Index 0 = current (bar 9 of setup).</param>
    /// <returns><c>true</c> if the setup is perfect; otherwise, <c>false</c>.</returns>
    private static bool IsSellSetupPerfect(IReadOnlyWindow<IBaseDataBar> window)
    {
        // In the 10-bar window (index 0-9), bars 6-9 of the setup are at:
        // Bar 6 = window[3], Bar 7 = window[2], Bar 8 = window[1], Bar 9 = window[0]
        var bar6 = window[3];
        var bar7 = window[2];
        var bar8 = window[1];
        var bar9 = window[0];

        return (bar8.High > bar6.High && bar8.High > bar7.High)
            || (bar9.High > bar6.High && bar9.High > bar7.High);
    }
}

/// <summary>
/// Represents the different phases of the TD Sequential indicator.
/// </summary>
public enum TdSequentialPhase
{
    /// <summary>
    /// No active phase. The indicator is waiting for a price flip to initiate a new setup.
    /// </summary>
    None = 0,

    /// <summary>
    /// Active Buy Setup phase. Counting consecutive bars where the close is less than
    /// the close 4 bars ago. Targets 9 consecutive bars.
    /// </summary>
    BuySetup = 1,

    /// <summary>
    /// Active Sell Setup phase. Counting consecutive bars where the close is greater than
    /// the close 4 bars ago. Targets 9 consecutive bars.
    /// </summary>
    SellSetup = 2,

    /// <summary>
    /// Active Buy Countdown phase. Counting bars where the close is less than or equal
    /// to the low of 2 bars ago. Targets 13 bars (not necessarily consecutive).
    /// </summary>
    BuyCountdown = 3,

    /// <summary>
    /// Active Sell Countdown phase. Counting bars where the close is greater than or equal
    /// to the high of 2 bars ago. Targets 13 bars (not necessarily consecutive).
    /// </summary>
    SellCountdown = 4,

    /// <summary>
    /// A completed Buy Setup with perfect price structure (bar 8 or 9 has lower low than bars 6 and 7).
    /// Perfect setups are considered higher-probability signals.
    /// </summary>
    BuySetupPerfect = 5,

    /// <summary>
    /// A completed Sell Setup with perfect price structure (bar 8 or 9 has higher high than bars 6 and 7).
    /// Perfect setups are considered higher-probability signals.
    /// </summary>
    SellSetupPerfect = 6
}

/// <summary>
/// Represents the directional trading signal generated by the TD Sequential indicator.
/// </summary>
public enum TdSequentialSignal
{
    /// <summary>
    /// No active signal. The indicator has not detected a valid setup or countdown.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bullish signal. A Buy Setup or Buy Countdown is in progress.
    /// When the setup completes (9 bars), it suggests a potential bullish reversal.
    /// When the countdown completes (13 bars), it signals trend exhaustion to the downside.
    /// </summary>
    Buy = 1,

    /// <summary>
    /// Bearish signal. A Sell Setup or Sell Countdown is in progress.
    /// When the setup completes (9 bars), it suggests a potential bearish reversal.
    /// When the countdown completes (13 bars), it signals trend exhaustion to the upside.
    /// </summary>
    Sell = -1
}
