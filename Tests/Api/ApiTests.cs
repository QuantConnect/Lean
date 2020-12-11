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
using QuantConnect.Configuration;

namespace QuantConnect.Tests.API
{
    [TestFixture, Explicit("These tests require QC User ID and API Token in the configuration")]
    class RestApiTests
    {
        private int _testAccount;
        private string _testToken;
        private string _dataFolder;
        private Api.Api _api;

        /// <summary>
        /// Run before test
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            _testAccount = Config.GetInt("job-user-id", 1);
            _testToken = Config.Get("api-access-token", "ec87b337ac970da4cbea648f24f1c851");
            _dataFolder = Config.Get("data-folder");

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
        /// Test downloading data that does not come with the repo (Oanda)
        /// Requires that your account has this data; its free at quantconnect.com/data
        /// </summary>
        [Test, Ignore("Requires EURUSD daily data and minute data for 10/2013 in cloud data library")]
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
        /// Test creating, compiling and backtesting a C# project via the Api
        /// </summary>
        [Test]
        public void CSharpProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            var algorithmName = "main.cs";
            var projectName = $"{DateTime.UtcNow.ToStringInvariant("u")} Test {_testAccount} Lang {language}";

            Perform_CreateCompileBackTest_Tests(projectName, language, algorithmName, code);
        }

        /// <summary>
        /// Test creating, compiling and backtesting a Python project via the Api
        /// </summary>
        [Test]
        public void PythonProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.Python;
            var code = File.ReadAllText("../../../Algorithm.Python/BasicTemplateAlgorithm.py");
            var algorithmName = "main.py";

            var projectName = $"{DateTime.UtcNow.ToStringInvariant("u")} Test {_testAccount} Lang {language}";

            Perform_CreateCompileBackTest_Tests(projectName, language, algorithmName, code);
        }

        /// <summary>
        /// Test getting links to forex data for FXCM
        /// </summary>
        [Test, Ignore("Requires configured FXCM account")]
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
        [Test, Ignore("Requires configured Oanda account")]
        public void OandaDataLinks_CanBeRetrieved_Successfully()
        {
            var minuteDataLink = _api.ReadDataLink(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                Resolution.Minute, new DateTime(2013, 10, 07));
            var dailyDataLink = _api.ReadDataLink(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                Resolution.Daily, new DateTime(2013, 10, 07));

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }

        [TestCase("organizationId")]
        [TestCase("")]
        public void ReadAccount(string organizationId)
        {
            var account = _api.ReadAccount(organizationId);

            Assert.IsTrue(account.Success);
            Assert.IsNotEmpty(account.OrganizationId);
            Assert.IsNotNull(account.Card);
            Assert.AreNotEqual(default(DateTime),account.Card.Expiration);
            Assert.IsNotEmpty(account.Card.Brand);
            Assert.AreNotEqual(0, account.Card.LastFourDigits);
        }

        private void Perform_CreateCompileBackTest_Tests(string projectName, Language language, string algorithmName, string code)
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
            Assert.IsTrue(backtestRead.Statistics["Total Trades"] == "1");
            Assert.IsTrue(backtestRead.Charts["Benchmark"].Series.Count > 0);

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
