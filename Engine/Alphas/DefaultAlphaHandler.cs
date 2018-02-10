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

namespace QuantConnect.Lean.Engine.Alphas
{
    /// <summary>
    /// Default alpha handler that supports sending alphas to the messaging handler, analyzing alphas online
    /// </summary>
    public class DefaultAlphaHandler : IAlphaHandler
    {
        private DateTime _nextMessagingUpdate;
        private DateTime _nextPersistenceUpdate;
        private DateTime _lastSecurityValuesSnapshotTime;

        private bool _isNotFrameworkAlgorithm;
        private IMessagingHandler _messagingHandler;
        private ChartingAlphaManagerExtension _charting;
        private ISecurityValuesProvider _securityValuesProvider;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentQueue<Packet> _messages = new ConcurrentQueue<Packet>();
        private readonly ConcurrentQueue<AlphaQueueItem> _alphaQueue = new ConcurrentQueue<AlphaQueueItem>();

        /// <summary>
        /// Gets a flag indicating if this handler's thread is still running and processing messages
        /// </summary>
        public bool IsActive => !_cancellationTokenSource?.IsCancellationRequested ?? false;

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
        /// Gets or sets the interval at which the alphas are persisted
        /// </summary>
        protected TimeSpan PersistenceUpdateInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the interval at which alpha updates are sent to the messaging handler
        /// </summary>
        protected TimeSpan MessagingUpdateInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets the alpha manager instance used to manage the analysis of algorithm alphas
        /// </summary>
        protected AlphaManager AlphaManager { get; private set; }

        /// <summary>
        /// Gets the collection of managers that events are forwarded to
        /// </summary>
        protected List<IAlphaManagerExtension> AlphaManagers { get; } = new List<IAlphaManagerExtension>();

        /// <summary>
        /// Initializes this alpha handler to accept alphas from the specified algorithm
        /// </summary>
        /// <param name="job">The algorithm job</param>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="messagingHandler">Handler used for sending alphas</param>
        /// <param name="api">Api instance</param>
        public virtual void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler, IApi api)
        {
            // initializing these properties just in case, doens't hurt to have them populated
            Job = job;
            Algorithm = algorithm;
            _messagingHandler = messagingHandler;
            _isNotFrameworkAlgorithm = !algorithm.IsFrameworkAlgorithm;
            if (_isNotFrameworkAlgorithm)
            {
                return;
            }


            _securityValuesProvider = new AlgorithmSecurityValuesProvider(algorithm);

            AlphaManager = CreateAlphaManager();

            var statistics = new StatisticsAlphaManagerExtension();
            RuntimeStatistics = statistics.Statistics;
            AlphaManager.AddExtension(statistics);
            _charting = new ChartingAlphaManagerExtension(algorithm, statistics);
            AlphaManager.AddExtension(_charting);

            // when alpha is generated, take snapshot of securities and place in queue for alpha manager to process on alpha thread
            algorithm.AlphasGenerated += (algo, collection) => _alphaQueue.Enqueue(new AlphaQueueItem(collection.DateTimeUtc, CreateSecurityValuesSnapshot(), collection));
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
            AlphaManager.InitializeExtensionsForRange(algorithm.StartDate, algorithm.EndDate, algorithm.UtcTime);
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

            // check the last snap shot time, we may have already produced a snapshot via OnAlphasGenerated
            if (_lastSecurityValuesSnapshotTime != Algorithm.UtcTime)
            {
                _alphaQueue.Enqueue(new AlphaQueueItem(Algorithm.UtcTime, CreateSecurityValuesSnapshot()));
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

            // finish alpha scoring analysis
            _alphaQueue.ProcessUntilEmpty(item => AlphaManager.Step(item.FrontierTimeUtc, item.SecurityValues, item.GeneratedAlphas));

            // send final alpha scoring updates before we exit
            var alphas = AlphaManager.GetUpdatedContexts().Select(context => context.Alpha).ToList();
            _messages.Enqueue(new AlphaResultPacket(AlgorithmId, Job.UserId, alphas));

            // finish sending packets
            _messages.ProcessUntilEmpty(packet => _messagingHandler.Send(packet));

            // persist alphas at exit
            StoreAlphas();

            Log.Trace("DefaultAlphaHandler.Run(): Ending Thread...");
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

            Log.Trace("DefaultAlphaHandler.Run(): Exiting Thread...");

            _cancellationTokenSource.Cancel(false);
        }

        /// <summary>
        /// Performs asynchronous processing, including broadcasting of alphas to messaging handler
        /// </summary>
        protected void ProcessAsynchronousEvents()
        {
            // step the alpha manager forward in time
            AlphaQueueItem item;
            while (_alphaQueue.TryDequeue(out item))
            {
                AlphaManager.Step(item.FrontierTimeUtc, item.SecurityValues, item.GeneratedAlphas);
            }

            // send alpha upate messages
            Packet packet;
            while (_messages.TryDequeue(out packet))
            {
                _messagingHandler.Send(packet);
            }

            // persist generated alphas to storage
            if (DateTime.UtcNow > _nextPersistenceUpdate)
            {
                StoreAlphas();
                _nextPersistenceUpdate = DateTime.UtcNow + PersistenceUpdateInterval;
            }

            // push updated alphas through messaging handler
            if (DateTime.UtcNow > _nextMessagingUpdate)
            {
                var alphas = AlphaManager.GetUpdatedContexts().Select(context => context.Alpha).ToList();
                if (alphas.Count > 0)
                {
                    _messages.Enqueue(new AlphaResultPacket
                    {
                        AlgorithmId = AlgorithmId,
                        Alphas = alphas
                    });
                }
                _nextMessagingUpdate = DateTime.UtcNow + MessagingUpdateInterval;
            }
        }

        /// <summary>
        /// Save alpha results to persistent storage
        /// </summary>
        protected virtual void StoreAlphas()
        {
            // default save all results to disk and don't remove any from memory
            // this will result in one file with all of the alphas/results in it
            var alphas = AlphaManager.AllAlphas.OrderBy(alpha => alpha.GeneratedTimeUtc).ToList();
            if (alphas.Count > 0)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), AlgorithmId, "alpha-results.json");
                Directory.CreateDirectory(new FileInfo(path).DirectoryName);
                File.WriteAllText(path, JsonConvert.SerializeObject(alphas, Formatting.Indented));
            }
        }

        /// <summary>
        /// Creates the <see cref="AlphaManager"/> to manage the analysis of generated alphas
        /// </summary>
        /// <returns>A new alpha manager instance</returns>
        protected virtual AlphaManager CreateAlphaManager()
        {
            var scoreFunctionProvider = new DefaultAlphaScoreFunctionProvider();
            return new AlphaManager(scoreFunctionProvider, 0);
        }

        private ReadOnlySecurityValuesCollection CreateSecurityValuesSnapshot()
        {
            _lastSecurityValuesSnapshotTime = Algorithm.UtcTime;
            return _securityValuesProvider.GetValues(Algorithm.Securities.Keys);
        }

        class AlphaQueueItem
        {
            public DateTime FrontierTimeUtc;
            public AlphaCollection GeneratedAlphas;
            public ReadOnlySecurityValuesCollection SecurityValues;

            public AlphaQueueItem(DateTime frontierTimeUtc, ReadOnlySecurityValuesCollection securityValues, AlphaCollection generatedAlphas = null)
            {
                FrontierTimeUtc = frontierTimeUtc;
                SecurityValues = securityValues;
                GeneratedAlphas = generatedAlphas ?? new AlphaCollection(frontierTimeUtc, Enumerable.Empty<Algorithm.Framework.Alphas.Alpha>());
            }
        }
    }
}