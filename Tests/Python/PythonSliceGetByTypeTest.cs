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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class PythonSliceGetByTypeTests
    {
        [Test]
        public void RunPythonSliceGetByTypeRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("SliceGetByTypeRegressionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "1"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "284.284%"},
                    {"Drawdown", "2.200%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "1.736%"},
                    {"Sharpe Ratio", "8.86"},
                    {"Probabilistic Sharpe Ratio", "67.609%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.004"},
                    {"Beta", "0.997"},
                    {"Annual Standard Deviation", "0.222"},
                    {"Annual Variance", "0.049"},
                    {"Information Ratio", "-14.547"},
                    {"Tracking Error", "0.001"},
                    {"Treynor Ratio", "1.972"},
                    {"Total Fees", "$3.45"},
                    {"OrderListHash", "275925e122dc6f40501d1e3f35339e26"}
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
