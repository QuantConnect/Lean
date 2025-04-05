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
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp.RegressionTests
{
    public class Collective2IndexOptionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Collective2 APIv4 KEY: This value is provided by Collective2 in your account section (See https://collective2.com/account-info)
        /// See API documentation at https://trade.collective2.com/c2-api
        /// </summary>
        private const string _collective2ApiKey = "YOUR APIV4 KEY";

        /// <summary>
        /// Collective2 System ID: This value is found beside the system's name (strategy's name) on the main system page
        /// </summary>
        private const int _collective2SystemId = 0;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private Symbol _symbol;
        private bool _firstCall = true;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 18);
            SetCash(100000);

            var underlying = AddIndex("SPX", Resolution.Minute).Symbol;

            // Create an SPXW option contract with a specific strike price and expiration date
            var option = QuantConnect.Symbol.CreateOption(
                underlying,
                "SPXW",
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3800m,
                new DateTime(2021, 1, 04));

            _symbol = AddIndexOptionContract(option, Resolution.Minute).Symbol;

            _fast = EMA(underlying, 10, Resolution.Minute);
            _slow = EMA(underlying, 50, Resolution.Minute);

            // Disable automatic exports as we manually set them
            SignalExport.AutomaticExportTimeSpan = null;
            // Set up the Collective2 Signal Export with the provided API key and system ID
            SignalExport.AddSignalExportProvider(new Collective2SignalExport(_collective2ApiKey, _collective2SystemId));

            // Set warm-up period for the indicators
            SetWarmUp(50);
        }

        public override void OnData(Slice slice)
        {
            // Execute only on the first data call to set initial portfolio
            if (_firstCall)
            {
                SetHoldings(_symbol, 0.1);
                SignalExport.SetTargetPortfolioFromPortfolio();
                _firstCall = false;
            }

            // If the fast EMA crosses above the slow EMA, open a long position
            if (_fast > _slow && !Portfolio.Invested)
            {
                MarketOrder(_symbol, 1);
                SignalExport.SetTargetPortfolioFromPortfolio();
            }

            // If the fast EMA crosses below the slow EMA, open a short position
            else if (_fast < _slow && Portfolio.Invested)
            {
                MarketOrder(_symbol, -1);
                SignalExport.SetTargetPortfolioFromPortfolio();
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4543;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.468%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99985"},
            {"Net Profit", "-0.015%"},
            {"Sharpe Ratio", "-15.229"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.781%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.003"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-5.216"},
            {"Tracking Error", "0.103"},
            {"Treynor Ratio", "5.946"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$8000.00"},
            {"Lowest Capacity Asset", "SPXW XKX6S2GM9PGU|SPX 31"},
            {"Portfolio Turnover", "0.01%"},
            {"OrderListHash", "44d9880b19d4709447faf505d24aad7f"}
        };
    }
}
