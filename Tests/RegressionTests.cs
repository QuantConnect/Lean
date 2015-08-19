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

using System.Collections.Generic;
using NUnit.Framework;

namespace QuantConnect.Tests
{
    [TestFixture, Ignore("Travis seems to have issues running this at the moment.")]
    public class RegressionTests
    {
        [Test]
        public void BasicTemplateAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("BasicTemplateAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "0%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "311.509%"},
                {"Drawdown", "1.500%"},
                {"Expectancy", "0"},
                {"Net Profit", "0%"},
                {"Sharpe Ratio", "4.411"},
                {"Loss Rate", "0%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.752"},
                {"Beta", "0.186"},
                {"Annual Standard Deviation", "0.193"},
                {"Annual Variance", "0.037"},
                {"Information Ratio", "1.316"},
                {"Tracking Error", "0.246"},
                {"Treynor Ratio", "4.572"},
                {"Total Fees", "$3.08"}
            });
        }

        [Test]
        public void LimitFillRegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("LimitFillRegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "35"},
                {"Average Win", "0.02%"},
                {"Average Loss", "-0.02%"},
                {"Compounding Annual Return", "9.128%"},
                {"Drawdown", "0.200%"},
                {"Expectancy", "0.448"},
                {"Net Profit", "0.102%"},
                {"Sharpe Ratio", "1.732"},
                {"Loss Rate", "31%"},
                {"Win Rate", "69%"},
                {"Profit-Loss Ratio", "1.11"},
                {"Alpha", "0.051"},
                {"Beta", "0.002"},
                {"Annual Standard Deviation", "0.03"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-2.452"},
                {"Tracking Error", "0.194"},
                {"Treynor Ratio", "28.678"},
                {"Total Fees", "$35.00"}
            });
        }

        [Test]
        public void UpdateOrderRegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("UpdateOrderRegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "21"},
                {"Average Win", "0%"},
                {"Average Loss", "-1.72%"},
                {"Compounding Annual Return", "-8.332%"},
                {"Drawdown", "16.800%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-15.970%"},
                {"Sharpe Ratio", "-1.358"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.093"},
                {"Beta", "0.037"},
                {"Annual Standard Deviation", "0.063"},
                {"Annual Variance", "0.004"},
                {"Information Ratio", "-2.4"},
                {"Tracking Error", "0.124"},
                {"Treynor Ratio", "-2.29"},
                {"Total Fees", "$21.00"}
            });
        }

        [Test]
        public void RegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("RegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "5433"},
                {"Average Win", "0.00%"},
                {"Average Loss", "0.00%"},
                {"Compounding Annual Return", "-4.212%"},
                {"Drawdown", "0.100%"},
                {"Expectancy", "-0.993"},
                {"Net Profit", "-0.054%"},
                {"Sharpe Ratio", "-30.339"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "8.05"},
                {"Alpha", "-0.023"},
                {"Beta", "0.001"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-4.203"},
                {"Tracking Error", "0.174"},
                {"Treynor Ratio", "-33.297"},
                {"Total Fees", "$5433.00"}
            });
        }
    }
}