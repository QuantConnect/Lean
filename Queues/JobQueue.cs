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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

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
        private bool _liveMode = Config.GetBool("live-mode");
        private static readonly string AccessToken = Config.Get("api-access-token");
        private static readonly int UserId = Config.GetInt("job-user-id", 0);
        private static readonly int ProjectId = Config.GetInt("job-project-id", 0);
        private readonly string AlgorithmTypeName = Config.Get("algorithm-type-name");
        private readonly Language Language = (Language)Enum.Parse(typeof(Language), Config.Get("algorithm-language"));

        /// <summary>
        /// Physical location of Algorithm DLL.
        /// </summary>
        private string AlgorithmLocation
        {
            get
            {
                // we expect this dll to be copied into the output directory
                return Config.Get("algorithm-location", "QuantConnect.Algorithm.CSharp.dll");
            }
        }

        /// <summary>
        /// Initialize the job queue:
        /// </summary>
        public void Initialize(IApi api)
        {
            //
        }

        /// <summary>
        /// Desktop/Local Get Next Task - Get task from the Algorithm folder of VS Solution.
        /// </summary>
        /// <returns></returns>
        public AlgorithmNodePacket NextJob(out string location)
        {
            location = GetAlgorithmLocation();

            Log.Trace($"JobQueue.NextJob(): Selected {location}");

            // check for parameters in the config
            var parameters = new Dictionary<string, string>();

            var parametersConfigString = Config.Get("parameters");
            if (parametersConfigString != string.Empty)
            {
                parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(parametersConfigString);
            }

            var controls = new Controls()
            {
                MinuteLimit = Config.GetInt("symbol-minute-limit", 10000),
                SecondLimit = Config.GetInt("symbol-second-limit", 10000),
                TickLimit = Config.GetInt("symbol-tick-limit", 10000),
                RamAllocation = int.MaxValue,
                MaximumDataPointsPerChartSeries =  Config.GetInt("maximum-data-points-per-chart-series", 4000)
            };

            if ((Language)Enum.Parse(typeof(Language), Config.Get("algorithm-language")) == Language.Python)
            {
                // Set the python path for loading python algorithms.
                var pythonFile = new FileInfo(location);
                var pythonPath = new List<string>
                {
                    pythonFile.Directory.FullName,
                    new DirectoryInfo(Environment.CurrentDirectory).FullName,
                };
                // Don't include an empty environment variable in pythonPath, otherwise the PYTHONPATH
                // environment variable won't be used in the module import process
                var pythonPathEnvironmentVariable = Environment.GetEnvironmentVariable("PYTHONPATH");
                if (!string.IsNullOrEmpty(pythonPathEnvironmentVariable))
                {
                    pythonPath.Add(pythonPathEnvironmentVariable);
                }
                Environment.SetEnvironmentVariable("PYTHONPATH", string.Join(OS.IsLinux ? ":" : ";", pythonPath));
            }

            //If this isn't a backtesting mode/request, attempt a live job.
            if (_liveMode)
            {
                var liveJob = new LiveNodePacket
                {
                    Type = PacketType.LiveNode,
                    Algorithm = File.ReadAllBytes(AlgorithmLocation),
                    Brokerage = Config.Get("live-mode-brokerage", PaperBrokerageTypeName),
                    HistoryProvider = Config.Get("history-provider", DefaultHistoryProvider),
                    DataQueueHandler = Config.Get("data-queue-handler", DefaultDataQueueHandler),
                    DataChannelProvider = Config.Get("data-channel-provider", DefaultDataChannelProvider),
                    Channel = AccessToken,
                    UserToken = AccessToken,
                    UserId = UserId,
                    ProjectId = ProjectId,
                    Version = Globals.Version,
                    DeployId = AlgorithmTypeName,
                    Parameters = parameters,
                    Language = Language,
                    Controls = controls
                };

                try
                {
                    // import the brokerage data for the configured brokerage
                    var brokerageFactory = Composer.Instance.Single<IBrokerageFactory>(factory => factory.BrokerageType.MatchesTypeName(liveJob.Brokerage));
                    liveJob.BrokerageData = brokerageFactory.BrokerageData;
                }
                catch (Exception err)
                {
                    Log.Error(err, $"Error resolving BrokerageData for live job for brokerage {liveJob.Brokerage}");
                }

                return liveJob;
            }

            //Default run a backtesting job.
            var backtestJob = new BacktestNodePacket(0, 0, "", new byte[] {}, 10000, "local")
            {
                Type = PacketType.BacktestNode,
                Algorithm = File.ReadAllBytes(AlgorithmLocation),
                HistoryProvider = Config.Get("history-provider", DefaultHistoryProvider),
                Channel = AccessToken,
                UserToken = AccessToken,
                UserId = UserId,
                ProjectId = ProjectId,
                Version = Globals.Version,
                BacktestId = AlgorithmTypeName,
                Language = Language,
                Parameters = parameters,
                Controls = controls
            };

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
                var pythonSource = AlgorithmTypeName + ".py";
                if (!File.Exists(pythonSource))
                {
                    // Copies file to execution location
                    foreach (var file in new DirectoryInfo(Path.GetDirectoryName(AlgorithmLocation)).GetFiles("*.py"))
                    {
                        file.CopyTo(file.FullName.Replace(file.DirectoryName, Environment.CurrentDirectory), true);
                    }

                    if (!File.Exists(pythonSource))
                    {
                        throw new FileNotFoundException($"JobQueue.TryCreatePythonAlgorithm(): Unable to find py file: {pythonSource}");
                    }
                }
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
            Console.WriteLine("Engine.Main(): Analysis Complete. Press any key to continue.");
            System.Console.Read();
        }
    }

}
