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
                    {"Average Loss", "-1.03%"},
                    {"Compounding Annual Return", "245.167%"},
                    {"Drawdown", "2.300%"},
                    {"Expectancy", "-1"},
                    {"Net Profit", "1.597%"},
                    {"Sharpe Ratio", "4.554"},
                    {"Probabilistic Sharpe Ratio", "65.613%"},
                    {"Loss Rate", "100%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.06"},
                    {"Beta", "1.015"},
                    {"Annual Standard Deviation", "0.223"},
                    {"Annual Variance", "0.05"},
                    {"Information Ratio", "-9.541"},
                    {"Tracking Error", "0.005"},
                    {"Treynor Ratio", "1.002"},
                    {"Total Fees", "$9.77"},
                    {"OrderListHash", "-1005558829"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 1000000);
        }
    }
}
