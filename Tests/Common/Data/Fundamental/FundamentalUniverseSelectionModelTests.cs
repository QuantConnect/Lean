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

using NUnit.Framework;
using QuantConnect.Statistics;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class FundamentalUniverseSelectionModelTests
    {
        [Test]
        public void PythonAlgorithmUsingCSharpSelection()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("FundamentalUniverseSelectionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "3"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "-3.123%"},
                    {"Drawdown", "0.100%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "-0.122%"},
                    {"Sharpe Ratio", "-5.568"},
                    {"Probabilistic Sharpe Ratio", "11.594%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.022"},
                    {"Beta", "0.048"},
                    {"Annual Standard Deviation", "0.006"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "1.712"},
                    {"Tracking Error", "0.093"},
                    {"Treynor Ratio", "-0.636"},
                    {"Total Fees", "$3.00"},
                    {"Estimated Strategy Capacity", "$2300000000.00"},
                    {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
                    {"Portfolio Turnover", "0.42%"},
                    {"OrderListHash", "9bd2017fdcf8f503e86dfa3bf6e33520"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }
    }
}
