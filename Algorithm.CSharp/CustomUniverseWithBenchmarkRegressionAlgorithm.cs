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
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm with a custom universe and benchmark, both using the same security.
    /// </summary>
    public class CustomUniverseWithBenchmarkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private const int ExpectedLeverage = 2;
        private const int ExpectedDataPoints = 4;
        private int _actualDatapoints;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 4, 2);  //Set Start Date
            SetEndDate(2014, 4, 6);    //Set End Date
            SetCash(100000);           //Set End Date

            AddUniverse("my-universe", x => new List<string> { "SPY" });

            SetBenchmark("SPY");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            Securities.Security security;
            if (!Securities.TryGetValue(_spy, out security))
                return;

            _actualDatapoints++;

            if (security.Leverage != ExpectedLeverage)
            {
                throw new Exception($"Leverage error - expected: {ExpectedLeverage}, actual: {security.Leverage}");
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_actualDatapoints != ExpectedDataPoints)
            {
                throw new Exception($"DataPoint count error - expected: {ExpectedDataPoints}, actual: {_actualDatapoints}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-74.620%"},
            {"Drawdown", "1.300%"},
            {"Expectancy", "0"},
            {"Net Profit", "-1.121%"},
            {"Sharpe Ratio", "-7.177"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.123"},
            {"Beta", "0.938"},
            {"Annual Standard Deviation", "0.098"},
            {"Annual Variance", "0.01"},
            {"Information Ratio", "-7.799"},
            {"Tracking Error", "0.011"},
            {"Treynor Ratio", "-0.752"},
            {"Total Fees", "$2.87"}
        };
    }
}
