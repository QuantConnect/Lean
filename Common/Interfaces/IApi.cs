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
 *
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using QuantConnect.Api;
using QuantConnect.Notifications;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Statistics;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// API for QuantConnect.com
    /// </summary>
    [InheritedExport(typeof(IApi))]
    public interface IApi : IDisposable
    {
        /// <summary>
        /// Initialize the control system
        /// </summary>
        void Initialize(int userId, string token, string dataFolder);

        /// <summary>
        /// Create a project with the specified name and language via QuantConnect.com API
        /// </summary>
        /// <param name="name">Project name</param>
        /// <param name="language">Programming language to use</param>
        /// <param name="organizationId">Organization to create this project under</param>
        /// <returns><see cref="ProjectResponse"/> that includes information about the newly created project</returns>
        ProjectResponse CreateProject(string name, Language language, string organizationId = null);

        /// <summary>
        /// Read in a project from the QuantConnect.com API.
        /// </summary>
        /// <param name="projectId">Project id you own</param>
        /// <returns><see cref="ProjectResponse"/> about a specific project</returns>
        ProjectResponse ReadProject(int projectId);

        /// <summary>
        /// Add a file to a project
        /// </summary>
        /// <param name="projectId">The project to which the file should be added</param>
        /// <param name="name">The name of the new file</param>
        /// <param name="content">The content of the new file</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes information about the newly created file</returns>
        RestResponse AddProjectFile(int projectId, string name, string content);

        /// <summary>
        /// Update the name of a file
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="oldFileName">The current name of the file</param>
        /// <param name="newFileName">The new name for the file</param>
        /// <returns><see cref="RestResponse"/> indicating success</returns>
        RestResponse UpdateProjectFileName(int projectId, string oldFileName, string newFileName);

        /// <summary>
        /// Update the contents of a file
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="fileName">The name of the file that should be updated</param>
        /// <param name="newFileContents">The new contents of the file</param>
        /// <returns><see cref="RestResponse"/> indicating success</returns>
        RestResponse UpdateProjectFileContent(int projectId, string fileName, string newFileContents);

        /// <summary>
        /// Read a file in a project
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="fileName">The name of the file</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes the file information</returns>
        ProjectFilesResponse ReadProjectFile(int projectId, string fileName);

        /// <summary>
        /// Read all files in a project
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes the information about all files in the project</returns>
        ProjectFilesResponse ReadProjectFiles(int projectId);

        /// <summary>
        /// Read all nodes in a project.
        /// </summary>
        /// <param name="projectId">Project id to which the nodes refer</param>
        /// <returns><see cref="ProjectNodesResponse"/> that includes the information about all nodes in the project</returns>
        ProjectNodesResponse ReadProjectNodes(int projectId);

        /// <summary>
        /// Update the active state of some nodes to true.
        /// If you don't provide any nodes, all the nodes become inactive and AutoSelectNode is true.
        /// </summary>
        /// <param name="projectId">Project id to which the nodes refer</param>
        /// <param name="nodes">List of node ids to update</param>
        /// <returns><see cref="ProjectNodesResponse"/> that includes the information about all nodes in the project</returns>
        ProjectNodesResponse UpdateProjectNodes(int projectId, string[] nodes);

        /// <summary>
        /// Delete a file in a project
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="name">The name of the file that should be deleted</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes the information about all files in the project</returns>
        RestResponse DeleteProjectFile(int projectId, string name);

        /// <summary>
        /// Delete a specific project owned by the user from QuantConnect.com
        /// </summary>
        /// <param name="projectId">Project id we own and wish to delete</param>
        /// <returns>RestResponse indicating success</returns>
        RestResponse DeleteProject(int projectId);

        /// <summary>
        /// Read back a list of all projects on the account for a user.
        /// </summary>
        /// <returns>Container for list of projects</returns>
        ProjectResponse ListProjects();

        /// <summary>
        /// Create a new compile job request for this project id.
        /// </summary>
        /// <param name="projectId">Project id we wish to compile.</param>
        /// <returns>Compile object result</returns>
        Compile CreateCompile(int projectId);

        /// <summary>
        /// Read a compile packet job result.
        /// </summary>
        /// <param name="projectId">Project id we sent for compile</param>
        /// <param name="compileId">Compile id return from the creation request</param>
        /// <returns>Compile object result</returns>
        Compile ReadCompile(int projectId, string compileId);

        /// <summary>
        /// Create a new backtest from a specified projectId and compileId
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="compileId"></param>
        /// <param name="backtestName"></param>
        /// <returns></returns>
        Backtest CreateBacktest(int projectId, string compileId, string backtestName);

        /// <summary>
        /// Read out the full result of a specific backtest
        /// </summary>
        /// <param name="projectId">Project id for the backtest we'd like to read</param>
        /// <param name="backtestId">Backtest id for the backtest we'd like to read</param>
        /// <param name="getCharts">True will return backtest charts</param>
        /// <returns>Backtest result object</returns>
        Backtest ReadBacktest(int projectId, string backtestId, bool getCharts = true);

        /// <summary>
        /// Update the backtest name
        /// </summary>
        /// <param name="projectId">Project id to update</param>
        /// <param name="backtestId">Backtest id to update</param>
        /// <param name="backtestName">New backtest name to set</param>
        /// <param name="backtestNote">Note attached to the backtest</param>
        /// <returns>Rest response on success</returns>
        RestResponse UpdateBacktest(int projectId, string backtestId, string backtestName = "", string backtestNote = "");

        /// <summary>
        /// Delete a backtest from the specified project and backtestId.
        /// </summary>
        /// <param name="projectId">Project for the backtest we want to delete</param>
        /// <param name="backtestId">Backtest id we want to delete</param>
        /// <returns>RestResponse on success</returns>
        RestResponse DeleteBacktest(int projectId, string backtestId);

        /// <summary>
        /// Get a list of backtest summaries for a specific project id
        /// </summary>
        /// <param name="projectId">Project id to search</param>
        /// <param name="includeStatistics">True for include statistics in the response, false otherwise</param>
        /// <returns>BacktestList container for list of backtests</returns>
        BacktestSummaryList ListBacktests(int projectId, bool includeStatistics = false);

        /// <summary>
        /// Estimate optimization with the specified parameters via QuantConnect.com API
        /// </summary>
        /// <param name="projectId">Project ID of the project the optimization belongs to</param>
        /// <param name="name">Name of the optimization</param>
        /// <param name="target">Target of the optimization, see examples in <see cref="PortfolioStatistics"/></param>
        /// <param name="targetTo">Target extremum of the optimization, for example "max" or "min"</param>
        /// <param name="targetValue">Optimization target value</param>
        /// <param name="strategy">Optimization strategy, <see cref="GridSearchOptimizationStrategy"/></param>
        /// <param name="compileId">Optimization compile ID</param>
        /// <param name="parameters">Optimization parameters</param>
        /// <param name="constraints">Optimization constraints</param>
        /// <returns>Estimate object from the API.</returns>
        public Estimate EstimateOptimization(
            int projectId,
            string name,
            string target,
            string targetTo,
            decimal? targetValue,
            string strategy,
            string compileId,
            HashSet<OptimizationParameter> parameters,
            IReadOnlyList<Constraint> constraints);

        /// <summary>
        /// Create an optimization with the specified parameters via QuantConnect.com API
        /// </summary>
        /// <param name="projectId">Project ID of the project the optimization belongs to</param>
        /// <param name="name">Name of the optimization</param>
        /// <param name="target">Target of the optimization, see examples in <see cref="PortfolioStatistics"/></param>
        /// <param name="targetTo">Target extremum of the optimization, for example "max" or "min"</param>
        /// <param name="targetValue">Optimization target value</param>
        /// <param name="strategy">Optimization strategy, <see cref="GridSearchOptimizationStrategy"/></param>
        /// <param name="compileId">Optimization compile ID</param>
        /// <param name="parameters">Optimization parameters</param>
        /// <param name="constraints">Optimization constraints</param>
        /// <param name="estimatedCost">Estimated cost for optimization</param>
        /// <param name="nodeType">Optimization node type</param>
        /// <param name="parallelNodes">Number of parallel nodes for optimization</param>
        /// <returns>BaseOptimization object from the API.</returns>
        public OptimizationSummary CreateOptimization(
            int projectId,
            string name,
            string target,
            string targetTo,
            decimal? targetValue,
            string strategy,
            string compileId,
            HashSet<OptimizationParameter> parameters,
            IReadOnlyList<Constraint> constraints,
            decimal estimatedCost,
            string nodeType,
            int parallelNodes);

        /// <summary>
        /// List all the optimizations for a project
        /// </summary>
        /// <param name="projectId">Project id we'd like to get a list of optimizations for</param>
        /// <returns>A list of BaseOptimization objects, <see cref="BaseOptimization"/></returns>
        public List<OptimizationSummary> ListOptimizations(int projectId);

        /// <summary>
        /// Read an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id for the optimization we want to read</param>
        /// <returns><see cref="Optimization"/></returns>
        public Optimization ReadOptimization(string optimizationId);

        /// <summary>
        /// Abort an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id for the optimization we want to abort</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse AbortOptimization(string optimizationId);

        /// <summary>
        /// Update an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id we want to update</param>
        /// <param name="name">Name we'd like to assign to the optimization</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse UpdateOptimization(string optimizationId, string name = null);

        /// <summary>
        /// Delete an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id for the optimization we want to delete</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse DeleteOptimization(string optimizationId);

        /// <summary>
        /// Gets the logs of a specific live algorithm
        /// </summary>
        /// <param name="projectId">Project Id of the live running algorithm</param>
        /// <param name="algorithmId">Algorithm Id of the live running algorithm</param>
        /// <param name="startLine">Start line of logs to read</param>
        /// <param name="endLine">End line of logs to read</param>
        /// <returns>List of strings that represent the logs of the algorithm</returns>
        LiveLog ReadLiveLogs(int projectId, string algorithmId, int startLine, int endLine);

        /// <summary>
        /// Returns a chart object from a live algorithm
        /// </summary>
        /// <param name="projectId">Project ID of the request</param>
        /// <param name="name">The requested chart name</param>
        /// <param name="start">The Utc start seconds timestamp of the request</param>
        /// <param name="end">The Utc end seconds timestamp of the request</param>
        /// <param name="count">The number of data points to request</param>
        /// <returns></returns>
        public ReadChartResponse ReadLiveChart(int projectId, string name, int start, int end, uint count);

        /// <summary>
        /// Read out the portfolio state of a live algorithm
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the live algorithm</param>
        /// <returns><see cref="PortfolioResponse"/></returns>
        public PortfolioResponse ReadLivePortfolio(int projectId);

        /// <summary>
        /// Read out the insights of a live algorithm
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the live algorithm</param>
        /// <param name="start">Starting index of the insights to be fetched</param>
        /// <param name="end">Last index of the insights to be fetched. Note that end - start must be less than 100</param>
        /// <returns><see cref="InsightResponse"/></returns>
        /// <exception cref="ArgumentException"></exception>
        public InsightResponse ReadLiveInsights(int projectId, int start = 0, int end = 0);

        /// <summary>
        /// Gets the link to the downloadable data.
        /// </summary>
        /// <param name="filePath">File path representing the data requested</param>
        /// <param name="organizationId">Organization to purchase this data with</param>
        /// <returns>Link to the downloadable data.</returns>
        DataLink ReadDataLink(string filePath, string organizationId);

        /// <summary>
        /// Get valid data entries for a given filepath from data/list
        /// </summary>
        /// <returns></returns>
        DataList ReadDataDirectory(string filePath);

        /// <summary>
        /// Gets data prices from data/prices
        /// </summary>
        public DataPricesList ReadDataPrices(string organizationId);

        /// <summary>
        /// Read out the report of a backtest in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="backtestId">Specific backtest id to read</param>
        /// <returns><see cref="BacktestReport"/></returns>
        public BacktestReport ReadBacktestReport(int projectId, string backtestId);

        /// <summary>
        /// Returns a requested chart object from a backtest
        /// <param name="projectId">Project ID of the request</param>
        /// <param name="name">The requested chart name</param>
        /// <param name="start">The Utc start seconds timestamp of the request</param>
        /// <param name="end">The Utc end seconds timestamp of the request</param>
        /// <param name="count">The number of data points to request</param>
        /// <param name="backtestId">Associated Backtest ID for this chart request</param>
        /// <returns></returns>
        public ReadChartResponse ReadBacktestChart(int projectId, string name, int start, int end, uint count, string backtestId);

        /// <summary>
        /// Method to download and save the data purchased through QuantConnect
        /// </summary>
        /// <param name="filePath">File path representing the data requested</param>
        /// <returns>A bool indicating whether the data was successfully downloaded or not.</returns>
        bool DownloadData(string filePath, string organizationId);

        /// <summary>
        /// Will read the organization account status
        /// </summary>
        /// <param name="organizationId">The target organization id, if null will return default organization</param>
        public Account ReadAccount(string organizationId = null);

        /// <summary>
        /// Fetch organization data from web API
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        public Organization ReadOrganization(string organizationId = null);

        /// <summary>
        /// Create a new live algorithm for a logged in user.
        /// </summary>
        /// <param name="projectId">Id of the project on QuantConnect</param>
        /// <param name="compileId">Id of the compilation on QuantConnect</param>
        /// <param name="serverType">Type of server instance that will run the algorithm</param>
        /// <param name="baseLiveAlgorithmSettings">Dictionary with Brokerage specific settings</param>
        /// <param name="versionId">The version identifier</param>
        /// <param name="dataProviders">Dictionary with data providers and their corresponding credentials</param>
        /// <returns>Information regarding the new algorithm <see cref="LiveAlgorithm"/></returns>
        CreateLiveAlgorithmResponse CreateLiveAlgorithm(int projectId, string compileId, string serverType, Dictionary<string, object> baseLiveAlgorithmSettings, string versionId = "-1", Dictionary<string, object> dataProviders = null);

        /// <summary>
        /// Get a list of live running algorithms for a logged in user.
        /// </summary>
        /// <param name="status">Filter the statuses of the algorithms returned from the api</param>
        /// <param name="startTime">Earliest launched time of the algorithms returned by the Api</param>
        /// <param name="endTime">Latest launched time of the algorithms returned by the Api</param>
        /// <returns>List of live algorithm instances</returns>
        LiveList ListLiveAlgorithms(AlgorithmStatus? status = null, DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// Read out a live algorithm in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="deployId">Specific instance id to read</param>
        /// <returns>Live object with the results</returns>
        LiveAlgorithmResults ReadLiveAlgorithm(int projectId, string deployId);

        /// <summary>
        /// Liquidate a live algorithm from the specified project.
        /// </summary>
        /// <param name="projectId">Project for the live instance we want to stop</param>
        /// <returns></returns>
        RestResponse LiquidateLiveAlgorithm(int projectId);

        /// <summary>
        /// Stop a live algorithm from the specified project.
        /// </summary>
        /// <param name="projectId">Project for the live algo we want to delete</param>
        /// <returns></returns>
        RestResponse StopLiveAlgorithm(int projectId);

        /// <summary>
        /// Sends a notification
        /// </summary>
        /// <param name="notification">The notification to send</param>
        /// <param name="projectId">The project id</param>
        /// <returns><see cref="RestResponse"/> containing success response and errors</returns>
        RestResponse SendNotification(Notification notification, int projectId);

        /// <summary>
        /// Get the algorithm current status, active or cancelled from the user
        /// </summary>
        /// <param name="algorithmId"></param>
        /// <returns></returns>
        AlgorithmControl GetAlgorithmStatus(string algorithmId);

        /// <summary>
        /// Set the algorithm status from the worker to update the UX e.g. if there was an error.
        /// </summary>
        /// <param name="algorithmId">Algorithm id we're setting.</param>
        /// <param name="status">Status enum of the current worker</param>
        /// <param name="message">Message for the algorithm status event</param>
        void SetAlgorithmStatus(string algorithmId, AlgorithmStatus status, string message = "");

        /// <summary>
        /// Send the statistics to storage for performance tracking.
        /// </summary>
        /// <param name="algorithmId">Identifier for algorithm</param>
        /// <param name="unrealized">Unrealized gainloss</param>
        /// <param name="fees">Total fees</param>
        /// <param name="netProfit">Net profi</param>
        /// <param name="holdings">Algorithm holdings</param>
        /// <param name="equity">Total equity</param>
        /// <param name="netReturn">Algorithm return</param>
        /// <param name="volume">Volume traded</param>
        /// <param name="trades">Total trades since inception</param>
        /// <param name="sharpe">Sharpe ratio since inception</param>
        void SendStatistics(string algorithmId, decimal unrealized, decimal fees, decimal netProfit, decimal holdings, decimal equity, decimal netReturn, decimal volume, int trades, double sharpe);

        /// <summary>
        /// Send an email to the user associated with the specified algorithm id
        /// </summary>
        /// <param name="algorithmId">The algorithm id</param>
        /// <param name="subject">The email subject</param>
        /// <param name="body">The email message body</param>
        void SendUserEmail(string algorithmId, string subject, string body);

        /// <summary>
        /// Local implementation for downloading data to algorithms
        /// </summary>
        /// <param name="address">URL to download</param>
        /// <param name="headers">KVP headers</param>
        /// <param name="userName">Username for basic authentication</param>
        /// <param name="password">Password for basic authentication</param>
        /// <returns></returns>
        string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password);

        /// <summary>
        /// Local implementation for downloading data to algorithms
        /// </summary>
        /// <param name="address">URL to download</param>
        /// <param name="headers">KVP headers</param>
        /// <param name="userName">Username for basic authentication</param>
        /// <param name="password">Password for basic authentication</param>
        /// <returns></returns>
        byte[] DownloadBytes(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password);

        /// <summary>
        /// Download the object store associated with the given organization ID and key
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to get the Object Store from</param>
        /// <param name="keys">Keys for the Object Store files</param>
        /// <param name="destinationFolder">Folder in which the object will be stored</param>
        /// <returns>True if the object was retrieved correctly, false otherwise</returns>
        public bool GetObjectStore(string organizationId, List<string> keys, string destinationFolder = null);

        /// <summary>
        /// Get Object Store properties given the organization ID and the Object Store key
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to get the Object Store from</param>
        /// <param name="key">Key for the Object Store file</param>
        /// <returns><see cref="PropertiesObjectStoreResponse"/></returns>
        /// <remarks>It does not work when the object store is a directory</remarks>
        public PropertiesObjectStoreResponse GetObjectStoreProperties(string organizationId, string key);

        /// <summary>
        /// Upload files to the Object Store
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to upload the file to</param>
        /// <param name="key">Key to the Object Store file</param>
        /// <param name="objectData">File to be uploaded</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse SetObjectStore(string organizationId, string key, byte[] objectData);

        /// <summary>
        /// Request to delete Object Store metadata of a specific organization and key
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to delete the Object Store file from</param>
        /// <param name="key">Key to the Object Store file</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse DeleteObjectStore(string organizationId, string key);

        /// <summary>
        /// Gets a list of LEAN versions with their corresponding basic descriptions
        /// </summary>
        public VersionsResponse ReadLeanVersions();
    }
}
