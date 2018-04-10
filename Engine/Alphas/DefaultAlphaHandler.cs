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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Alpha;
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
        private DateTime _lastSecurityValuesSnapshotTime;

        private bool _isNotFrameworkAlgorithm;
        private ChartingInsightManagerExtension _charting;
        private ISecurityValuesProvider _securityValuesProvider;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Gets a flag indicating if this handler's thread is still running and processing messages
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets the current alpha runtime statistics
        /// </summary>
        public AlphaRuntimeStatistics RuntimeStatistics { get; private set; }

        /// <summary>
        /// Gets the algorithm's unique identifier
        /// </summary>
        protected string AlgorithmId => Job.AlgorithmId;

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
        protected InsightManager InsightManager { get; private set; }

        /// <summary>
        /// Initializes this alpha handler to accept insights from the specified algorithm
        /// </summary>
        /// <param name="job">The algorithm job</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="messagingHandler">Handler used for sending insights</param>
        /// <param name="api">Api instance</param>
        public virtual void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler, IApi api)
        {
            // initializing these properties just in case, doens't hurt to have them populated
            Job = job;
            Algorithm = algorithm;
            MessagingHandler = messagingHandler;
            _isNotFrameworkAlgorithm = !algorithm.IsFrameworkAlgorithm;
            if (_isNotFrameworkAlgorithm)
            {
                return;
            }

            _securityValuesProvider = new AlgorithmSecurityValuesProvider(algorithm);

            InsightManager = CreateInsightManager();

            // send scored insights to messaging handler
            InsightManager.AddExtension(CreateAlphaResultPacketSender());

            var statistics = new StatisticsInsightManagerExtension();
            RuntimeStatistics = statistics.Statistics;
            InsightManager.AddExtension(statistics);
            _charting = new ChartingInsightManagerExtension(algorithm, statistics);
            InsightManager.AddExtension(_charting);

            // when insight is generated, take snapshot of securities and place in queue for insight manager to process on alpha thread
            algorithm.InsightsGenerated += (algo, collection) => InsightManager.Step(collection.DateTimeUtc, CreateSecurityValuesSnapshot(), collection);
        }

        /// <summary>
        /// Invoked after the algorithm's Initialize method was called allowing the alpha handler to check
        /// other things, such as sampling period for backtests
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public void OnAfterAlgorithmInitialized(IAlgorithm algorithm)
        {
            if (_isNotFrameworkAlgorithm)
            {
                return;
            }

            // send date ranges to extensions for initialization -- this data wasn't available when the handler was
            // initialzied, so we need to invoke it here
            InsightManager.InitializeExtensionsForRange(algorithm.StartDate, algorithm.EndDate, algorithm.UtcTime);
        }

        /// <summary>
        /// Performs processing in sync with the algorithm's time loop to provide consisten reading of data
        /// </summary>
        public virtual void ProcessSynchronousEvents()
        {
            if (_isNotFrameworkAlgorithm)
            {
                return;
            }

            // check the last snap shot time, we may have already produced a snapshot via OnInsightssGenerated
            if (_lastSecurityValuesSnapshotTime != Algorithm.UtcTime)
            {
                InsightManager.Step(Algorithm.UtcTime, CreateSecurityValuesSnapshot(), new GeneratedInsightsCollection(Algorithm.UtcTime, Enumerable.Empty<Insight>()));
            }
        }

        /// <summary>
        /// Thread entry point for asynchronous processing
        /// </summary>
        public virtual void Run()
        {
            if (_isNotFrameworkAlgorithm)
            {
                return;
            }

            IsActive = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // run main loop until canceled, will clean out work queues separately
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    ProcessAsynchronousEvents();
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    throw;
                }

                Thread.Sleep(1);
            }

            // persist insights at exit
            StoreInsights();

            InsightManager.DisposeSafely();

            Log.Trace("DefaultAlphaHandler.Run(): Ending Thread...");
            IsActive = false;
        }

        /// <summary>
        /// Stops processing in the <see cref="IAlphaHandler.Run"/> method
        /// </summary>
        public void Exit()
        {
            if (_isNotFrameworkAlgorithm)
            {
                return;
            }

            Log.Trace("DefaultAlphaHandler.Exit(): Exiting Thread...");

            _cancellationTokenSource.Cancel(false);
        }

        /// <summary>
        /// Performs asynchronous processing, including broadcasting of insights to messaging handler
        /// </summary>
        protected void ProcessAsynchronousEvents()
        {
        }

        /// <summary>
        /// Save insight results to persistent storage
        /// </summary>
        protected virtual void StoreInsights()
        {
            // default save all results to disk and don't remove any from memory
            // this will result in one file with all of the insights/results in it
            var insights = InsightManager.AllInsights.OrderBy(insight => insight.GeneratedTimeUtc).ToList();
            if (insights.Count > 0)
            {
                var directory = Path.Combine(Directory.GetCurrentDirectory(), AlgorithmId);
                var path = Path.Combine(directory, "alpha-results.json");
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, JsonConvert.SerializeObject(insights, Formatting.Indented));
            }
        }

        /// <summary>
        /// Creates the <see cref="InsightManager"/> to manage the analysis of generated insights
        /// </summary>
        /// <returns>A new insight manager instance</returns>
        protected virtual InsightManager CreateInsightManager()
        {
            var scoreFunctionProvider = new DefaultInsightScoreFunctionProvider();
            return new InsightManager(scoreFunctionProvider, 0);
        }

        /// <summary>
        /// Creates the <see cref="AlphaResultPacketSender"/> to manage sending finalized insights via the messaging handler
        /// </summary>
        /// <returns>A new <see cref="CreateAlphaResultPacketSender"/> instance</returns>
        protected virtual AlphaResultPacketSender CreateAlphaResultPacketSender()
        {
            return new AlphaResultPacketSender(Job, MessagingHandler, TimeSpan.FromSeconds(1), 50);
        }

        private ReadOnlySecurityValuesCollection CreateSecurityValuesSnapshot()
        {
            _lastSecurityValuesSnapshotTime = Algorithm.UtcTime;
            return _securityValuesProvider.GetValues(Algorithm.Securities.Keys);
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

            public AlphaResultPacketSender(AlgorithmNodePacket job, IMessagingHandler messagingHandler, TimeSpan interval, int maximumNumberOfInsightsPerPacket)
            {
                _job = job;
                _interval = interval;
                _messagingHandler = messagingHandler;
                _insights = new ConcurrentQueue<Insight>();
                _maximumNumberOfInsightsPerPacket = maximumNumberOfInsightsPerPacket;

                _timer = new Timer(MessagingUpdateIntervalElapsed);
                _timer.Change(interval, interval);

                // don't bother holding on more than makes sense. this makes the maximum
                // number of insights we'll hold in the queue equal to one hour's worth of
                // processing. For 50 insights/message @ 1message/sec this is 90K
                _maximumQueueLength = (int) (TimeSpan.FromMinutes(30).Ticks / interval.Ticks * maximumNumberOfInsightsPerPacket);
            }

            private void MessagingUpdateIntervalElapsed(object state)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

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

                _timer.Change(_interval, _interval);
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

            public void Step(DateTime frontierTimeUtc)
            {
                //NOP
            }

            public void InitializeForRange(DateTime algorithmStartDate, DateTime algorithmEndDate, DateTime algorithmUtcTime)
            {
                //NOP
            }

            public void OnInsightGenerated(InsightAnalysisContext context)
            {
                //NOP
            }

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