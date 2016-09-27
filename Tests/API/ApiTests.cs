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
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.API;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using RestSharp.Extensions.MonoHttp;

namespace QuantConnect.Tests.API
{
    [TestFixture, Category("TravisExclude")]
    class RestApiTests
    {
        //Test Authentication Credentials
        private int _testAccount = 1;
        private string _testToken = "ec87b337ac970da4cbea648f24f1c851";
        private string _dataFolder = Config.Get("data-folder");
        private int _testProjectId = 339925;
        private Api.Api _api;

        [SetUp]
        public void Setup()
        {
            _api = new Api.Api();
            _api.Initialize(_testAccount, _testToken, _dataFolder);
        }

        /// <summary>
        /// Test successfully authenticates with the API using valid credentials.
        /// </summary>
        [Test]
        public void ApiWithValidCredentials_WillAuthenticate_Successfully()
        {
            var connection = new ApiConnection(_testAccount, _testToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Rejects invalid credentials
        /// </summary>
        [Test]
        public void ApiWithInvalidValidCredentials_WillAuthenticate_Unsuccessfully()
        {
            var connection = new ApiConnection(_testAccount, "");
            Assert.IsFalse(connection.Connected);
        }

        /// <summary>
        /// Tests all the API methods linked to a project id.
        ///  - Creates project,
        ///  - Adds files to project,
        ///  - Updates the files, makes sure they are still present,
        ///  - Builds the project,
        /// </summary>
        [Test]
        public void CreatesProjectCompilesAndBacktestsProject()
        {
            var sources = LanguageSourcesList(SecurityType.Equity);

            foreach (var source in sources)
            {
                // Test create a new project successfully
                var name = DateTime.UtcNow.ToString("u") + " Test " + _testAccount + " Lang " + source.Language;
                var project = _api.CreateProject(name, source.Language);
                Assert.IsTrue(project.Success);
                Assert.IsTrue(project.ProjectId > 0);
                Console.WriteLine("API Test: {0} Project created successfully", source.Language);

                // Gets the list of projects from the account.
                // Should at least be the one we created.
                var projects = _api.ProjectList();
                Assert.IsTrue(projects.Success);
                Assert.IsTrue(projects.Projects.Count >= 1);
                Console.WriteLine("API Test: All Projects listed successfully");

                // Test read back the project we just created
                var readProject = _api.ReadProject(project.ProjectId);
                Assert.IsTrue(readProject.Success);
                Assert.IsTrue(readProject.Files.Count == 0);
                Assert.IsTrue(readProject.Name == name);
                Console.WriteLine("API Test: {0} Project read successfully", source.Language);

                // Test set a project file for the project
                var files = NewProjectFile(source);
                var updateProject = _api.UpdateProject(project.ProjectId, files);
                Assert.IsTrue(updateProject.Success);
                Console.WriteLine("API Test: {0} Project updated successfully", source.Language);

                // Download the project again to validate its got the new file
                var verifyRead = _api.ReadProject(project.ProjectId);
                Assert.IsTrue(verifyRead.Files.Count == 1);
                Assert.IsTrue(verifyRead.Files.First().Name == source.Name);
                Console.WriteLine("API Test: {0} Project read back successfully", source.Language);

                // Test successfully compile the project we've created
                var compileCreate = _api.CreateCompile(project.ProjectId);
                Assert.IsTrue(compileCreate.Success);
                Assert.IsTrue(compileCreate.State == CompileState.InQueue);
                Console.WriteLine("API Test: {0} Compile created successfully", source.Language);

                //Read out the compile; wait for it to be completed for 10 seconds
                var compileSuccess = WaitForCompilerResponse(_api, project.ProjectId, compileCreate.CompileId);
                Assert.IsTrue(compileSuccess.Success);
                Assert.IsTrue(compileSuccess.State == CompileState.BuildSuccess);
                Console.WriteLine("API Test: {0} Project built successfully", source.Language);

                // Update the file, create a build error, test we get build error
                files[0].Code += "[Jibberish at end of the file to cause a build error]";
                _api.UpdateProject(project.ProjectId, files);
                var compileError = _api.CreateCompile(project.ProjectId);
                compileError = WaitForCompilerResponse(_api, project.ProjectId, compileError.CompileId);
                Assert.IsTrue(compileError.Success); // Successfully processed rest request.
                Assert.IsTrue(compileError.State == CompileState.BuildError); //Resulting in build fail.
                Console.WriteLine("API Test: {0} Project errored successfully", source.Language);

                // Using our successful compile; launch a backtest!
                var backtestName = DateTime.Now.ToString("u") + " API Backtest";
                var backtest = _api.CreateBacktest(project.ProjectId, compileSuccess.CompileId, backtestName);
                Assert.IsTrue(backtest.Success);
                Console.WriteLine("API Test: {0} Backtest created successfully", source.Language);

                // Now read the backtest and wait for it to complete
                var backtestRead = WaitForBacktestCompletion(_api, project.ProjectId, backtest.BacktestId);
                Assert.IsTrue(backtestRead.Success);
                Assert.IsTrue(backtestRead.Progress == 1);
                Assert.IsTrue(backtestRead.Name == backtestName);
                Assert.IsTrue(backtestRead.Result.Statistics["Total Trades"] == "1");
                Console.WriteLine("API Test: {0} Backtest completed successfully", source.Language);

                // Verify we have the backtest in our project
                var listBacktests = _api.BacktestList(project.ProjectId);
                Assert.IsTrue(listBacktests.Success);
                Assert.IsTrue(listBacktests.Backtests.Count >= 1);
                Assert.IsTrue(listBacktests.Backtests[0].Name == backtestName);
                Console.WriteLine("API Test: {0} Backtests listed successfully", source.Language);

                // Update the backtest name and test its been updated
                backtestName += "-Amendment";
                var renameBacktest = _api.UpdateBacktest(project.ProjectId, backtest.BacktestId, backtestName);
                Assert.IsTrue(renameBacktest.Success);
                backtestRead = _api.ReadBacktest(project.ProjectId, backtest.BacktestId);
                Assert.IsTrue(backtestRead.Name == backtestName);
                Console.WriteLine("API Test: {0} Backtest renamed successfully", source.Language);

                //Update the note and make sure its been updated:
                var newNote = DateTime.Now.ToString("u");
                var noteBacktest = _api.UpdateBacktest(project.ProjectId, backtest.BacktestId, note: newNote);
                Assert.IsTrue(noteBacktest.Success);
                backtestRead = _api.ReadBacktest(project.ProjectId, backtest.BacktestId);
                Assert.IsTrue(backtestRead.Note == newNote);
                Console.WriteLine("API Test: {0} Backtest note added successfully", source.Language);

                // Delete the backtest we just created
                var deleteBacktest = _api.DeleteBacktest(project.ProjectId, backtest.BacktestId);
                Assert.IsTrue(deleteBacktest.Success);
                Console.WriteLine("API Test: {0} Backtest deleted successfully", source.Language);

                // Test delete the project we just created
                var deleteProject = _api.Delete(project.ProjectId);
                Assert.IsTrue(deleteProject.Success);
                Console.WriteLine("API Test: {0} Project deleted successfully", source.Language);
            }
        }



        /// <summary>
        /// Test creating the settings object that provide the necessary parameters for each broker
        /// </summary>
        [Test]
        public void LiveAlgorithmSettings_CanBeCreated_Successfully()
        {
            string user = "";
            string password = "";
            string environment = "Live";
            string account = "1";

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
                        settings = new DefaultBaseLiveAlogrithmSettings(user, password, BrokerageName.Default, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.Default.ToString());
                        break;
                    case BrokerageName.FxcmBrokerage:
                        user = Config.Get("fxcm-user-name");
                        password = Config.Get("fxcm-password");
                        settings = new FxcmBaseLiveAlogrithmSettings(user, password, BrokerageName.FxcmBrokerage, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.FxcmBrokerage.ToString());
                        break;
                    case BrokerageName.InteractiveBrokersBrokerage:
                        user = Config.Get("ib-user-name");
                        password = Config.Get("ib-password");
                        settings = new InteractiveBrokersBaseLiveAlogrithmSettings(user, password, BrokerageName.InteractiveBrokersBrokerage, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.InteractiveBrokersBrokerage.ToString());
                        break;
                    case BrokerageName.OandaBrokerage:
                        user = Config.Get("oandaUser");
                        password = Config.Get("oandaPassword");
                        accessToken = Config.Get("oandaDate");
                        settings = new OandaBaseLiveAlogrithmSettings(accessToken, dateIssuedString, user, password, BrokerageName.OandaBrokerage, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.OandaBrokerage.ToString());
                        break;
                    case BrokerageName.TradierBrokerage:
                        user = Config.Get("tradierUser");
                        password = Config.Get("tradierPassword");
                        dateIssued = Config.Get("tradierDateIssued");
                        refreshToken = Config.Get("tradierRefreshToken");
                        lifetime = Config.Get("tradierLifetime");
                        settings = new TradierBaseLiveAlogrithmSettings(refreshToken, dateIssued, refreshToken, lifetime, user, password, BrokerageName.TradierBrokerage, environment, account);

                        break;
                    default:
                        throw new Exception("Settings have not been implemented for this brokerage: " + brokerageName.ToString());
                }


                Assert.IsTrue(settings != null);
                Assert.IsTrue(settings.Password == password);
                Assert.IsTrue(settings.User == user);
                Assert.IsTrue(settings.Environment == environment);

                // Oanda specific settings
                if (brokerageName == BrokerageName.OandaBrokerage)
                {
                    var oandaSetting = settings as OandaBaseLiveAlogrithmSettings;
                    Assert.IsTrue(oandaSetting.AccessToken == accessToken);
                    Assert.IsTrue(oandaSetting.DateIssued == dateIssuedString);
                }

                // Tradier specific settings
                if (brokerageName == BrokerageName.TradierBrokerage)
                {
                    var tradierLiveAlogrithmSettings = settings as TradierBaseLiveAlogrithmSettings;
                    Assert.IsTrue(tradierLiveAlogrithmSettings.DateIssued == dateIssued);
                    Assert.IsTrue(tradierLiveAlogrithmSettings.RefreshToken == refreshToken);
                    Assert.IsTrue(tradierLiveAlogrithmSettings.Lifetime == lifetime);
                }
            }
        }


        /// <summary>
        /// Reading live algorithm tests
        ///   - Get a list of live algorithms
        ///   - Get logs for the first algorithm returned
        /// </summary>
        [Test]
        public void LiveAlgorithms_AndLiveLogs_CanBeRead()
        {
            // Read all previously deployed algorithms
            var liveAlgorithms = _api.ListLive();

            Assert.IsTrue(liveAlgorithms.Success);
            Assert.IsTrue(liveAlgorithms.Algorithms.Any());

            // Read the logs of the first live algorithm
            var firstLiveAlgo = liveAlgorithms.Algorithms[0];
            var liveLogs = _api.ReadLiveLogs(firstLiveAlgo.ProjectId.ToInt32(), firstLiveAlgo.DeployId);

            Assert.IsTrue(liveLogs.Success);
            Assert.IsTrue(liveLogs.Logs.Any());
        }

        /// <summary>
        /// Paper trading FXCM
        /// </summary>
        [Test]
        public void FXCMLiveAlgorithm_EntireLifeCycle_Test()
        {
            var user = "1";
            var password = "2";
            var account = "3";

            // Create project
            var project = _api.CreateProject("Live Test: " + DateTime.Now, Language.CSharp);
            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs") }
                };

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new FxcmBaseLiveAlogrithmSettings(user,
                                                             password,
                                                             BrokerageName.FxcmBrokerage,
                                                             "paper",
                                                             account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            // Liquidate live algorithm
            var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(liquidateLive.Success);

            // Stop live algorithm
            var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(stopLive.Success);

            // Delete project
            var deleteProject = _api.Delete(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Live paper trading via IB.
        /// </summary>
        [Test]
        public void InteractiveBroker_EntireLifeCycle_Live_Test()
        {
            var user = "1";
            var password = "2";
            var account = "3";

            // Create project
            var project = _api.CreateProject("Live Test: " + DateTime.Now, Language.CSharp);
            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs" )}
                };

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new InteractiveBrokersBaseLiveAlogrithmSettings(user,
                                                                           password, BrokerageName.InteractiveBrokersBrokerage,
                                                                           "paper",
                                                                           account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            // Liquidate live algorithm
            var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(liquidateLive.Success);

            // Stop live algorithm
            var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(stopLive.Success);

            // Delete project
            var deleteProject = _api.Delete(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Live paper trading via Oanda
        /// </summary>
        [Test]
        public void OandaLiveAlgorithm_EntireLifeCycle_Test()
        {
            var user = "1";
            var dateIssued = "20160923";
            var password = "2";
            var token = "3";
            var account = "4";

            // Create project
            var project = _api.CreateProject("Live Test: " + DateTime.Now, Language.CSharp);
            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs" ) }
                };

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new OandaBaseLiveAlogrithmSettings(token,
                                                              dateIssued,
                                                              user,
                                                              password,
                                                              BrokerageName.OandaBrokerage,
                                                              "paper",
                                                              account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            // Liquidate live algorithm
            var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(liquidateLive.Success);

            // Stop live algorithm
            var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(stopLive.Success);

            // Delete project
            var deleteProject = _api.Delete(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Live paper trading via Tradier
        /// </summary>
        [Test]
        public void TradierLiveAlgorithm_EntireLifeCycle_Test()
        {
            var user = "";
            var password = "";

            string refreshToken = "0";
            string lifetime = "1";
            var account = "2";
            var accessToken = "3";

            var dateIssued = DateTime.Parse("9/23/2016");

            // Create project
            var project = _api.CreateProject("Live Test: " + DateTime.Now, Language.CSharp);
            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs" )}
                };

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            var readProject = _api.ReadProject(project.ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.IsTrue(readProject.Files.Count == 1);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new TradierBaseLiveAlogrithmSettings(accessToken,
                                                                dateIssued.ToString("yyyyMMdd"),
                                                                refreshToken,
                                                                lifetime,
                                                                user,
                                                                password,
                                                                BrokerageName.TradierBrokerage,
                                                                "live",
                                                                account);



            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            // Liquidate live algorithm
            var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(liquidateLive.Success);

            // Stop live algorithm
            var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
            Assert.IsTrue(stopLive.Success);

            // Delete project
            var deleteProject = _api.Delete(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Test stopping an algorithm deployed live
        /// </summary>
        [Test]
        public void Test_Stopping_LiveAlgo()
        {
            // Stop live algorithm
            var stopLive = _api.StopLiveAlgorithm(_testProjectId);
            Assert.IsTrue(stopLive.Success);
        }

        /// <summary>
        /// Test liquidating a live algorithm
        /// </summary>
        [Test]
        public void Test_Liquidating_LiveAlgo()
        {
            // Stop live algorithm
            var liquidateLive = _api.LiquidateLiveAlgorithm(_testProjectId);
            Assert.IsTrue(liquidateLive.Success);
        }

        /// <summary>
        /// Test getting links to forex data for FXCM
        /// </summary>
        [Test]
        public void GetLinks_ToDownloadData_ForFXCM()
        {
            var minuteDataLink = _api.ReadDataLink(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD"),
                Resolution.Minute, new DateTime(2013, 10, 07));
            var dailyDataLink = _api.ReadDataLink(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD"),
                Resolution.Daily, new DateTime(2013, 10, 07));

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }

        /// <summary>
        /// Test getting links to forex data for Oanda
        /// </summary>
        [Test]
        public void GetLinks_ToDownloadData_ForOanda()
        {
            var minuteDataLink = _api.ReadDataLink(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                Resolution.Minute, new DateTime(2013, 10, 07));
            var dailyDataLink = _api.ReadDataLink(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                Resolution.Daily, new DateTime(2013, 10, 07));

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }

        /// <summary>
        /// Test downloading data that does not come with the repo (Oanda)
        /// </summary>
        [Test]
        public void Download_AndSave_DataCorrectly()
        {
            var minutePath = Path.Combine(_dataFolder, "forex/oanda/minute/eurusd/20131011_quote.zip");
            var dailyPath  = Path.Combine(_dataFolder, "forex/oanda/daily/eurusd.zip");

            if (File.Exists(dailyPath))
                File.Delete(dailyPath);

            if (File.Exists(minutePath))
                File.Delete(minutePath);

            var downloadedMinuteData = _api.DownloadData(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                Resolution.Minute, new DateTime(2013, 10, 11));
            var downloadedDailyData = _api.DownloadData(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                Resolution.Daily, new DateTime(2013, 10, 07));

            Assert.IsTrue(downloadedMinuteData);
            Assert.IsTrue(downloadedDailyData);

            Assert.IsTrue(File.Exists(dailyPath));
            Assert.IsTrue(File.Exists(minutePath));
        }

        /// <summary>
        /// Test downloading non existent data
        /// </summary>
        [Test]
        public void TryDownload_NonExistent_Data()
        {
            var nonExistentData = _api.DownloadData(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
               Resolution.Minute, new DateTime(1989, 10, 11));

            Assert.IsFalse(nonExistentData);
        }

        /// <summary>
        /// Wait for the compiler to respond to a specified compile request
        /// </summary>
        /// <param name="_api">API Method</param>
        /// <param name="projectId"></param>
        /// <param name="compileId"></param>
        /// <returns></returns>
        private Compile WaitForCompilerResponse(IApi _api, int projectId, string compileId)
        {
            var compile = new Compile();
            var finish = DateTime.Now.AddSeconds(30);
            while (DateTime.Now < finish)
            {
                compile = _api.ReadCompile(projectId, compileId);
                if (compile.State != CompileState.InQueue) break;
                Thread.Sleep(500);
            }
            return compile;
        }

        /// <summary>
        /// Wait for the backtest to complete
        /// </summary>
        /// <param name="_api">IApi Object to make requests</param>
        /// <param name="projectId">Project id to scan</param>
        /// <param name="backtestId">Backtest id previously started</param>
        /// <returns>Completed backtest object</returns>
        private Backtest WaitForBacktestCompletion(IApi _api, int projectId, string backtestId)
        {
            var result = new Backtest();
            var finish = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < finish)
            {
                result = _api.ReadBacktest(projectId, backtestId);
                if (result.Progress == 1) break;
                if (!result.Success) break;
                Thread.Sleep(500);
            }
            return result;
        }

        class TestAlgorithm
        {
            public Language Language;
            public string Code;
            public string Name;
            public SecurityType Type;

            public TestAlgorithm(Language language, string name, string code, SecurityType type)
            {
                Language = language;
                Code = code;
                Name = name;
                Type = type;
            }
        }

        private static List<ProjectFile> NewProjectFile(TestAlgorithm source)
        {
            return new List<ProjectFile>
                {
                    new ProjectFile { Name = source.Name, Code = source.Code }
                };
        }

        private static List<TestAlgorithm> LanguageSourcesList(SecurityType type)
        {
            return new List<TestAlgorithm>()
            {
                new TestAlgorithm(Language.CSharp, "main.cs", File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs"), type),
                new TestAlgorithm(Language.FSharp, "main.fs", File.ReadAllText("../../../Algorithm.FSharp/BasicTemplateAlgorithm.fs"), type),
                new TestAlgorithm(Language.Python, "main.py", File.ReadAllText("../../../Algorithm.Python/BasicTemplateAlgorithm.py"), type)
            };
        }
    }


}
