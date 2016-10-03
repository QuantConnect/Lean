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
using RestSharp.Extensions.MonoHttp;

namespace QuantConnect.Tests.API
{
    [TestFixture, Ignore("These tests require configured and active accounts to Tradier, FXCM and Oanda")]
    class RestApiTests
    {
        private int _testAccount = 1;
        private string _testToken = "ec87b337ac970da4cbea648f24f1c851";
        private string _dataFolder = Config.Get("data-folder");
        private Api.Api _api;
        private const bool stopLiveAlgos = true;

        /// <summary>
        /// Run before every test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _api = new Api.Api();
            _api.Initialize(_testAccount, _testToken, _dataFolder);
        }

        /// <summary>
        /// Test creating and deleting projects with the Api
        /// </summary>
        [Test]
        public void Projects_CanBeCreatedAndDeleted_Successfully()
        {
            var name = "Test Project " + DateTime.Now;

            //Test create a new project successfully
            var project = _api.CreateProject(name, Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.ProjectId > 0);
            Assert.IsTrue(project.Name == name);
            Assert.IsTrue(project.Files.Count == 0);

            // Delete the project
            var deleteProject = _api.DeleteProject(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);

            // Make sure the project is really deleted
            var projectList = _api.ListProjects();
            Assert.IsFalse(projectList.Projects.Any(p => p.ProjectId == project.ProjectId));
        }

        /// <summary>
        /// Test successfully authenticating with the API using valid credentials.
        /// </summary>
        [Test]
        public void ApiWillAuthenticate_ValidCredentials_Successfully()
        {
            var connection = new ApiConnection(_testAccount, _testToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Test that the Api will reject invalid credentials
        /// </summary>
        [Test]
        public void ApiWillAuthenticate_InvalidCredentials_Unsuccessfully()
        {
            var connection = new ApiConnection(_testAccount, "");
            Assert.IsFalse(connection.Connected);
        }

        /// <summary>
        /// Test updating the files associated with a project
        /// </summary>
        [Test]
        public void Update_ProjectFiles_Successfully()
        {
            var unrealFiles = new List<ProjectFile>
                {
                    new ProjectFile
                    {
                        Name = "Hello.cs",
                        Code = HttpUtility.HtmlEncode("Hello, world!" )
                    }
                };

            var realFiles = new List<ProjectFile>
                {
                    new ProjectFile
                    {
                        Name = "main.cs",
                        Code = HttpUtility.HtmlEncode(File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs" ))
                    }
                };

            // Create a new project and make sure there are no files
            var project = _api.CreateProject("Test project - " + DateTime.Now, Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.ProjectId > 0);
            Assert.IsTrue(project.Files.Count == 0);

            // Insert random file
            var randomeUpdate = _api.UpdateProject(project.ProjectId, unrealFiles);
            Assert.IsTrue(randomeUpdate.Success);
            Assert.IsTrue(randomeUpdate.Files.First().Code == "Hello, world!");
            Assert.IsTrue(randomeUpdate.Files.First().Name == "Hello.cs");
            Assert.IsTrue(randomeUpdate.Files.Count == 1);

            // Replace with real files
            var updateProject = _api.UpdateProject(project.ProjectId, realFiles);
            Assert.IsTrue(updateProject.Success);
            Assert.IsTrue(updateProject.Files.First().Name == "main.cs");
            Assert.IsTrue(updateProject.Files.Count == 1);

            // Delete the project
            var deleteProject = _api.DeleteProject(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
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
                        user     = Config.Get("default-username");
                        password = Config.Get("default-password");
                        settings = new DefaultLiveAlgorithmSettings(user, password, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.Default.ToString());
                        break;
                    case BrokerageName.FxcmBrokerage:
                        user     = Config.Get("fxcm-user-name");
                        password = Config.Get("fxcm-password");
                        settings = new FXCMLiveAlgorithmSettings(user, password, environment, account);

                        Assert.IsTrue(settings.Id == BrokerageName.FxcmBrokerage.ToString());
                        break;
                    case BrokerageName.InteractiveBrokersBrokerage:
                        user     = Config.Get("ib-user-name");
                        password = Config.Get("ib-password");
                        account = Config.Get("ib-account");
                        settings = new InteractiveBrokersLiveAlgorithmSettings(user, password, account);

                        Assert.IsTrue(settings.Id == BrokerageName.InteractiveBrokersBrokerage.ToString());
                        break;
                    case BrokerageName.OandaBrokerage:
                        accessToken = Config.Get("oanda-access-token");
                        account     = Config.Get("oanda-account-id");

                        settings = new OandaLiveAlgorithmSettings(accessToken, environment, account); 
                        Assert.IsTrue(settings.Id == BrokerageName.OandaBrokerage.ToString());
                        break;
                    case BrokerageName.TradierBrokerage:
                        dateIssued   = Config.Get("tradier-issued-at");
                        refreshToken = Config.Get("tradier-refresh-token");
                        account      = Config.Get("tradier-account-id");

                        settings = new TradierLiveAlgorithmSettings(refreshToken, dateIssued, refreshToken, account);

                        break;
                    default:
                        throw new Exception("Settings have not been implemented for this brokerage: " + brokerageName.ToString());
                }

                // Tests common to all brokerage configuration classes
                Assert.IsTrue(settings != null);
                Assert.IsTrue(settings.Password == password);
                Assert.IsTrue(settings.User == user);

                // tradier brokerage is always live, the rest are variable
                if (brokerageName != BrokerageName.TradierBrokerage)
                    Assert.IsTrue(settings.Environment == environment);

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
        /// Paper trading FXCM
        /// </summary>
        [Test]
        public void LiveForexAlgorithms_CanBeUsedWithFXCM_Successfully()
        {
            var user     = Config.Get("fxcm-user-name");
            var password = Config.Get("fxcm-password");
            var account  = Config.Get("fxcm-account-id");
            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs") }
                };

            // Create a new project
            var project = _api.CreateProject("Test project - " + DateTime.Now, Language.CSharp);
            Assert.IsTrue(project.Success);

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new FXCMLiveAlgorithmSettings(user,
                                                         password,
                                                         BrokerageEnvironment.Paper,
                                                         account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.ProjectId);
                Assert.IsTrue(deleteProject.Success);
            }
        }

        /// <summary>
        /// Live paper trading via IB.
        /// </summary>
        [Test]
        public void LiveEquityAlgorithms_CanBeUsedWithInteractiveBrokers_Successfully()
        {
            var user     = Config.Get("ib-user-name");
            var password = Config.Get("ib-password");
            var account  = Config.Get("ib-account");

            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs" )}
                };

            // Create a new project
            var project = _api.CreateProject("Test project - " + DateTime.Now, Language.CSharp);

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new InteractiveBrokersLiveAlgorithmSettings(user,
                                                                       password,
                                                                       account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.ProjectId);
                Assert.IsTrue(deleteProject.Success);
            }
        }

        /// <summary>
        /// Live paper trading via Oanda
        /// </summary>
        [Test]
        public void LiveForexAlgorithms_CanBeUsedWithOanda_Successfully()
        {
            var token       = Config.Get("oanda-access-token");
            var account     = Config.Get("oanda-account-id");

            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs" ) }
                };

            // Create a new project
            var project = _api.CreateProject("Test project - " + DateTime.Now, Language.CSharp);

            // Update Project
            var update = _api.UpdateProject(project.ProjectId, file);
            Assert.IsTrue(update.Success);

            // Create compile
            var compile = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new OandaLiveAlgorithmSettings(token,
                                                          BrokerageEnvironment.Paper,
                                                          account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.ProjectId);
                Assert.IsTrue(deleteProject.Success);
            }
        }

        /// <summary>
        /// Live paper trading via Tradier
        /// </summary>
        [Test]
        public void LiveEquityAlgorithms_CanBeUsedWithTradier_Successfully()
        {
            var refreshToken    = Config.Get("tradier-refresh-token");
            var account         = Config.Get("tradier-account-id");
            var accessToken     = Config.Get("tradier-access-token");
            var dateIssued      = Config.Get("tradier-issued-at");

            var file = new List<ProjectFile>
                {
                    new ProjectFile { Name = "main.cs", Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs" )}
                };

            // Create a new project
            var project = _api.CreateProject("Test project - " + DateTime.Now, Language.CSharp);

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
            var settings = new TradierLiveAlgorithmSettings(accessToken,
                                                            dateIssued,
                                                            refreshToken,
                                                            account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.ProjectId);
                Assert.IsTrue(deleteProject.Success);
            }
        }

        /// <summary>
        /// Test getting links to forex data for FXCM
        /// </summary>
        [Test]
        public void FXCMDataLinks_CanBeRetrieved_Successfully()
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
        public void OandaDataLinks_CanBeRetrieved_Successfully()
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
        public void BacktestingData_CanBeDownloadedAndSaved_Successfully()
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
        public void NonExistantData_WillBeDownloaded_Unsuccessfully()
        {
            var nonExistentData = _api.DownloadData(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
               Resolution.Minute, new DateTime(1989, 10, 11));

            Assert.IsFalse(nonExistentData);
        }

        /// <summary>
        /// Test creating, compiling and bactesting a C# project via the Api
        /// </summary>
        [Test]
        public void CSharpProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            var algorithmName = "main.cs";
            var projectName = DateTime.UtcNow.ToString("u") + " Test " + _testAccount + " Lang " + language;

            Perform_CreateCompileBactest_Tests(projectName, language, algorithmName, code);
        }

        /// <summary>
        /// Test creating, compiling and bactesting a F# project via the Api
        /// </summary>
        [Test]
        public void FSharpProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.FSharp;
            var code = File.ReadAllText("../../../Algorithm.FSharp/BasicTemplateAlgorithm.fs");
            var algorithmName = "main.fs";
            var projectName = DateTime.UtcNow.ToString("u") + " Test " + _testAccount + " Lang " + language;

            Perform_CreateCompileBactest_Tests(projectName, language, algorithmName, code);
        }

        /// <summary>
        /// Test creating, compiling and bactesting a Python project via the Api
        /// </summary>
        [Test]
        public void PythonProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.Python;
            var code = File.ReadAllText("../../../Algorithm.Python/BasicTemplateAlgorithm.py");
            var algorithmName = "main.py";

            var projectName = DateTime.UtcNow.ToString("u") + " Test " + _testAccount + " Lang " + language;

            Perform_CreateCompileBactest_Tests(projectName, language, algorithmName, code);
        }

        private void Perform_CreateCompileBactest_Tests(string projectName, Language language, string algorithmName, string code)
        {
            //Test create a new project successfully
            var project = _api.CreateProject(projectName, language);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.ProjectId > 0);
            Assert.IsTrue(project.Name == projectName);

            // Make sure the project just created is now present
            var projects = _api.ListProjects();
            Assert.IsTrue(projects.Success);
            Assert.IsTrue(projects.Projects.Any(p => p.ProjectId == project.ProjectId));

            // Test read back the project we just created
            var readProject = _api.ReadProject(project.ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.IsTrue(readProject.Files.Count == 0);
            Assert.IsTrue(readProject.Name == projectName);

            // Test set a project file for the project
            var files = new List<ProjectFile> { new ProjectFile { Name = algorithmName, Code = code } };
            var updateProject = _api.UpdateProject(project.ProjectId, files);
            Assert.IsTrue(updateProject.Success);

            // Download the project again to validate its got the new file
            var verifyRead = _api.ReadProject(project.ProjectId);
            Assert.IsTrue(verifyRead.Files.Count == 1);
            Assert.IsTrue(verifyRead.Files.First().Name == algorithmName);

            // Compile the project we've created
            var compileCreate = _api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compileCreate.Success);
            Assert.IsTrue(compileCreate.State == CompileState.InQueue);

            // Read out the compile
            var compileSuccess = WaitForCompilerResponse(project.ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success);
            Assert.IsTrue(compileSuccess.State == CompileState.BuildSuccess);

            // Update the file, create a build error, test we get build error
            files[0].Code += "[Jibberish at end of the file to cause a build error]";
            _api.UpdateProject(project.ProjectId, files);
            var compileError = _api.CreateCompile(project.ProjectId);
            compileError = WaitForCompilerResponse(project.ProjectId, compileError.CompileId);
            Assert.IsTrue(compileError.Success); // Successfully processed rest request.
            Assert.IsTrue(compileError.State == CompileState.BuildError); //Resulting in build fail.

            // Using our successful compile; launch a backtest!
            var backtestName = DateTime.Now.ToString("u") + " API Backtest";
            var backtest = _api.CreateBacktest(project.ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);

            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);
            Assert.IsTrue(backtestRead.Progress == 1);
            Assert.IsTrue(backtestRead.Name == backtestName);
            Assert.IsTrue(backtestRead.Result.Statistics["Total Trades"] == "1");

            // Verify we have the backtest in our project
            var listBacktests = _api.ListBacktests(project.ProjectId);
            Assert.IsTrue(listBacktests.Success);
            Assert.IsTrue(listBacktests.Backtests.Count >= 1);
            Assert.IsTrue(listBacktests.Backtests[0].Name == backtestName);

            // Update the backtest name and test its been updated
            backtestName += "-Amendment";
            var renameBacktest = _api.UpdateBacktest(project.ProjectId, backtest.BacktestId, backtestName);
            Assert.IsTrue(renameBacktest.Success);
            backtestRead = _api.ReadBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Name == backtestName);

            //Update the note and make sure its been updated:
            var newNote = DateTime.Now.ToString("u");
            var noteBacktest = _api.UpdateBacktest(project.ProjectId, backtest.BacktestId, note: newNote);
            Assert.IsTrue(noteBacktest.Success);
            backtestRead = _api.ReadBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Note == newNote);

            // Delete the backtest we just created
            var deleteBacktest = _api.DeleteBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);

            // Test delete the project we just created
            var deleteProject = _api.DeleteProject(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Wait for the compiler to respond to a specified compile request
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="compileId">Id of the compilation of the project</param>
        /// <returns></returns>
        private Compile WaitForCompilerResponse(int projectId, string compileId)
        {
            var compile = new Compile();
            var finish = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < finish)
            {
                compile = _api.ReadCompile(projectId, compileId);
                if (compile.State == CompileState.BuildSuccess) break;
                Thread.Sleep(1000);
            }
            return compile;
        }

        /// <summary>
        /// Wait for the backtest to complete
        /// </summary>
        /// <param name="projectId">Project id to scan</param>
        /// <param name="backtestId">Backtest id previously started</param>
        /// <returns>Completed backtest object</returns>
        private Backtest WaitForBacktestCompletion(int projectId, string backtestId)
        {
            var result = new Backtest();
            var finish = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < finish)
            {
                result = _api.ReadBacktest(projectId, backtestId);
                if (result.Progress == 1) break;
                if (!result.Success) break;
                Thread.Sleep(1000);
            }
            return result;
        }
    }
}
