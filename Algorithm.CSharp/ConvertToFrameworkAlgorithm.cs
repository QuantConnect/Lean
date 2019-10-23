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

        public readonly int FastEmaPeriod = 12;
        public readonly int SlowEmaPeriod = 26;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Daily);

            // define our daily macd(12,26) with a 9 day signal
            _macd = MACD(_symbol, FastEmaPeriod, SlowEmaPeriod, 9, MovingAverageType.Exponential, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
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
                    Insight.Price(_symbol, TimeSpan.FromDays(FastEmaPeriod), InsightDirection.Up)
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
                    Insight.Price(_symbol, TimeSpan.FromDays(FastEmaPeriod), InsightDirection.Down)
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
            Plot(_symbol, "Open", data[_symbol].Open);
            Plot(_symbol, _macd.Fast, _macd.Slow);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "85"},
            {"Average Win", "4.85%"},
            {"Average Loss", "-4.21%"},
            {"Compounding Annual Return", "-3.108%"},
            {"Drawdown", "52.900%"},
            {"Expectancy", "-0.053"},
            {"Net Profit", "-29.358%"},
            {"Sharpe Ratio", "-0.084"},
            {"Loss Rate", "56%"},
            {"Win Rate", "44%"},
            {"Profit-Loss Ratio", "1.15"},
            {"Alpha", "-0.006"},
            {"Beta", "-0.093"},
            {"Annual Standard Deviation", "0.181"},
            {"Annual Variance", "0.033"},
            {"Information Ratio", "-0.396"},
            {"Tracking Error", "0.278"},
            {"Treynor Ratio", "0.164"},
            {"Total Fees", "$754.82"},
            {"Total Insights Generated", "85"},
            {"Total Insights Closed", "85"},
            {"Total Insights Analysis Completed", "85"},
            {"Long Insight Count", "42"},
            {"Short Insight Count", "43"},
            {"Long/Short Ratio", "97.67%"},
            {"Estimated Monthly Alpha Value", "$-607698.1"},
            {"Total Accumulated Estimated Alpha Value", "$-81395260"},
            {"Mean Population Estimated Insight Value", "$-957591.3"},
            {"Mean Population Direction", "50.5882%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "46.5677%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}
