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
    public class BaseDictionaryTests
    {
        [Test]
        public void RunPythonDictionaryFeatureRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("PythonDictionaryFeatureRegressionAlgorithm",
                new Dictionary<string, string> {
                    {"Total Trades", "3"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "-100%"},
                    {"Drawdown", "99.600%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "-99.546%"},
                    {"Sharpe Ratio", "-0.126"},
                    {"Probabilistic Sharpe Ratio", "1.654%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "3.788"},
                    {"Beta", "-2.542"},
                    {"Annual Standard Deviation", "7.945"},
                    {"Annual Variance", "63.118"},
                    {"Information Ratio", "-0.362"},
                    {"Tracking Error", "7.963"},
                    {"Treynor Ratio", "0.393"},
                    {"Total Fees", "$0.00"},
                    {"OrderListHash", "1168233401"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 100000);
        }
    }
}
