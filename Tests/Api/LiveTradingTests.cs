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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Live endpoint tests
    /// </summary>
    [TestFixture, Explicit("Requires configured api access, a live node to run on, and brokerage configurations.")]
    public class LiveTradingTests : ApiTestBase
    {
        private const bool StopLiveAlgos = true;

        /// <summary>
        /// Live paper trading via Interactive Brokers
        /// </summary>
        [Test]
        public void LiveIBTest()
        {
            var user = Config.Get("ib-user-name");
            var password = Config.Get("ib-password");
            var account = Config.Get("ib-account");

            // Create default algorithm settings
            var settings = new InteractiveBrokersLiveAlgorithmSettings(user,
                password,
                account);

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live paper trading via FXCM
        /// </summary>
        [Test]
        public void LiveFXCMTest()
        {
            var user = Config.Get("fxcm-user-name");
            var password = Config.Get("fxcm-password");
            var account = Config.Get("fxcm-account-id");

            // Create default algorithm settings
            var settings = new FXCMLiveAlgorithmSettings(user,
                password,
                BrokerageEnvironment.Paper,
                account);

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live paper trading via Oanda
        /// </summary>
        [Test]
        public void LiveOandaTest()
        {
            var token = Config.Get("oanda-access-token");
            var account = Config.Get("oanda-account-id");

            // Create default algorithm settings
            var settings = new OandaLiveAlgorithmSettings(token,
                BrokerageEnvironment.Paper,
                account);

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live paper trading via Tradier
        /// </summary>
        [Test]
        public void LiveTradierTest()
        {
            var refreshToken = Config.Get("tradier-refresh-token");
            var account = Config.Get("tradier-account-id");
            var accessToken = Config.Get("tradier-access-token");
            var dateIssued = Config.Get("tradier-issued-at");

            // Create default algorithm settings
            var settings = new TradierLiveAlgorithmSettings(
                accessToken,
                dateIssued,
                refreshToken,
                account
            );

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live trading via Bitfinex
        /// </summary>
        [Test]
        public void LiveBitfinexTest()
        {
            var key = Config.Get("bitfinex-api-key");
            var secretKey = Config.Get("bitfinex-api-secret");

            // Create default algorithm settings
            var settings = new BitfinexLiveAlgorithmSettings(key, secretKey);

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Live trading via GDAX (Coinbase)
        /// </summary>
        [Test]
        public void LiveGDAXTest()
        {
            var key = Config.Get("gdax-api-key");
            var secretKey = Config.Get("gdax-api-secret");
            var passphrase = Config.Get("gdax-passphrase");

            // Create default algorithm settings
            var settings = new GDAXLiveAlgorithmSettings(key, secretKey, passphrase);

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            RunLiveAlgorithm(settings, file, StopLiveAlgos);
        }

        /// <summary>
        /// Test creating the settings object that provide the necessary parameters for each broker
        /// </summary>
        [Test]
        public void LiveAlgorithmSettings_CanBeCreated_Successfully()
        {
            string user = "";
            string password = "";
            BrokerageEnvironment environment = BrokerageEnvironment.Paper;
            string account = "";
            string key = "";
            string secretKey = "";

            // Oanda Custom Variables
            string accessToken = "";

            // Tradier Custom Variables
            string dateIssued = "";
            string refreshToken = "";

            // Create and test settings for each brokerage
            foreach (BrokerageName brokerageName in Enum.GetValues(typeof(BrokerageName)))
            {
                BaseLiveAlgorithmSettings settings = null;

                switch (brokerageName)
                {
                    case BrokerageName.Default:
                        user = Config.Get("default-username");
                        password = Config.Get("default-password");
                        settings = new DefaultLiveAlgorithmSettings(user, password, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.Default.ToString());
                        break;
                    case BrokerageName.FxcmBrokerage:
                        user = Config.Get("fxcm-user-name");
                        password = Config.Get("fxcm-password");
                        settings = new FXCMLiveAlgorithmSettings(user, password, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.FxcmBrokerage.ToString());
                        break;
                    case BrokerageName.InteractiveBrokersBrokerage:
                        user = Config.Get("ib-user-name");
                        password = Config.Get("ib-password");
                        account = Config.Get("ib-account");
                        settings = new InteractiveBrokersLiveAlgorithmSettings(user, password, account);

                        Assert.IsTrue(settings.Id == BrokerageName.InteractiveBrokersBrokerage.ToString());
                        break;
                    case BrokerageName.OandaBrokerage:
                        accessToken = Config.Get("oanda-access-token");
                        account = Config.Get("oanda-account-id");

                        settings = new OandaLiveAlgorithmSettings(accessToken, environment, account);
                        Assert.IsTrue(settings.Id == BrokerageName.OandaBrokerage.ToString());
                        break;
                    case BrokerageName.TradierBrokerage:
                        dateIssued = Config.Get("tradier-issued-at");
                        refreshToken = Config.Get("tradier-refresh-token");
                        account = Config.Get("tradier-account-id");

                        settings = new TradierLiveAlgorithmSettings(refreshToken, dateIssued, refreshToken, account);
                        break;
                    case BrokerageName.Bitfinex:
                        key = Config.Get("bitfinex-api-key");
                        secretKey = Config.Get("bitfinex-api-secret");

                        settings = new BitfinexLiveAlgorithmSettings(key, secretKey);
                        break;
                    case BrokerageName.GDAX:
                        key = Config.Get("gdax-api-key");
                        secretKey = Config.Get("gdax-api-secret");
                        var passphrase = Config.Get("gdax-api-passphrase");

                        settings = new GDAXLiveAlgorithmSettings(key, secretKey, passphrase);
                        break;
                    case BrokerageName.AlphaStreams:
                        // No live algorithm settings
                        settings = new BaseLiveAlgorithmSettings();
                        break;
                    case BrokerageName.Binance:
                        // No live algorithm settings
                        settings = new BaseLiveAlgorithmSettings();
                        break;
                    default:
                        throw new Exception($"Settings have not been implemented for this brokerage: {brokerageName}");
                }

                // Tests common to all brokerage configuration classes
                Assert.IsTrue(settings != null);
                Assert.IsTrue(settings.Password == password);
                Assert.IsTrue(settings.User == user);

                // Oanda specific settings
                if (brokerageName == BrokerageName.OandaBrokerage)
                {
                    var oandaSetting = settings as OandaLiveAlgorithmSettings;

                    Assert.IsTrue(oandaSetting.AccessToken == accessToken);
                }

                // Tradier specific settings
                if (brokerageName == BrokerageName.TradierBrokerage)
                {
                    var tradierLiveAlogrithmSettings = settings as TradierLiveAlgorithmSettings;

                    Assert.IsTrue(tradierLiveAlogrithmSettings.DateIssued == dateIssued);
                    Assert.IsTrue(tradierLiveAlogrithmSettings.RefreshToken == refreshToken);
                    Assert.IsTrue(settings.Environment == BrokerageEnvironment.Live);
                }

                // reset variables
                user = "";
                password = "";
                environment = BrokerageEnvironment.Paper;
                account = "";
            }
        }

        /// <summary>
        /// Reading live algorithm tests
        ///   - Get a list of live algorithms
        ///   - Get logs for the first algorithm returned
        /// Will there always be a live algorithm for the test user?
        /// </summary>
        [Test]
        public void LiveAlgorithmsAndLiveLogs_CanBeRead_Successfully()
        {
            // Read all currently running algorithms
            var liveAlgorithms = ApiClient.ListLiveAlgorithms(AlgorithmStatus.Running);

            Assert.IsTrue(liveAlgorithms.Success);
            // There has to be at least one running algorithm
            Assert.IsTrue(liveAlgorithms.Algorithms.Any());

            // Read the logs of the first live algorithm
            var firstLiveAlgo = liveAlgorithms.Algorithms[0];
            var liveLogs = ApiClient.ReadLiveLogs(firstLiveAlgo.ProjectId, firstLiveAlgo.DeployId);

            Assert.IsTrue(liveLogs.Success);
            Assert.IsTrue(liveLogs.Logs.Any());
        }

        /// <summary>
        /// Runs the algorithm with the given settings and file
        /// </summary>
        /// <param name="settings">Settings for Lean</param>
        /// <param name="file">File to run</param>
        /// <param name="stopLiveAlgos">If true the algorithm will be stopped at the end of the method.
        /// Otherwise, it will keep running</param>
        /// <returns>The id of the project created with the algorithm in</returns>
        private int RunLiveAlgorithm(BaseLiveAlgorithmSettings settings, ProjectFile file, bool stopLiveAlgos)
        {
            // Create a new project
            var project = ApiClient.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(projectId, compile.CompileId, 30);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            // Create live default algorithm
            var createLiveAlgorithm = ApiClient.CreateLiveAlgorithm(projectId, compile.CompileId, freeNode.FirstOrDefault().Id, settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm; will also stop algorithm
                var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
                Assert.IsTrue(liquidateLive.Success);

                // Delete the project
                var deleteProject = ApiClient.DeleteProject(projectId);
                Assert.IsTrue(deleteProject.Success);
            }

            return projectId;
        }

        [Test]
        public void ReadLiveOrders()
        {
            // Create default algorithm settings
            var settings = new DefaultLiveAlgorithmSettings("", "", BrokerageEnvironment.Paper, "");

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            // Run the live algorithm
            var projectId = RunLiveAlgorithm(settings, file, false);

            // Wait to receive the orders
            var readLiveOrders = WaitForReadLiveOrdersResponse(projectId, 60 * 5);
            Assert.IsTrue(readLiveOrders.Any());
            Assert.AreEqual(Symbols.SPY, readLiveOrders.First().Symbol);

            // Liquidate live algorithm; will also stop algorithm
            var liquidateLive = ApiClient.LiquidateLiveAlgorithm(projectId);
            Assert.IsTrue(liquidateLive.Success);

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(projectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Wait for the compiler to respond to a specified compile request
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="compileId">Id of the compilation of the project</param>
        /// <param name="seconds">Seconds to allow for compile time</param>
        /// <returns></returns>
        private Compile WaitForCompilerResponse(int projectId, string compileId, int seconds)
        {
            var compile = new Compile();
            var finish = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < finish)
            {
                Thread.Sleep(1000);
                compile = ApiClient.ReadCompile(projectId, compileId);
                if (compile.State == CompileState.BuildSuccess) break;
            }
            return compile;
        }

        /// <summary>
        /// Wait to receive at least one order
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="seconds">Seconds to allow for receive an order</param>
        /// <returns></returns>
        private List<Order> WaitForReadLiveOrdersResponse(int projectId, int seconds)
        {
            var readLiveOrders = new List<Order>();
            var finish = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < finish && !readLiveOrders.Any())
            {
                Thread.Sleep(10000);
                readLiveOrders = ApiClient.ReadLiveOrders(projectId);
            }
            return readLiveOrders;
        }
    }
}
