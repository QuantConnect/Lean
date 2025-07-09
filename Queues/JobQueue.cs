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

using Fasterflect;
using Newtonsoft.Json;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Python;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Implementation of local/desktop job request:
    /// </summary>
    public class JobQueue : IJobQueueHandler
    {
        // The type name of the QuantConnect.Brokerages.Paper.PaperBrokerage
        private static readonly TextWriter Console = System.Console.Out;

        private const string PaperBrokerageTypeName = "PaperBrokerage";
        private const string DefaultHistoryProvider = "SubscriptionDataReaderHistoryProvider";
        private const string DefaultDataQueueHandler = "LiveDataQueue";
        private const string DefaultDataChannelProvider = "DataChannelProvider";
        private static readonly string Channel = Config.Get("data-channel");
        private readonly string AlgorithmTypeName = Config.Get("algorithm-type-name");
        private Language? _language;

        /// <summary>
        /// This property is protected for testing purposes
        /// </summary>
        protected Language Language
        {
            get
            {
                if (_language == null)
                {
                    string algorithmLanguage = Config.Get("algorithm-language");
                    if (string.IsNullOrEmpty(algorithmLanguage))
                    {
                        var extension = Path.GetExtension(AlgorithmLocation).ToLower();
                        switch (extension)
                        {
                            case ".dll":
                                _language = Language.CSharp;
                                break;
                            case ".py":
                                _language = Language.Python;
                                break;
                            default:
                                throw new ArgumentException($"Unknown extension, algorithm extension was {extension}");
                        }
                    }
                    else
                    {
                        _language = (Language)Enum.Parse(typeof(Language), algorithmLanguage, ignoreCase: true);
                    }
                }

                return (Language)_language;
            }
        }

        /// <summary>
        /// Physical location of Algorithm DLL.
        /// </summary>
        /// <remarks>We expect this dll to be copied into the output directory</remarks>
        private string AlgorithmLocation { get; } = Config.Get("algorithm-location", "QuantConnect.Algorithm.CSharp.dll");

        /// <summary>
        /// Initialize the job queue:
        /// </summary>
        public void Initialize(IApi api)
        {
            api.Initialize(Globals.UserId, Globals.UserToken, Globals.DataFolder);
        }

        /// <summary>
        /// Gets Brokerage Factory for provided IDQH
        /// </summary>
        /// <param name="dataQueueHandler"></param>
        /// <returns>An Instance of Brokerage Factory if possible, otherwise null</returns>
        public static IBrokerageFactory GetFactoryFromDataQueueHandler(string dataQueueHandler)
        {
            IBrokerageFactory brokerageFactory = null;
            var dataQueueHandlerType = Composer.Instance.GetExportedTypes<IBrokerage>()
                .FirstOrDefault(x =>
                    x.FullName != null &&
                    x.FullName.EndsWith(dataQueueHandler, StringComparison.InvariantCultureIgnoreCase) &&
                    x.HasAttribute(typeof(BrokerageFactoryAttribute)));

            if (dataQueueHandlerType != null)
            {
                var attribute = dataQueueHandlerType.GetCustomAttribute<BrokerageFactoryAttribute>();
                brokerageFactory = (BrokerageFactory)Activator.CreateInstance(attribute.Type);
            }
            return brokerageFactory;
        }

        /// <summary>
        /// Desktop/Local Get Next Task - Get task from the Algorithm folder of VS Solution.
        /// </summary>
        /// <returns></returns>
        public AlgorithmNodePacket NextJob(out string algorithmPath)
        {
            algorithmPath = GetAlgorithmLocation();

            Log.Trace($"JobQueue.NextJob(): Selected {algorithmPath}");

            // check for parameters in the config
            var parameters = new Dictionary<string, string>();

            var parametersConfigString = Config.Get("parameters");
            if (!string.IsNullOrEmpty(parametersConfigString))
            {
                parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(parametersConfigString);
            }

            var controls = new Controls()
            {
                MinuteLimit = Config.GetInt("symbol-minute-limit", 10000),
                SecondLimit = Config.GetInt("symbol-second-limit", 10000),
                TickLimit = Config.GetInt("symbol-tick-limit", 10000),
                RamAllocation = int.MaxValue,
                MaximumDataPointsPerChartSeries = Config.GetInt("maximum-data-points-per-chart-series", 1000000),
                MaximumChartSeries = Config.GetInt("maximum-chart-series", 30),
                StorageLimit = Config.GetValue("storage-limit", 10737418240L),
                StorageFileCount = Config.GetInt("storage-file-count", 10000),
                StorageAccess = Config.GetValue("storage-permissions", new Packets.StoragePermissions())
            };

            var algorithmId = Config.Get("algorithm-id", AlgorithmTypeName);

            //If this isn't a backtesting mode/request, attempt a live job.
            if (Globals.LiveMode)
            {
                var dataHandlers = Config.Get("data-queue-handler", DefaultDataQueueHandler);
                var liveJob = new LiveNodePacket
                {
                    Type = PacketType.LiveNode,
                    Algorithm = File.ReadAllBytes(AlgorithmLocation),
                    Brokerage = Config.Get("live-mode-brokerage", PaperBrokerageTypeName),
                    HistoryProvider = Config.Get("history-provider", DefaultHistoryProvider),
                    DataQueueHandler = dataHandlers,
                    DataChannelProvider = Config.Get("data-channel-provider", DefaultDataChannelProvider),
                    Channel = Channel,
                    UserToken = Globals.UserToken,
                    UserId = Globals.UserId,
                    ProjectId = Globals.ProjectId,
                    OrganizationId = Globals.OrganizationID,
                    Version = Globals.Version,
                    DeployId = algorithmId,
                    Parameters = parameters,
                    Language = Language,
                    Controls = controls,
                    PythonVirtualEnvironment = Config.Get("python-venv"),
                    DeploymentTarget = DeploymentTarget.LocalPlatform,
                };

                Type brokerageName = null;
                try
                {
                    // import the brokerage data for the configured brokerage
                    var brokerageFactory = Composer.Instance.Single<IBrokerageFactory>(factory => factory.BrokerageType.MatchesTypeName(liveJob.Brokerage));
                    brokerageName = brokerageFactory.BrokerageType;
                    liveJob.BrokerageData = brokerageFactory.BrokerageData;
                }
                catch (Exception err)
                {
                    Log.Error(err, $"Error resolving BrokerageData for live job for brokerage {liveJob.Brokerage}");
                }

                var brokerageBasedHistoryProvider = liveJob.HistoryProvider.DeserializeList().Select(x =>
                {
                    HistoryExtensions.TryGetBrokerageName(x, out var brokerageName);
                    return brokerageName;
                }).Where(x => x != null);

                foreach (var dataHandlerName in dataHandlers.DeserializeList().Concat(brokerageBasedHistoryProvider).Distinct())
                {
                    var brokerageFactoryForDataHandler = GetFactoryFromDataQueueHandler(dataHandlerName);
                    if (brokerageFactoryForDataHandler == null)
                    {
                        Log.Trace($"JobQueue.NextJob(): Not able to fetch brokerage factory with name: {dataHandlerName}");
                        continue;
                    }
                    if (brokerageFactoryForDataHandler.BrokerageType == brokerageName)
                    {
                        //Don't need to add brokerageData again if added by brokerage
                        continue;
                    }
                    foreach (var data in brokerageFactoryForDataHandler.BrokerageData)
                    {
                        if (data.Key == "live-holdings" || data.Key == "live-cash-balance")
                        {
                            //live holdings & cash balance not required for data handler
                            continue;
                        }

                        liveJob.BrokerageData.TryAdd(data.Key, data.Value);
                    }
                }
                return liveJob;
            }

            var optimizationId = Config.Get("optimization-id");
            //Default run a backtesting job.
            var backtestJob = new BacktestNodePacket(0, 0, "", new byte[] { }, Config.Get("backtest-name", "local"))
            {
                Type = PacketType.BacktestNode,
                Algorithm = File.ReadAllBytes(AlgorithmLocation),
                HistoryProvider = Config.Get("history-provider", DefaultHistoryProvider),
                Channel = Channel,
                UserToken = Globals.UserToken,
                UserId = Globals.UserId,
                ProjectId = Globals.ProjectId,
                OrganizationId = Globals.OrganizationID,
                Version = Globals.Version,
                BacktestId = algorithmId,
                Language = Language,
                Parameters = parameters,
                Controls = controls,
                PythonVirtualEnvironment = Config.Get("python-venv"),
                DeploymentTarget = DeploymentTarget.LocalPlatform,
            };

            var outOfSampleMaxEndDate = Config.Get("out-of-sample-max-end-date");
            if (!string.IsNullOrEmpty(outOfSampleMaxEndDate))
            {
                backtestJob.OutOfSampleMaxEndDate = Time.ParseDate(outOfSampleMaxEndDate);
            }
            backtestJob.OutOfSampleDays = Config.GetInt("out-of-sample-days");

            // Only set optimization id when backtest is for optimization
            if (!optimizationId.IsNullOrEmpty())
            {
                backtestJob.OptimizationId = optimizationId;
            }

            return backtestJob;
        }

        /// <summary>
        /// Get the algorithm location for client side backtests.
        /// </summary>
        /// <returns></returns>
        private string GetAlgorithmLocation()
        {
            if (Language == Language.Python)
            {
                if (!File.Exists(AlgorithmLocation))
                {
                    throw new FileNotFoundException($"JobQueue.TryCreatePythonAlgorithm(): Unable to find py file: {AlgorithmLocation}");
                }

                // Add this directory to our Python Path so it may be imported properly
                var pythonFile = new FileInfo(AlgorithmLocation);
                PythonInitializer.AddAlgorithmLocationPath(pythonFile.Directory.FullName);
            }

            return AlgorithmLocation;
        }

        /// <summary>
        /// Desktop/Local acknowledge the task processed. Nothing to do.
        /// </summary>
        /// <param name="job"></param>
        public void AcknowledgeJob(AlgorithmNodePacket job)
        {
            // Make the console window pause so we can read log output before exiting and killing the application completely
            Console.WriteLine("Engine.Main(): Analysis Complete.");
            // closing automatically is useful for optimization, we don't want to leave open all the ended lean instances
            if (!Config.GetBool("close-automatically"))
            {
                Console.WriteLine("Engine.Main(): Press any key to continue.");
                System.Console.Read();
            }
        }
    }
}
