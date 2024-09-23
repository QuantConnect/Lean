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

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm showing how to easily convert an old algorithm into the framework.
    ///
    ///  1. When making orders, also create insights for the correct direction (up/down/flat), can also set insight prediction period/magnitude/direction
    ///  2. Emit insights before placing any trades
    ///  3. Profit :)
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="plotting indicators" />
    public class ConvertToFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private MovingAverageConvergenceDivergence _macd;
        private readonly string _symbol = "SPY";

        private readonly int _fastEmaPeriod = 12;
        private readonly int _slowEmaPeriod = 26;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Daily);

            // define our daily macd(12,26) with a 9 day signal
            _macd = MACD(_symbol, _fastEmaPeriod, _slowEmaPeriod, 9, MovingAverageType.Exponential, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            // wait for our indicator to be ready
            if (!_macd.IsReady) return;

            var holding = Portfolio[_symbol];

            var signalDeltaPercent = (_macd - _macd.Signal) / _macd.Fast;
            var tolerance = 0.0025m;

            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance)
            {
                // 1. Call EmitInsights with insights created in correct direction, here we're going long
                //    The EmitInsights method can accept multiple insights separated by commas
                EmitInsights(
                    // Creates an insight for our symbol, predicting that it will move up within the fast ema period number of days
                    Insight.Price(_symbol, TimeSpan.FromDays(_fastEmaPeriod), InsightDirection.Up)
                );

                // longterm says buy as well
                SetHoldings(_symbol, 1.0);
            }
            // if our macd is less than our signal, then let's go short
            else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance)
            {
                // 1. Call EmitInsights with insights created in correct direction, here we're going short
                //    The EmitInsights method can accept multiple insights separated by commas
                EmitInsights(
                    // Creates an insight for our symbol, predicting that it will move down within the fast ema period number of days
                    Insight.Price(_symbol, TimeSpan.FromDays(_fastEmaPeriod), InsightDirection.Down)
                );

                // shortterm says sell as well
                SetHoldings(_symbol, -1.0);
            }

            // if we wanted to liquidate our positions
            // 1. Call EmitInsights with insights create in the correct direction -- Flat
        
            // EmitInsights(
                   // Creates an insight for our symbol, predicting that it will move down or up within the fast ema period number of days, depending on our current position
                   // Insight.Price(_symbol, TimeSpan.FromDays(FastEmaPeriod), InsightDirection.Flat);
            // );
        
            // Liquidate();

            // plot both lines
            Plot("MACD", _macd, _macd.Signal);
            if (slice.Bars.ContainsKey(_symbol))
            {
                Plot(_symbol, "Open", slice[_symbol].Open);
            }
            Plot(_symbol, _macd.Fast, _macd.Slow);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 22136;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "85"},
            {"Average Win", "4.85%"},
            {"Average Loss", "-4.22%"},
            {"Compounding Annual Return", "-3.119%"},
            {"Drawdown", "52.900%"},
            {"Expectancy", "-0.053"},
            {"Start Equity", "100000"},
            {"End Equity", "70553.97"},
            {"Net Profit", "-29.446%"},
            {"Sharpe Ratio", "-0.223"},
            {"Sortino Ratio", "-0.243"},
            {"Probabilistic Sharpe Ratio", "0.001%"},
            {"Loss Rate", "56%"},
            {"Win Rate", "44%"},
            {"Profit-Loss Ratio", "1.15"},
            {"Alpha", "-0.029"},
            {"Beta", "-0.095"},
            {"Annual Standard Deviation", "0.149"},
            {"Annual Variance", "0.022"},
            {"Information Ratio", "-0.34"},
            {"Tracking Error", "0.23"},
            {"Treynor Ratio", "0.351"},
            {"Total Fees", "$797.27"},
            {"Estimated Strategy Capacity", "$1400000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "4.23%"},
            {"OrderListHash", "0422632afa17df1379757085f951de7b"}
        };
    }
}
