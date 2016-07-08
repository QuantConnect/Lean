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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.API
{
    [TestFixture, Category("TravisExclude")]
    class RestApiTests
    {

        //Test languages for the API
        private Language[] _languages =
        {
            Language.CSharp,
            Language.Python,
            Language.FSharp
        };

        //Test Authentication Credentials
        private int _testAccount = 1;
        private string _email = "tests@quantconnect.com";
        private string _testToken = "ec87b337ac970da4cbea648f24f1c851";

        /// <summary>
        /// Test successfully authenticates with the API using valid credentials.
        /// </summary>
        [Test]
        public void AuthenticatesSuccessfully()
        {
            var connection = new ApiConnection(_testAccount, _testToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Rejects invalid credentials
        /// </summary>
        [Test]
        public void RejectsInvalidCredentials()
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
            // Initialize the test:
            var api = CreateApiAccessor();
            var testSourceFile = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");

            // Test create a new project successfully
            var name = DateTime.UtcNow.ToString("u") + " Test " + _testAccount;
            var project = api.CreateProject(name, Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.ProjectId > 0);
            Console.WriteLine("API Test: Project created successfully");

            // Gets the list of projects from the account. 
            // Should at least be the one we created.
            var projects = api.ProjectList();
            Assert.IsTrue(projects.Success);
            Assert.IsTrue(projects.Projects.Count >= 1);
            Console.WriteLine("API Test: Projects listed successfully");

            // Test read back the project we just created
            var readProject = api.ReadProject(project.ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.IsTrue(readProject.Files.Count == 0);
            Assert.IsTrue(readProject.Name == name);
            Console.WriteLine("API Test: Project read successfully");

            // Test set a project file for the project
            var files = new List<ProjectFile>
            {
                new ProjectFile
                {
                    Name = "Main.cs",
                    Code = testSourceFile
                }
            };
            var updateProject = api.UpdateProject(project.ProjectId, files);
            Assert.IsTrue(updateProject.Success);
            Console.WriteLine("API Test: Project updated successfully");

            // Download the project again to validate its got the new file
            var verifyRead = api.ReadProject(project.ProjectId);
            Assert.IsTrue(verifyRead.Files.Count == 1);
            Assert.IsTrue(verifyRead.Files.First().Name == "Main.cs");
            Console.WriteLine("API Test: Project re-read successfully");

            // Test successfully compile the project we've created
            var compileCreate = api.CreateCompile(project.ProjectId);
            Assert.IsTrue(compileCreate.Success);
            Assert.IsTrue(compileCreate.State == CompileState.InQueue);
            Console.WriteLine("API Test: Compile created successfully");

            //Read out the compile; wait for it to be completed for 10 seconds
            var compileSuccess = WaitForCompilerResponse(api, project.ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success);
            Assert.IsTrue(compileSuccess.State == CompileState.BuildSuccess);
            Console.WriteLine("API Test: Project built successfully");

            // Update the file, create a build error, test we get build error
            files[0].Code += "[Jibberish at end of the file to cause a build error]";
            api.UpdateProject(project.ProjectId, files);
            var compileError = api.CreateCompile(project.ProjectId);
            compileError = WaitForCompilerResponse(api, project.ProjectId, compileError.CompileId);
            Assert.IsTrue(compileError.Success); // Successfully processed rest request.
            Assert.IsTrue(compileError.State == CompileState.BuildError); //Resulting in build fail.
            Console.WriteLine("API Test: Project errored successfully");

            // Using our successful compile; launch a backtest! 
            var backtestName = DateTime.Now.ToString("u") + " API Backtest";
            var backtest = api.CreateBacktest(project.ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);
            Console.WriteLine("API Test: Backtest created successfully");

            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(api, project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);
            Assert.IsTrue(backtestRead.Progress == 1);
            Assert.IsTrue(backtestRead.Name == backtestName);
            Assert.IsTrue(backtestRead.Result.Statistics["Total Trades"] == "1");
            Console.WriteLine("API Test: Backtest completed successfully");

            // Verify we have the backtest in our project
            var listBacktests = api.BacktestList(project.ProjectId);
            Assert.IsTrue(listBacktests.Success);
            Assert.IsTrue(listBacktests.Backtests.Count >= 1);
            Assert.IsTrue(listBacktests.Backtests[0].Name == backtestName);
            Console.WriteLine("API Test: Backtests listed successfully");

            // Update the backtest name and test its been updated
            backtestName += "-Amendment";
            var renameBacktest = api.UpdateBacktest(project.ProjectId, backtest.BacktestId, backtestName);
            Assert.IsTrue(renameBacktest.Success);
            backtestRead = api.ReadBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Name == backtestName);
            Console.WriteLine("API Test: Backtest renamed successfully");

            //Update the note and make sure its been updated:
            var newNote = DateTime.Now.ToString("u");
            var noteBacktest = api.UpdateBacktest(project.ProjectId, backtest.BacktestId, backtestNote: newNote);
            Assert.IsTrue(noteBacktest.Success);
            backtestRead = api.ReadBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Note == newNote);
            Console.WriteLine("API Test: Backtest note added successfully");

            // Delete the backtest we just created
            var deleteBacktest = api.DeleteBacktest(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);
            Console.WriteLine("API Test: Backtest deleted successfully");

            // Test delete the project we just created
            var deleteProject = api.Delete(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
            Console.WriteLine("API Test: Project deleted successfully");
        }


        /// <summary>
        /// Create an authenticated API accessor object.
        /// </summary>
        /// <returns></returns>
        private IApi CreateApiAccessor()
        {
            return CreateApiAccessor(_testAccount, _testToken);
        }

        /// <summary>
        /// Create an API Class with the specified credentials
        /// </summary>
        /// <param name="uid">User id</param>
        /// <param name="token">Token string</param>
        /// <returns>API class for placing calls</returns>
        private IApi CreateApiAccessor(int uid, string token)
        {
            Config.Set("job-user-id", uid.ToString());
            Config.Set("api-access-token", token);
            var api = new Api.Api();
            api.Initialize();
            return api;
        }

        /// <summary>
        /// Wait for the compiler to respond to a specified compile request
        /// </summary>
        /// <param name="api">API Method</param>
        /// <param name="projectId"></param>
        /// <param name="compileId"></param>
        /// <returns></returns>
        private Compile WaitForCompilerResponse(IApi api, int projectId, string compileId)
        {
            var compile = new Compile();
            var finish = DateTime.Now.AddSeconds(30);
            while (DateTime.Now < finish)
            {
                compile = api.ReadCompile(projectId, compileId);
                if (compile.State != CompileState.InQueue) break;
                Thread.Sleep(500);
            }
            return compile;
        }

        /// <summary>
        /// Wait for the backtest to complete
        /// </summary>
        /// <param name="api">IApi Object to make requests</param>
        /// <param name="projectId">Project id to scan</param>
        /// <param name="backtestId">Backtest id previously started</param>
        /// <returns>Completed backtest object</returns>
        private Backtest WaitForBacktestCompletion(IApi api, int projectId, string backtestId)
        {
            var result = new Backtest();
            var finish = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < finish)
            {
                result = api.ReadBacktest(projectId, backtestId);
                if (result.Progress == 1) break;
                if (!result.Success) break;
                Thread.Sleep(500);
            }
            return result;
        }
    }
}
