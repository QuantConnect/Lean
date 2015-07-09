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
                {"Average Win", "3.33%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "1546.436%"},
                {"Drawdown", "3.000%"},
                {"Expectancy", "0"},
                {"Net Profit", "3.332%"},
                {"Sharpe Ratio", "4.42"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.026"},
                {"Beta", "2.025"},
                {"Annual Standard Deviation", "0.388"},
                {"Annual Variance", "0.151"},
                {"Information Ratio", "4.353"},
                {"Tracking Error", "0.197"},
                {"Treynor Ratio", "0.848"},
                {"Total Fees", "$12.30"}
            });
        }

        [Test]
        public void LimitFillRegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("LimitFillRegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "17"},
                {"Average Win", "0.02%"},
                {"Average Loss", "-0.01%"},
                {"Compounding Annual Return", "9.034%"},
                {"Drawdown", "0.200%"},
                {"Expectancy", "0.450"},
                {"Net Profit", "0.101%"},
                {"Sharpe Ratio", "1.714"},
                {"Loss Rate", "35%"},
                {"Win Rate", "65%"},
                {"Profit-Loss Ratio", "1.24"},
                {"Alpha", "-0.081"},
                {"Beta", "0.154"},
                {"Annual Standard Deviation", "0.03"},
                {"Annual Variance", "0.001"},
                {"Information Ratio", "-4.982"},
                {"Tracking Error", "0.162"},
                {"Treynor Ratio", "0.335"},
                {"Total Fees", "$36.00"}
            });
        }

        [Test]
        public void UpdateOrderRegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("UpdateOrderRegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "5"},
                {"Average Win", "0.01%"},
                {"Average Loss", "-0.22%"},
                {"Compounding Annual Return", "-0.386%"},
                {"Drawdown", "1.100%"},
                {"Expectancy", "-0.794"},
                {"Net Profit", "-0.771%"},
                {"Sharpe Ratio", "-0.88"},
                {"Loss Rate", "80%"},
                {"Win Rate", "20%"},
                {"Profit-Loss Ratio", "0.03"},
                {"Alpha", "-0.004"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0.004"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-1.818"},
                {"Tracking Error", "0.11"},
                {"Treynor Ratio", "-11.909"},
                {"Total Fees", "$11.05"}
            });
        }

        [Test]
        public void BasicTemplateFillForwardAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("BasicTemplateFillForwardAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "1"},
                {"Average Win", "34.56%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "521.383%"},
                {"Drawdown", "18.400%"},
                {"Expectancy", "0"},
                {"Net Profit", "34.562%"},
                {"Sharpe Ratio", "2.599"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "0.899"},
                {"Beta", "2.879"},
                {"Annual Standard Deviation", "0.785"},
                {"Annual Variance", "0.616"},
                {"Information Ratio", "2.192"},
                {"Tracking Error", "0.749"},
                {"Treynor Ratio", "0.708"},
                {"Total Fees", "$460.82"}
            });
        }

        [Test]
        public void RegressionAlgorithm()
        {
            AlgorithmRunner.RunLocalBacktest("RegressionAlgorithm", new Dictionary<string, string>
            {
                {"Total Trades", "2145"},
                {"Average Win", "0.00%"},
                {"Average Loss", "0.00%"},
                {"Compounding Annual Return", "-3.361%"},
                {"Drawdown", "0.000%"},
                {"Expectancy", "-0.990"},
                {"Net Profit", "-0.043%"},
                {"Sharpe Ratio", "-28.984"},
                {"Loss Rate", "100%"},
                {"Win Rate", "0%"},
                {"Profit-Loss Ratio", "1.65"},
                {"Alpha", "-0.018"},
                {"Beta", "0"},
                {"Annual Standard Deviation", "0.001"},
                {"Annual Variance", "0"},
                {"Information Ratio", "-4.251"},
                {"Tracking Error", "0.173"},
                {"Treynor Ratio", "611.111"},
                {"Total Fees", "$4292.00"}
            });
        }
    }
}