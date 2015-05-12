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
                {"Average Win", "3.39%"},
                {"Average Loss", "0%"},
                {"Compounding Annual Return", "1629.801%"},
                {"Drawdown", "3.100%"},
                {"Expectancy", "0"},
                {"Net Profit", "3.392%"},
                {"Sharpe Ratio", "4.445"},
                {"Loss Rate", "0%"},
                {"Win Rate", "100%"},
                {"Profit-Loss Ratio", "0"},
                {"Alpha", "-0.016"},
                {"Beta", "2.049"},
                {"Annual Standard Deviation", "0.393"},
                {"Annual Variance", "0.155"},
                {"Information Ratio", "4.403"},
                {"Tracking Error", "0.201"},
                {"Treynor Ratio", "0.853"}
            });
        }
    }
}