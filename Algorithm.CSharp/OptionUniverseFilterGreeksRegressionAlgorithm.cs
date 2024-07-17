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
 *
*/

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating the option universe filter by greeks and other options data feature
    /// </summary>
    public class OptionUniverseFilterGreeksRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        private Symbol _optionSymbol;
        private bool _optionChainReceived;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);
            _optionSymbol = option.Symbol;

            SetOptionFilter(option);
        }

        protected virtual void SetOptionFilter(Option security)
        {
            // Contracts can be filtered by greeks, implied volatility, open interest:
            security.SetFilter(u => u
                .Strikes(-3, +3)
                .Expiration(0, 180)
                .Delta(0.64m, 0.65m)
                .Gamma(0.0008m, 0.0010m)
                .Vega(7.5m, 10.5m)
                .Theta(-1.10m, -0.50m)
                .Rho(4m, 10m)
                .ImpliedVolatility(0.10m, 0.20m)
                .OpenInterest(100, 1000));

            // Note: there are also shortcuts for these filter methods:
            /*
            security.SetFilter(u => u
                .D(0.64m, 0.65m)
                .G(0.0008m, 0.0010m)
                .V(7.5m, 10.5m)
                .T(-1.10m, -0.50m)
                .R(4m, 10m)
                .IV(0.10m, 0.20m)
                .OI(100, 1000));
            */
        }

        public override void OnData(Slice slice)
        {
            if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain) && chain.Any())
            {
                _optionChainReceived = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_optionChainReceived)
            {
                throw new RegressionTestException("Option chain was not received.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2021;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 6;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
