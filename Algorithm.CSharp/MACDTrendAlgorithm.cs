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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Simple indicator demonstration algorithm of MACD
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="plotting indicators" />
    public class MACDTrendAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _previous;
        private MovingAverageConvergenceDivergence _macd;
        private readonly string _symbol = "SPY";

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Daily);

            // define our daily macd(12,26) with a 9 day signal
            _macd = MACD(_symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            // only once per day
            if (_previous.Date == Time.Date) return;

            if (!_macd.IsReady) return;

            var holding = Portfolio[_symbol];

            var signalDeltaPercent = (_macd - _macd.Signal)/_macd.Fast;
            var tolerance = 0.0025m;

            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance) // 0.01%
            {
                // longterm says buy as well
                SetHoldings(_symbol, 1.0);
            }
            // of our macd is less than our signal, then let's go short
            else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance)
            {
                Liquidate(_symbol);
            }

            // plot both lines
            Plot("MACD", _macd, _macd.Signal);
            Plot(_symbol, "Open", data[_symbol].Open);
            Plot(_symbol, _macd.Fast, _macd.Slow);

            _previous = Time;
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
            {"Total Trades", "68"},
            {"Average Win", "4.83%"},
            {"Average Loss", "-4.61%"},
            {"Compounding Annual Return", "1.438%"},
            {"Drawdown", "34.700%"},
            {"Expectancy", "0.144"},
            {"Net Profit", "17.015%"},
            {"Sharpe Ratio", "0.191"},
            {"Probabilistic Sharpe Ratio", "0.600%"},
            {"Loss Rate", "44%"},
            {"Win Rate", "56%"},
            {"Profit-Loss Ratio", "1.05"},
            {"Alpha", "0.027"},
            {"Beta", "-0.044"},
            {"Annual Standard Deviation", "0.126"},
            {"Annual Variance", "0.016"},
            {"Information Ratio", "-0.211"},
            {"Tracking Error", "0.241"},
            {"Treynor Ratio", "-0.55"},
            {"Total Fees", "$301.50"},
            {"Fitness Score", "0.014"},
            {"Kelly Criterion Estimate", "7.361"},
            {"Kelly Criterion Probability Value", "0.359"},
            {"Sortino Ratio", "0.094"},
            {"Return Over Maximum Drawdown", "0.041"},
            {"Portfolio Turnover", "0.027"},
            {"Total Insights Generated", "68"},
            {"Total Insights Closed", "67"},
            {"Total Insights Analysis Completed", "67"},
            {"Long Insight Count", "34"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$-46834.31"},
            {"Total Accumulated Estimated Alpha Value", "$-6273000"},
            {"Mean Population Estimated Insight Value", "$-93626.86"},
            {"Mean Population Direction", "54.5908%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "75.9436%"},
            {"Rolling Averaged Population Magnitude", "0%"}
            {"OrderListHash", "-1880031556"}
        };
    }
}
