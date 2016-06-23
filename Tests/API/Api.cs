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
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    class Api
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
        /// Creates a new QuantConnect Project
        /// </summary>
        [Test]
        public void Project_Create_Read_Update_Delete()
        {
            // Initialize the test:
            var api = CreateApiAccessor();

            // Create a new project
            var name = DateTime.UtcNow.ToString("u") + " Test " + _testAccount;
            var project = api.ProjectCreate(name, Language.CSharp);
            Assert.IsTrue(project.Success);
            Assert.IsTrue(project.ProjectId > 0);

            // Read back the project we just created
            var readProject = api.ProjectRead(project.ProjectId);
            Assert.IsTrue(readProject.Success);
            Assert.IsTrue(readProject.Files.Count == 0);
            Assert.IsTrue(readProject.Name == name);

            //Set a project file for the project.
            var files = new List<ProjectFile>();
            files.Add(new ProjectFile()
            {
                Name = "Main.cs",
                Code = File.ReadAllText("../../../Algorithm.CSharp/BasicTemplateAlgorithm.cs")
            });
            var updateProject = api.UpdateProject(project.ProjectId, files);
            Assert.IsTrue(updateProject.Success);
            
            //Download the project again to validate its got the new file:
            var verifyRead = api.ProjectRead(project.ProjectId);
            Assert.IsTrue(verifyRead.Files.Count == 1);
            Assert.IsTrue(verifyRead.Files.First().Name == "Main.cs");

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
            Assert.IsTrue(projects.Projects.Count > 0);
        }
        

        /// <summary>
        /// Create a new compile / build from a project
        /// </summary>
        [Test]
        public void Compiler_Create()
        {
            // Todo
        }

        /// <summary>
        /// Start a new backtest.
        /// </summary>
        [Test]
        public void Backtests_Create()
        {
            // Todo
        }

        /// <summary>
        /// Read the result of the previous backtest
        /// </summary>
        [Test]
        public void Backtests_ReadOne()
        {
            // Todo
        }

        /// <summary>
        /// Read in a list of the backtest names and properties.
        /// </summary>
        [Test]
        public void Backtests_ReadAll()
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
        private QuantConnect.Api.Api CreateApiAccessor()
        {
            return CreateApiAccessor(_testAccount, _testToken);
        }

        /// <summary>
        /// Create an API Class with the specified credentials
        /// </summary>
        /// <param name="uid">User id</param>
        /// <param name="token">Token string</param>
        /// <returns>API class for placing calls</returns>
        private QuantConnect.Api.Api CreateApiAccessor(int uid, string token)
        {
            Config.Set("job-user-id", uid.ToString());
            Config.Set("api-access-token", token);
            var api = new QuantConnect.Api.Api();
            api.Initialize();
            return api;
        }
    }
}
