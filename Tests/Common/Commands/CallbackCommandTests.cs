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
using System.IO;
using System.Web;
using NUnit.Framework;
using Newtonsoft.Json;
using QuantConnect.Commands;
using QuantConnect.Statistics;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Commands
{
    [TestFixture]
    public class CallbackCommandTests
    {
        [Test]
        public void BaseTypedLink()
        {
            var algorithmStub = new AlgorithmStub
            {
                ProjectId = 19542033
            };
            algorithmStub.AddCommand<MyCommand>();
            var commandInstance = new MyCommand
            {
                Quantity = 0.1m,
                Target = "BTCUSD"
            };
            var link = algorithmStub.Link(commandInstance);

            var parse = HttpUtility.ParseQueryString(link);
            Assert.IsFalse(string.IsNullOrEmpty(link));
        }

        [Test]
        public void ComplexTypedLink()
        {
            var algorithmStub = new AlgorithmStub
            {
                ProjectId = 19542033
            };
            algorithmStub.AddCommand<MyCommand2>();
            var commandInstance = new MyCommand2
            {
                Parameters = new Dictionary<string, object> { { "quantity", 0.1 } },
                Target = new[] { "BTCUSD", "AAAA" }
            };
            var link = algorithmStub.Link(commandInstance);

            var parse = HttpUtility.ParseQueryString(link);
            Assert.IsFalse(string.IsNullOrEmpty(link));
        }

        [Test]
        public void UntypedLink()
        {
            var algorithmStub = new AlgorithmStub
            {
                ProjectId = 19542033
            };
            var link = algorithmStub.Link(new { quantity = -0.1, target = "BTCUSD" });

            var parse = HttpUtility.ParseQueryString(link);
            Assert.IsFalse(string.IsNullOrEmpty(link));
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void CommanCallback(Language language)
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(typeof(CallbackCommandRegressionAlgorithm).Name,
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "3"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "0.212%"},
                    {"Drawdown", "0.000%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "0.003%"},
                    {"Sharpe Ratio", "-5.552"},
                    {"Sortino Ratio", "0"},
                    {"Probabilistic Sharpe Ratio", "66.765%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "-0.01"},
                    {"Beta", "0.003"},
                    {"Annual Standard Deviation", "0.001"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "-8.919"},
                    {"Tracking Error", "0.222"},
                    {"Treynor Ratio", "-1.292"},
                    {"Total Fees", "$3.00"},
                    {"Estimated Strategy Capacity", "$670000000.00"},
                    {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
                    {"Portfolio Turnover", "0.06%"}
                },
                language,
                AlgorithmStatus.Completed);

            Config.Set("lean-manager-type", typeof(TestLocalLeanManager).Name);

            var result = AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        internal class TestLocalLeanManager : LocalLeanManager
        {
            private bool _sentCommands;
            public override void Update()
            {
                if (!_sentCommands && Algorithm.Time.TimeOfDay > TimeSpan.FromHours(9.50))
                {
                    _sentCommands = true;
                    var commands = new List<Dictionary<string, object>>
                    {
                        new()
                        {
                            { "$type", "" },
                            { "id", 1 },
                            { "Symbol", "SPY" },
                            { "Parameters", new Dictionary<string, decimal> { {  "quantity", 1 } } },
                            { "unused", 99 }
                        },
                        new()
                        {
                            { "$type", "VoidCommand" },
                            { "id", null },
                            { "Quantity", 1 },
                            { "targettime", Algorithm.Time },
                            { "target", new [] { "BAC" } },
                            { "Parameters", new Dictionary<string, string> { {  "tag", "a tag" }, { "something", "else" } } },
                        },
                        new()
                        {
                            { "id", "2" },
                            { "$type", "BoolCommand" },
                            { "Result", true },
                            { "unused", new [] { 99 } }
                        },
                        new()
                        {
                            { "$type", "BoolCommand" },
                            { "Result", null },
                        }
                    };

                    for (var i = 1; i <= commands.Count; i++)
                    {
                        var command = commands[i - 1];
                        command["id"] = i;
                        File.WriteAllText($"command-{i}.json", JsonConvert.SerializeObject(command));
                    }
                    base.Update();
                }
            }
            public override void OnAlgorithmStart()
            {
                SetCommandHandler();
            }
        }

        private class MyCommand2 : Command
        {
            public string[] Target { get; set; }
            public Dictionary<string, object> Parameters;
        }

        private class MyCommand : Command
        {
            public string Target { get; set; }
            public decimal Quantity;
        }
    }
}
