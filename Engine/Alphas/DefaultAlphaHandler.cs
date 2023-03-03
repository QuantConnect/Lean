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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Alpha;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Statistics;
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
        private List<Insight> _insights;
        private ISecurityValuesProvider _securityValuesProvider;
        private FitnessScoreManager _fitnessScore;
        private DateTime _lastFitnessScoreCalculation;
        private Timer _storeTimer;
        private readonly object _lock = new object();
        private string _alphaResultsPath;

        /// <summary>
        /// Gets a flag indicating if this handler's thread is still running and processing messages
        /// </summary>
        public virtual bool IsActive { get; private set; }

        /// <summary>
        /// Gets the current alpha runtime statistics
        /// </summary>
        public AlphaRuntimeStatistics RuntimeStatistics { get; private set; }

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
        protected virtual IInsightManager InsightManager { get; private set; }

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

            _fitnessScore = new FitnessScoreManager();
            _insights = new List<Insight>();
            _securityValuesProvider = new AlgorithmSecurityValuesProvider(algorithm);

            InsightManager = CreateInsightManager();

            var statistics = new StatisticsInsightManagerExtension();
            RuntimeStatistics = statistics.Statistics;
            InsightManager.AddExtension(statistics);

            AddInsightManagerCustomExtensions(statistics);

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
                lock (_insights)
                {
                    _insights.AddRange(collection.Insights);
                }
            };
        }

        /// <summary>
        /// Allows each alpha handler implementation to add there own optional extensions
        /// </summary>
        protected virtual void AddInsightManagerCustomExtensions(StatisticsInsightManagerExtension statistics)
        {
            // send scored insights to messaging handler
            InsightManager.AddExtension(new AlphaResultPacketSender(Job, MessagingHandler, TimeSpan.FromSeconds(3), 50));
            InsightManager.AddExtension(new ChartingInsightManagerExtension(Algorithm, statistics));
        }

        /// <summary>
        /// Invoked after the algorithm's Initialize method was called allowing the alpha handler to check
        /// other things, such as sampling period for backtests
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public void OnAfterAlgorithmInitialized(IAlgorithm algorithm)
        {
            _fitnessScore.Initialize(algorithm);
            // send date ranges to extensions for initialization -- this data wasn't available when the handler was
            // initialzied, so we need to invoke it here
            InsightManager.InitializeExtensionsForRange(algorithm.StartDate, algorithm.EndDate, algorithm.UtcTime);

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
                _lastStepTime = Algorithm.UtcTime;
                lock (_insights)
                {
                    InsightManager.Step(_lastStepTime,
                        _securityValuesProvider.GetAllValues(),
                        new GeneratedInsightsCollection(_lastStepTime, _insights.Count == 0 ? Enumerable.Empty<Insight>() : _insights, clone: false));
                    _insights.Clear();
                }
            }

            if (_lastFitnessScoreCalculation.Date != Algorithm.UtcTime.Date)
            {
                _lastFitnessScoreCalculation = Algorithm.UtcTime.Date;
                _fitnessScore.UpdateScores();

                RuntimeStatistics.PortfolioTurnover = _fitnessScore.PortfolioTurnover;
                RuntimeStatistics.ReturnOverMaxDrawdown = _fitnessScore.ReturnOverMaxDrawdown;
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

            InsightManager?.DisposeSafely();

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
                        var insights = InsightManager.AllInsights.OrderBy(insight => insight.GeneratedTimeUtc).ToList();
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

        /// <summary>
        /// Creates the <see cref="InsightManager"/> to manage the analysis of generated insights
        /// </summary>
        /// <returns>A new insight manager instance</returns>
        protected virtual IInsightManager CreateInsightManager()
        {
            var scoreFunctionProvider = new DefaultInsightScoreFunctionProvider();
            return new InsightManager(scoreFunctionProvider, 0);
        }

        /// <summary>
        /// Encapsulates routing finalized insights to the messaging handler
        /// </summary>
        protected class AlphaResultPacketSender : IInsightManagerExtension, IDisposable
        {
            private readonly Timer _timer;
            private readonly TimeSpan _interval;
            private readonly int _maximumQueueLength;
            private readonly AlgorithmNodePacket _job;
            private readonly ConcurrentQueue<Insight> _insights;
            private readonly IMessagingHandler _messagingHandler;
            private readonly int _maximumNumberOfInsightsPerPacket;

            /// <summary>
            /// Initialize a new instance of <see cref="AlphaResultPacketSender"/>
            /// </summary>
            /// <param name="job">Algorithm job</param>
            /// <param name="messagingHandler">Message handler to use</param>
            /// <param name="interval">Timespan interval</param>
            /// <param name="maximumNumberOfInsightsPerPacket">Limit on insights per packet</param>
            public AlphaResultPacketSender(AlgorithmNodePacket job, IMessagingHandler messagingHandler, TimeSpan interval, int maximumNumberOfInsightsPerPacket)
            {
                _job = job;
                _interval = interval;
                _messagingHandler = messagingHandler;
                _insights = new ConcurrentQueue<Insight>();
                _maximumNumberOfInsightsPerPacket = maximumNumberOfInsightsPerPacket;

                _timer = new Timer(MessagingUpdateIntervalElapsed);
                _timer.Change(interval, Timeout.InfiniteTimeSpan);

                // don't bother holding on more than makes sense. this makes the maximum
                // number of insights we'll hold in the queue equal to one hour's worth of
                // processing. For 50 insights/message @ 1message/sec this is 90K
                _maximumQueueLength = (int) (TimeSpan.FromMinutes(30).Ticks / interval.Ticks * maximumNumberOfInsightsPerPacket);
            }

            private void MessagingUpdateIntervalElapsed(object state)
            {
                try
                {
                    Insight insight;
                    var insights = new List<Insight>();
                    while (insights.Count < _maximumNumberOfInsightsPerPacket && _insights.TryDequeue(out insight))
                    {
                        insights.Add(insight);
                    }

                    if (insights.Count > 0)
                    {
                        _messagingHandler.Send(new AlphaResultPacket(_job.AlgorithmId, _job.UserId, insights));
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
                finally
                {
                    try
                    {
                        var nextDueTime = Time.GetSecondUnevenWait((int)Math.Ceiling(_interval.TotalMilliseconds));
                        _timer.Change(nextDueTime, Timeout.Infinite);
                    }
                    catch (ObjectDisposedException)
                    {
                        // ignored disposed
                    }
                }
            }

            /// <summary>
            /// Enqueue finalized insights to be sent via the messaging handler
            /// </summary>
            public void OnInsightAnalysisCompleted(InsightAnalysisContext context)
            {
                if (_insights.Count < _maximumQueueLength)
                {
                    _insights.Enqueue(context.Insight);
                }
            }

            /// <summary>
            /// No operation
            /// </summary>
            /// <param name="frontierTimeUtc"></param>
            public void Step(DateTime frontierTimeUtc)
            {
                //NOP
            }

            /// <summary>
            /// No operation
            /// </summary>
            /// <param name="algorithmStartDate"></param>
            /// <param name="algorithmEndDate"></param>
            /// <param name="algorithmUtcTime"></param>
            public void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime)
            {
                //NOP
            }

            /// <summary>
            /// No operation
            /// </summary>
            /// <param name="context"></param>
            public void OnInsightGenerated(InsightAnalysisContext context)
            {
                //NOP
            }

            /// <summary>
            /// No operation
            /// </summary>
            /// <param name="context"></param>
            public void OnInsightClosed(InsightAnalysisContext context)
            {
                //NOP
            }

            /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
                _timer?.DisposeSafely();
            }
        }
    }
}
