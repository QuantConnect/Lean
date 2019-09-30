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
using QuantConnect.API;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;

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
            var name = "Test Project " + DateTime.Now.ToStringInvariant();

            //Test create a new project successfully
            var project = _api.CreateProject(name, Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.Projects.First().ProjectId > 0);
            Assert.IsTrue(project.Projects.First().Name == name);

            // Delete the project
            var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
            Assert.IsTrue(deleteProject.Success);

            // Make sure the project is really deleted
            var projectList = _api.ListProjects();
            Assert.IsFalse(projectList.Projects.Any(p => p.ProjectId == project.Projects.First().ProjectId));
        }

        /// <summary>
        /// Test successfully authenticating with the ApiConnection using valid credentials.
        /// </summary>
        [Test]
        public void ApiConnectionWillAuthenticate_ValidCredentials_Successfully()
        {
            var connection = new ApiConnection(_testAccount, _testToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Test successfully authenticating with the API using valid credentials.
        /// </summary>
        [Test]
        public void ApiWillAuthenticate_ValidCredentials_Successfully()
        {
            var api = new Api.Api();
            api.Initialize(_testAccount, _testToken, _dataFolder);
            Assert.IsTrue(api.Connected);
        }

        /// <summary>
        /// Test that the ApiConnection will reject invalid credentials
        /// </summary>
        [Test]
        public void ApiConnectionWillAuthenticate_InvalidCredentials_Unsuccessfully()
        {
            var connection = new ApiConnection(_testAccount, "");
            Assert.IsFalse(connection.Connected);
        }

        /// <summary>
        /// Test that the Api will reject invalid credentials
        /// </summary>
        [Test]
        public void ApiWillAuthenticate_InvalidCredentials_Unsuccessfully()
        {
            var api = new Api.Api();
            api.Initialize(_testAccount, "", _dataFolder);
            Assert.IsFalse(api.Connected);
        }

        /// <summary>
        /// Test updating the files associated with a project
        /// </summary>
        [Test]
        public void CRUD_ProjectFiles_Successfully()
        {
            var fakeFile = new ProjectFile
            {
                Name = "Hello.cs",
                Code = HttpUtility.HtmlEncode("Hello, world!")
            };

            var realFile = new ProjectFile
            {
                Name = "main.cs",
                Code = HttpUtility.HtmlEncode(File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs"))
            };

            var secondRealFile = new ProjectFile()
            {
                Name = "lol.cs",
                Code = HttpUtility.HtmlEncode(File.ReadAllText("../../../Algorithm.CSharp/BubbleAlgorithm.cs"))
            };

            // Create a new project and make sure there are no files
            var project = _api.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.Projects.First().ProjectId > 0);

            // Add random file
            var randomAdd = _api.AddProjectFile(project.Projects.First().ProjectId, fakeFile.Name, fakeFile.Code);
            Assert.IsTrue(randomAdd.Success);
            Assert.IsTrue(randomAdd.Files.First().Code == fakeFile.Code);
            Assert.IsTrue(randomAdd.Files.First().Name == fakeFile.Name);
            // Update names of file
            var updatedName = _api.UpdateProjectFileName(project.Projects.First().ProjectId, randomAdd.Files.First().Name, realFile.Name);
            Assert.IsTrue(updatedName.Success);

            // Replace content of file
            var updateContents = _api.UpdateProjectFileContent(project.Projects.First().ProjectId, realFile.Name, realFile.Code);
            Assert.IsTrue(updateContents.Success);

            // Read single file
            var readFile = _api.ReadProjectFile(project.Projects.First().ProjectId, realFile.Name);
            Assert.IsTrue(readFile.Success);
            Assert.IsTrue(readFile.Files.First().Code == realFile.Code);
            Assert.IsTrue(readFile.Files.First().Name == realFile.Name);

            // Add a second file
            var secondFile = _api.AddProjectFile(project.Projects.First().ProjectId, secondRealFile.Name, secondRealFile.Code);
            Assert.IsTrue(secondFile.Success);
            Assert.IsTrue(secondFile.Files.First().Code == secondRealFile.Code);
            Assert.IsTrue(secondFile.Files.First().Name == secondRealFile.Name);

            // Read multiple files
            var readFiles = _api.ReadProjectFiles(project.Projects.First().ProjectId);
            Assert.IsTrue(readFiles.Success);
            Assert.IsTrue(readFiles.Files.Count == 2);

            // Delete the second file
            var deleteFile = _api.DeleteProjectFile(project.Projects.First().ProjectId, secondRealFile.Name);
            Assert.IsTrue(deleteFile.Success);

            // Read files
            var readFilesAgain = _api.ReadProjectFiles(project.Projects.First().ProjectId);
            Assert.IsTrue(readFilesAgain.Success);
            Assert.IsTrue(readFilesAgain.Files.Count == 1);
            Assert.IsTrue(readFilesAgain.Files.First().Name == realFile.Name);


            // Delete the project
            var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
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
                        throw new Exception($"Settings have not been implemented for this brokerage: {brokerageName}");
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
            var file = new ProjectFile
            {
                Name = "main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
            };

            // Create a new project
            var project = _api.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp);
            Assert.IsTrue(project.Success);

            // Add Project Files
            var addProjectFile = _api.AddProjectFile(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(addProjectFile.Success);

            // Create compile
            var compile = _api.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new FXCMLiveAlgorithmSettings(user,
                                                         password,
                                                         BrokerageEnvironment.Paper,
                                                         account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.Projects.First().ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
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

            var file = new ProjectFile
            {
                Name = "main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };


            // Create a new project
            var project = _api.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp);

            // Add Project Files
            var addProjectFile = _api.AddProjectFile(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(addProjectFile.Success);

            // Create compile
            var compile = _api.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new InteractiveBrokersLiveAlgorithmSettings(user,
                                                                       password,
                                                                       account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.Projects.First().ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
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

            var file = new ProjectFile
                {
                    Name = "main.cs",
                    Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateForexAlgorithm.cs")
                };

            // Create a new project
            var project = _api.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp);

            // Add Project Files
            var addProjectFile = _api.AddProjectFile(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(addProjectFile.Success);

            // Create compile
            var compile = _api.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new OandaLiveAlgorithmSettings(token,
                                                          BrokerageEnvironment.Paper,
                                                          account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.Projects.First().ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
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

            var file = new ProjectFile
            {
                Name = "main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

            // Create a new project
            var project = _api.CreateProject($"Test project - {DateTime.Now.ToStringInvariant()}", Language.CSharp);

            // Add Project Files
            var addProjectFile = _api.AddProjectFile(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(addProjectFile.Success);

            var readProject = _api.ReadProject(project.Projects.First().ProjectId);
            Assert.IsTrue(readProject.Success);

            // Create compile
            var compile = _api.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compile.Success);

            // Create default algorithm settings
            var settings = new TradierLiveAlgorithmSettings(accessToken,
                                                            dateIssued,
                                                            refreshToken,
                                                            account);

            // Wait for project to compile
            Thread.Sleep(10000);

            // Create live default algorithm
            var createLiveAlgorithm = _api.CreateLiveAlgorithm(project.Projects.First().ProjectId, compile.CompileId, "server512", settings);
            Assert.IsTrue(createLiveAlgorithm.Success);

            if (stopLiveAlgos)
            {
                // Liquidate live algorithm
                var liquidateLive = _api.LiquidateLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(liquidateLive.Success);

                // Stop live algorithm
                var stopLive = _api.StopLiveAlgorithm(project.Projects.First().ProjectId);
                Assert.IsTrue(stopLive.Success);

                // Delete the project
                var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
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
        /// Test read price API for given symbol
        /// </summary>
        [Test]
        public void ReadPriceWorksCorrectlyFor1Symbol()
        {
            var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var pricesList = _api.ReadPrices(new [] { spy });

            Assert.IsTrue(pricesList.Success);
            Assert.AreEqual(pricesList.Prices.Count, 1);

            var price = pricesList.Prices.First();
            Assert.AreEqual(price.Symbol, spy);
            Assert.AreNotEqual(price.Price, 0);
            var updated = price.Updated;
            var reference = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3));
            Assert.IsTrue(updated > reference);
        }

        /// <summary>
        /// Test read price API for multiple symbols
        /// </summary>
        [Test]
        public void ReadPriceWorksCorrectlyForMultipleSymbols()
        {
            var spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var pricesList = _api.ReadPrices(new[] { spy, aapl });

            Assert.IsTrue(pricesList.Success);
            Assert.AreEqual(pricesList.Prices.Count, 2);

            Assert.IsTrue(pricesList.Prices.All(x => x.Price != 0));
            Assert.AreEqual(pricesList.Prices.Count(x => x.Symbol == aapl), 1);
            Assert.AreEqual(pricesList.Prices.Count(x => x.Symbol == spy), 1);

            var reference = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3));
            Assert.IsTrue(pricesList.Prices.All(x => x.Updated > reference));
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
        /// Test creating, compiling and backtesting a C# project via the Api
        /// </summary>
        [Test]
        public void CSharpProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            var algorithmName = "main.cs";
            var projectName = $"{DateTime.UtcNow.ToStringInvariant("u")} Test {_testAccount} Lang {language}";

            Perform_CreateCompileBactest_Tests(projectName, language, algorithmName, code);
        }

        /// <summary>
        /// Test creating, compiling and backtesting a F# project via the Api
        /// </summary>
        [Test]
        public void FSharpProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.FSharp;
            var code = File.ReadAllText("../../../Algorithm.FSharp/BasicTemplateAlgorithm.fs");
            var algorithmName = "main.fs";
            var projectName = $"{DateTime.UtcNow.ToStringInvariant("u")} Test {_testAccount} Lang {language}";

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

            var projectName = $"{DateTime.UtcNow.ToStringInvariant("u")} Test {_testAccount} Lang {language}";

            Perform_CreateCompileBactest_Tests(projectName, language, algorithmName, code);
        }

        [Test]
        public void GetsSplits()
        {
            var date = new DateTime(2014, 06, 09);
            var AAPL = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var splits = _api.GetSplits(date, date);
            var aapl = splits.Single(s => s.Symbol == AAPL);
            Assert.AreEqual((1 / 7m).RoundToSignificantDigits(6), aapl.SplitFactor);
            Assert.AreEqual(date, aapl.Time);
            Assert.AreEqual(SplitType.SplitOccurred, aapl.Type);
            Assert.AreEqual(645.57m, aapl.ReferencePrice);
        }

        [Test]
        public void GetDividends()
        {
            var date = new DateTime(2018, 05, 11);
            var AAPL = new Symbol(SecurityIdentifier.Parse("AAPL R735QTJ8XC9X"), "AAPL");
            var dividends = _api.GetDividends(date, date);
            var aapl = dividends.Single(s => s.Symbol == AAPL);
            Assert.AreEqual(0.73m, aapl.Distribution);
            Assert.AreEqual(date, aapl.Time);
            Assert.AreEqual(190.03m, aapl.ReferencePrice);
        }

        private void Perform_CreateCompileBactest_Tests(string projectName, Language language, string algorithmName, string code)
        {
            //Test create a new project successfully
            var project = _api.CreateProject(projectName, language);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.Projects.First().ProjectId > 0);
            Assert.IsTrue(project.Projects.First().Name == projectName);

            // Make sure the project just created is now present
            var projects = _api.ListProjects();
            Assert.IsTrue(projects.Success);
            Assert.IsTrue(projects.Projects.Any(p => p.ProjectId == project.Projects.First().ProjectId));

            // Test read back the project we just created
            var readProject = _api.ReadProject(project.Projects.First().ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.IsTrue(readProject.Projects.First().Name == projectName);

            // Test set a project file for the project
            var file = new ProjectFile { Name = algorithmName, Code = code };
            var addProjectFile = _api.AddProjectFile(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(addProjectFile.Success);

            // Download the project again to validate its got the new file
            var verifyRead = _api.ReadProject(project.Projects.First().ProjectId);
            Assert.IsTrue(verifyRead.Success);

            // Compile the project we've created
            var compileCreate = _api.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compileCreate.Success);
            Assert.IsTrue(compileCreate.State == CompileState.InQueue);

            // Read out the compile
            var compileSuccess = WaitForCompilerResponse(project.Projects.First().ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success);
            Assert.IsTrue(compileSuccess.State == CompileState.BuildSuccess);

            // Update the file, create a build error, test we get build error
            file.Code += "[Jibberish at end of the file to cause a build error]";
            _api.UpdateProjectFileContent(project.Projects.First().ProjectId, file.Name, file.Code);
            var compileError = _api.CreateCompile(project.Projects.First().ProjectId);
            compileError = WaitForCompilerResponse(project.Projects.First().ProjectId, compileError.CompileId);
            Assert.IsTrue(compileError.Success); // Successfully processed rest request.
            Assert.IsTrue(compileError.State == CompileState.BuildError); //Resulting in build fail.

            // Using our successful compile; launch a backtest!
            var backtestName = $"{DateTime.Now.ToStringInvariant("u")} API Backtest";
            var backtest = _api.CreateBacktest(project.Projects.First().ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);

            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);
            Assert.IsTrue(backtestRead.Progress == 1);
            Assert.IsTrue(backtestRead.Name == backtestName);
            Assert.IsTrue(backtestRead.Result.Statistics["Total Trades"] == "1");

            // Verify we have the backtest in our project
            var listBacktests = _api.ListBacktests(project.Projects.First().ProjectId);
            Assert.IsTrue(listBacktests.Success);
            Assert.IsTrue(listBacktests.Backtests.Count >= 1);
            Assert.IsTrue(listBacktests.Backtests[0].Name == backtestName);

            // Update the backtest name and test its been updated
            backtestName += "-Amendment";
            var renameBacktest = _api.UpdateBacktest(project.Projects.First().ProjectId, backtest.BacktestId, backtestName);
            Assert.IsTrue(renameBacktest.Success);
            backtestRead = _api.ReadBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Name == backtestName);

            //Update the note and make sure its been updated:
            var newNote = DateTime.Now.ToStringInvariant("u");
            var noteBacktest = _api.UpdateBacktest(project.Projects.First().ProjectId, backtest.BacktestId, note: newNote);
            Assert.IsTrue(noteBacktest.Success);
            backtestRead = _api.ReadBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Note == newNote);

            // Delete the backtest we just created
            var deleteBacktest = _api.DeleteBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);

            // Test delete the project we just created
            var deleteProject = _api.DeleteProject(project.Projects.First().ProjectId);
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
