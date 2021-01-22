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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonOptionTests
    {
        [Test]
        public void PythonFilterFunctionReturnsList()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spyOption = algorithm.AddOption("SPY");

            using (Py.GIL())
            {
                //Filter function that returns a list of symbols
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "def filter(universe):\n" +
                    "   universe = universe.WeeklysOnly().Expiration(0, 10)\n" +
                    "   return [symbol for symbol in universe\n"+
                    "           if symbol.ID.OptionRight != OptionRight.Put\n" +
                    "           and universe.Underlying.Price - symbol.ID.StrikePrice < 10]\n"
                );

                var filterFunction = module.GetAttr("filter");
                Assert.DoesNotThrow(() => spyOption.SetFilter(filterFunction));
            }


        }

        [Test]
        public void PythonFilterFunctionReturnsUniverse()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spyOption = algorithm.AddOption("SPY");

            using (Py.GIL())
            {
                //Filter function that returns a OptionFilterUniverse
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "def filter(universe):\n" +
                    "   universe = universe.WeeklysOnly().Expiration(0, 5)\n" +
                    "   return universe"
                );

                var filterFunction = module.GetAttr("filter");
                Assert.DoesNotThrow(() => spyOption.SetFilter(filterFunction));
            }
        }

        [Test]
        public void FilterReturnsUniverseRegression()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("FilterUniverseRegressionAlgorithm",
                new Dictionary<string, string> {
                    {"Total Trades", "4"},
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
                    {"Information Ratio", "0"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$2.00"},
                    {"OrderListHash", "1571614294"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        [Test]
        public void FilterReturnsListRegression()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("BasicTemplateOptionsFilterUniverseAlgorithm",
                new Dictionary<string, string> {
                    {"Total Trades", "2"},
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
                    {"Information Ratio", "0"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$1.00"},
                    {"Fitness Score", "0"},
                    {"Kelly Criterion Estimate", "0"},
                    {"Kelly Criterion Probability Value", "0"},
                    {"Sortino Ratio", "0"},
                    {"Return Over Maximum Drawdown", "0"},
                    {"Portfolio Turnover", "0"},
                    {"Total Insights Generated", "0"},
                    {"Total Insights Closed", "0"},
                    {"Total Insights Analysis Completed", "0"},
                    {"Long Insight Count", "0"},
                    {"Short Insight Count", "0"},
                    {"Long/Short Ratio", "100%"},
                    {"Estimated Monthly Alpha Value", "$0"},
                    {"Total Accumulated Estimated Alpha Value", "$0"},
                    {"Mean Population Estimated Insight Value", "$0"},
                    {"Mean Population Direction", "0%"},
                    {"Mean Population Magnitude", "0%"},
                    {"Rolling Averaged Population Direction", "0%"},
                    {"Rolling Averaged Population Magnitude", "0%"},
                    {"OrderListHash", "-91832511"}
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
