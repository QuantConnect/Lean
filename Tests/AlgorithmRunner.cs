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
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.Storage;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Provides methods for running an algorithm and testing it's performance metrics
    /// </summary>
    public static class AlgorithmRunner
    {
        public static AlgorithmRunnerResults RunLocalBacktest(
            string algorithm,
            Dictionary<string, string> expectedStatistics,
            Language language,
            AlgorithmStatus expectedFinalStatus,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string setupHandler = "RegressionSetupHandlerWrapper",
            decimal? initialCash = null,
            string algorithmLocation = null,
            bool returnLogs = false)
        {
            AlgorithmManager algorithmManager = null;
            var statistics = new Dictionary<string, string>();
            BacktestingResultHandler results = null;

            Composer.Instance.Reset();
            SymbolCache.Clear();
            TextSubscriptionDataSourceReader.ClearCache();
            MarketOnCloseOrder.SubmissionTimeBuffer = MarketOnCloseOrder.DefaultSubmissionTimeBuffer;

            // clean up object storage
            var objectStorePath = LocalObjectStore.DefaultObjectStore;
            if (Directory.Exists(objectStorePath))
            {
                Directory.Delete(objectStorePath, true);
            }

            var ordersLogFile = string.Empty;
            var logFile = $"./regression/{algorithm}.{language.ToLower()}.log";
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            File.Delete(logFile);
            var logs = new List<string>();

            var reducedDiskSize = TestContext.Parameters.Exists("reduced-disk-size") &&
                bool.Parse(TestContext.Parameters["reduced-disk-size"]);

            try
            {
                // set the configuration up
                Config.Set("algorithm-type-name", algorithm);
                Config.Set("live-mode", "false");
                Config.Set("environment", "");
                Config.Set("messaging-handler", "QuantConnect.Tests.RegressionTestMessageHandler");
                Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
                Config.Set("setup-handler", setupHandler);
                Config.Set("history-provider", "RegressionHistoryProviderWrapper");
                Config.Set("api-handler", "QuantConnect.Api.Api");
                Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.RegressionResultHandler");
                Config.Set("fundamental-data-provider", "QuantConnect.Tests.Common.Data.Fundamental.TestFundamentalDataProvider");
                Config.Set("algorithm-language", language.ToString());
                if (string.IsNullOrEmpty(algorithmLocation))
                {
                    Config.Set("algorithm-location",
                        language == Language.Python
                            ? "../../../Algorithm.Python/" + algorithm + ".py"
                            : "QuantConnect.Algorithm." + language + ".dll");
                }
                else
                {
                    Config.Set("algorithm-location", algorithmLocation);
                }

                // Store initial log variables
                var initialLogHandler = Log.LogHandler;
                var initialDebugEnabled = Log.DebuggingEnabled;

                var newLogHandlers = new List<ILogHandler>() { MaintainLogHandlerAttribute.LogHandler };
                // Use our current test LogHandler and a FileLogHandler
                if (!reducedDiskSize)
                {
                    newLogHandlers.Add(new FileLogHandler(logFile, false));
                }
                if (returnLogs)
                {
                    var storeLog = (string logMessage) => logs.Add(logMessage);
                    newLogHandlers.Add(new FunctionalLogHandler(storeLog, storeLog, storeLog));
                }

                using (Log.LogHandler = new CompositeLogHandler(newLogHandlers.ToArray()))
                using (var algorithmHandlers = Initializer.GetAlgorithmHandlers())
                using (var systemHandlers = Initializer.GetSystemHandlers())
                using (var workerThread  = new TestWorkerThread())
                {
                    Log.DebuggingEnabled = !reducedDiskSize;

                    Log.Trace("");
                    Log.Trace("{0}: Running " + algorithm + "...", DateTime.UtcNow);
                    Log.Trace("");

                    // run the algorithm in its own thread
                    var engine = new Lean.Engine.Engine(systemHandlers, algorithmHandlers, false);
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            string algorithmPath;
                            var job = (BacktestNodePacket)systemHandlers.JobQueue.NextJob(out algorithmPath);
                            job.BacktestId = algorithm;
                            job.PeriodStart = startDate;
                            job.PeriodFinish = endDate;
                            if (initialCash.HasValue)
                            {
                                job.CashAmount = new CashAmount(initialCash.Value, Currencies.USD);
                            }
                            algorithmManager = new AlgorithmManager(false, job);

                            var regressionTestMessageHandler = systemHandlers.Notify as RegressionTestMessageHandler;
                            if (regressionTestMessageHandler != null)
                            {
                                regressionTestMessageHandler.SetAlgorithmManager(algorithmManager);
                            }

                            systemHandlers.LeanManager.Initialize(systemHandlers, algorithmHandlers, job, algorithmManager);

                            engine.Run(job, algorithmManager, algorithmPath, workerThread);
                            ordersLogFile = ((RegressionResultHandler)algorithmHandlers.Results).LogFilePath;
                        }
                        catch (Exception e)
                        {
                            Log.Trace($"Error in AlgorithmRunner task: {e}");
                        }
                    }).Wait();

                    var regressionResultHandler = (RegressionResultHandler)algorithmHandlers.Results;
                    results = regressionResultHandler;
                    statistics = regressionResultHandler.FinalStatistics;

                    if (expectedFinalStatus == AlgorithmStatus.Completed && regressionResultHandler.HasRuntimeError)
                    {
                        Assert.Fail($"There was a runtime error running the algorithm");
                    }
                }

                // Reset settings to initial values
                Log.LogHandler = initialLogHandler;
                Log.DebuggingEnabled = initialDebugEnabled;
            }
            catch (Exception ex)
            {
                if (expectedFinalStatus != AlgorithmStatus.RuntimeError)
                {
                    Log.Error("{0} {1}", ex.Message, ex.StackTrace);
                }
            }

            if (algorithmManager?.State != expectedFinalStatus)
            {
                Assert.Fail($"Algorithm state should be {expectedFinalStatus} and is: {algorithmManager?.State}");
            }

            foreach (var expectedStat in expectedStatistics)
            {
                string result;
                Assert.IsTrue(statistics.TryGetValue(expectedStat.Key, out result), "Missing key: " + expectedStat.Key);

                // normalize -0 & 0, they are the same thing
                var expected = expectedStat.Value;
                if (expected == "-0")
                {
                    expected = "0";
                }

                if (result == "-0")
                {
                    result = "0";
                }

                Assert.AreEqual(expected, result, "Failed on " + expectedStat.Key);
            }

            if (!reducedDiskSize)
            {
                // we successfully passed the regression test, copy the log file so we don't have to continually
                // re-run master in order to compare against a passing run
                var passedFile = logFile.Replace("./regression/", "./passed/");
                Directory.CreateDirectory(Path.GetDirectoryName(passedFile));
                File.Delete(passedFile);
                File.Copy(logFile, passedFile);

                var passedOrderLogFile = ordersLogFile.Replace("./regression/", "./passed/");
                Directory.CreateDirectory(Path.GetDirectoryName(passedFile));
                File.Delete(passedOrderLogFile);
                if (File.Exists(ordersLogFile)) File.Copy(ordersLogFile, passedOrderLogFile);

            }
            return new AlgorithmRunnerResults(algorithm, language, algorithmManager, results, logs);
        }

        /// <summary>
        /// Used to intercept the algorithm instance to aid the <see cref="RegressionHistoryProviderWrapper"/>
        /// </summary>
        public class RegressionSetupHandlerWrapper : BacktestingSetupHandler
        {
            public static IAlgorithm Algorithm { get; protected set; }
            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = base.CreateAlgorithmInstance(algorithmNodePacket, assemblyPath);
                var framework = Algorithm as QCAlgorithm;
                if (framework != null)
                {
                    framework.DebugMode = true;
                }
                return Algorithm;
            }
        }

        /// <summary>
        /// Used to perform checks against history requests for all regression algorithms
        /// </summary>
        public class RegressionHistoryProviderWrapper : SubscriptionDataReaderHistoryProvider
        {
            public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
            {
                requests = requests.ToList();
                if (requests.Any(r => r.Symbol.SecurityType != SecurityType.Future && r.Symbol.IsCanonical()))
                {
                    throw new Exception($"Invalid history reuqest symbols: {string.Join(",", requests.Select(x => x.Symbol))}");
                }
                return base.GetHistory(requests, sliceTimeZone);
            }
        }

        public class TestWorkerThread : WorkerThread
        {
        }
    }
}
