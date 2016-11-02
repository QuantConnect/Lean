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
using Newtonsoft.Json;
using QuantConnect.API;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using RestSharp.Extensions;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// QuantConnect.com Interaction Via API.
    /// </summary>
    public class Api : IApi
    {
        private ApiConnection _connection;
        private static MarketHoursDatabase _marketHoursDatabase;
        private string _dataFolder;

        /// <summary>
        /// Initialize the API using the config.json file.
        /// </summary>
        public virtual void Initialize(int userId, string token, string dataFolder)
        {
            _connection = new ApiConnection(userId, token);
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _dataFolder = dataFolder;

            //Allow proper decoding of orders from the API.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };
        }

        /// <summary>
        /// Create a project with the specified name and language via QuantConnect.com API
        /// </summary>
        /// <param name="name">Project name</param>
        /// <param name="language">Programming language to use</param>
        /// <returns>Project object from the API.</returns>
        public Project CreateProject(string name, Language language)
        {
            var request = new RestRequest("projects/create", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                name = name,
                language = language
            }), ParameterType.RequestBody);

            Project result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Read in a project from the QuantConnect.com API.
        /// </summary>
        /// <param name="projectId">Project id you own</param>
        /// <returns></returns>
        public Project ReadProject(int projectId)
        {
            var request = new RestRequest("projects/read", Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("projectId", projectId);
            Project result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Read back a list of all projects on the account for a user.
        /// This is a rate limited request.  
        /// If you have more that 200 projects, you may not get them all back with this api call.
        /// </summary>
        /// <returns>Container for list of projects</returns>
        public ProjectList ListProjects()
        {
            var request = new RestRequest("projects/read", Method.GET);
            request.RequestFormat = DataFormat.Json;
            ProjectList result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Update a specific project with a list of files. All other files will be deleted.
        /// </summary>
        /// <param name="projectId">Project id for project to be updated</param>
        /// <param name="files">Files list to update</param>
        /// <returns>RestResponse indicating success</returns>
        public ProjectUpdateResponse UpdateProject(int projectId, List<ProjectFile> files)
        {
            var request = new RestRequest("projects/update", Method.POST);

            request.AddParameter("projectId", projectId);

            var count = 0;
            foreach (var projectFile in files)
            {
                request.AddParameter("files[" + count + "][name]", projectFile.Name);
                request.AddParameter("files[" + count + "][code]", projectFile.Code);
                count++;
            }

            ProjectUpdateResponse result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Delete a specific project owned by the user from QuantConnect.com
        /// </summary>
        /// <param name="projectId">Project id we own and wish to delete</param>
        /// <returns>RestResponse indicating success</returns>
        public RestResponse DeleteProject(int projectId)
        {
            var request = new RestRequest("projects/delete", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId = projectId
            }), ParameterType.RequestBody);
            RestResponse result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Create a new compile job request for this project id.
        /// </summary>
        /// <param name="projectId">Project id we wish to compile.</param>
        /// <returns>Compile object result</returns>
        public Compile CreateCompile(int projectId)
        {
            var request = new RestRequest("compile/create", Method.POST);
            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId = projectId
            }), ParameterType.RequestBody);
            Compile result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Read a compile packet job result.
        /// </summary>
        /// <param name="projectId">Project id we sent for compile</param>
        /// <param name="compileId">Compile id return from the creation request</param>
        /// <returns>Compile object result</returns>
        public Compile ReadCompile(int projectId, string compileId)
        {
            var request = new RestRequest("compile/read", Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("projectId", projectId);
            request.AddParameter("compileId", compileId);
            Compile result;
            _connection.TryRequest(request, out result);
            return result;
        }


        /// <summary>
        /// Create a new backtest request and get the id.
        /// </summary>
        /// <param name="projectId">Id for the project we'd like to backtest</param>
        /// <param name="compileId">Successfuly compile id for the project</param>
        /// <param name="backtestName">Name for the new backtest</param>
        /// <returns>Backtest object</returns>
        public Backtest CreateBacktest(int projectId, string compileId, string backtestName)
        {
            var request = new RestRequest("backtests/create", Method.POST);
            request.AddParameter("projectId", projectId);
            request.AddParameter("compileId", compileId);
            request.AddParameter("backtestName", backtestName);
            Backtest result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Read out a backtest in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="backtestId">Specific backtest id to read</param>
        /// <returns>Backtest object with the results</returns>
        public Backtest ReadBacktest(int projectId, string backtestId)
        {
            var request = new RestRequest("backtests/read", Method.GET);
            request.AddParameter("backtestId", backtestId);
            request.AddParameter("projectId", projectId);
            Backtest result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Update a backtest name
        /// </summary>
        /// <param name="projectId">Project for the backtest we want to update</param>
        /// <param name="backtestId">Backtest id we want to update</param>
        /// <param name="name">Name we'd like to assign to the backtest</param>
        /// <param name="note">Note attached to the backtest</param>
        /// <returns>Rest response class indicating success</returns>
        public RestResponse UpdateBacktest(int projectId, string backtestId, string name = "", string note = "")
        {
            var request = new RestRequest("backtests/update", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId = projectId,
                backtestId = backtestId,
                name = name,
                note = note
            }), ParameterType.RequestBody);
            Backtest result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// List all the backtests in a prokect
        /// </summary>
        /// <param name="projectId">Project id we'd like to get a list of backtest for</param>
        /// <returns>Backtest list container</returns>
        public BacktestList ListBacktests(int projectId)
        {
            var request = new RestRequest("backtests/read", Method.GET);
            request.AddParameter("projectId", projectId);
            BacktestList result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Delete a backtest from the specified project and backtestId.
        /// </summary>
        /// <param name="projectId">Project for the backtest we want to delete</param>
        /// <param name="backtestId">Backtest id we want to delete</param>
        /// <returns></returns>
        public RestResponse DeleteBacktest(int projectId, string backtestId)
        {
            var request = new RestRequest("backtests/delete", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("backtestId", backtestId);
            request.AddParameter("projectId", projectId);
            RestResponse result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Create a live algorithm.
        /// </summary>
        /// <param name="projectId">Id of the project on QuantConnect</param>
        /// <param name="compileId">Id of the compilation on QuantConnect</param>
        /// <param name="serverType">Type of server instance that will run the algorithm</param>
        /// <param name="baseLiveAlgorithmSettings">Brokerage specific <see cref="BaseLiveAlgorithmSettings">BaseLiveAlgorithmSettings</see>.</param>
        /// <param name="versionId">The version of the Lean used to run the algorithm.  
        ///                         -1 is master, however, sometimes this can create problems with live deployements.
        ///                         If you experience problems using, try specifying the version of Lean you would like to use.</param>
        /// <returns>Information regarding the new algorithm <see cref="LiveAlgorithm"/></returns>
        public LiveAlgorithm CreateLiveAlgorithm(int projectId, 
                                                 string compileId, 
                                                 string serverType, 
                                                 BaseLiveAlgorithmSettings baseLiveAlgorithmSettings, 
                                                 string versionId = "-1")
        {
            var request = new RestRequest("live/create", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            var body = JsonConvert.SerializeObject(new LiveAlgorithmApiSettingsWrapper(projectId, 
                                                                                       compileId, 
                                                                                       serverType, 
                                                                                       baseLiveAlgorithmSettings, 
                                                                                       versionId));
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            LiveAlgorithm result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Get a list of live running algorithms for a logged in user.
        /// </summary>
        /// <param name="status">Filter the statuses of the algorithms returned from the api</param>
        /// <param name="startTime">Earliest launched time of the algorithms returned by the Api</param>
        /// <param name="endTime">Latest launched time of the algorithms returned by the Api</param>
        /// <returns>List of live algorithm instances</returns>
        public LiveList ListLiveAlgorithms(AlgorithmStatus? status = null,
                                           DateTime? startTime = null,
                                           DateTime? endTime = null)
        {
            // Only the following statuses are supported by the Api
            if (status.HasValue                        &&
                status != AlgorithmStatus.Running      &&
                status != AlgorithmStatus.RuntimeError &&
                status != AlgorithmStatus.Stopped      &&
                status != AlgorithmStatus.Liquidated)
            {
                throw new ArgumentException(
                    "The Api only supports Algorithm Statuses of Running, Stopped, RuntimeError and Liquidated");
            }

            var request = new RestRequest("live/read", Method.GET);

            if (status.HasValue)
            {
                request.AddParameter("status", status.ToString());
            }

            var epochStartTime = startTime == null ? 0 : Time.DateTimeToUnixTimeStamp(startTime.Value);
            var epochEndTime   = endTime   == null ? Time.DateTimeToUnixTimeStamp(DateTime.UtcNow) : Time.DateTimeToUnixTimeStamp(endTime.Value);

            request.AddParameter("start", epochStartTime);
            request.AddParameter("end", epochEndTime);

            LiveList result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Read out a live algorithm in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="deployId">Specific instance id to read</param>
        /// <returns>Live object with the results</returns>
        public LiveAlgorithmResults ReadLiveAlgorithm(int projectId, string deployId)
        {
            var request = new RestRequest("live/read", Method.GET);
            request.AddParameter("projectId", projectId);
            request.AddParameter("deployId", deployId);
            LiveAlgorithmResults result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Liquidate a live algorithm from the specified project and deployId.
        /// </summary>
        /// <param name="projectId">Project for the live instance we want to stop</param>
        /// <returns></returns>
        public RestResponse LiquidateLiveAlgorithm(int projectId)
        {
            var request = new RestRequest("live/update/liquidate", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("projectId", projectId);
            RestResponse result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Stop a live algorithm from the specified project and deployId.
        /// </summary>
        /// <param name="projectId">Project for the live instance we want to stop</param>
        /// <returns></returns>
        public RestResponse StopLiveAlgorithm(int projectId)
        {
            var request = new RestRequest("live/update/stop", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("projectId", projectId);
            RestResponse result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Gets the logs of a specific live algorithm
        /// </summary>
        /// <param name="projectId">Project Id of the live running algorithm</param>
        /// <param name="algorithmId">Algorithm Id of the live running algorithm</param>
        /// <param name="startTime">No logs will be returned before this time</param>
        /// <param name="endTime">No logs will be returned after this time</param>
        /// <returns>List of strings that represent the logs of the algorithm</returns>
        public LiveLog ReadLiveLogs(int projectId, string algorithmId, DateTime? startTime = null, DateTime? endTime = null)
        {
            var epochStartTime = startTime == null ? 0 : Time.DateTimeToUnixTimeStamp(startTime.Value);
            var epochEndTime   = endTime   == null ? Time.DateTimeToUnixTimeStamp(DateTime.UtcNow) : Time.DateTimeToUnixTimeStamp(endTime.Value);

            var request = new RestRequest("live/read/log", Method.GET);

            request.AddParameter("format", "json");
            request.AddParameter("projectId", projectId);
            request.AddParameter("algorithmId", algorithmId);
            request.AddParameter("start", epochStartTime);
            request.AddParameter("end", epochEndTime);

            LiveLog result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Gets the link to the downloadable data.
        /// </summary>
        /// <param name="symbol">Symbol of security of which data will be requested.</param>
        /// <param name="resolution">Resolution of data requested.</param>
        /// <param name="date">Date of the data requested.</param>
        /// <returns>Link to the downloadable data.</returns>
        public Link ReadDataLink(Symbol symbol, Resolution resolution, DateTime date)
        {
            var request = new RestRequest("data/read", Method.GET);

            request.AddParameter("format", "link");
            request.AddParameter("ticker", symbol.Value.ToLower());
            request.AddParameter("type", symbol.ID.SecurityType.ToLower());
            request.AddParameter("market", symbol.ID.Market);
            request.AddParameter("resolution", resolution);
            request.AddParameter("date", date.ToString("yyyyMMdd"));

            Link result;
            _connection.TryRequest(request, out result);
            return result;
        }

        /// <summary>
        /// Method to download and save the data purchased through QuantConnect
        /// </summary>
        /// <param name="symbol">Symbol of security of which data will be requested.</param>
        /// <param name="resolution">Resolution of data requested.</param>
        /// <param name="date">Date of the data requested.</param>
        /// <returns>A bool indicating whether the data was successfully downloaded or not.</returns>
        public bool DownloadData(Symbol symbol, Resolution resolution, DateTime date)
        {
            // Get a link to the data
            var link = ReadDataLink(symbol, resolution, date);

            // Make sure the link was successfully retrieved
            if (!link.Success)
                return false;

            // Save csv in same folder heirarchy as Lean
            var path = Path.Combine(_dataFolder, LeanData.GenerateRelativeZipFilePath(symbol.Value, symbol.ID.SecurityType, symbol.ID.Market, date, resolution));

            // Make sure the directory exist before writing
            (new FileInfo(path)).Directory.Create();

            // Download and save the data
            var uri     = new Uri(link.DataLink);
            var client  = new RestClient(uri.Scheme + "://" + uri.Host);
            var request = new RestRequest(uri.PathAndQuery, Method.GET);
            client.DownloadData(request).SaveAs(path);

            return true;
        }

        /// <summary>
        /// Calculate the remaining bytes of user log allowed based on the user's cap and daily cumulative usage.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="userToken">User API token</param>
        /// <returns>int[3] iUserBacktestLimit, iUserDailyLimit, remaining</returns>
        public virtual int[] ReadLogAllowance(int userId, int projectId, string userToken)
        {
            return new[] { int.MaxValue, int.MaxValue, int.MaxValue };
        }

        /// <summary>
        /// Update the daily log of allowed logging-data
        /// </summary>
        /// <param name="userId">Id of the User</param>
        /// <param name="backtestId">BacktestId</param>
        /// <param name="url">URL of the log entry</param>
        /// <param name="length">length of data</param>
        /// <param name="userToken">User access token</param>
        /// <param name="hitLimit">Boolean signifying hit log limit</param>
        /// <returns>Number of bytes remaining</returns>
        public virtual void UpdateDailyLogUsed(int userId, string backtestId, string url, int length, string userToken, bool hitLimit = false)
        {
            //
        }

        /// <summary>
        /// Get the algorithm status from the user with this algorithm id.
        /// </summary>
        /// <param name="algorithmId">String algorithm id we're searching for.</param>
        /// <param name="userId">The user id of the algorithm</param>
        /// <returns>Algorithm status enum</returns>
        public virtual AlgorithmControl GetAlgorithmStatus(string algorithmId, int userId)
        {
            return new AlgorithmControl();
        }

        /// <summary>
        /// Algorithm passes back its current status to the UX.
        /// </summary>
        /// <param name="status">Status of the current algorithm</param>
        /// <param name="algorithmId">String algorithm id we're setting.</param>
        /// <param name="message">Message for the algorithm status event</param>
        /// <returns>Algorithm status enum</returns>
        public virtual void SetAlgorithmStatus(string algorithmId, AlgorithmStatus status, string message = "")
        {
            //
        }

        /// <summary>
        /// Send the statistics to storage for performance tracking.
        /// </summary>
        /// <param name="algorithmId">Identifier for algorithm</param>
        /// <param name="unrealized">Unrealized gainloss</param>
        /// <param name="fees">Total fees</param>
        /// <param name="netProfit">Net profi</param>
        /// <param name="holdings">Algorithm holdings</param>
        /// <param name="equity">Total equity</param>
        /// <param name="netReturn">Net return for the deployment</param>
        /// <param name="volume">Volume traded</param>
        /// <param name="trades">Total trades since inception</param>
        /// <param name="sharpe">Sharpe ratio since inception</param>
        public virtual void SendStatistics(string algorithmId, decimal unrealized, decimal fees, decimal netProfit, decimal holdings, decimal equity, decimal netReturn, decimal volume, int trades, double sharpe)
        {
            //
        }

        /// <summary>
        /// Get the calendar open hours for the date.
        /// </summary>
        public virtual IEnumerable<MarketHoursSegment> MarketToday(DateTime time, Symbol symbol)
        {
            if (Config.GetBool("force-exchange-always-open"))
            {
                yield return MarketHoursSegment.OpenAllDay();
                yield break;
            }

            var hours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.ID.SecurityType);
            foreach (var segment in hours.MarketHours[time.DayOfWeek].Segments)
            {
                yield return segment;
            }
        }

        /// <summary>
        /// Store logs with these authentication type
        /// </summary>
        public virtual void Store(string data, string location, StoragePermissions permissions, bool async = false)
        {
            //
        }

        /// <summary>
        /// Send an email to the user associated with the specified algorithm id
        /// </summary>
        /// <param name="algorithmId">The algorithm id</param>
        /// <param name="subject">The email subject</param>
        /// <param name="body">The email message body</param>
        public virtual void SendUserEmail(string algorithmId, string subject, string body)
        {
            //
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
            // NOP
        }
    }
}