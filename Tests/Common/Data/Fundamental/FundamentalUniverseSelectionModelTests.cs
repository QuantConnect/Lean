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
                    {PerformanceMetrics.TotalOrders, "2"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "-0.223%"},
                    {"Drawdown", "0.100%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "-0.009%"},
                    {"Sharpe Ratio", "-6.313"},
                    {"Probabilistic Sharpe Ratio", "12.055%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.019"},
                    {"Beta", "0.027"},
                    {"Annual Standard Deviation", "0.004"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "1.749"},
                    {"Tracking Error", "0.095"},
                    {"Treynor Ratio", "-0.876"},
                    {"Total Fees", "$2.00"},
                    {"Estimated Strategy Capacity", "$2200000000.00"},
                    {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
                    {"Portfolio Turnover", "0.28%"},
                    {"OrderListHash", "c963ecc09802eff2b88f36b60e521c9d"}
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
