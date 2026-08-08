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

using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstrates the usage of the Tom DeMark (TD) Sequential indicator.
    /// This algorithm uses the TdSequential indicator to identify potential
    /// trend exhaustion points and generates buy/sell signals based on
    /// completed setups and countdowns.
    /// </summary>
    /// <remarks>
    /// Trading Strategy:
    /// - Buy when a Buy Setup completes (9 consecutive closes &lt; close 4 bars ago)
    ///   and the countdown phase begins.
    /// - Sell when a Sell Setup completes (9 consecutive closes &gt; close 4 bars ago)
    ///   and the countdown phase begins.
    /// - Strong Buy signal: Buy Countdown completes (13 countdown bars)
    /// - Strong Sell signal: Sell Countdown completes (13 countdown bars)
    ///
    /// The indicator tracks:
    ///   - SetupCount: Number of consecutive qualifying bars (1-9)
    ///   - CountdownCount: Number of qualifying countdown bars (1-13)
    ///   - IsSetupComplete: True when 9-bar setup is complete
    ///   - IsCountdownComplete: True when 13-bar countdown is complete
    ///   - Signal: Current directional signal (None/Buy/Sell)
    ///   - SupportPrice: Lowest low during the last completed Sell Setup
    ///   - ResistancePrice: Highest high during the last completed Buy Setup
    /// </remarks>
    public class TdSequentialExampleAlgorithm : QCAlgorithm
    {
        private TdSequential _tdSequential;

        public override void Initialize()
        {
            SetStartDate(2023, 1, 1);
            SetEndDate(2023, 12, 31);
            SetCash(100000);

            // Add a security and create the TD Sequential indicator
            var spy = AddEquity("SPY", Resolution.Daily).Symbol;
            _tdSequential = new TdSequential("SPY_TDSEQ");

            // Register the indicator for automatic updates with daily data
            RegisterIndicator(spy, _tdSequential, Resolution.Daily);

            // Enable automatic indicator warm-up to have the indicator ready
            // after the algorithm starts
            EnableAutomaticIndicatorWarmUp = true;

            // Log the indicator setup
            Debug($"TD Sequential indicator initialized. WarmUp: {_tdSequential.WarmUpPeriod} bars");
        }

        public override void OnData(Slice slice)
        {
            // Ensure the indicator is ready before trading
            if (!_tdSequential.IsReady)
            {
                return;
            }

            var currentPhase = (TdSequentialPhase)_tdSequential.Current.Value;

            // Plot the indicator values for visualization
            Plot("TD Sequential", "Phase", (int)currentPhase);
            Plot("TD Sequential", "SetupCount", _tdSequential.SetupCount);
            Plot("TD Sequential", "CountdownCount", _tdSequential.CountdownCount);
            Plot("TD Sequential", "Signal", (int)_tdSequential.Signal);

            // Trading logic based on TD Sequential signals

            // Buy Setup completed — potential bullish reversal
            if (_tdSequential.IsSetupComplete && _tdSequential.Signal == TdSequentialSignal.Buy)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings("SPY", 0.5m);
                    Debug($"BUY: Setup complete. SetupCount={_tdSequential.SetupCount}, " +
                          $"ResistancePrice={_tdSequential.ResistancePrice}");
                }
            }

            // Sell Setup completed — potential bearish reversal
            if (_tdSequential.IsSetupComplete && _tdSequential.Signal == TdSequentialSignal.Sell)
            {
                if (Portfolio.Invested)
                {
                    Liquidate("SPY");
                    Debug($"SELL: Setup complete. SetupCount={_tdSequential.SetupCount}, " +
                          $"SupportPrice={_tdSequential.SupportPrice}");
                }
            }

            // Countdown completed — trend exhaustion confirmed
            if (_tdSequential.IsCountdownComplete)
            {
                Debug($"COUNTDOWN COMPLETED: CountdownCount={_tdSequential.CountdownCount}");
                Plot("TD Sequential", "CountdownCompleted", 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Log final indicator state
            Log($"Final TD Sequential State:");
            Log($"  Phase: {(TdSequentialPhase)_tdSequential.Current.Value}");
            Log($"  SetupCount: {_tdSequential.SetupCount}");
            Log($"  CountdownCount: {_tdSequential.CountdownCount}");
            Log($"  Signal: {_tdSequential.Signal}");
            Log($"  SupportPrice: {_tdSequential.SupportPrice}");
            Log($"  ResistancePrice: {_tdSequential.ResistancePrice}");
        }
    }
}
