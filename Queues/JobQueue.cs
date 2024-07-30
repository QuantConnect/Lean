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
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Python;
using QuantConnect.Packets;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Queues
{
    /// <summary>
    /// Implementation of local/desktop job request:
    /// </summary>
    public class JobQueue : IJobQueueHandler
    {
        // The type name of the QuantConnect.Brokerages.Paper.PaperBrokerage
        private static readonly TextWriter Console = System.Console.Out;
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

            var algorithmId = Config.Get("algorithm-id", AlgorithmTypeName);

            //If this isn't a backtesting mode/request, attempt a live job.
            if (Globals.LiveMode)
            {
                return JobQueueExtensions.GetLiveNodeConfigurationWithAlgorithmConfiguration(AlgorithmLocation, algorithmId, parameters, Language);
            }

            var optimizationId = Config.Get("optimization-id");
            //Default run a backtesting job.
            var backtestJob = JobQueueExtensions.GetBacktestNodePacketConfiguration(AlgorithmLocation, algorithmId, parameters, Language);

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
