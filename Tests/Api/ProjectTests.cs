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

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Project endpoints, includes some Backtest endpoints testing as well
    /// </summary>
    [TestFixture, Explicit("Requires configured api access and available backtest node to run on")]
    public class ProjectTests : ApiTestBase
    {
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

            // Create a new project
            var project = ApiClient.CreateProject($"Test project - {GetTimestamp()}", Language.CSharp, TestOrganization);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.Projects.First().ProjectId > 0);

            // Add random file
            var randomAdd = ApiClient.AddProjectFile(project.Projects.First().ProjectId, fakeFile.Name, fakeFile.Code);
            Assert.IsTrue(randomAdd.Success);
            Assert.IsTrue(randomAdd.Files.First().Code == fakeFile.Code);
            Assert.IsTrue(randomAdd.Files.First().Name == fakeFile.Name);
            // Update names of file
            var updatedName = ApiClient.UpdateProjectFileName(project.Projects.First().ProjectId, randomAdd.Files.First().Name, realFile.Name);
            Assert.IsTrue(updatedName.Success);

            // Replace content of file
            var updateContents = ApiClient.UpdateProjectFileContent(project.Projects.First().ProjectId, realFile.Name, realFile.Code);
            Assert.IsTrue(updateContents.Success);

            // Read single file
            var readFile = ApiClient.ReadProjectFile(project.Projects.First().ProjectId, realFile.Name);
            Assert.IsTrue(readFile.Success);
            Assert.IsTrue(readFile.Files.First().Code == realFile.Code);
            Assert.IsTrue(readFile.Files.First().Name == realFile.Name);

            // Add a second file
            var secondFile = ApiClient.AddProjectFile(project.Projects.First().ProjectId, secondRealFile.Name, secondRealFile.Code);
            Assert.IsTrue(secondFile.Success);
            Assert.IsTrue(secondFile.Files.First().Code == secondRealFile.Code);
            Assert.IsTrue(secondFile.Files.First().Name == secondRealFile.Name);

            // Read multiple files
            var readFiles = ApiClient.ReadProjectFiles(project.Projects.First().ProjectId);
            Assert.IsTrue(readFiles.Success);
            Assert.IsTrue(readFiles.Files.Count == 4); // 2 Added + 2 Automatic (Research.ipynb & Main.cs)

            // Delete the second file
            var deleteFile = ApiClient.DeleteProjectFile(project.Projects.First().ProjectId, secondRealFile.Name);
            Assert.IsTrue(deleteFile.Success);

            // Read files
            var readFilesAgain = ApiClient.ReadProjectFiles(project.Projects.First().ProjectId);
            Assert.IsTrue(readFilesAgain.Success);
            Assert.IsTrue(readFilesAgain.Files.Count == 3);
            Assert.IsTrue(readFilesAgain.Files.Any(x => x.Name == realFile.Name));

            // Delete the project
            var deleteProject = ApiClient.DeleteProject(project.Projects.First().ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }

        /// <summary>
        /// Test updating the nodes associated with a project
        /// </summary>
        [Test]
        public void RU_ProjectNodes_Successfully()
        {
            // Create a new project
            var project = ApiClient.CreateProject($"Test project - {GetTimestamp()}", Language.CSharp, TestOrganization);
            Assert.IsTrue(project.Success);

            var projectId = project.Projects.First().ProjectId;
            Assert.Greater(projectId, 0);

            // Read the nodes
            var nodesResponse = ApiClient.ReadProjectNodes(projectId);
            Assert.IsTrue(nodesResponse.Success);
            Assert.Greater(nodesResponse.Nodes.BacktestNodes.Count, 0);

            // Save reference node
            var node = nodesResponse.Nodes.BacktestNodes.First();
            var nodeId = node.Id;
            var active = node.Active;

            // If the node is active, deactivate it. Otherwise, set active to true
            var nodes = node.Active ? Array.Empty<string>() : new[] { nodeId };

            // Update the nodes
            nodesResponse = ApiClient.UpdateProjectNodes(projectId, nodes);
            Assert.IsTrue(nodesResponse.Success);

            // Node has a new active state
            node = nodesResponse.Nodes.BacktestNodes.First(x => x.Id == nodeId);
            Assert.AreNotEqual(active, node.Active);

            // Set it back to previous state
            nodes = node.Active ? Array.Empty<string>() : new[] { nodeId };

            nodesResponse = ApiClient.UpdateProjectNodes(projectId, nodes);
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
            var backtestName = $"{DateTime.Now.ToStringInvariant("u")} API Backtest";
            var backtest = ApiClient.CreateBacktest(project.Projects.First().ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);

            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);
            Assert.AreEqual(1, backtestRead.Progress);
            Assert.AreEqual(backtestName, backtestRead.Name);
            Assert.AreEqual("1", backtestRead.Statistics["Total Trades"]);
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
        public void ReadBacktestOrders()
        {
            // Project settings
            var language = Language.CSharp;
            var code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");
            var algorithmName = "Main.cs";
            var projectName = $"{GetTimestamp()} Test {TestAccount} Lang {language}";

            // Create a default project
            var project = ApiClient.CreateProject(projectName, language, TestOrganization);
            var file = new ProjectFile { Name = algorithmName, Code = code };
            var updateProjectFileContent = ApiClient.UpdateProjectFileContent(project.Projects.First().ProjectId, file.Name, file.Code);
            var compileCreate = ApiClient.CreateCompile(project.Projects.First().ProjectId);
            var compileSuccess = WaitForCompilerResponse(project.Projects.First().ProjectId, compileCreate.CompileId);
            var backtestName = $"{DateTime.Now.ToStringInvariant("u")} API Backtest";
            var backtest = ApiClient.CreateBacktest(project.Projects.First().ProjectId, compileSuccess.CompileId, backtestName);

            // Read ongoing backtest
            var backtestRead = ApiClient.ReadBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);

            // Now wait until the backtest is completed and request the orders again
            backtestRead = WaitForBacktestCompletion(project.Projects.First().ProjectId, backtest.BacktestId);
            var backtestOrdersRead = ApiClient.ReadBacktestOrders(project.Projects.First().ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestOrdersRead.Any());
            Assert.AreEqual(Symbols.SPY.Value, backtestOrdersRead.First().Symbol.Value);

            // Delete the backtest we just created
            var deleteBacktest = ApiClient.DeleteBacktest(project.Projects.First().ProjectId, backtest.BacktestId);
            var deleteProject = ApiClient.DeleteProject(project.Projects.First().ProjectId);
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
        public void AddAndDeleteBacktestTags()
        {
            // We will be using the existing TestBacktest for this test
            var tags = new List<string> { "tag1", "tag2", "tag3" };

            // Add the tags to the backtest
            var addTagsResult = ApiClient.AddBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId, tags);
            Assert.IsTrue(addTagsResult.Success, $"Error adding tags to backtest:\n    {string.Join("\n    ", addTagsResult.Errors)}");

            // Read the backtest tags and verify they were added
            var readTagsResult = ApiClient.GetBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(readTagsResult.Success, $"Error reading backtest tags:\n    {string.Join("\n    ", readTagsResult.Errors)}");
            CollectionAssert.AreEquivalent(tags, readTagsResult.Tags);

            // Delete one tag from the backtest
            var deleteTagResult = ApiClient.DeleteBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId, tags.Take(1).ToList());
            Assert.IsTrue(deleteTagResult.Success, $"Error deleting tag from backtest:\n    {string.Join("\n    ", deleteTagResult.Errors)}");

            // Read the backtest tags and verify the tag was deleted
            readTagsResult = ApiClient.GetBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(readTagsResult.Success, $"Error reading backtest tags:\n    {string.Join("\n    ", readTagsResult.Errors)}");
            var remainingTags = tags.Skip(1).ToList();
            CollectionAssert.AreEquivalent(remainingTags, readTagsResult.Tags);

            // Delete the remaining tags from the backtest
            deleteTagResult = ApiClient.DeleteBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId, remainingTags);
            Assert.IsTrue(deleteTagResult.Success, $"Error deleting tag from backtest:\n    {string.Join("\n    ", deleteTagResult.Errors)}");

            // Read the backtest tags and verify the tags were deleted
            readTagsResult = ApiClient.GetBacktestTags(TestProject.ProjectId, TestBacktest.BacktestId);
            Assert.IsTrue(readTagsResult.Success, $"Error reading backtest tags:\n    {string.Join("\n    ", readTagsResult.Errors)}");
            CollectionAssert.IsEmpty(readTagsResult.Tags);
        }

        private static string GetTimestamp()
        {
            return DateTime.UtcNow.ToStringInvariant("yyyyMMddHHmmssfffff");
        }
    }
}
