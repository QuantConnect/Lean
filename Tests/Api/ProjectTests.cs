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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using System.Collections.Generic;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Util;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using System.Threading;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Project endpoints, includes some Backtest endpoints testing as well
    /// </summary>
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on")]
    public class ProjectTests : ApiTestBase
    {
        private readonly Dictionary<string, object> _defaultSettings = new Dictionary<string, object>()
            {
                { "id", "QuantConnectBrokerage" },
                { "environment", "paper" },
                { "cash", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "currency" , "USD"},
                            { "amount", 300000}
                        }
                    }
                    }
                },
                { "holdings", new List<Dictionary<object, object>>()
                    {
                    {new Dictionary<object, object>
                        {
                            { "symbolId" , Symbols.AAPL.ID.ToString()},
                            { "symbol", Symbols.AAPL.Value},
                            { "quantity", 1 },
                            { "averagePrice", 1}
                        }
                    }
                    }
                },
            };

        [Test]
        public void ReadProject()
        {
            var readProject = ApiClient.ReadProject(TestProject.ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.AreEqual(1, readProject.Projects.Count);

            var project = readProject.Projects[0];
            Assert.AreNotEqual(0, project.OwnerId);
        }

        /// <summary>
        /// Test creating and deleting projects with the Api
        /// </summary>
        [Test]
        public void Projects_CanBeCreatedAndDeleted_Successfully()
        {
            var name = $"TestProject{GetTimestamp()}";

            //Test create a new project successfully
            var project = ApiClient.CreateProject(name, Language.CSharp, TestOrganization);
            Assert.IsTrue(project.Success);
            Assert.Greater(project.Projects.First().ProjectId, 0);
            Assert.AreEqual(name, project.Projects.First().Name);

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(project.Projects.First().ProjectId);
            Assert.IsTrue(deleteProject.Success);

            // Make sure the project is really deleted
            var projectList = ApiClient.ListProjects();
            Assert.IsFalse(projectList.Projects.Any(p => p.ProjectId == project.Projects.First().ProjectId));
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
                Name = "algorithm.cs",
                Code = HttpUtility.HtmlEncode(File.ReadAllText("../../../Algorithm.CSharp/BubbleAlgorithm.cs"))
            };

            // Add random file
            var randomAdd = ApiClient.AddProjectFile(TestProject.ProjectId, fakeFile.Name, fakeFile.Code);
            Assert.IsTrue(randomAdd.Success);
            // Update names of file
            var updatedName = ApiClient.UpdateProjectFileName(TestProject.ProjectId, fakeFile.Name, realFile.Name);
            Assert.IsTrue(updatedName.Success);

            // Replace content of file
            var updateContents = ApiClient.UpdateProjectFileContent(TestProject.ProjectId, realFile.Name, realFile.Code);
            Assert.IsTrue(updateContents.Success);

            // Read single file
            var readFile = ApiClient.ReadProjectFile(TestProject.ProjectId, realFile.Name);
            Assert.IsTrue(readFile.Success);
            Assert.IsTrue(readFile.Files.First().Code == realFile.Code);
            Assert.IsTrue(readFile.Files.First().Name == realFile.Name);

            // Add a second file
            var secondFile = ApiClient.AddProjectFile(TestProject.ProjectId, secondRealFile.Name, secondRealFile.Code);
            Assert.IsTrue(secondFile.Success);

            // Read multiple files
            var readFiles = ApiClient.ReadProjectFiles(TestProject.ProjectId);
            Assert.IsTrue(readFiles.Success);
            Assert.IsTrue(readFiles.Files.Count == 4); // 2 Added + 2 Automatic (Research.ipynb & Main.cs)

            // Delete the second file
            var deleteFile = ApiClient.DeleteProjectFile(TestProject.ProjectId, secondRealFile.Name);
            Assert.IsTrue(deleteFile.Success);

            // Read files
            var readFilesAgain = ApiClient.ReadProjectFiles(TestProject.ProjectId);
            Assert.IsTrue(readFilesAgain.Success);
            Assert.IsTrue(readFilesAgain.Files.Count == 3);
            Assert.IsTrue(readFilesAgain.Files.Any(x => x.Name == realFile.Name));
        }

        /// <summary>
        /// Test updating the nodes associated with a project
        /// </summary>
        [Test]
        public void RU_ProjectNodes_Successfully()
        {
            // Read the nodes
            var nodesResponse = ApiClient.ReadProjectNodes(TestProject.ProjectId);
            Assert.IsTrue(nodesResponse.Success);
            Assert.Greater(nodesResponse.Nodes.BacktestNodes.Count, 0);

            // Save reference node
            var node = nodesResponse.Nodes.BacktestNodes.First();
            var nodeId = node.Id;
            var active = node.Active;

            // If the node is active, deactivate it. Otherwise, set active to true
            var nodes = node.Active ? Array.Empty<string>() : new[] { nodeId };

            // Update the nodes
            nodesResponse = ApiClient.UpdateProjectNodes(TestProject.ProjectId, nodes);
            Assert.IsTrue(nodesResponse.Success);

            // Node has a new active state
            node = nodesResponse.Nodes.BacktestNodes.First(x => x.Id == nodeId);
            Assert.AreNotEqual(active, node.Active);

            // Set it back to previous state
            nodes = node.Active ? Array.Empty<string>() : new[] { nodeId };

            nodesResponse = ApiClient.UpdateProjectNodes(TestProject.ProjectId, nodes);
            Assert.IsTrue(nodesResponse.Success);

            // Node has a new active state
            node = nodesResponse.Nodes.BacktestNodes.First(x => x.Id == nodeId);
            Assert.AreEqual(active, node.Active);
        }

        /// <summary>
        /// Test creating, compiling and backtesting a C# project via the Api
        /// </summary>
        [Test]
        public void CSharpProject_CreatedCompiledAndBacktested_Successully()
        {
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            var algorithmName = "Main.cs";
            var projectName = $"{GetTimestamp()} Test {TestAccount} Lang {language}";

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

            var projectName = $"{GetTimestamp()} Test {TestAccount} Lang {language}";

            Perform_CreateCompileBackTest_Tests(projectName, language, algorithmName, code);
        }

        private void Perform_CreateCompileBackTest_Tests(string projectName, Language language, string algorithmName, string code)
        {
            //Test create a new project successfully
            var project = ApiClient.CreateProject(projectName, language, TestOrganization);
            Assert.IsTrue(project.Success);
            Assert.Greater(project.Projects.First().ProjectId, 0);
            Assert.AreEqual(projectName, project.Projects.First().Name);

            // Make sure the project just created is now present
            var projects = ApiClient.ListProjects();
            Assert.IsTrue(projects.Success);
            Assert.IsTrue(projects.Projects.Any(p => p.ProjectId == project.Projects.First().ProjectId));

            // Test read back the project we just created
            var readProject = ApiClient.ReadProject(project.Projects.First().ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.AreEqual(projectName, readProject.Projects.First().Name);

            // Test change project file name and content
            var file = new ProjectFile { Name = algorithmName, Code = code };
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(project.Projects.First().ProjectId, file.Name, file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Download the project again to validate its got the new file
            var verifyRead = ApiClient.ReadProject(project.Projects.First().ProjectId);
            Assert.IsTrue(verifyRead.Success);

            // Compile the project we've created
            var compileCreate = ApiClient.CreateCompile(project.Projects.First().ProjectId);
            Assert.IsTrue(compileCreate.Success);
            Assert.AreEqual(CompileState.InQueue, compileCreate.State);

            // Read out the compile
            var compileSuccess = WaitForCompilerResponse(project.Projects.First().ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success);
            Assert.AreEqual(CompileState.BuildSuccess, compileSuccess.State);

            // Update the file, create a build error, test we get build error
            file.Code += "[Jibberish at end of the file to cause a build error]";
            ApiClient.UpdateProjectFileContent(project.Projects.First().ProjectId, file.Name, file.Code);
            var compileError = ApiClient.CreateCompile(project.Projects.First().ProjectId);
            compileError = WaitForCompilerResponse(project.Projects.First().ProjectId, compileError.CompileId);
            Assert.IsTrue(compileError.Success); // Successfully processed rest request.
            Assert.AreEqual(CompileState.BuildError, compileError.State); //Resulting in build fail.

            // Using our successful compile; launch a backtest!
            var backtestName = $"{DateTime.UtcNow.ToStringInvariant("u")} API Backtest";
            var backtest = ApiClient.CreateBacktest(project.Projects.First().ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);

            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);
            Assert.AreEqual(1, backtestRead.Progress);
            Assert.AreEqual(backtestName, backtestRead.Name);
            Assert.AreEqual("1", backtestRead.Statistics["Total Orders"]);
            Assert.Greater(backtestRead.Charts["Benchmark"].Series.Count, 0);

            // In the same way, read the orders returned in the backtest
            var backtestOrdersRead = ApiClient.ReadBacktestOrders(project.Projects.First().ProjectId, backtest.BacktestId, 0, 1);
            Assert.IsTrue(backtestOrdersRead.Any());
            Assert.AreEqual(Symbols.SPY.Value, backtestOrdersRead.First().Symbol.Value);

            // Verify we have the backtest in our project
            var listBacktests = ApiClient.ListBacktests(project.Projects.First().ProjectId);
            Assert.IsTrue(listBacktests.Success);
            Assert.GreaterOrEqual(listBacktests.Backtests.Count, 1);
            Assert.AreEqual(backtestName, listBacktests.Backtests[0].Name);

            // Update the backtest name and test its been updated
            backtestName += "-Amendment";
            var renameBacktest = ApiClient.UpdateBacktest(project.Projects.First().ProjectId, backtest.BacktestId, backtestName);
            Assert.IsTrue(renameBacktest.Success);
            backtestRead = ApiClient.ReadBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.AreEqual(backtestName, backtestRead.Name);

            //Update the note and make sure its been updated:
            var newNote = DateTime.Now.ToStringInvariant("u");
            var noteBacktest = ApiClient.UpdateBacktest(project.Projects.First().ProjectId, backtest.BacktestId, note: newNote);
            Assert.IsTrue(noteBacktest.Success);
            backtestRead = ApiClient.ReadBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.AreEqual(newNote, backtestRead.Note);

            // Delete the backtest we just created
            var deleteBacktest = ApiClient.DeleteBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);

            // Test delete the project we just created
            var deleteProject = ApiClient.DeleteProject(project.Projects.First().ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        [Test]
        public void ReadBacktestOrdersReportAndChart()
        {
            // Project settings
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            var algorithmName = "Main.cs";
            var projectName = $"{GetTimestamp()} Test {TestAccount} Lang {language}";

            // Create a default project
            var projectResult = ApiClient.CreateProject(projectName, language, TestOrganization);
            Assert.IsTrue(projectResult.Success, $"Error creating project:\n    {string.Join("\n    ", projectResult.Errors)}");
            var project = projectResult.Projects.First();

            var file = new ProjectFile { Name = algorithmName, Code = code };
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(project.ProjectId, file.Name, file.Code);
            Assert.IsTrue(updateProjectFileContent.Success,
                $"Error updating project file:\n    {string.Join("\n    ", updateProjectFileContent.Errors)}");

            var compileCreate = ApiClient.CreateCompile(project.ProjectId);
            var compileSuccess = WaitForCompilerResponse(project.ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success, $"Error compiling project:\n    {string.Join("\n    ", compileSuccess.Errors)}");

            var backtestName = $"ReadBacktestOrders Backtest {GetTimestamp()}";
            var backtest = ApiClient.CreateBacktest(project.ProjectId, compileSuccess.CompileId, backtestName);

            // Read ongoing backtest
            var backtestRead = ApiClient.ReadBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);

            // Now wait until the backtest is completed and request the orders again
            backtestRead = WaitForBacktestCompletion(project.ProjectId, backtest.BacktestId);
            var backtestOrdersRead = ApiClient.ReadBacktestOrders(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestOrdersRead.Any());
            Assert.AreEqual(Symbols.SPY.Value, backtestOrdersRead.First().Symbol.Value);

            var readBacktestReport = ApiClient.ReadBacktestReport(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(readBacktestReport.Success);
            Assert.IsFalse(string.IsNullOrEmpty(readBacktestReport.Report));

            var readBacktestChart = ApiClient.ReadBacktestChart(
                project.ProjectId, "Strategy Equity",
                new DateTime(2013, 10, 07).Second,
                new DateTime(2013, 10, 11).Second,
                1000,
                backtest.BacktestId);
            Assert.IsTrue(readBacktestChart.Success);
            Assert.IsNotNull(readBacktestChart.Chart);

            // Delete the backtest we just created
            var deleteBacktest = ApiClient.DeleteBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);

            // Delete the project we just created
            var deleteProject = ApiClient.DeleteProject(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        [Test]
        public void UpdateBacktestName()
        {
            // We will be using the existing TestBacktest for this test
            var originalName = TestBacktest.Name;
            var newName = $"{originalName} - Amended - {DateTime.UtcNow.ToStringInvariant("u")}";

            // Update the backtest name
            var updateResult = ApiClient.UpdateBacktest(TestProject.ProjectId, TestBacktest.BacktestId, name: newName);
            Assert.IsTrue(updateResult.Success, $"Error updating backtest name:\n    {string.Join("\n    ", updateResult.Errors)}");

            // Read the backtest and verify the name has been updated
            var readResult = ApiClient.ReadBacktest(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(readResult.Success, $"Error reading backtest:\n    {string.Join("\n    ", readResult.Errors)}");
            Assert.AreEqual(newName, readResult.Name);

            // Revert the name back to the original
            updateResult = ApiClient.UpdateBacktest(TestProject.ProjectId, TestBacktest.BacktestId, name: originalName);
            Assert.IsTrue(updateResult.Success, $"Error updating backtest name:\n    {string.Join("\n    ", updateResult.Errors)}");

            // Read the backtest and verify the name has been updated
            readResult = ApiClient.ReadBacktest(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(readResult.Success, $"Error reading backtest:\n    {string.Join("\n    ", readResult.Errors)}");
            Assert.AreEqual(originalName, readResult.Name);
        }

        [Test]
        public void ReadLiveInsightsWorksAsExpected()
        {
            var quantConnectDataProvider = new Dictionary<string, object>
            {
                { "id", "QuantConnectBrokerage" },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "QuantConnectBrokerage", quantConnectDataProvider }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateCryptoFrameworkAlgorithm.cs")
            };

            // Create a new project
            var project = ApiClient.CreateProject($"Test project insight - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(projectId, compile.CompileId);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            try
            {
                // Create live default algorithm
                var createLiveAlgorithm = ApiClient.CreateLiveAlgorithm(projectId, compile.CompileId, freeNode.FirstOrDefault().Id, _defaultSettings, dataProviders: dataProviders);
                Assert.IsTrue(createLiveAlgorithm.Success, $"ApiClient.CreateLiveAlgorithm(): Error: {string.Join(",", createLiveAlgorithm.Errors)}");

                // Wait 2 minutes
                Thread.Sleep(120000);

                // Stop the algorithm
                var stopLive = ApiClient.StopLiveAlgorithm(projectId);
                Assert.IsTrue(stopLive.Success, $"ApiClient.StopLiveAlgorithm(): Error: {string.Join(",", stopLive.Errors)}");

                // Try to read the insights from the algorithm
                var readInsights = ApiClient.ReadLiveInsights(projectId, 0, 5);
                var finish = DateTime.UtcNow.AddMinutes(2);
                do
                {
                    Thread.Sleep(5000);
                    readInsights = ApiClient.ReadLiveInsights(projectId, 0, 5);
                }
                while (finish > DateTime.UtcNow && !readInsights.Insights.Any());

                Assert.IsTrue(readInsights.Success, $"ApiClient.ReadLiveInsights(): Error: {string.Join(",", readInsights.Errors)}");
                Assert.IsNotEmpty(readInsights.Insights);
                Assert.IsTrue(readInsights.Length >= 0);
                Assert.Throws<ArgumentException>(() => ApiClient.ReadLiveInsights(projectId, 0, 101));
                Assert.DoesNotThrow(() => ApiClient.ReadLiveInsights(projectId));
            }
            catch (Exception ex)
            {
                // Delete the project in case of an error
                Assert.IsTrue(ApiClient.DeleteProject(projectId).Success);
                throw ex;
            }

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(projectId);
            Assert.IsTrue(deleteProject.Success);
        }

        [Test]
        public void UpdatesBacktestTags()
        {
            // We will be using the existing TestBacktest for this test
            var tags = new List<string> { "tag1", "tag2", "tag3" };

            // Add the tags to the backtest
            var addTagsResult = ApiClient.UpdateBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId, tags);
            Assert.IsTrue(addTagsResult.Success, $"Error adding tags to backtest:\n    {string.Join("\n    ", addTagsResult.Errors)}");

            // Read the backtest and verify the tags were added
            var backtestsResult = ApiClient.ListBacktests(TestProject.ProjectId);
            Assert.IsTrue(backtestsResult.Success, $"Error getting backtests:\n    {string.Join("\n    ", backtestsResult.Errors)}");
            Assert.AreEqual(1, backtestsResult.Backtests.Count);
            CollectionAssert.AreEquivalent(tags, backtestsResult.Backtests[0].Tags);

            // Remove all tags from the backtest
            var deleteTagsResult = ApiClient.UpdateBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId, new List<string>());
            Assert.IsTrue(deleteTagsResult.Success, $"Error deleting tags from backtest:\n    {string.Join("\n    ", deleteTagsResult.Errors)}");

            // Read the backtest and verify the tags were deleted
            backtestsResult = ApiClient.ListBacktests(TestProject.ProjectId);
            Assert.IsTrue(backtestsResult.Success, $"Error getting backtests:\n    {string.Join("\n    ", backtestsResult.Errors)}");
            Assert.AreEqual(1, backtestsResult.Backtests.Count);
            Assert.AreEqual(0, backtestsResult.Backtests[0].Tags.Count);
        }

        [Test]
        public void CreatesLiveAlgorithm()
        {
            var quantConnectDataProvider = new Dictionary<string, object>
            {
                { "id", "QuantConnectBrokerage" },
            };

            var dataProviders = new Dictionary<string, object>
            {
                { "QuantConnectBrokerage", quantConnectDataProvider }
            };

            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            };

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
            var compileCheck = WaitForCompilerResponse(projectId, compile.CompileId);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            try
            {
                // Create live default algorithm
                var createLiveAlgorithm = ApiClient.CreateLiveAlgorithm(projectId, compile.CompileId, freeNode.FirstOrDefault().Id, _defaultSettings, dataProviders: dataProviders);
                Assert.IsTrue(createLiveAlgorithm.Success, $"ApiClient.CreateLiveAlgorithm(): Error: {string.Join(",", createLiveAlgorithm.Errors)}");

                // Read live algorithm
                var readLiveAlgorithm = ApiClient.ReadLiveAlgorithm(projectId, createLiveAlgorithm.DeployId);
                Assert.IsTrue(readLiveAlgorithm.Success, $"ApiClient.ReadLiveAlgorithm(): Error: {string.Join(",", readLiveAlgorithm.Errors)}");

                // Stop the algorithm
                var stopLive = ApiClient.StopLiveAlgorithm(projectId);
                Assert.IsTrue(stopLive.Success, $"ApiClient.StopLiveAlgorithm(): Error: {string.Join(",", stopLive.Errors)}");

                var readChart = ApiClient.ReadLiveChart(projectId, "Strategy Equity", new DateTime(2013, 10, 07).Second, new DateTime(2013, 10, 11).Second, 1000);
                Assert.IsTrue(readChart.Success, $"ApiClient.ReadLiveChart(): Error: {string.Join(",", readChart.Errors)}");
                Assert.IsNotNull(readChart.Chart);

                var readLivePortfolio = ApiClient.ReadLivePortfolio(projectId);
                Assert.IsTrue(readLivePortfolio.Success, $"ApiClient.ReadLivePortfolio(): Error: {string.Join(",", readLivePortfolio.Errors)}");
                Assert.IsNotNull(readLivePortfolio.Portfolio, "Portfolio was null!");
                Assert.IsNotNull(readLivePortfolio.Portfolio.Cash, "Portfolio.Cash was null!");
                Assert.IsNotNull(readLivePortfolio.Portfolio.Holdings, "Portfolio Holdings was null!");

                var readLiveLogs = ApiClient.ReadLiveLogs(projectId, createLiveAlgorithm.DeployId, 0, 20);
                Assert.IsTrue(readLiveLogs.Success, $"ApiClient.ReadLiveLogs(): Error: {string.Join(",", readLiveLogs.Errors)}");
                Assert.IsNotNull(readLiveLogs.Logs, "Logs was null!");
                Assert.IsTrue(readLiveLogs.Length >= 0, "The length of the logs was negative!");
                Assert.IsTrue(readLiveLogs.DeploymentOffset >= 0, "The deploymentOffset");
            }
            catch(Exception ex)
            {
                // Delete the project in case of an error
                Assert.IsTrue(ApiClient.DeleteProject(projectId).Success);
                throw ex;
            }

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(projectId);
            Assert.IsTrue(deleteProject.Success);
        }

        [Test]
        public void ReadVersionsWorksAsExpected()
        {
            var result = ApiClient.ReadLeanVersions();
            Assert.IsTrue(result.Success);
            Assert.IsNotEmpty(result.Versions);
        }

        [Test]
        public void CreatesOptimization()
        {
            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/ParameterizedAlgorithm.cs")
            };


            // Create a new project
            var project = ApiClient.CreateProject($"Test project optimization - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            var projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(projectId, compile.CompileId);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            var backtestName = $"Estimate optimization Backtest";
            var backtest = ApiClient.CreateBacktest(projectId, compile.CompileId, backtestName);

            // Now wait until the backtest is completed and request the orders again
            var backtestReady = WaitForBacktestCompletion(projectId, backtest.BacktestId);
            Assert.IsTrue(backtestReady.Success);

            var optimization = ApiClient.CreateOptimization(
                projectId: projectId,
                name: "My Testable Optimization",
                target: "TotalPerformance.PortfolioStatistics.SharpeRatio",
                targetTo: "max",
                targetValue: null,
                strategy: "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy",
                compileId: compile.CompileId,
                parameters: new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-fast", 50, 150, 1, 1) // Replace params with valid optimization parameter data for test project
                },
                constraints: new List<Constraint>
                {
                    new Constraint("TotalPerformance.PortfolioStatistics.SharpeRatio", ComparisonOperatorTypes.GreaterOrEqual, 1)
                },
                estimatedCost: 0.06m,
                nodeType: OptimizationNodes.O2_8,
                parallelNodes: 12
            );

            var finish = DateTime.UtcNow.AddMinutes(5);
            var readOptimization = ApiClient.ReadOptimization(optimization.OptimizationId);
            do
            {
                Thread.Sleep(5000);
                readOptimization = ApiClient.ReadOptimization(optimization.OptimizationId);
            }
            while (finish > DateTime.UtcNow && readOptimization.Status != OptimizationStatus.Completed);

            Assert.IsNotNull(optimization);
            Assert.IsNotEmpty(optimization.OptimizationId);
            Assert.AreNotEqual(default(DateTime), optimization.Created);
            Assert.Positive(optimization.ProjectId);
            Assert.IsNotEmpty(optimization.Name);
            Assert.IsInstanceOf<OptimizationStatus>(optimization.Status);
            Assert.IsNotEmpty(optimization.NodeType);
            Assert.IsTrue(0 <= optimization.OutOfSampleDays);
            Assert.AreNotEqual(default(DateTime), optimization.OutOfSampleMaxEndDate);
            Assert.IsNotNull(optimization.Criterion);

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(projectId);
            Assert.IsTrue(deleteProject.Success);
        }

        private static string GetTimestamp()
        {
            return DateTime.UtcNow.ToStringInvariant("yyyyMMddHHmmssfffff");
        }
    }
}
