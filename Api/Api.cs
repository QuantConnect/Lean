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
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Extensions;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using QuantConnect.Util;
using QuantConnect.Notifications;
using Python.Runtime;
using System.Threading;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Api
{
    /// <summary>
    /// QuantConnect.com Interaction Via API.
    /// </summary>
    public class Api : IApi, IDownloadProvider
    {
        private readonly BlockingCollection<Lazy<HttpClient>> _clientPool;
        private string _dataFolder;

        /// <summary>
        /// Serializer settings to use
        /// </summary>
        protected JsonSerializerSettings SerializerSettings { get; set; } = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    OverrideSpecifiedNames = true
                }
            }
        };

        /// <summary>
        /// Returns the underlying API connection
        /// </summary>
        protected ApiConnection ApiConnection { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="Api"/>
        /// </summary>
        public Api()
        {
            _clientPool = new BlockingCollection<Lazy<HttpClient>>(new ConcurrentQueue<Lazy<HttpClient>>(), 5);
            for (int i = 0; i < _clientPool.BoundedCapacity; i++)
            {
                _clientPool.Add(new Lazy<HttpClient>());
            }
        }

        /// <summary>
        /// Initialize the API with the given variables
        /// </summary>
        public virtual void Initialize(int userId, string token, string dataFolder)
        {
            ApiConnection = new ApiConnection(userId, token);
            _dataFolder = dataFolder?.Replace("\\", "/", StringComparison.InvariantCulture);

            //Allow proper decoding of orders from the API.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };
        }

        /// <summary>
        /// Check if Api is successfully connected with correct credentials
        /// </summary>
        public bool Connected => ApiConnection.Connected;

        /// <summary>
        /// Create a project with the specified name and language via QuantConnect.com API
        /// </summary>
        /// <param name="name">Project name</param>
        /// <param name="language">Programming language to use</param>
        /// <param name="organizationId">Optional param for specifying organization to create project under.
        /// If none provided web defaults to preferred.</param>
        /// <returns>Project object from the API.</returns>

        public ProjectResponse CreateProject(string name, Language language, string organizationId = null)
        {
            var request = new RestRequest("projects/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            // Only include organization Id if its not null or empty
            string jsonParams;
            if (string.IsNullOrEmpty(organizationId))
            {
                jsonParams = JsonConvert.SerializeObject(new
                {
                    name,
                    language
                });
            }
            else
            {
                jsonParams = JsonConvert.SerializeObject(new
                {
                    name,
                    language,
                    organizationId
                });
            }

            request.AddParameter("application/json", jsonParams, ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ProjectResponse result);
            return result;
        }

        /// <summary>
        /// Get details about a single project
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <returns><see cref="ProjectResponse"/> that contains information regarding the project</returns>

        public ProjectResponse ReadProject(int projectId)
        {
            var request = new RestRequest("projects/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ProjectResponse result);
            return result;
        }

        /// <summary>
        /// List details of all projects
        /// </summary>
        /// <returns><see cref="ProjectResponse"/> that contains information regarding the project</returns>

        public ProjectResponse ListProjects()
        {
            var request = new RestRequest("projects/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            ApiConnection.TryRequest(request, out ProjectResponse result);
            return result;
        }


        /// <summary>
        /// Add a file to a project
        /// </summary>
        /// <param name="projectId">The project to which the file should be added</param>
        /// <param name="name">The name of the new file</param>
        /// <param name="content">The content of the new file</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes information about the newly created file</returns>

        public RestResponse AddProjectFile(int projectId, string name, string content)
        {
            var request = new RestRequest("files/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name,
                content
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }


        /// <summary>
        /// Update the name of a file
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="oldFileName">The current name of the file</param>
        /// <param name="newFileName">The new name for the file</param>
        /// <returns><see cref="RestResponse"/> indicating success</returns>

        public RestResponse UpdateProjectFileName(int projectId, string oldFileName, string newFileName)
        {
            var request = new RestRequest("files/update", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name = oldFileName,
                newName = newFileName
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }


        /// <summary>
        /// Update the contents of a file
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="fileName">The name of the file that should be updated</param>
        /// <param name="newFileContents">The new contents of the file</param>
        /// <returns><see cref="RestResponse"/> indicating success</returns>

        public RestResponse UpdateProjectFileContent(int projectId, string fileName, string newFileContents)
        {
            var request = new RestRequest("files/update", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name = fileName,
                content = newFileContents
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }


        /// <summary>
        /// Read all files in a project
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes the information about all files in the project</returns>

        public ProjectFilesResponse ReadProjectFiles(int projectId)
        {
            var request = new RestRequest("files/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ProjectFilesResponse result);
            return result;
        }

        /// <summary>
        /// Read all nodes in a project.
        /// </summary>
        /// <param name="projectId">Project id to which the nodes refer</param>
        /// <returns><see cref="ProjectNodesResponse"/> that includes the information about all nodes in the project</returns>
        public ProjectNodesResponse ReadProjectNodes(int projectId)
        {
            var request = new RestRequest("projects/nodes/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ProjectNodesResponse result);
            return result;
        }

        /// <summary>
        /// Update the active state of some nodes to true.
        /// If you don't provide any nodes, all the nodes become inactive and AutoSelectNode is true.
        /// </summary>
        /// <param name="projectId">Project id to which the nodes refer</param>
        /// <param name="nodes">List of node ids to update</param>
        /// <returns><see cref="ProjectNodesResponse"/> that includes the information about all nodes in the project</returns>
        public ProjectNodesResponse UpdateProjectNodes(int projectId, string[] nodes)
        {
            var request = new RestRequest("projects/nodes/update", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                nodes
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ProjectNodesResponse result);
            return result;
        }

        /// <summary>
        /// Read a file in a project
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="fileName">The name of the file</param>
        /// <returns><see cref="ProjectFilesResponse"/> that includes the file information</returns>

        public ProjectFilesResponse ReadProjectFile(int projectId, string fileName)
        {
            var request = new RestRequest("files/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name = fileName
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ProjectFilesResponse result);
            return result;
        }

        /// <summary>
        /// Gets a list of LEAN versions with their corresponding basic descriptions
        /// </summary>
        public VersionsResponse ReadLeanVersions()
        {
            var request = new RestRequest("lean/versions/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            ApiConnection.TryRequest(request, out VersionsResponse result);
            return result;
        }

        /// <summary>
        /// Delete a file in a project
        /// </summary>
        /// <param name="projectId">Project id to which the file belongs</param>
        /// <param name="name">The name of the file that should be deleted</param>
        /// <returns><see cref="RestResponse"/> that includes the information about all files in the project</returns>

        public RestResponse DeleteProjectFile(int projectId, string name)
        {
            var request = new RestRequest("files/delete", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name,
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        /// <param name="projectId">Project id we own and wish to delete</param>
        /// <returns>RestResponse indicating success</returns>

        public RestResponse DeleteProject(int projectId)
        {
            var request = new RestRequest("projects/delete", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Create a new compile job request for this project id.
        /// </summary>
        /// <param name="projectId">Project id we wish to compile.</param>
        /// <returns>Compile object result</returns>

        public Compile CreateCompile(int projectId)
        {
            var request = new RestRequest("compile/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out Compile result);
            return result;
        }

        /// <summary>
        /// Read a compile packet job result.
        /// </summary>
        /// <param name="projectId">Project id we sent for compile</param>
        /// <param name="compileId">Compile id return from the creation request</param>
        /// <returns><see cref="Compile"/></returns>

        public Compile ReadCompile(int projectId, string compileId)
        {
            var request = new RestRequest("compile/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                compileId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out Compile result);
            return result;
        }

        /// <summary>
        /// Sends a notification
        /// </summary>
        /// <param name="notification">The notification to send</param>
        /// <param name="projectId">The project id</param>
        /// <returns><see cref="RestResponse"/> containing success response and errors</returns>
        public virtual RestResponse SendNotification(Notification notification, int projectId)
        {
            throw new NotImplementedException($"{nameof(Api)} does not support sending notifications");
        }

        /// <summary>
        /// Create a new backtest request and get the id.
        /// </summary>
        /// <param name="projectId">Id for the project to backtest</param>
        /// <param name="compileId">Compile id for the project</param>
        /// <param name="backtestName">Name for the new backtest</param>
        /// <returns><see cref="Backtest"/>t</returns>

        public Backtest CreateBacktest(int projectId, string compileId, string backtestName)
        {
            var request = new RestRequest("backtests/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                compileId,
                backtestName
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out BacktestResponseWrapper result);

            // Use API Response values for Backtest Values
            result.Backtest.Success = result.Success;
            result.Backtest.Errors = result.Errors;

            // Return only the backtest object
            return result.Backtest;
        }

        /// <summary>
        /// Read out a backtest in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="backtestId">Specific backtest id to read</param>
        /// <param name="getCharts">True will return backtest charts</param>
        /// <returns><see cref="Backtest"/></returns>

        public Backtest ReadBacktest(int projectId, string backtestId, bool getCharts = true)
        {
            var request = new RestRequest("backtests/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                backtestId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out BacktestResponseWrapper result);

            if (result == null)
            {
                // api call failed
                return null;
            }

            if (!result.Success)
            {
                // place an empty place holder so we can return any errors back to the user and not just null
                result.Backtest = new Backtest { BacktestId = backtestId };
            }
            // Go fetch the charts if the backtest is completed and success
            else if (getCharts && result.Backtest.Completed)
            {
                // For storing our collected charts
                var updatedCharts = new Dictionary<string, Chart>();

                // Create backtest requests for each chart that is empty
                foreach (var chart in result.Backtest.Charts)
                {
                    if (!chart.Value.Series.IsNullOrEmpty())
                    {
                        continue;
                    }

                    var chartRequest = new RestRequest("backtests/chart/read", Method.POST)
                    {
                        RequestFormat = DataFormat.Json
                    };

                    chartRequest.AddParameter("application/json", JsonConvert.SerializeObject(new
                    {
                        projectId,
                        backtestId,
                        name = chart.Key,
                        count = 100
                    }), ParameterType.RequestBody);

                    // Add this chart to our updated collection
                    if (ApiConnection.TryRequest(chartRequest, out ReadChartResponse chartResponse) && chartResponse.Success)
                    {
                        updatedCharts.Add(chart.Key, chartResponse.Chart);
                    }
                }

                // Update our result
                foreach(var updatedChart in updatedCharts)
                {
                    result.Backtest.Charts[updatedChart.Key] = updatedChart.Value;
                }
            }

            // Use API Response values for Backtest Values
            result.Backtest.Success = result.Success;
            result.Backtest.Errors = result.Errors;

            // Return only the backtest object
            return result.Backtest;
        }

        /// <summary>
        /// Returns the orders of the specified backtest and project id.
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the orders</param>
        /// <param name="backtestId">Id of the backtest from which to read the orders</param>
        /// <param name="start">Starting index of the orders to be fetched. Required if end > 100</param>
        /// <param name="end">Last index of the orders to be fetched. Note that end - start must be less than 100</param>
        /// <remarks>Will throw an <see cref="WebException"/> if there are any API errors</remarks>
        /// <returns>The list of <see cref="Order"/></returns>

        public List<ApiOrderResponse> ReadBacktestOrders(int projectId, string backtestId, int start = 0, int end = 100)
        {
            var request = new RestRequest("backtests/orders/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                start,
                end,
                projectId,
                backtestId
            }), ParameterType.RequestBody);

            return MakeRequestOrThrow<OrdersResponseWrapper>(request, nameof(ReadBacktestOrders)).Orders;
        }

        /// <summary>
        /// Returns a requested chart object from a backtest
        /// </summary>
        /// <param name="projectId">Project ID of the request</param>
        /// <param name="name">The requested chart name</param>
        /// <param name="start">The Utc start seconds timestamp of the request</param>
        /// <param name="end">The Utc end seconds timestamp of the request</param>
        /// <param name="count">The number of data points to request</param>
        /// <param name="backtestId">Associated Backtest ID for this chart request</param>
        /// <returns>The chart</returns>
        public ReadChartResponse ReadBacktestChart(int projectId, string name, int start, int end, uint count, string backtestId)
        {
            var request = new RestRequest("backtests/chart/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name,
                start,
                end,
                count,
                backtestId,
            }), ParameterType.RequestBody);

            ReadChartResponse result;
            ApiConnection.TryRequest(request, out result);

            var finish = DateTime.UtcNow.AddMinutes(1);
            while (DateTime.UtcNow < finish && result.Chart == null)
            {
                Thread.Sleep(5000);
                ApiConnection.TryRequest(request, out result);
            }

            return result;
        }

        /// <summary>
        /// Update a backtest name
        /// </summary>
        /// <param name="projectId">Project for the backtest we want to update</param>
        /// <param name="backtestId">Backtest id we want to update</param>
        /// <param name="name">Name we'd like to assign to the backtest</param>
        /// <param name="note">Note attached to the backtest</param>
        /// <returns><see cref="RestResponse"/></returns>

        public RestResponse UpdateBacktest(int projectId, string backtestId, string name = "", string note = "")
        {
            var request = new RestRequest("backtests/update", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                backtestId,
                name,
                note
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// List all the backtest summaries for a project
        /// </summary>
        /// <param name="projectId">Project id we'd like to get a list of backtest for</param>
        /// <param name="includeStatistics">True for include statistics in the response, false otherwise</param>
        /// <returns><see cref="BacktestList"/></returns>

        public BacktestSummaryList ListBacktests(int projectId, bool includeStatistics = true)
        {
            var request = new RestRequest("backtests/list", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            var obj = new Dictionary<string, object>()
            {
                { "projectId", projectId },
                { "includeStatistics", includeStatistics }
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out BacktestSummaryList result);
            return result;
        }

        /// <summary>
        /// Delete a backtest from the specified project and backtestId.
        /// </summary>
        /// <param name="projectId">Project for the backtest we want to delete</param>
        /// <param name="backtestId">Backtest id we want to delete</param>
        /// <returns><see cref="RestResponse"/></returns>

        public RestResponse DeleteBacktest(int projectId, string backtestId)
        {
            var request = new RestRequest("backtests/delete", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                backtestId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Updates the tags collection for a backtest
        /// </summary>
        /// <param name="projectId">Project for the backtest we want to update</param>
        /// <param name="backtestId">Backtest id we want to update</param>
        /// <param name="tags">The new backtest tags</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse UpdateBacktestTags(int projectId, string backtestId, IReadOnlyCollection<string> tags)
        {
            var request = new RestRequest("backtests/tags/update", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                backtestId,
                tags
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Read out the insights of a backtest
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the backtest</param>
        /// <param name="backtestId">Backtest id from which we want to get the insights</param>
        /// <param name="start">Starting index of the insights to be fetched</param>
        /// <param name="end">Last index of the insights to be fetched. Note that end - start must be less than 100</param>
        /// <returns><see cref="InsightResponse"/></returns>
        /// <exception cref="ArgumentException"></exception>
        public InsightResponse ReadBacktestInsights(int projectId, string backtestId, int start = 0, int end = 0)
        {
            var request = new RestRequest("backtests/insights/read", Method.POST)
            {
                RequestFormat = DataFormat.Json,
            };

            var diff = end - start;
            if (diff > 100)
            {
                throw new ArgumentException($"The difference between the start and end index of the insights must be smaller than 100, but it was {diff}.");
            }
            else if (end == 0)
            {
                end = start + 100;
            }

            JObject obj = new()
            {
                { "projectId", projectId },
                { "backtestId", backtestId },
                { "start", start },
                { "end", end },
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out InsightResponse result);
            return result;
        }

        /// <summary>
        /// Create a live algorithm.
        /// </summary>
        /// <param name="projectId">Id of the project on QuantConnect</param>
        /// <param name="compileId">Id of the compilation on QuantConnect</param>
        /// <param name="nodeId">Id of the node that will run the algorithm</param>
        /// <param name="brokerageSettings">Dictionary with brokerage specific settings. Each brokerage requires certain specific credentials
        ///                         in order to process the given orders. Each key in this dictionary represents a required field/credential
        ///                         to provide to the brokerage API and its value represents the value of that field. For example: "brokerageSettings: {
        ///                         "id": "Binance", "binance-api-secret": "123ABC", "binance-api-key": "ABC123"}. It is worth saying,
        ///                         that this dictionary must always contain an entry whose key is "id" and its value is the name of the brokerage
        ///                         (see <see cref="Brokerages.BrokerageName"/>)</param>
        /// <param name="versionId">The version of the Lean used to run the algorithm.
        ///                         -1 is master, however, sometimes this can create problems with live deployments.
        ///                         If you experience problems using, try specifying the version of Lean you would like to use.</param>
        /// <param name="dataProviders">Dictionary with data providers credentials. Each data provider requires certain credentials
        ///                         in order to retrieve data from their API. Each key in this dictionary describes a data provider name
        ///                         and its corresponding value is another dictionary with the required key-value pairs of credential
        ///                         names and values. For example: "dataProviders: { "InteractiveBrokersBrokerage" : { "id": 12345, "environment" : "paper",
        ///                         "username": "testUsername", "password": "testPassword"}}"</param>
        /// <returns>Information regarding the new algorithm <see cref="CreateLiveAlgorithmResponse"/></returns>
        public CreateLiveAlgorithmResponse CreateLiveAlgorithm(int projectId,
                                                 string compileId,
                                                 string nodeId,
                                                 Dictionary<string, object> brokerageSettings,
                                                 string versionId = "-1",
                                                 Dictionary<string, object> dataProviders = null)
        {
            var request = new RestRequest("live/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(
                new LiveAlgorithmApiSettingsWrapper
                (projectId,
                compileId,
                nodeId,
                brokerageSettings,
                versionId,
                dataProviders
                )
                ), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out CreateLiveAlgorithmResponse result);
            return result;
        }

        /// <summary>
        /// Create a live algorithm.
        /// </summary>
        /// <param name="projectId">Id of the project on QuantConnect</param>
        /// <param name="compileId">Id of the compilation on QuantConnect</param>
        /// <param name="nodeId">Id of the node that will run the algorithm</param>
        /// <param name="brokerageSettings">Python Dictionary with brokerage specific settings. Each brokerage requires certain specific credentials
        ///                         in order to process the given orders. Each key in this dictionary represents a required field/credential
        ///                         to provide to the brokerage API and its value represents the value of that field. For example: "brokerageSettings: {
        ///                         "id": "Binance", "binance-api-secret": "123ABC", "binance-api-key": "ABC123"}. It is worth saying,
        ///                         that this dictionary must always contain an entry whose key is "id" and its value is the name of the brokerage
        ///                         (see <see cref="Brokerages.BrokerageName"/>)</param>
        /// <param name="versionId">The version of the Lean used to run the algorithm.
        ///                         -1 is master, however, sometimes this can create problems with live deployments.
        ///                         If you experience problems using, try specifying the version of Lean you would like to use.</param>
        /// <param name="dataProviders">Python Dictionary with data providers credentials. Each data provider requires certain credentials
        ///                         in order to retrieve data from their API. Each key in this dictionary describes a data provider name
        ///                         and its corresponding value is another dictionary with the required key-value pairs of credential
        ///                         names and values. For example: "dataProviders: { "InteractiveBrokersBrokerage" : { "id": 12345, "environment" : "paper",
        ///                         "username": "testUsername", "password": "testPassword"}}"</param>
        /// <returns>Information regarding the new algorithm <see cref="CreateLiveAlgorithmResponse"/></returns>

        public CreateLiveAlgorithmResponse CreateLiveAlgorithm(int projectId, string compileId, string nodeId, PyObject brokerageSettings, string versionId = "-1", PyObject dataProviders = null)
        {
            return CreateLiveAlgorithm(projectId, compileId, nodeId, ConvertToDictionary(brokerageSettings), versionId, dataProviders != null ? ConvertToDictionary(dataProviders) : null);
        }

        /// <summary>
        /// Converts a given Python dictionary into a C# <see cref="Dictionary{string, object}"/>
        /// </summary>
        /// <param name="brokerageSettings">Python dictionary to be converted</param>
        private static Dictionary<string, object> ConvertToDictionary(PyObject brokerageSettings)
        {
            using (Py.GIL())
            {
                var stringBrokerageSettings = brokerageSettings.ToString();
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(stringBrokerageSettings);
            }
        }

        /// <summary>
        /// Get a list of live running algorithms for user
        /// </summary>
        /// <param name="status">Filter the statuses of the algorithms returned from the api</param>
        /// <param name="startTime">Earliest launched time of the algorithms returned by the Api</param>
        /// <param name="endTime">Latest launched time of the algorithms returned by the Api</param>
        /// <returns><see cref="LiveList"/></returns>

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

            var request = new RestRequest("live/list", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            var epochStartTime = startTime == null ? 0 : Time.DateTimeToUnixTimeStamp(startTime.Value);
            var epochEndTime   = endTime   == null ? Time.DateTimeToUnixTimeStamp(DateTime.UtcNow) : Time.DateTimeToUnixTimeStamp(endTime.Value);

            JObject obj = new JObject
            {
                { "start", epochStartTime },
                { "end", epochEndTime }
            };

            if (status.HasValue)
            {
                obj.Add("status", status.ToString());
            }

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out LiveList result);
            return result;
        }

        /// <summary>
        /// Read out a live algorithm in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="deployId">Specific instance id to read</param>
        /// <returns><see cref="LiveAlgorithmResults"/></returns>

        public LiveAlgorithmResults ReadLiveAlgorithm(int projectId, string deployId)
        {
            var request = new RestRequest("live/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                deployId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out LiveAlgorithmResults result);
            return result;
        }

        /// <summary>
        /// Read out the portfolio state of a live algorithm
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the live algorithm</param>
        /// <returns><see cref="PortfolioResponse"/></returns>
        public PortfolioResponse ReadLivePortfolio(int projectId)
        {
            var request = new RestRequest("live/portfolio/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out PortfolioResponse result);
            return result;
        }

        /// <summary>
        /// Returns the orders of the specified project id live algorithm.
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the live orders</param>
        /// <param name="start">Starting index of the orders to be fetched. Required if end > 100</param>
        /// <param name="end">Last index of the orders to be fetched. Note that end - start must be less than 100</param>
        /// <remarks>Will throw an <see cref="WebException"/> if there are any API errors</remarks>
        /// <returns>The list of <see cref="Order"/></returns>

        public List<ApiOrderResponse> ReadLiveOrders(int projectId, int start = 0, int end = 100)
        {
            var request = new RestRequest("live/orders/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                start,
                end,
                projectId
            }), ParameterType.RequestBody);

            return MakeRequestOrThrow<OrdersResponseWrapper>(request, nameof(ReadLiveOrders)).Orders;
        }

        /// <summary>
        /// Liquidate a live algorithm from the specified project and deployId.
        /// </summary>
        /// <param name="projectId">Project for the live instance we want to stop</param>
        /// <returns><see cref="RestResponse"/></returns>

        public RestResponse LiquidateLiveAlgorithm(int projectId)
        {
            var request = new RestRequest("live/update/liquidate", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Stop a live algorithm from the specified project and deployId.
        /// </summary>
        /// <param name="projectId">Project for the live instance we want to stop</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse StopLiveAlgorithm(int projectId)
        {
            var request = new RestRequest("live/update/stop", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Create a live command
        /// </summary>
        /// <param name="projectId">Project for the live instance we want to run the command against</param>
        /// <param name="command">The command to run</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse CreateLiveCommand(int projectId, object command)
        {
            var request = new RestRequest("live/commands/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                command
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Broadcast a live command
        /// </summary>
        /// <param name="organizationId">Organization ID of the projects we would like to broadcast the command to</param>
        /// <param name="excludeProjectId">Project for the live instance we want to exclude from the broadcast list</param>
        /// <param name="command">The command to run</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse BroadcastLiveCommand(string organizationId, int? excludeProjectId, object command)
        {
            var request = new RestRequest("live/commands/broadcast", Method.POST)
            {
                RequestFormat = DataFormat.Json,
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                organizationId,
                excludeProjectId,
                command
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Gets the logs of a specific live algorithm
        /// </summary>
        /// <param name="projectId">Project Id of the live running algorithm</param>
        /// <param name="algorithmId">Algorithm Id of the live running algorithm</param>
        /// <param name="startLine">Start line of logs to read</param>
        /// <param name="endLine">End line of logs to read</param>
        /// <returns><see cref="LiveLog"/> List of strings that represent the logs of the algorithm</returns>
        public LiveLog ReadLiveLogs(int projectId, string algorithmId, int startLine, int endLine)
        {
            var logLinesNumber = endLine - startLine;
            if (logLinesNumber > 250)
            {
                throw new ArgumentException($"The maximum number of log lines allowed is 250. But the number of log lines was {logLinesNumber}.");
            }

            var request = new RestRequest("live/logs/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                format = "json",
                projectId,
                algorithmId,
                startLine,
                endLine,
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out LiveLog result);
            return result;
        }

        /// <summary>
        /// Returns a chart object from a live algorithm
        /// </summary>
        /// <param name="projectId">Project ID of the request</param>
        /// <param name="name">The requested chart name</param>
        /// <param name="start">The Utc start seconds timestamp of the request</param>
        /// <param name="end">The Utc end seconds timestamp of the request</param>
        /// <param name="count">The number of data points to request</param>
        /// <returns>The chart</returns>
        public ReadChartResponse ReadLiveChart(int projectId, string name, int start, int end, uint count)
        {
            var request = new RestRequest("live/chart/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name,
                start,
                end,
                count
            }), ParameterType.RequestBody);

            ReadChartResponse result = default;
            ApiConnection.TryRequest(request, out result);

            var finish = DateTime.UtcNow.AddMinutes(1);
            while(DateTime.UtcNow < finish && result.Chart == null)
            {
                Thread.Sleep(5000);
                ApiConnection.TryRequest(request, out result);
            }
            return result;
        }

        /// <summary>
        /// Read out the insights of a live algorithm
        /// </summary>
        /// <param name="projectId">Id of the project from which to read the live algorithm</param>
        /// <param name="start">Starting index of the insights to be fetched</param>
        /// <param name="end">Last index of the insights to be fetched. Note that end - start must be less than 100</param>
        /// <returns><see cref="InsightResponse"/></returns>
        /// <exception cref="ArgumentException"></exception>
        public InsightResponse ReadLiveInsights(int projectId, int start = 0, int end = 0)
        {
            var request = new RestRequest("live/insights/read", Method.POST)
            {
                RequestFormat = DataFormat.Json,
            };

            var diff = end - start;
            if (diff > 100)
            {
                throw new ArgumentException($"The difference between the start and end index of the insights must be smaller than 100, but it was {diff}.");
            }
            else if (end == 0)
            {
                end = start + 100;
            }

            JObject obj = new JObject
            {
                { "projectId", projectId },
                { "start", start },
                { "end", end },
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out InsightResponse result);
            return result;
        }

        /// <summary>
        /// Gets the link to the downloadable data.
        /// </summary>
        /// <param name="filePath">File path representing the data requested</param>
        /// <param name="organizationId">Organization to download from</param>
        /// <returns><see cref="DataLink"/> to the downloadable data.</returns>
        public DataLink ReadDataLink(string filePath, string organizationId)
        {
            if (filePath == null)
            {
                throw new ArgumentException("Api.ReadDataLink(): Filepath must not be null");
            }

            // Prepare filePath for request
            filePath = FormatPathForDataRequest(filePath);

            var request = new RestRequest("data/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                format = "link",
                filePath,
                organizationId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out DataLink result);
            return result;
        }

        /// <summary>
        /// Get valid data entries for a given filepath from data/list
        /// </summary>
        /// <returns></returns>
        public DataList ReadDataDirectory(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentException("Api.ReadDataDirectory(): Filepath must not be null");
            }

            // Prepare filePath for request
            filePath = FormatPathForDataRequest(filePath);

            // Verify the filePath for this request is at least three directory deep
            // (requirement of endpoint)
            if (filePath.Count(x => x == '/') < 3)
            {
                throw new ArgumentException($"Api.ReadDataDirectory(): Data directory requested must be at least" +
                    $" three directories deep. FilePath: {filePath}");
            }

            var request = new RestRequest("data/list", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                filePath
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out DataList result);
            return result;
        }

        /// <summary>
        /// Gets data prices from data/prices
        /// </summary>
        public DataPricesList ReadDataPrices(string organizationId)
        {
            var request = new RestRequest("data/prices", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                organizationId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out DataPricesList result);
            return result;
        }

        /// <summary>
        /// Read out the report of a backtest in the project id specified.
        /// </summary>
        /// <param name="projectId">Project id to read</param>
        /// <param name="backtestId">Specific backtest id to read</param>
        /// <returns><see cref="BacktestReport"/></returns>
        public BacktestReport ReadBacktestReport(int projectId, string backtestId)
        {
            var request = new RestRequest("backtests/read/report", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                backtestId,
                projectId
            }), ParameterType.RequestBody);

            BacktestReport report = new BacktestReport();
            var finish = DateTime.UtcNow.AddMinutes(1);
            while (DateTime.UtcNow < finish && !report.Success)
            {
                Thread.Sleep(10000);
                ApiConnection.TryRequest(request, out report);
            }
            return report;
        }

        /// <summary>
        /// Method to purchase and download data from QuantConnect
        /// </summary>
        /// <param name="filePath">File path representing the data requested</param>
        /// <param name="organizationId">Organization to buy the data with</param>
        /// <returns>A <see cref="bool"/> indicating whether the data was successfully downloaded or not.</returns>

        public bool DownloadData(string filePath, string organizationId)
        {
            // Get a link to the data
            var dataLink = ReadDataLink(filePath, organizationId);

            // Make sure the link was successfully retrieved
            if (!dataLink.Success)
            {
                Log.Trace($"Api.DownloadData(): Failed to get link for {filePath}. " +
                    $"Errors: {string.Join(',', dataLink.Errors)}");
                return false;
            }

            // Make sure the directory exist before writing
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var client = BorrowClient();
            try
            {
                // Download the file
                var uri = new Uri(dataLink.Link);
                using var dataStream = client.Value.GetStreamAsync(uri);

                using var fileStream = new FileStream(FileExtension.ToNormalizedPath(filePath), FileMode.Create);
                dataStream.Result.CopyTo(fileStream);
            }
            catch
            {
                Log.Error($"Api.DownloadData(): Failed to download zip for path ({filePath})");
                return false;
            }
            finally
            {
                ReturnClient(client);
            }

            return true;
        }

        /// <summary>
        /// Get the algorithm status from the user with this algorithm id.
        /// </summary>
        /// <param name="algorithmId">String algorithm id we're searching for.</param>
        /// <returns>Algorithm status enum</returns>

        public virtual AlgorithmControl GetAlgorithmStatus(string algorithmId)
        {
            return new AlgorithmControl()
            {
                ChartSubscription = "*"
            };
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
        /// Local implementation for downloading data to algorithms
        /// </summary>
        /// <param name="address">URL to download</param>
        /// <param name="headers">KVP headers</param>
        /// <param name="userName">Username for basic authentication</param>
        /// <param name="password">Password for basic authentication</param>
        /// <returns></returns>
        public virtual string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
        {
            return Encoding.UTF8.GetString(DownloadBytes(address, headers, userName, password));
        }

        /// <summary>
        /// Local implementation for downloading data to algorithms
        /// </summary>
        /// <param name="address">URL to download</param>
        /// <param name="headers">KVP headers</param>
        /// <param name="userName">Username for basic authentication</param>
        /// <param name="password">Password for basic authentication</param>
        /// <returns>A stream from which the data can be read</returns>
        /// <remarks>Stream.Close() most be called to avoid running out of resources</remarks>
        public virtual byte[] DownloadBytes(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
        {
            var client = BorrowClient();
            try
            {
                client.Value.DefaultRequestHeaders.Clear();

                // Add a user agent header in case the requested URI contains a query.
                client.Value.DefaultRequestHeaders.TryAddWithoutValidation("user-agent", "QCAlgorithm.Download(): User Agent Header");

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.Value.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                if (!userName.IsNullOrEmpty() || !password.IsNullOrEmpty())
                {
                    var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
                    client.Value.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }

                return client.Value.GetByteArrayAsync(new Uri(address)).Result;
            }
            catch (Exception exception)
            {
                var message = $"Api.DownloadBytes(): Failed to download data from {address}";
                if (!userName.IsNullOrEmpty() || !password.IsNullOrEmpty())
                {
                    message += $" with username: {userName} and password {password}";
                }

                throw new WebException($"{message}. Please verify the source for missing http:// or https://", exception);
            }
            finally
            {
                client.Value.DefaultRequestHeaders.Clear();
                ReturnClient(client);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
            // Dispose of the HttpClient pool
            _clientPool.CompleteAdding();
            foreach (var client in _clientPool.GetConsumingEnumerable())
            {
                if (client.IsValueCreated)
                {
                    client.Value.DisposeSafely();
                }
            }
            _clientPool.DisposeSafely();
        }

        /// <summary>
        /// Generate a secure hash for the authorization headers.
        /// </summary>
        /// <returns>Time based hash of user token and timestamp.</returns>
        public static string CreateSecureHash(int timestamp, string token)
        {
            // Create a new hash using current UTC timestamp.
            // Hash must be generated fresh each time.
            var data = $"{token}:{timestamp.ToStringInvariant()}";
            return data.ToSHA256();
        }

        /// <summary>
        /// Will read the organization account status
        /// </summary>
        /// <param name="organizationId">The target organization id, if null will return default organization</param>
        public Account ReadAccount(string organizationId = null)
        {
            var request = new RestRequest("account/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            if (organizationId != null)
            {
                request.AddParameter("application/json", JsonConvert.SerializeObject(new { organizationId }), ParameterType.RequestBody);
            }

            ApiConnection.TryRequest(request, out Account account);
            return account;
        }

        /// <summary>
        /// Fetch organization data from web API
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        public Organization ReadOrganization(string organizationId = null)
        {
            var request = new RestRequest("organizations/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            if (organizationId != null)
            {
                request.AddParameter("application/json", JsonConvert.SerializeObject(new { organizationId }), ParameterType.RequestBody);
            }

            ApiConnection.TryRequest(request, out OrganizationResponse response);
            return response.Organization;
        }

        /// <summary>
        /// Estimate optimization with the specified parameters via QuantConnect.com API
        /// </summary>
        /// <param name="projectId">Project ID of the project the optimization belongs to</param>
        /// <param name="name">Name of the optimization</param>
        /// <param name="target">Target of the optimization, see examples in <see cref="PortfolioStatistics"/></param>
        /// <param name="targetTo">Target extremum of the optimization, for example "max" or "min"</param>
        /// <param name="targetValue">Optimization target value</param>
        /// <param name="strategy">Optimization strategy, <see cref="QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy"/></param>
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
            IReadOnlyList<Constraint> constraints)
        {
            var request = new RestRequest("optimizations/estimate", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name,
                target,
                targetTo,
                targetValue,
                strategy,
                compileId,
                parameters,
                constraints
            }, SerializerSettings), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out EstimateResponseWrapper response);
            return response.Estimate;
        }

        /// <summary>
        /// Create an optimization with the specified parameters via QuantConnect.com API
        /// </summary>
        /// <param name="projectId">Project ID of the project the optimization belongs to</param>
        /// <param name="name">Name of the optimization</param>
        /// <param name="target">Target of the optimization, see examples in <see cref="PortfolioStatistics"/></param>
        /// <param name="targetTo">Target extremum of the optimization, for example "max" or "min"</param>
        /// <param name="targetValue">Optimization target value</param>
        /// <param name="strategy">Optimization strategy, <see cref="QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy"/></param>
        /// <param name="compileId">Optimization compile ID</param>
        /// <param name="parameters">Optimization parameters</param>
        /// <param name="constraints">Optimization constraints</param>
        /// <param name="estimatedCost">Estimated cost for optimization</param>
        /// <param name="nodeType">Optimization node type <see cref="OptimizationNodes"/></param>
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
            int parallelNodes)
        {
            var request = new RestRequest("optimizations/create", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
                name,
                target,
                targetTo,
                targetValue,
                strategy,
                compileId,
                parameters,
                constraints,
                estimatedCost,
                nodeType,
                parallelNodes
            }, SerializerSettings), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out OptimizationList result);
            return result.Optimizations.FirstOrDefault();
        }

        /// <summary>
        /// List all the optimizations for a project
        /// </summary>
        /// <param name="projectId">Project id we'd like to get a list of optimizations for</param>
        /// <returns>A list of BaseOptimization objects, <see cref="BaseOptimization"/></returns>
        public List<OptimizationSummary> ListOptimizations(int projectId)
        {
            var request = new RestRequest("optimizations/list", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                projectId,
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out OptimizationList result);
            return result.Optimizations;
        }

        /// <summary>
        /// Read an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id for the optimization we want to read</param>
        /// <returns><see cref="Optimization"/></returns>
        public Optimization ReadOptimization(string optimizationId)
        {
            var request = new RestRequest("optimizations/read", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                optimizationId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out OptimizationResponseWrapper response);
            return response.Optimization;
        }

        /// <summary>
        /// Abort an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id for the optimization we want to abort</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse AbortOptimization(string optimizationId)
        {
            var request = new RestRequest("optimizations/abort", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                optimizationId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Update an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id we want to update</param>
        /// <param name="name">Name we'd like to assign to the optimization</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse UpdateOptimization(string optimizationId, string name = null)
        {
            var request = new RestRequest("optimizations/update", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            var obj = new JObject
            {
                { "optimizationId", optimizationId }
            };

            if (name.HasValue())
            {
                obj.Add("name", name);
            }

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Delete an optimization
        /// </summary>
        /// <param name="optimizationId">Optimization id for the optimization we want to delete</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse DeleteOptimization(string optimizationId)
        {
            var request = new RestRequest("optimizations/delete", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                optimizationId
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Download the object store files associated with the given organization ID and key
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to get the Object Store files from</param>
        /// <param name="keys">Keys for the Object Store files</param>
        /// <param name="destinationFolder">Folder in which the object store files will be stored</param>
        /// <returns>True if the object store files were retrieved correctly, false otherwise</returns>
        public bool GetObjectStore(string organizationId, List<string> keys, string destinationFolder = null)
        {
            var request = new RestRequest("object/get", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                organizationId,
                keys
            }), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out GetObjectStoreResponse result);

            if (result == null || !result.Success)
            {
                Log.Error($"Api.GetObjectStore(): Failed to get the jobId to request the download URL for the object store files."
                    + (result != null ? $" Errors: {string.Join(",", result.Errors)}" : ""));
                return false;
            }

            var jobId = result.JobId;
            var getUrlRequest = new RestRequest("object/get", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };
            getUrlRequest.AddParameter("application/json", JsonConvert.SerializeObject(new
            {
                organizationId,
                jobId
            }), ParameterType.RequestBody);

            var frontier = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            while (string.IsNullOrEmpty(result?.Url) && (DateTime.UtcNow < frontier))
            {
                Thread.Sleep(3000);
                ApiConnection.TryRequest(getUrlRequest, out result);
            }

            if (result == null || string.IsNullOrEmpty(result.Url))
            {
                Log.Error($"Api.GetObjectStore(): Failed to get the download URL from the jobId {jobId}."
                    + (result != null ? $" Errors: {string.Join(",", result.Errors)}" : ""));
                return false;
            }

            var directory = destinationFolder ?? Directory.GetCurrentDirectory();
            var client = BorrowClient();

            try
            {
                if (client.Value.Timeout != TimeSpan.FromMinutes(20))
                {
                    client.Value.Timeout = TimeSpan.FromMinutes(20);
                }

                // Download the file
                var uri = new Uri(result.Url);
                using var byteArray = client.Value.GetByteArrayAsync(uri);

                Compression.UnzipToFolder(byteArray.Result, directory);
            }
            catch (Exception e)
            {
                Log.Error($"Api.GetObjectStore(): Failed to download zip for path ({directory}). Error: {e.Message}");
                return false;
            }
            finally
            {
                ReturnClient(client);
            }

            return true;
        }

        /// <summary>
        /// Get Object Store properties given the organization ID and the Object Store key
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to get the Object Store from</param>
        /// <param name="key">Key for the Object Store file</param>
        /// <returns><see cref="PropertiesObjectStoreResponse"/></returns>
        /// <remarks>It does not work when the object store is a directory</remarks>
        public PropertiesObjectStoreResponse GetObjectStoreProperties(string organizationId, string key)
        {
            var request = new RestRequest("object/properties", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("organizationId", organizationId);
            request.AddParameter("key", key);

            ApiConnection.TryRequest(request, out PropertiesObjectStoreResponse result);

            if (result == null || !result.Success)
            {
                Log.Error($"Api.ObjectStore(): Failed to get the properties for the object store key {key}." + (result != null ? $" Errors: {string.Join(",", result.Errors)}" : ""));
            }
            return result;
        }

        /// <summary>
        /// Upload files to the Object Store
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to upload the file to</param>
        /// <param name="key">Key to the Object Store file</param>
        /// <param name="objectData">File (as an array of bytes) to be uploaded</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse SetObjectStore(string organizationId, string key, byte[] objectData)
        {
            var request = new RestRequest("object/set", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            request.AddParameter("organizationId", organizationId);
            request.AddParameter("key", key);
            request.AddFileBytes("objectData", objectData, "objectData");
            request.AlwaysMultipartFormData = true;

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Request to delete Object Store metadata of a specific organization and key
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to delete the Object Store file from</param>
        /// <param name="key">Key to the Object Store file</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse DeleteObjectStore(string organizationId, string key)
        {
            var request = new RestRequest("object/delete", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            var obj = new Dictionary<string, object>
            {
                { "organizationId", organizationId },
                { "key", key }
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out RestResponse result);
            return result;
        }

        /// <summary>
        /// Request to list Object Store files of a specific organization and path
        /// </summary>
        /// <param name="organizationId">Organization ID we would like to list the Object Store files from</param>
        /// <param name="path">Path to the Object Store files</param>
        /// <returns><see cref="ListObjectStoreResponse"/></returns>
        public ListObjectStoreResponse ListObjectStore(string organizationId, string path)
        {
            var request = new RestRequest("object/list", Method.POST)
            {
                RequestFormat = DataFormat.Json
            };

            var obj = new Dictionary<string, object>
            {
                { "organizationId", organizationId },
                { "path", path }
            };

            request.AddParameter("application/json", JsonConvert.SerializeObject(obj), ParameterType.RequestBody);

            ApiConnection.TryRequest(request, out ListObjectStoreResponse result);
            return result;
        }

        /// <summary>
        /// Helper method to normalize path for api data requests
        /// </summary>
        /// <param name="filePath">Filepath to format</param>
        /// <param name="dataFolder">The data folder to use</param>
        /// <returns>Normalized path</returns>
        public static string FormatPathForDataRequest(string filePath, string dataFolder = null)
        {
            if (filePath == null)
            {
                Log.Error("Api.FormatPathForDataRequest(): Cannot format null string");
                return null;
            }

            dataFolder ??= Globals.DataFolder;
            // Normalize windows paths to linux format
            dataFolder = dataFolder.Replace("\\", "/", StringComparison.InvariantCulture);
            filePath = filePath.Replace("\\", "/", StringComparison.InvariantCulture);

            // First remove data root directory from path for request if included
            if (filePath.StartsWith(dataFolder, StringComparison.InvariantCulture))
            {
                filePath = filePath.Substring(dataFolder.Length);
            }

            // Trim '/' from start, this can cause issues for _dataFolders without final directory separator in the config
            filePath = filePath.TrimStart('/');
            return filePath;
        }

        /// <summary>
        /// Helper method that will execute the given api request and throw an exception if it fails
        /// </summary>
        private T MakeRequestOrThrow<T>(RestRequest request, string callerName)
            where T : RestResponse
        {
            if (!ApiConnection.TryRequest(request, out T result))
            {
                var errors = string.Empty;
                if (result != null && result.Errors != null && result.Errors.Count > 0)
                {
                    errors = $". Errors: ['{string.Join(",", result.Errors)}']";
                }
                throw new WebException($"{callerName} api request failed{errors}");
            }

            return result;
        }

        /// <summary>
        /// Borrows and HTTP client from the pool
        /// </summary>
        private Lazy<HttpClient> BorrowClient()
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            return _clientPool.Take(cancellationTokenSource.Token);
        }

        /// <summary>
        /// Returns the HTTP client to the pool
        /// </summary>
        private void ReturnClient(Lazy<HttpClient> client)
        {
            _clientPool.Add(client);
        }
    }
}
