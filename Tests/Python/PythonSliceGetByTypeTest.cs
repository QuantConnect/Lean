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
                    {"Total Trades", "1"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "277.455%"},
                    {"Drawdown", "2.200%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "1.713%"},
                    {"Sharpe Ratio", "8.755"},
                    {"Probabilistic Sharpe Ratio", "67.311%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.003"},
                    {"Beta", "0.997"},
                    {"Annual Standard Deviation", "0.219"},
                    {"Annual Variance", "0.048"},
                    {"Information Ratio", "-14.15"},
                    {"Tracking Error", "0.001"},
                    {"Treynor Ratio", "1.924"},
                    {"Total Fees", "$3.26"},
                    {"OrderListHash", "16564191ddd913e841e3f51febc035aa"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }
    }
}
