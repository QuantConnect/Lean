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

using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Parameters;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the parameter system of QuantConnect. Using parameters you can pass the values required into C# algorithms for optimization.
    /// </summary>
    /// <meta name="tag" content="optimization" />
    /// <meta name="tag" content="using quantconnect" />
    public class ParameterizedAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // we place attributes on top of our fields or properties that should receive
        // their values from the job. The values 100 and 200 are just default values that
        // or only used if the parameters do not exist
        [Parameter("ema-fast")]
        public int FastPeriod = 100;

        [Parameter("ema-slow")]
        public int SlowPeriod = 200;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100*1000);

            AddSecurity(SecurityType.Equity, "SPY");

            Fast = EMA("SPY", FastPeriod);
            Slow = EMA("SPY", SlowPeriod);
        }

        public void OnData(TradeBars data)
        {
            // wait for our indicators to ready
            if (!Fast.IsReady || !Slow.IsReady) return;

            if (Fast > Slow*1.001m)
            {
                SetHoldings("SPY", 1);
            }
            else if (Fast < Slow*0.999m)
            {
                Liquidate("SPY");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "278.218%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.715%"},
            {"Sharpe Ratio", "11.018"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "78.005"},
            {"Annual Standard Deviation", "0.078"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "10.897"},
            {"Tracking Error", "0.078"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$3.27"}
        };
    }
}
