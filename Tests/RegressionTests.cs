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
    [TestFixture, Category("TravisExclude")]
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
                {"Compounding Annual Return", "264.956%"},
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
                {"Total Fees", "$3.09"}
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
                {"Compounding Annual Return", "8.265%"},
                {"Drawdown", "0.200%"},
                {"Expectancy", "0.447"},
                {"Net Profit", "0.102%"},
                {"Sharpe Ratio", "1.729"},
                {"Loss Rate", "31%"},
                {"Win Rate", "69%"},
                {"Profit-Loss Ratio", "1.10"},
                {"Alpha", "0.051"},
                {"Beta", "0.002"},
                {"Annual Standard Deviation", "0.03"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-2.454"},
                {"Tracking Error", "0.194"},
                {"Treynor Ratio", "28.639"},
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
                {"Average Loss", "-1.71%"},
                {"Compounding Annual Return", "-8.289%"},
                {"Drawdown", "16.700%"},
                {"Expectancy", "-1"},
                {"Net Profit", "-15.892%"},
                {"Sharpe Ratio", "-1.353"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.092"},
                {"Beta", "0.037"},
                {"Annual Standard Deviation", "0.062"},
                {"Annual Variance", "0.004"},
                {"Information Ratio", "-2.39"},
                {"Tracking Error", "0.124"},
                {"Treynor Ratio", "-2.256"},
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
                {"Compounding Annual Return", "-3.886%"},
                {"Drawdown", "0.100%"},
                {"Expectancy", "-0.991"},
                {"Net Profit", "-0.054%"},
                {"Sharpe Ratio", "-30.336"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "2.40"},
                {"Alpha", "-0.023"},
                {"Beta", "0.001"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-4.203"},
                {"Tracking Error", "0.174"},
                {"Treynor Ratio", "-33.666"},
                {"Total Fees", "$5433.00"}
            });
        }

        [Test]
        public void UniverseSelectionRegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("UniverseSelectionRegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "4"},
                {"Average Win", "0.70%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "-56.034%"},
                {"Drawdown", "3.800%"},
                {"Expectancy", "0"},
                {"Net Profit", "-3.755%"},
                {"Sharpe Ratio", "-4.049"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.808"},
                {"Beta", "0.836"},
                {"Annual Standard Deviation", "0.194"},
                {"Annual Variance", "0.038"},
                {"Information Ratio", "-4.565"},
                {"Tracking Error", "0.178"},
                {"Treynor Ratio", "-0.939"},
                {"Total Fees", "$2.00"}
            });
        }
    }
}