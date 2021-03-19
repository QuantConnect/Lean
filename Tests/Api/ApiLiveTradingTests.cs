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
using System.Threading;
using System.Web;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.API
{
    [TestFixture, Ignore("These tests require configured and active accounts to Interactive Brokers, FXCM, Oanda, and Tradier " +
         "as well as your Organization ID and available live nodes")]
    public class ApiLiveTradingTests
    {
        private int _testAccount;
        private string _testToken;
        private string _testOrganization;
        private string _dataFolder;
        private Api.Api _api;
        private const bool stopLiveAlgos = true;

        /// <summary>
        /// Run before test
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            _testAccount = Config.GetInt("job-user-id");
            _testToken = Config.Get("api-access-token");
            _testOrganization = Config.Get("job-organization-id", "EnterOrgHere"); //This org must be your preferred org
            _dataFolder = Config.Get("data-folder");

            _api = new Api.Api();
            _api.Initialize(_testAccount, _testToken, _dataFolder);
        }

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

            RunLiveAlgorithm(settings, file);
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

            RunLiveAlgorithm(settings, file);
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

            RunLiveAlgorithm(settings, file);
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

            RunLiveAlgorithm(settings, file);
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

            RunLiveAlgorithm(settings, file);
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

            RunLiveAlgorithm(settings, file);
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
            var dateIssuedString = "20160920";

            // Tradier Custom Variables
            string dateIssued = "";
            string refreshToken = "";
            string lifetime = "";

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
        [Test, Ignore("Requires a live algorithm to be running")]
        public void LiveAlgorithmsAndLiveLogs_CanBeRead_Successfully()
        {
            // Read all currently running algorithms
            var liveAlgorithms = _api.ListLiveAlgorithms(AlgorithmStatus.Running);

            Assert.IsTrue(liveAlgorithms.Success);
            // There has to be at least one running algorithm
            Assert.IsTrue(liveAlgorithms.Algorithms.Any());

            // Read the logs of the first live algorithm
            var firstLiveAlgo = liveAlgorithms.Algorithms[0];
            var liveLogs = _api.ReadLiveLogs(firstLiveAlgo.ProjectId, firstLiveAlgo.DeployId);

            Assert.IsTrue(liveLogs.Success);
            Assert.IsTrue(liveLogs.Logs.Any());
        }

        /// <summary>
        /// Runs the algorithm with the given settings and file
        /// </summary>
        /// <param name="settings">Settings for Lean</param>
        /// <param name="file">File to run</param>
        private void RunLiveAlgorithm(BaseLiveAlgorithmSettings settings, ProjectFile file)
        {
            // Create a new project
            var project = _api.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp);

            // Add Project Files
            var addProjectFile = _api.AddProjectFile(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(addProjectFile.Success);

            // Create compile
            var compile = _api.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            Compile compileCheck = WaitForCompilerResponse(project.Projects.First().ProjectId, compile.CompileId, 30);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodes = _api.ReadNodes(_testOrganization);
            Assert.IsTrue(nodes.Success);
            var freeNode = nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.Projects.First().ProjectId, compile.CompileId, freeNode.FirstOrDefault().Id, settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm; will also stop algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
                Assert.IsTrue(deleteProject.Success);
            }
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
                compile = _api.ReadCompile(projectId, compileId);
                if (compile.State == CompileState.BuildSuccess) break;
            }
            return compile;
        }
    }
}
