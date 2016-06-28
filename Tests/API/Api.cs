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
    [TestFixture]
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
        public void Authentication_AuthenticatesSuccessfully()
        {
            var connection = new ApiConnection(_testAccount, _testToken);
            Assert.IsTrue(connection.Connected);
        }

        /// <summary>
        /// Rejects invalid credentials
        /// </summary>
        [Test]
        public void Authentication_RejectsInvalidCredentials()
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
        public void Project_Compile_Backtest()
        {
            // Initialize the test:
            var api = CreateApiAccessor();
            var testSourceFile = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs");

            // Test create a new project successfully
            var name = DateTime.UtcNow.ToString("u") + " Test " + _testAccount;
            var project = api.ProjectCreate(name, Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.ProjectId > 0);

            // Test read back the project we just created
            var readProject = api.ProjectRead(project.ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.IsTrue(readProject.Files.Count == 0);
            Assert.IsTrue(readProject.Name == name);

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
            
            // Download the project again to validate its got the new file
            var verifyRead = api.ProjectRead(project.ProjectId);
            Assert.IsTrue(verifyRead.Files.Count == 1);
            Assert.IsTrue(verifyRead.Files.First().Name == "Main.cs");

            // Test successfully compile the project we've created
            var compileCreate = api.CompileCreate(project.ProjectId);
            Assert.IsTrue(compileCreate.Success);
            Assert.IsTrue(compileCreate.State == CompileState.InQueue);

            //Read out the compile; wait for it to be completed for 10 seconds
            var compileSuccess = WaitForCompilerResponse(api, project.ProjectId, compileCreate.CompileId);
            Assert.IsTrue(compileSuccess.Success);
            Assert.IsTrue(compileSuccess.State == CompileState.BuildSuccess);

            // Update the file, create a build error, test we get build error
            files[0].Code += "[Jibberish at end of the file to cause a build error]";
            api.UpdateProject(project.ProjectId, files);
            var compileError = api.CompileCreate(project.ProjectId);
            compileError = WaitForCompilerResponse(api, project.ProjectId, compileError.CompileId);
            Assert.IsTrue(compileError.Success); // Successfully processed rest request.
            Assert.IsTrue(compileError.State == CompileState.BuildError); //Resulting in build fail.

            // Using our successful compile; launch a backtest! 
            var backtestName = DateTime.Now.ToString("u") + " API Backtest";
            var backtest = api.BacktestCreate(project.ProjectId, compileSuccess.CompileId, backtestName);
            Assert.IsTrue(backtest.Success);
           
            // Now read the backtest and wait for it to complete
            var backtestRead = WaitForBacktestCompletion(api, project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Success);
            Assert.IsTrue(backtestRead.Progress == 1);
            Assert.IsTrue(backtestRead.Name == backtestName);
            Assert.IsTrue(backtestRead.Result.Statistics["Total Trades"] == "1");

            // Verify we have the backtest in our project
            var listBacktests = api.BacktestList(project.ProjectId);
            Assert.IsTrue(listBacktests.Success);
            Assert.IsTrue(listBacktests.Backtests.Count == 1);
            Assert.IsTrue(listBacktests.Backtests[0].Name == backtestName);

            // Update the backtest name and test its been updated
            backtestName += "-Amendment";
            var updateBacktest = api.BacktestUpdate(project.ProjectId, backtest.BacktestId, backtestName);
            Assert.IsTrue(updateBacktest.Success);
            backtestRead = WaitForBacktestCompletion(api, project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(backtestRead.Name == backtestName);

            // Delete the backtest we just created
            var deleteBacktest = api.BacktestDelete(project.ProjectId, backtest.BacktestId);
            Assert.IsTrue(deleteBacktest.Success);

            

            // Test delete the project we just created
            var deleteProject = api.Delete(project.ProjectId);
            Assert.IsTrue(deleteProject.Success);
        }


        /// <summary>
        /// Reads in the files and project properties.
        /// </summary>
        [Test]
        public void Project_List()
        {
            var api = CreateApiAccessor();
            var projects = api.ProjectList();
            Assert.IsTrue(projects.Success);
        }
        
        
        /// <summary>
        /// Read in a list of the backtest names and properties.
        /// </summary>
        [Test]
        public void Backtests_List()
        {
            // Todo
        }

        /// <summary>
        /// Stop an executing backtest
        /// </summary>
        [Test]
        public void Backtests_Update_Stop()
        {
            // Todo
        }

        /// <summary>
        /// Delete the backtest specified.
        /// </summary>
        [Test]
        public void Backtests_Delete()
        {
            // Todo
        }

        /// <summary>
        /// Deploy a new live trading algorithm to the specified brokerage with the specified credentials.
        /// </summary>
        [Test]
        public void Live_Deploy_Paper()
        {
            // Todo
        }

        /// <summary>
        /// Deploy a new live trading algorithm to the specified brokerage with the specified credentials.
        /// </summary>
        [Test]
        public void Live_Deploy_Interactive()
        {
            // Todo
        }

        /// <summary>
        /// Deploy a new live trading algorithm to the specified brokerage with the specified credentials.
        /// </summary>
        [Test]
        public void Live_Deploy_Oanda()
        {
            // Todo
        }

        /// <summary>
        /// Deploy a new live trading algorithm to the specified brokerage with the specified credentials.
        /// </summary>
        [Test]
        public void Live_Deploy_FXCM()
        {
            // Todo
        }

        /// <summary>
        /// Read the list of live trading algorithm names.
        /// </summary>
        [Test]
        public void Live_ReadAll()
        {
            // Todo
        }

        /// <summary>
        /// Read the code files for this live trading algorithm
        /// </summary>
        [Test]
        public void Live_ReadsOne_Files()
        {
            //Todo
        }

        /// <summary>
        /// Read the logs for this live trading algorithm for the specified date range.
        /// </summary>
        [Test]
        public void Live_ReadsOne_Logs()
        {
            // Todo
        }

        /// <summary>
        /// Read the specified charts for the specified date range, for one live algorithm
        /// </summary>
        [Test]
        public void Live_ReadsOne_Charts()
        {
            // Todo
        }

        /// <summary>
        /// Set the chart we're subscribed to.
        /// </summary>
        [Test]
        public void Live_Update_SetChartSubscription()
        {
            // Todo
        }

        /// <summary>
        /// Stop a live trading algorithm
        /// </summary>
        [Test]
        public void Live_Update_Stop()
        {
            // Todo
        }

        /// <summary>
        /// Update the statistics for a live running algorthm
        /// </summary>
        [Test]
        public void Live_Update_Statistics()
        {
            // Todo
        }

        /// <summary>
        /// Liquidate holdings and stop a live trading algorithm
        /// </summary>
        [Test]
        public void Live_Update_Liquidate()
        {
            // Todo
        }

        /// <summary>
        /// Add a backtest log to the records.
        /// </summary>
        [Test]
        public void LogRecords_Create_AddBacktest()
        {
            // Todo
        }

        /// <summary>
        /// Read the backtest log allowance.
        /// </summary>
        [Test]
        public void Logs_ReadAllowance()
        {
            // Todo
        }

        /// <summary>
        /// Get the market hours for the provided date/symbol pair.
        /// </summary>
        [Test]
        public void MarketHour_Read()
        {
            // Todo
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
                compile = api.CompileRead(projectId, compileId);
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
            var finish = DateTime.Now.AddSeconds(30);
            while (DateTime.Now < finish)
            {
                result = api.BacktestRead(projectId, backtestId);
                if (result.Progress == 1) break;
                if (!result.Success) break;
                Thread.Sleep(500);
            }
            return result;
        }
    }
}
