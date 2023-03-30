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

using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the correct market simulation instance is used when setting it using <see cref="IAlgorithm.SetMarketSimulation"/>
    /// </summary>
    class SetMarketSimulationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _simulateMarketConditionsCallCount;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            AddEquity("SPY", Resolution.Hour);

            try
            {
                SetMarketSimulation(null);
                throw new Exception("Expected SetMarketSimulation to throw an exception when passed null");
            }
            catch (ArgumentNullException e)
            {
                // expected
            }

            var marketSimulation = new TestMarketSimulation();
            marketSimulation.OnSimulate += (_, _) => _simulateMarketConditionsCallCount++;
            SetMarketSimulation(marketSimulation);
        }

        /// <summary>
        /// Runs after algorithm, used to check our portfolio and orders
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            if (_simulateMarketConditionsCallCount == 0)
            {
                throw new Exception("The market simulation was never used");
            }
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 30;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "7.438"},
            {"Tracking Error", "0.017"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }

    public class TestMarketSimulation : IBacktestingMarketSimulation
    {
        public event EventHandler OnSimulate;

        public void SimulateMarketConditions(IBrokerage brokerage, IAlgorithm algorithm)
        {
            OnSimulate.Invoke(this, null);
        }
    }
}
