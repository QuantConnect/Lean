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
using System.Text.RegularExpressions;
using System.Globalization;
using NUnit.Framework;
using QuantConnect.Api;
using System.Collections.Generic;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Util;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Interfaces;
using System.Threading;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Project endpoints, includes some Backtest endpoints testing as well
    /// </summary>
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on"), Parallelizable(ParallelScope.Fixtures)]
    public class ProjectTests : ApiTestBase
    {
        /// <summary>
        /// Places a market order per minute bar until 150 exist: several pages for a 100 window, still quick for a small one
        /// </summary>
        private const string ManyOrdersAlgorithm = @"
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    public class ManyOrdersAlgorithm : QCAlgorithm
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);
            SetCash(100000);
            _spy = AddEquity(""SPY"", Resolution.Minute).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (Transactions.OrdersCount < 150)
            {
                MarketOrder(_spy, Time.Minute % 2 == 0 ? 1 : -1);
            }
        }
    }
}";
        /// <summary>
        /// Logs one numbered line per minute bar on a single day, marking every tenth one, so a backtest
        /// has a few hundred log lines to page through and a subset to search for
        /// </summary>
        private const string ManyLogsAlgorithm = @"
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    public class ManyLogsAlgorithm : QCAlgorithm
    {
        private int _lines;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);
            SetCash(100000);
            AddEquity(""SPY"", Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            _lines++;
            Log(_lines % 10 == 0 ? $""Marker line {_lines}"" : $""Plain line {_lines}"");
        }
    }
}";
        private const string CodeSourceId = "Lean API Tests";

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
            var stringRepresentation = project.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            Assert.IsTrue(project.Success);
            Assert.Greater(project.Projects.First().ProjectId, 0);
            Assert.AreEqual(name, project.Projects.First().Name);

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(project.Projects.First().ProjectId);
            Assert.IsTrue(deleteProject.Success);

            // Make sure the project is really deleted
            // The API soft deletes projects (moves them to "Recycle Bin/..."), so exclude those
            var projectList = ApiClient.ListProjects();
            Assert.IsFalse(projectList.Projects
                .Where(p => !p.Name.StartsWith("Recycle Bin/", StringComparison.Ordinal))
                .Any(p => p.ProjectId == project.Projects.First().ProjectId));
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
            var stringRepresentation = randomAdd.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            Assert.IsTrue(randomAdd.Success);
            // Update names of file
            var updatedName = ApiClient.UpdateProjectFileName(TestProject.ProjectId, fakeFile.Name, realFile.Name);
            Assert.IsTrue(updatedName.Success);

            // Replace content of file
            var updateContents = ApiClient.UpdateProjectFileContent(TestProject.ProjectId, realFile.Name, realFile.Code);
            Assert.IsTrue(updateContents.Success);

            // Read single file
            var readFile = ApiClient.ReadProjectFile(TestProject.ProjectId, realFile.Name);
            stringRepresentation = readFile.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
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
            var stringRepresentation = nodesResponse.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
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

        private void Perform_CreateCompileBackTest_Tests(string projectName, Language language, string algorithmName, string code, string expectedStatus = "Completed.")
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
            var compileSuccess = WaitForCompilerResponse(ApiClient, project.Projects.First().ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success);
            Assert.AreEqual(CompileState.BuildSuccess, compileSuccess.State);

            // Update the file, create a build error, test we get build error
            file.Code += "[Jibberish at end of the file to cause a build error]";
            ApiClient.UpdateProjectFileContent(project.Projects.First().ProjectId, file.Name, file.Code);
            var compileError = ApiClient.CreateCompile(project.Projects.First().ProjectId);
            compileError = WaitForCompilerResponse(ApiClient, project.Projects.First().ProjectId, compileError.CompileId);
            Assert.IsTrue(compileError.Success); // Successfully processed rest request.
            Assert.AreEqual(CompileState.BuildError, compileError.State); //Resulting in build fail.

            // Using our successful compile; launch a backtest!
            var backtestName = $"{DateTime.UtcNow.ToStringInvariant("yyyy-MM-dd HH-mm-ss")} API Backtest";
            var backtest = ApiClient.CreateBacktest(project.Projects.First().ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);

            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(ApiClient, project.Projects.First().ProjectId, backtest.BacktestId, secondsTimeout: 600, returnFailedBacktest: true);
            Assert.IsTrue(backtestRead.Success);

            // Backtest completed, let's wait a second to allow status update
            backtestRead = ApiClient.ReadBacktest(project.Projects.First().ProjectId, backtestRead.BacktestId);
            Assert.AreEqual(expectedStatus, backtestRead.Status);

            if (expectedStatus == "Runtime Error")
            {
                Assert.IsTrue(backtestRead.Error.Contains("Intentional Failure", StringComparison.InvariantCulture) || backtestRead.HasInitializeError);
            }
            else
            {
                Assert.AreEqual(1, backtestRead.Progress);
                Assert.AreEqual(backtestName, backtestRead.Name);
                Assert.AreEqual("1", backtestRead.Statistics["Total Orders"]);
                Assert.Greater(backtestRead.Charts["Benchmark"].Series.Count, 0);

                // In the same way, read the orders returned in the backtest
                var backtestOrdersRead = ApiClient.ReadBacktestOrders(project.Projects.First().ProjectId, backtest.BacktestId, 0, 1);
                Assert.GreaterOrEqual(backtestOrdersRead.Length, backtestOrdersRead.Orders.Count);
                Assert.IsTrue(backtestOrdersRead.Orders.Any());
                Assert.AreEqual(Symbols.SPY.Value, backtestOrdersRead.Orders.First().Symbol.Value);

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
                var newNote = DateTime.Now.ToStringInvariant("yyyy-MM-dd HH-mm-ss");
                var noteBacktest = ApiClient.UpdateBacktest(project.Projects.First().ProjectId, backtest.BacktestId, note: newNote);
                Assert.IsTrue(noteBacktest.Success);
                backtestRead = ApiClient.ReadBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
                Assert.AreEqual(newNote, backtestRead.Note);
            }

            // Delete the backtest we just created
            var deleteBacktest = ApiClient.DeleteBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);

            // Test delete the project we just created
            var deleteProject = ApiClient.DeleteProject(project.Projects.First().ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Pages through every order of a backtest using the given window size and checks
        /// that the reported total length matches the orders actually received
        /// </summary>
        [TestCase(20)]
        [TestCase(50)]
        [TestCase(100)]
        public void ReadBacktestOrdersPaginatesThroughAllOrders(int windowSize)
        {
            var projectName = $"{GetTimestamp()} Test {TestAccount} Orders Pagination";
            var projectResult = ApiClient.CreateProject(projectName, Language.CSharp, TestOrganization);
            Assert.IsTrue(projectResult.Success, $"Error creating project:\n    {string.Join("\n    ", projectResult.Errors)}");
            var project = projectResult.Projects.First();

            try
            {
                var updateProjectFileContent = ApiClient.UpdateProjectFileContent(project.ProjectId, "Main.cs", ManyOrdersAlgorithm);
                Assert.IsTrue(updateProjectFileContent.Success,
                    $"Error updating project file:\n    {string.Join("\n    ", updateProjectFileContent.Errors)}");

                var compile = ApiClient.CreateCompile(project.ProjectId);
                compile = WaitForCompilerResponse(ApiClient, project.ProjectId, compile.CompileId);
                Assert.IsTrue(compile.Success, $"Error compiling project:\n    {string.Join("\n    ", compile.Errors)}");

                var backtest = ApiClient.CreateBacktest(project.ProjectId, compile.CompileId, $"Orders Pagination Backtest {GetTimestamp()}");
                backtest = WaitForBacktestCompletion(ApiClient, project.ProjectId, backtest.BacktestId, secondsTimeout: 300);
                Assert.IsTrue(backtest.Success, $"Error running backtest:\n    {string.Join("\n    ", backtest.Errors)}");
                var totalOrders = int.Parse(backtest.Statistics["Total Orders"], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Greater(totalOrders, windowSize, "The backtest needs more orders than the window size to exercise pagination");

                var orders = new List<QuantConnect.Orders.ApiOrderResponse>();
                var pages = 0;
                int length;
                do
                {
                    var page = ApiClient.ReadBacktestOrders(project.ProjectId, backtest.BacktestId, orders.Count, orders.Count + windowSize);
                    Assert.IsTrue(page.Success, $"Error reading orders:\n    {string.Join("\n    ", page.Errors)}");
                    Assert.IsNotEmpty(page.Orders, $"Received an empty page at index {orders.Count} of {page.Length}");
                    pages++;
                    QuantConnect.Logging.Log.Trace($"Page {pages}: start {orders.Count}, window {windowSize}, received {page.Orders.Count}, length {page.Length}");

                    length = page.Length;
                    orders.AddRange(page.Orders);
                }
                while (orders.Count < length);

                Assert.AreEqual(totalOrders, length, "The length reported by the API should be the total order count of the backtest");
                Assert.AreEqual(totalOrders, orders.Count, "Paging should have received every order exactly once");
                CollectionAssert.AllItemsAreUnique(orders.Select(x => x.Order.Id));
                Assert.Greater(pages, 1);
            }
            finally
            {
                ApiClient.DeleteProject(project.ProjectId);
            }
        }
        /// <summary>
        /// Pages through every log line of a backtest using the given window size and checks that
        /// the reported total matches the lines received and that the numbered lines arrive once each
        /// </summary>
        [TestCase(100)]
        [TestCase(200)]
        public void ReadBacktestLogPaginatesThroughAllLines(int windowSize)
        {
            RunBacktest(ManyLogsAlgorithm, "Logs Pagination", out var projectId, out var backtestId);
            try
            {
                var lines = ReadAllBacktestLogLines(projectId, backtestId, null, windowSize, out var length);

                foreach (var line in lines.Take(20))
                {
                    Console.WriteLine(line);
                }

                Assert.AreEqual(length, lines.Count, "Paging should have received every log line exactly once");
                var numbers = NumberedLines(lines);
                Assert.Greater(numbers.Count, windowSize, "The backtest needs more numbered lines than the window size to exercise pagination");
                CollectionAssert.AreEqual(Enumerable.Range(1, numbers.Count), numbers, "The numbered lines should arrive in order with no gaps or duplicates");
            }
            finally
            {
                ApiClient.DeleteProject(projectId);
            }
        }

        /// <summary>
        /// Searches the backtest log with the query filter, paging through the matches, and checks that
        /// only the marked lines come back and that all of them do
        /// </summary>
        [TestCase(20)]
        [TestCase(100)]
        public void ReadBacktestLogFiltersLinesByQuery(int windowSize)
        {
            RunBacktest(ManyLogsAlgorithm, "Logs Query", out var projectId, out var backtestId);
            try
            {
                var allNumbers = NumberedLines(ReadAllBacktestLogLines(projectId, backtestId, null, 200, out _));
                var expected = allNumbers.Where(x => x % 10 == 0).ToList();
                Assert.Greater(expected.Count, 1);

                var lines = ReadAllBacktestLogLines(projectId, backtestId, "Marker", windowSize, out var length);

                foreach (var line in lines.Take(20))
                {
                    Console.WriteLine(line);
                }

                Assert.AreEqual(length, lines.Count, "Paging should have received every matching line exactly once");
                Assert.IsTrue(lines.All(x => x.Contains("Marker", StringComparison.Ordinal)), "Every returned line should contain the query");
                CollectionAssert.AreEqual(expected, NumberedLines(lines), "The query should return exactly the marked lines, in order");
            }
            finally
            {
                ApiClient.DeleteProject(projectId);
            }
        }

        /// <summary>
        /// The paged read methods reject a window wider than the endpoint maximum before any request is sent
        /// </summary>
        [Test]
        public void PagedReadsRejectAWindowWiderThanTheMaximum()
        {
            var projectId = TestProject.ProjectId;
            var backtestId = TestBacktest.BacktestId;
            Assert.Throws<ArgumentException>(() => ApiClient.ReadBacktestOrders(projectId, backtestId, 0, 101));
            Assert.Throws<ArgumentException>(() => ApiClient.ReadLiveOrders(projectId, null, 0, 101));
            Assert.Throws<ArgumentException>(() => ApiClient.ReadBacktestInsights(projectId, backtestId, 0, 101));
            Assert.Throws<ArgumentException>(() => ApiClient.ReadLiveInsights(projectId, null, 0, 101));
            Assert.Throws<ArgumentException>(() => ApiClient.ReadBacktestLog(projectId, backtestId, 0, 201));
            Assert.Throws<ArgumentException>(() => ApiClient.ReadLiveLogs(projectId, "L-deploy-id", 0, 201));
        }

        /// <summary>
        /// A paged read given only a start index requests a full window from it instead of a negative one
        /// </summary>
        [Test]
        public void PagedReadsDefaultTheWindowWhenOnlyStartIsGiven()
        {
            var orders = ApiClient.ReadBacktestOrders(TestProject.ProjectId, TestBacktest.BacktestId, start: 1);
            Assert.IsTrue(orders.Success, $"Error reading orders: {string.Join(", ", orders.Errors)}");
            Assert.GreaterOrEqual(orders.Length, orders.Orders.Count);

            var logs = ApiClient.ReadBacktestLog(TestProject.ProjectId, TestBacktest.BacktestId, start: 1);
            Assert.IsTrue(logs.Success, $"Error reading the backtest log: {string.Join(", ", logs.Errors)}");
            Assert.GreaterOrEqual(logs.Length, logs.Logs.Count);
        }

        /// <summary>
        /// Creates a project with the given algorithm, compiles it and runs a backtest to completion
        /// </summary>
        private void RunBacktest(string algorithm, string testName, out int projectId, out string backtestId)
        {
            var projectResult = ApiClient.CreateProject($"{GetTimestamp()} Test {TestAccount} {testName}", Language.CSharp, TestOrganization);
            Assert.IsTrue(projectResult.Success, $"Error creating project: {string.Join(", ", projectResult.Errors)}");
            projectId = projectResult.Projects.First().ProjectId;

            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", algorithm);
            Assert.IsTrue(updateProjectFileContent.Success, $"Error updating project file: {string.Join(", ", updateProjectFileContent.Errors)}");

            var compile = ApiClient.CreateCompile(projectId);
            compile = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId);
            Assert.IsTrue(compile.Success, $"Error compiling project: {string.Join(", ", compile.Errors)}");

            var backtest = ApiClient.CreateBacktest(projectId, compile.CompileId, $"{testName} Backtest {GetTimestamp()}");
            backtest = WaitForBacktestCompletion(ApiClient, projectId, backtest.BacktestId, secondsTimeout: 300);
            Assert.IsTrue(backtest.Success, $"Error running backtest: {string.Join(", ", backtest.Errors)}");
            backtestId = backtest.BacktestId;
        }

        /// <summary>
        /// Reads the whole backtest log, or only the lines matching the query, in pages of the given size
        /// </summary>
        private List<string> ReadAllBacktestLogLines(int projectId, string backtestId, string query, int windowSize, out int length)
        {
            var lines = new List<string>();
            var pages = 0;
            do
            {
                var page = ApiClient.ReadBacktestLog(projectId, backtestId, lines.Count, lines.Count + windowSize, query);
                Assert.IsTrue(page.Success, $"Error reading the backtest log: {string.Join(", ", page.Errors)}");
                Assert.IsNotEmpty(page.Logs, $"Received an empty page at index {lines.Count} of {page.Length}");
                pages++;
                QuantConnect.Logging.Log.Trace($"Page {pages}: query {query ?? "(none)"}, start {lines.Count}, window {windowSize}, received {page.Logs.Count}, length {page.Length}");

                length = page.Length;
                lines.AddRange(page.Logs);
            }
            while (lines.Count < length);

            return lines;
        }

        /// <summary>
        /// Extracts the number of every line the test algorithm wrote, ignoring any other engine output
        /// </summary>
        private static List<int> NumberedLines(IEnumerable<string> lines)
        {
            return lines
                .Select(x => Regex.Match(x, @"(?:Plain|Marker) line (\d+)"))
                .Where(x => x.Success)
                .Select(x => int.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture))
                .ToList();
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
            var compileSuccess = WaitForCompilerResponse(ApiClient, project.ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success, $"Error compiling project:\n    {string.Join("\n    ", compileSuccess.Errors)}");

            var backtestName = $"ReadBacktestOrders Backtest {GetTimestamp()}";
            var backtest = ApiClient.CreateBacktest(project.ProjectId, compileSuccess.CompileId, backtestName);

            // Read ongoing backtest
            var backtestRead = ApiClient.ReadBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);

            // Now wait until the backtest is completed and request the orders again
            backtestRead = WaitForBacktestCompletion(ApiClient, project.ProjectId, backtest.BacktestId);
            var backtestOrdersRead = ApiClient.ReadBacktestOrders(project.ProjectId, backtest.BacktestId);
            string stringRepresentation;
            foreach (var backtestOrder in backtestOrdersRead.Orders)
            {
                stringRepresentation = backtestOrder.ToString();
                Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            }
            Assert.GreaterOrEqual(backtestOrdersRead.Length, backtestOrdersRead.Orders.Count);
            Assert.IsTrue(backtestOrdersRead.Orders.Any());
            Assert.AreEqual(Symbols.SPY.Value, backtestOrdersRead.Orders.First().Symbol.Value);

            var readBacktestReport = ApiClient.ReadBacktestReport(project.ProjectId, backtest.BacktestId);
            stringRepresentation = readBacktestReport.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            Assert.IsTrue(readBacktestReport.Success);
            Assert.IsFalse(string.IsNullOrEmpty(readBacktestReport.Report));

            var readBacktestChart = ApiClient.ReadBacktestChart(
                project.ProjectId, "Strategy Equity",
                new DateTime(2013, 10, 07).Second,
                new DateTime(2013, 10, 11).Second,
                1000,
                backtest.BacktestId);
            stringRepresentation = readBacktestChart.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
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
            var newName = $"{originalName} - Amended - {DateTime.UtcNow.ToStringInvariant("yyyy-MM-dd HH-mm-ss")}";

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

            GetProjectAndCompileIdToReadInsights(out var projectId, out var compileId);

            // Get a live node to launch the algorithm on
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            var freeNode = nodesResponse.Nodes.LiveNodes.Where(x => x.Busy == false);
            Assert.IsNotEmpty(freeNode, "No free Live Nodes found");

            try
            {
                // Create live default algorithm
                var createLiveAlgorithm = ApiClient.CreateLiveAlgorithm(projectId, compileId, freeNode.FirstOrDefault().Id, _defaultSettings, dataProviders: dataProviders);
                Assert.IsTrue(createLiveAlgorithm.Success, $"ApiClient.CreateLiveAlgorithm(): Error: {string.Join(",", createLiveAlgorithm.Errors)}");

                // Wait 2 minutes
                Thread.Sleep(120000);

                // Stop the algorithm
                var stopLive = ApiClient.StopLiveAlgorithm(projectId);
                Assert.IsTrue(stopLive.Success, $"ApiClient.StopLiveAlgorithm(): Error: {string.Join(",", stopLive.Errors)}");

                // Try to read the insights from the algorithm
                var readInsights = ApiClient.ReadLiveInsights(projectId, null, 0, 5);
                var finish = DateTime.UtcNow.AddMinutes(2);
                do
                {
                    Thread.Sleep(5000);
                    readInsights = ApiClient.ReadLiveInsights(projectId, null, 0, 5);
                }
                while (finish > DateTime.UtcNow && !readInsights.Insights.Any());

                Assert.IsTrue(readInsights.Success, $"ApiClient.ReadLiveInsights(): Error: {string.Join(",", readInsights.Errors)}");
                Assert.IsNotEmpty(readInsights.Insights);
                Assert.IsTrue(readInsights.Length >= 0);
                Assert.Throws<ArgumentException>(() => ApiClient.ReadLiveInsights(projectId, null, 0, 101));
                Assert.DoesNotThrow(() => ApiClient.ReadLiveInsights(projectId, null));

                // the documented algorithmId narrows the read to a single deployment of the project
                var byAlgorithmId = ApiClient.ReadLiveInsights(projectId, createLiveAlgorithm.DeployId, 0, 5);
                Assert.IsTrue(byAlgorithmId.Success, $"ApiClient.ReadLiveInsights(): Error: {string.Join(",", byAlgorithmId.Errors)}");
                CollectionAssert.AreEqual(readInsights.Insights.Select(x => x.Id).ToList(),
                    byAlgorithmId.Insights.Select(x => x.Id).ToList());
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
            var stringRepresentation = backtestsResult.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
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
        public void ReadBacktestInsightsWorksAsExpected()
        {
            GetProjectAndCompileIdToReadInsights(out var projectId, out var compileId);
            try
            {
                // Create backtest
                var backtestName = $"ReadBacktestOrders Backtest {GetTimestamp()}";
                var backtest = ApiClient.CreateBacktest(projectId, compileId, backtestName);
                var stringRepresentation = backtest.ToString();
                Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));

                // Try to read the insights from the algorithm
                var readInsights = ApiClient.ReadBacktestInsights(projectId, backtest.BacktestId, 0, 5);
                stringRepresentation = readInsights.ToString();
                Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
                var finish = DateTime.UtcNow.AddMinutes(2);
                do
                {
                    Thread.Sleep(1000);
                    readInsights = ApiClient.ReadBacktestInsights(projectId, backtest.BacktestId, 0, 5);
                }
                while (finish > DateTime.UtcNow && !readInsights.Insights.Any());

                Assert.IsTrue(readInsights.Success, $"ApiClient.ReadBacktestInsights(): Error: {string.Join(",", readInsights.Errors)}");
                Assert.IsNotEmpty(readInsights.Insights);
                Assert.IsTrue(readInsights.Length >= 0);
                Assert.Throws<ArgumentException>(() => ApiClient.ReadBacktestInsights(projectId, backtest.BacktestId, 0, 101));
                Assert.DoesNotThrow(() => ApiClient.ReadBacktestInsights(projectId, backtest.BacktestId));
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
            var stringRepresentation = compile.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
            Assert.IsTrue(compile.Success);

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId);
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
                stringRepresentation = createLiveAlgorithm.ToString();
                Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
                Assert.IsTrue(createLiveAlgorithm.Success, $"ApiClient.CreateLiveAlgorithm(): Error: {string.Join(",", createLiveAlgorithm.Errors)}");

                // Read live algorithm
                var readLiveAlgorithm = ApiClient.ReadLiveAlgorithm(projectId, createLiveAlgorithm.DeployId);
                stringRepresentation = readLiveAlgorithm.ToString();
                Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
                Assert.IsTrue(readLiveAlgorithm.Success, $"ApiClient.ReadLiveAlgorithm(): Error: {string.Join(",", readLiveAlgorithm.Errors)}");

                // Stop the algorithm
                var stopLive = ApiClient.StopLiveAlgorithm(projectId);
                Assert.IsTrue(stopLive.Success, $"ApiClient.StopLiveAlgorithm(): Error: {string.Join(",", stopLive.Errors)}");

                var readChart = ApiClient.ReadLiveChart(projectId, "Strategy Equity", new DateTime(2013, 10, 07).Second, new DateTime(2013, 10, 11).Second, 1000);
                Assert.IsTrue(readChart.Success, $"ApiClient.ReadLiveChart(): Error: {string.Join(",", readChart.Errors)}");
                Assert.IsNotNull(readChart.Chart);

                var readLivePortfolio = ApiClient.ReadLivePortfolio(projectId);
                stringRepresentation = readLivePortfolio.ToString();
                Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
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
        public void ReadVersionsWorksAsExpected()
        {
            var result = ApiClient.ReadLeanVersions();
            var stringRepresentation = result.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));
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
            var compileCheck = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);

            var backtestName = $"Estimate optimization Backtest";
            var backtest = ApiClient.CreateBacktest(projectId, compile.CompileId, backtestName);

            // Now wait until the backtest is completed and request the orders again
            var backtestReady = WaitForBacktestCompletion(ApiClient, projectId, backtest.BacktestId);
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
            var stringRepresentation = optimization.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));

            var finish = DateTime.UtcNow.AddMinutes(5);
            var readOptimization = ApiClient.ReadOptimization(optimization.OptimizationId);
            do
            {
                Thread.Sleep(5000);
                readOptimization = ApiClient.ReadOptimization(optimization.OptimizationId);
            }
            while (finish > DateTime.UtcNow && readOptimization.Status != OptimizationStatus.Completed);
            stringRepresentation = readOptimization.ToString();
            Assert.IsTrue(ApiTestBase.IsValidJson(stringRepresentation));

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

        /// <summary>
        /// The documented start and end paging of projects/read is sent and narrows the response
        /// </summary>
        [Test]
        public void ListProjectsPagesTheAccountProjects()
        {
            var firstPage = ApiClient.ListProjects(0, 1);
            Assert.IsTrue(firstPage.Success, $"Error listing projects: {string.Join(", ", firstPage.Errors)}");
            Assert.AreEqual(1, firstPage.Projects.Count, "A one project window should come back with a single project");

            var everyProject = ApiClient.ListProjects();
            Assert.IsTrue(everyProject.Success, $"Error listing projects: {string.Join(", ", everyProject.Errors)}");
            Assert.GreaterOrEqual(everyProject.Projects.Count, firstPage.Projects.Count);
            CollectionAssert.Contains(everyProject.Projects.Select(x => x.ProjectId).ToList(), firstPage.Projects[0].ProjectId);
        }

        /// <summary>
        /// The project response carries the documented pinning, file size and backtest sharing members
        /// </summary>
        [Test]
        public void ReadProjectReturnsTheDocumentedMembers()
        {
            var result = ApiClient.ReadProject(TestProject.ProjectId);
            Assert.IsTrue(result.Success, $"Error reading the project: {string.Join(", ", result.Errors)}");

            var project = result.Projects.Single();
            Assert.Greater(project.MaxFileSize, 0, "Every project documents the maximum length of its files");
            Assert.IsFalse(project.IsPinned, "A project the tests just created is not pinned");
            // documented as nullable: only a project with backtest sharing enabled carries a token
            Assert.IsTrue(project.SharingTokenBacktest == null || project.SharingTokenBacktest.Length >= 64,
                $"Unexpected backtest sharing token: {project.SharingTokenBacktest}");
        }

        /// <summary>
        /// projects/update posts only the properties it was given, so a rename leaves the description alone
        /// </summary>
        [Test]
        public void UpdateProjectNameAndDescription()
        {
            var originalName = TestProject.Name;
            var newName = $"{originalName}-Renamed";
            var description = $"Updated at {GetTimestamp()}";

            try
            {
                var update = ApiClient.UpdateProject(TestProject.ProjectId, newName, description);
                Assert.IsTrue(update.Success, $"Error updating the project: {string.Join(", ", update.Errors)}");

                var project = ApiClient.ReadProject(TestProject.ProjectId).Projects.Single();
                Assert.AreEqual(newName, project.Name);
                Assert.AreEqual(description, project.Description);

                var rename = ApiClient.UpdateProject(TestProject.ProjectId, name: originalName);
                Assert.IsTrue(rename.Success, $"Error updating the project: {string.Join(", ", rename.Errors)}");

                project = ApiClient.ReadProject(TestProject.ProjectId).Projects.Single();
                Assert.AreEqual(originalName, project.Name);
                Assert.AreEqual(description, project.Description, "A name only update must not clear the description");
            }
            finally
            {
                ApiClient.UpdateProject(TestProject.ProjectId, originalName, string.Empty);
            }
        }

        /// <summary>
        /// Every file method takes the documented codeSourceId and the api accepts it
        /// </summary>
        [Test]
        public void FileMethodsSendTheDocumentedCodeSourceId()
        {
            var fileName = $"CodeSource{GetTimestamp()}.cs";
            var renamedFileName = $"Renamed{fileName}";

            try
            {
                var added = ApiClient.AddProjectFile(TestProject.ProjectId, fileName, "// created", CodeSourceId);
                Assert.IsTrue(added.Success, $"Error adding the file: {string.Join(", ", added.Errors)}");

                var read = ApiClient.ReadProjectFile(TestProject.ProjectId, fileName, CodeSourceId);
                Assert.IsTrue(read.Success, $"Error reading the file: {string.Join(", ", read.Errors)}");
                Assert.AreEqual("// created", read.Files.Single().Code);

                var readAll = ApiClient.ReadProjectFiles(TestProject.ProjectId, CodeSourceId);
                Assert.IsTrue(readAll.Success, $"Error reading the project files: {string.Join(", ", readAll.Errors)}");
                Assert.IsTrue(readAll.Files.Any(x => x.Name == fileName));

                var updatedContent = ApiClient.UpdateProjectFileContent(TestProject.ProjectId, fileName, "// updated", CodeSourceId);
                Assert.IsTrue(updatedContent.Success, $"Error updating the file content: {string.Join(", ", updatedContent.Errors)}");

                var updatedName = ApiClient.UpdateProjectFileName(TestProject.ProjectId, fileName, renamedFileName, CodeSourceId);
                Assert.IsTrue(updatedName.Success, $"Error updating the file name: {string.Join(", ", updatedName.Errors)}");

                var deleted = ApiClient.DeleteProjectFile(TestProject.ProjectId, renamedFileName, CodeSourceId);
                Assert.IsTrue(deleted.Success, $"Error deleting the file: {string.Join(", ", deleted.Errors)}");
            }
            finally
            {
                ApiClient.DeleteProjectFile(TestProject.ProjectId, fileName);
                ApiClient.DeleteProjectFile(TestProject.ProjectId, renamedFileName);
            }
        }

        /// <summary>
        /// files/patch applies a unified diff to a file of the project
        /// </summary>
        [Test]
        public void PatchProjectFileAppliesAUnifiedDiff()
        {
            var fileName = $"Patched{GetTimestamp()}.cs";
            var patch = $"diff --git a/{fileName} b/{fileName}\nindex 1234567..abcdefg 100644\n--- a/{fileName}\n+++ b/{fileName}\n" +
                "@@ -1,3 +1,3 @@\n // line one\n-// line two\n+// patched line\n // line three\n";

            try
            {
                var added = ApiClient.AddProjectFile(TestProject.ProjectId, fileName, "// line one\n// line two\n// line three\n");
                Assert.IsTrue(added.Success, $"Error adding the file: {string.Join(", ", added.Errors)}");

                var patched = ApiClient.PatchProjectFile(TestProject.ProjectId, patch);
                Assert.IsTrue(patched.Success, $"Error patching the file: {string.Join(", ", patched.Errors)}");

                var read = ApiClient.ReadProjectFile(TestProject.ProjectId, fileName);
                Assert.IsTrue(read.Success, $"Error reading the file: {string.Join(", ", read.Errors)}");
                var code = read.Files.Single().Code;
                StringAssert.Contains("// patched line", code);
                StringAssert.DoesNotContain("// line two", code);
            }
            finally
            {
                ApiClient.DeleteProjectFile(TestProject.ProjectId, fileName);
            }
        }

        /// <summary>
        /// compile/create answers with the documented parameters list. The api sends it empty at creation, before
        /// the build runs, and the read response does not carry it, so only the presence of the list can be asserted
        /// </summary>
        [Test]
        public void CreateCompileReportsTheDetectedFileParameters()
        {
            var projectResult = ApiClient.CreateProject($"{GetTimestamp()} Test {TestAccount} Compile Parameters",
                Language.CSharp, TestOrganization);
            Assert.IsTrue(projectResult.Success, $"Error creating project: {string.Join(", ", projectResult.Errors)}");
            var projectId = projectResult.Projects.First().ProjectId;

            try
            {
                // The algorithm declares the ema-fast and ema-slow parameters through the parameter attribute
                var code = File.ReadAllText("../../../Algorithm.CSharp/ParameterizedAlgorithm.cs");
                var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", code);
                Assert.IsTrue(updateProjectFileContent.Success,
                    $"Error updating project file: {string.Join(", ", updateProjectFileContent.Errors)}");

                var compile = ApiClient.CreateCompile(projectId);
                Assert.IsTrue(compile.Success, $"Error creating the compile: {string.Join(", ", compile.Errors)}");
                Assert.IsNotNull(compile.Parameters, "compile/create is documented to report the parameters detected in each file");
                Assert.IsTrue(compile.Parameters.All(x => !string.IsNullOrEmpty(x.File) && x.Parameters != null),
                    "Each detected file reports its path and parameters");

                compile = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId);
                Assert.AreEqual(CompileState.BuildSuccess, compile.State, $"Error compiling project: {string.Join(", ", compile.Errors)}");
            }
            finally
            {
                ApiClient.DeleteProject(projectId);
            }
        }

        /// <summary>
        /// backtests/create sends the documented parameters and reports the debugging flag of the run
        /// </summary>
        [Test]
        public void CreateBacktestSendsTheGivenParameters()
        {
            var projectResult = ApiClient.CreateProject($"{GetTimestamp()} Test {TestAccount} Backtest Parameters",
                Language.CSharp, TestOrganization);
            Assert.IsTrue(projectResult.Success, $"Error creating project: {string.Join(", ", projectResult.Errors)}");
            var projectId = projectResult.Projects.First().ProjectId;

            try
            {
                var code = File.ReadAllText("../../../Algorithm.CSharp/ParameterizedAlgorithm.cs");
                var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", code);
                Assert.IsTrue(updateProjectFileContent.Success,
                    $"Error updating project file: {string.Join(", ", updateProjectFileContent.Errors)}");

                var compile = ApiClient.CreateCompile(projectId);
                compile = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId);
                Assert.IsTrue(compile.Success, $"Error compiling project: {string.Join(", ", compile.Errors)}");

                var parameters = new Dictionary<string, string> { { "ema-fast", "20" }, { "ema-slow", "60" } };
                var backtest = ApiClient.CreateBacktest(projectId, compile.CompileId, $"Parameters Backtest {GetTimestamp()}", parameters);
                Assert.IsTrue(backtest.Success, $"Error creating backtest: {string.Join(", ", backtest.Errors)}");
                Assert.IsFalse(backtest.Debugging, "A backtest created through the api does not run under debugging mode");

                backtest = WaitForBacktestCompletion(ApiClient, projectId, backtest.BacktestId, secondsTimeout: 300);
                Assert.IsTrue(backtest.Success, $"Error running backtest: {string.Join(", ", backtest.Errors)}");
                Assert.IsFalse(backtest.Debugging);
                Assert.IsNotNull(backtest.ParameterSet, "The backtest reports the parameters it ran with");
                Assert.AreEqual("20", backtest.ParameterSet.Value["ema-fast"]);
                Assert.AreEqual("60", backtest.ParameterSet.Value["ema-slow"]);
            }
            finally
            {
                ApiClient.DeleteProject(projectId);
            }
        }

        /// <summary>
        /// The interface default matches the class, so a listing through IApi still asks for the statistics
        /// </summary>
        [Test]
        public void ListBacktestsThroughTheInterfaceIncludesStatistics()
        {
            IApi api = ApiClient;
            var throughInterface = api.ListBacktests(TestProject.ProjectId);
            Assert.IsTrue(throughInterface.Success, $"Error listing backtests: {string.Join(", ", throughInterface.Errors)}");

            var withoutStatistics = ApiClient.ListBacktests(TestProject.ProjectId, includeStatistics: false);
            Assert.IsTrue(withoutStatistics.Success, $"Error listing backtests: {string.Join(", ", withoutStatistics.Errors)}");

            Assert.IsNull(withoutStatistics.Backtests.Single(x => x.BacktestId == TestBacktest.BacktestId).Trades);
            Assert.IsNotNull(throughInterface.Backtests.Single(x => x.BacktestId == TestBacktest.BacktestId).Trades,
                "The interface default must ask for the statistics, like the class default does");
        }

        /// <summary>
        /// The interface default matches the class, so updating only the note through IApi keeps the name
        /// </summary>
        [Test]
        public void UpdateBacktestThroughTheInterfaceKeepsTheName()
        {
            IApi api = ApiClient;
            var note = $"Note {GetTimestamp()}";

            var update = api.UpdateBacktest(TestProject.ProjectId, TestBacktest.BacktestId, note: note);
            Assert.IsTrue(update.Success, $"Error updating the backtest: {string.Join(", ", update.Errors)}");

            var read = ApiClient.ReadBacktest(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(read.Success, $"Error reading the backtest: {string.Join(", ", read.Errors)}");
            Assert.AreEqual(note, read.Note);
            Assert.AreEqual(TestBacktest.Name, read.Name, "A null name default must leave the backtest name alone");
        }

        /// <summary>
        /// backtests/read/report answers with a generating flag until the report is ready, and the client
        /// keeps polling instead of handing back an empty report
        /// </summary>
        [Test]
        public void ReadBacktestReportWaitsUntilTheReportIsGenerated()
        {
            var report = ApiClient.ReadBacktestReport(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(report.Success, $"Error reading the backtest report: {string.Join(", ", report.Errors)}");
            Assert.IsFalse(report.Generating, "The polling must not hand back a report that is still being generated");
            Assert.IsNotEmpty(report.Report);
        }

        /// <summary>
        /// backtests/chart/read answers with a loading status and its progress while the chart is being
        /// generated, and with the chart itself once it is ready
        /// </summary>
        [Test]
        public void ReadBacktestChartReportsItsLoadingStatus()
        {
            var chart = ApiClient.ReadBacktestChart(TestProject.ProjectId, "Strategy Equity", 0, 0, 100, TestBacktest.BacktestId);
            var finish = DateTime.UtcNow.AddMinutes(2);
            while (IsLoading(chart.Status) && DateTime.UtcNow < finish)
            {
                Assert.GreaterOrEqual(chart.Progress, 0m);
                Assert.LessOrEqual(chart.Progress, 1m);
                Thread.Sleep(5000);
                chart = ApiClient.ReadBacktestChart(TestProject.ProjectId, "Strategy Equity", 0, 0, 100, TestBacktest.BacktestId);
            }

            Assert.IsTrue(chart.Success, $"Error reading the backtest chart: {string.Join(", ", chart.Errors)}");
            Assert.IsFalse(IsLoading(chart.Status), "The chart was still loading after two minutes");
            Assert.IsNotNull(chart.Chart);
        }

        /// <summary>
        /// backtests/orders/read answers with a loading status and its progress while the orders are being
        /// generated, and with the orders themselves once they are ready
        /// </summary>
        [Test]
        public void ReadBacktestOrdersReportsItsLoadingStatus()
        {
            var orders = ApiClient.ReadBacktestOrders(TestProject.ProjectId, TestBacktest.BacktestId, 0, 10);
            var finish = DateTime.UtcNow.AddMinutes(2);
            while (IsLoading(orders.Status) && DateTime.UtcNow < finish)
            {
                Assert.GreaterOrEqual(orders.Progress, 0m);
                Assert.LessOrEqual(orders.Progress, 1m);
                Thread.Sleep(5000);
                orders = ApiClient.ReadBacktestOrders(TestProject.ProjectId, TestBacktest.BacktestId, 0, 10);
            }

            Assert.IsTrue(orders.Success, $"Error reading the backtest orders: {string.Join(", ", orders.Errors)}");
            Assert.IsFalse(IsLoading(orders.Status), "The orders were still loading after two minutes");
            Assert.IsNotEmpty(orders.Orders);
        }

        /// <summary>
        /// The paged endpoints report "loading" while the result they page through is still being built
        /// </summary>
        private static bool IsLoading(string status)
        {
            return string.Equals(status, "loading", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTimestamp()
        {
            return DateTime.UtcNow.ToStringInvariant("yyyyMMddHHmmssfffff");
        }

        private void GetProjectAndCompileIdToReadInsights(out int projectId, out string compileId)
        {
            var file = new ProjectFile
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateCryptoFrameworkAlgorithm.cs")
            };

            // Create a new project
            var project = ApiClient.CreateProject($"Test project insight - {DateTime.Now.ToStringInvariant()}", Language.CSharp, TestOrganization);
            projectId = project.Projects.First().ProjectId;

            // Update Project Files
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(projectId, "Main.cs", file.Code);
            Assert.IsTrue(updateProjectFileContent.Success);

            // Create compile
            var compile = ApiClient.CreateCompile(projectId);
            Assert.IsTrue(compile.Success);
            compileId = compile.CompileId;

            // Wait at max 30 seconds for project to compile
            var compileCheck = WaitForCompilerResponse(ApiClient, projectId, compile.CompileId);
            Assert.IsTrue(compileCheck.Success);
            Assert.IsTrue(compileCheck.State == CompileState.BuildSuccess);
        }

        /// <summary>
        /// Test creating, compiling and backtesting a failure C# project via the Api
        /// </summary>
        [TestCase("Constructor")]
        [TestCase("Initialize")]
        [TestCase("OnData")]

        public void CSharpProject_CreatedCompiledAndBacktested_Unsuccessully(string section)
        {
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            if (section == "Constructor")
            {
                code = code.Replace("private Symbol _spy = QuantConnect.Symbol.Create(\"SPY\", SecurityType.Equity, Market.USA);",
                    "private Symbol _spy = QuantConnect.Symbol.Create(\"SPY\", SecurityType.Equity, Market.USA);" +
                    "public BasicTemplateAlgorithm(): base() { throw new RegressionTestException(\"Intentional Failure\"); }", StringComparison.InvariantCulture);
            }
            else if (section == "Initialize")
            {
                code = code.Replace("SetStartDate(2013, 10, 07);", "throw new RegressionTestException($\"Intentional Failure\");", StringComparison.InvariantCulture);
            }
            else if (section == "OnData")
            {
                code = code.Replace("Debug(\"Purchased Stock\");", "throw new RegressionTestException($\"Intentional Failure\");", StringComparison.InvariantCulture);
            }
            var algorithmName = "Main.cs";
            var projectName = $"{GetTimestamp()} Test {TestAccount} Lang {language}";

            Perform_CreateCompileBackTest_Tests(projectName, language, algorithmName, code, "Runtime Error");
        }

        /// <summary>
        /// Test creating, compiling and backtesting a failure Python project via the Api
        /// </summary>
        [TestCase("Constructor")]
        [TestCase("Initialize")]
        [TestCase("OnData")]

        public void PythonProject_CreatedCompiledAndBacktested_Unsuccessully(string section)
        {
            var language = Language.Python;
            var code = File.ReadAllText("../../../Algorithm.Python/BasicTemplateAlgorithm.py");
            if (section == "Constructor")
            {
                code = code.Replace("self.set_start_date(2013,10, 7)  #Set Start Date",
                    "self.set_start_date(2013,10, 7)  #Set Start Date\n" +
                    "    def __init__(self):\r\n        super().__init__()\r\n        raise Exception(\"Intentional Failure\")", StringComparison.InvariantCulture);
            }
            else if (section == "Initialize")
            {
                code = code.Replace("self.set_start_date(2013,10, 7)  #Set Start Date", "raise Exception(\"Intentional Failure\")", StringComparison.InvariantCulture);
            }
            else if (section == "OnData")
            {
                code = code.Replace("self.set_holdings(\"SPY\", 1)",
                    "self.set_holdings(\"SPY\", 1)\r\n" +
                    "        raise Exception(\"Intentional Failure\")", StringComparison.InvariantCulture);
            }
            var algorithmName = "main.py";
            var projectName = $"{GetTimestamp()} Test {TestAccount} Lang {language}";

            Perform_CreateCompileBackTest_Tests(projectName, language, algorithmName, code, "Runtime Error");
        }
    }
}
