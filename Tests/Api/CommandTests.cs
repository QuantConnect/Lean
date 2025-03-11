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
using NUnit.Framework;
using QuantConnect.Api;
using System.Threading;
using System.Collections.Generic;

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("Requires configured api access, a live node to run on, and brokerage configurations.")]
    public class CommandTests
    {
        private Api.Api _apiClient;

        [OneTimeSetUp]
        public void Setup()
        {
            ApiTestBase.ReloadConfiguration();

            _apiClient = new Api.Api();
            _apiClient.Initialize(Globals.UserId, Globals.UserToken, Globals.DataFolder);
        }

        [TestCase("MyCommand")]
        [TestCase("MyCommand2")]
        [TestCase("MyCommand3")]
        [TestCase("")]
        public void LiveCommand(string commandType)
        {
            var command = new Dictionary<string, object>
            {
                { "quantity", 0.1 },
                { "target", "BTCUSD" },
                { "$type", commandType }
            };

            var projectId = RunLiveAlgorithm();
            try
            {
                // allow algo to be deployed and prices to be set so we can trade
                Thread.Sleep(TimeSpan.FromSeconds(10));
                var result = _apiClient.CreateLiveCommand(projectId, command);
                Assert.IsTrue(result.Success);
            }
            finally
            {
                _apiClient.StopLiveAlgorithm(projectId);
                _apiClient.DeleteProject(projectId);
            }
        }

        [TestCase("MyCommand")]
        [TestCase("MyCommand2")]
        [TestCase("MyCommand3")]
        [TestCase("")]
        [TestCase("", true)]
        public void BroadcastCommand(string commandType, bool excludeProject = false)
        {
            var command = new Dictionary<string, object>
            {
                { "quantity", 0.1 },
                { "target", "BTCUSD" },
                { "$type", commandType }
            };

            var projectId = RunLiveAlgorithm();
            try
            {
                // allow algo to be deployed and prices to be set so we can trade
                Thread.Sleep(TimeSpan.FromSeconds(10));
                // Our project will not receive the broadcast, unless we pass null value, but we can still use it to send a command
                var result = _apiClient.BroadcastLiveCommand(Globals.OrganizationID, excludeProject ? projectId : null, command);
                Assert.IsTrue(result.Success);
            }
            finally
            {
                _apiClient.StopLiveAlgorithm(projectId);
                _apiClient.DeleteProject(projectId);
            }
        }

        private int RunLiveAlgorithm()
        {
            var settings = new Dictionary<string, object>()
            {
                { "id", "QuantConnectBrokerage" },
                { "environment", "paper" },
                { "user", "" },
                { "password", "" },
                { "account", "" }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = @"from AlgorithmImports import *

class MyCommand():
    quantity = 0
    target = ''
    def run(self, algo: QCAlgorithm) -> bool | None:
        self.execute_order(algo)
    def execute_order(self, algo):
        algo.order(self.target, self.quantity)

class MyCommand2():
    quantity = 0
    target = ''
    def run(self, algo: QCAlgorithm) -> bool | None:
        algo.order(self.target, self.quantity)
        return True

class MyCommand3():
    quantity = 0
    target = ''
    def run(self, algo: QCAlgorithm) -> bool | None:
        algo.order(self.target, self.quantity)
        return False

class DeterminedSkyBlueGorilla(QCAlgorithm):
    def initialize(self):
        self.set_start_date(2023, 3, 17)
        self.add_crypto(""BTCUSD"", Resolution.SECOND)
        self.add_command(MyCommand)
        self.add_command(MyCommand2)
        self.add_command(MyCommand3)

    def on_command(self, data):
        self.order(data.target, data.quantity)"
            };

            // Run the live algorithm
            return LiveTradingTests.RunLiveAlgorithm(_apiClient, settings, file, stopLiveAlgos: false, language: Language.Python);
        }
    }
}
