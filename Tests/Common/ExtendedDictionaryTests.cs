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
    public class ExtendedDictionaryTests
    {
        [Test]
        public void RunPythonDictionaryFeatureRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("PythonDictionaryFeatureRegressionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "3"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "349.409%"},
                    {"Drawdown", "2.600%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "1.940%"},
                    {"Sharpe Ratio", "10.771"},
                    {"Probabilistic Sharpe Ratio", "66.098%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0.571"},
                    {"Beta", "1.247"},
                    {"Annual Standard Deviation", "0.282"},
                    {"Annual Variance", "0.079"},
                    {"Information Ratio", "14.457"},
                    {"Tracking Error", "0.073"},
                    {"Treynor Ratio", "2.433"},
                    {"Total Fees", "$0.00"},
                    {"OrderListHash", "a91c50e19b6b9ee19007be4555029779"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 100000);
        }
    }
}
