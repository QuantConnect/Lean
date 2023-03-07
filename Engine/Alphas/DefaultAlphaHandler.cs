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
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Alpha;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Alphas
{
    /// <summary>
    /// Default alpha handler that supports sending insights to the messaging handler, analyzing insights online
    /// </summary>
    public class DefaultAlphaHandler : IAlphaHandler
    {
        private static int _storePeriodMs = Convert.ToInt32(TimeSpan.FromMinutes(10).TotalMilliseconds);
        private DateTime _lastStepTime;
        private Timer _storeTimer;
        private readonly object _lock = new object();
        private string _alphaResultsPath;

        /// <summary>
        /// Gets a flag indicating if this handler's thread is still running and processing messages
        /// </summary>
        public virtual bool IsActive { get; private set; }

        /// <summary>
        /// Gets the algorithm's unique identifier
        /// </summary>
        protected virtual string AlgorithmId => Job.AlgorithmId;

        /// <summary>
        /// Gets whether or not the job is a live job
        /// </summary>
        protected bool LiveMode => Job is LiveNodePacket;

        /// <summary>
        /// Gets the algorithm job packet
        /// </summary>
        protected AlgorithmNodePacket Job { get; private set; }

        /// <summary>
        /// Gets the algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm { get; private set; }

        /// <summary>
        /// Gets the confgured messaging handler for sending packets
        /// </summary>
        protected IMessagingHandler MessagingHandler { get; private set; }

        /// <summary>
        /// Gets the insight manager instance used to manage the analysis of algorithm insights
        /// </summary>
        protected virtual InsightManager InsightManager { get; private set; }

        /// <summary>
        /// Initializes this alpha handler to accept insights from the specified algorithm
        /// </summary>
        /// <param name="job">The algorithm job</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="messagingHandler">Handler used for sending insights</param>
        /// <param name="api">Api instance</param>
        /// <param name="transactionHandler">Algorithms transaction handler</param>
        public virtual void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler)
        {
            // initializing these properties just in case, doesn't hurt to have them populated
            Job = job;
            Algorithm = algorithm;
            MessagingHandler = messagingHandler;

            InsightManager = new InsightManager();
            Algorithm.SetInsightManager(InsightManager);

            var baseDirectory = Config.Get("results-destination-folder", Directory.GetCurrentDirectory());
            var directory = Path.Combine(baseDirectory, AlgorithmId);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _alphaResultsPath = Path.Combine(directory, "alpha-results.json");
            
            // when insight is generated, take snapshot of securities and place in queue for insight manager to process on alpha thread
            algorithm.InsightsGenerated += (algo, collection) =>
            {
                InsightManager.AddInsights(collection.Insights);
            };
        }

        /// <summary>
        /// Invoked after the algorithm's Initialize method was called allowing the alpha handler to check
        /// other things, such as sampling period for backtests
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public void OnAfterAlgorithmInitialized(IAlgorithm algorithm)
        {
            if (LiveMode)
            {
                _storeTimer = new Timer(_ => StoreInsights(),
                    null,
                    _storePeriodMs,
                    Timeout.Infinite);
            }
            IsActive = true;
        }

        /// <summary>
        /// Performs processing in sync with the algorithm's time loop to provide consisten reading of data
        /// </summary>
        public virtual void ProcessSynchronousEvents()
        {
            // check the last snap shot time, we may have already produced a snapshot via OnInsightsGenerated
            if (_lastStepTime != Algorithm.UtcTime)
            {
                // does nothing by default
                Algorithm.InsightEvaluator.Score(Algorithm.InsightManager, Algorithm.UtcTime);

                InsightManager.Step(Algorithm.UtcTime);

                _lastStepTime = Algorithm.UtcTime;
            }
        }

        /// <summary>
        /// Stops processing and stores insights
        /// </summary>
        public void Exit()
        {
            Log.Trace("DefaultAlphaHandler.Exit(): Exiting...");

            _storeTimer.DisposeSafely();
            _storeTimer = null;

            // persist insights at exit
            StoreInsights();

            IsActive = false;

            Log.Trace("DefaultAlphaHandler.Exit(): Ended");
        }

        /// <summary>
        /// Save insight results to persistent storage
        /// </summary>
        /// <remarks>Method called by the storing timer and on exit</remarks>
        protected virtual void StoreInsights()
        {
            try
            {
                // avoid reentrancy
                if (Monitor.TryEnter(_lock))
                {
                    try
                    {
                        if (InsightManager == null)
                        {
                            // could be null if we are not initialized and exit is called
                            return;
                        }
                        // default save all results to disk and don't remove any from memory
                        // this will result in one file with all of the insights/results in it
                        var insights = InsightManager.GetInsights().OrderBy(insight => insight.GeneratedTimeUtc).ToList();
                        if (insights.Count > 0)
                        {
                            var directory = Directory.GetParent(_alphaResultsPath);
                            if (!directory.Exists)
                            {
                                directory.Create();
                            }
                            File.WriteAllText(_alphaResultsPath, JsonConvert.SerializeObject(insights, Formatting.Indented));
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_lock);
                    }
                }
            }
            finally
            {
                try
                {
                    // restart timer following end of persistence
                    _storeTimer?.Change(Time.GetSecondUnevenWait(_storePeriodMs), Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // ignored disposed
                }
            }
        }
    }
}
