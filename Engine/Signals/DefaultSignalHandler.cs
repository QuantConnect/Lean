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
using QuantConnect.Algorithm.Framework.Signals;
using QuantConnect.Algorithm.Framework.Signals.Analysis;
using QuantConnect.Algorithm.Framework.Signals.Analysis.Providers;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Signals
{
    /// <summary>
    /// Base signal handler that supports sending signals to the messaging handler
    /// </summary>
    public class DefaultSignalHandler : ISignalHandler
    {
        private static readonly IReadOnlyCollection<SignalScoreType> ScoreTypes = SignalManager.ScoreTypes;

        private DateTime _nextPersistenceUpdate;
        private DateTime _nextChartUpdateAlgorithmTimeUtc;

        private IMessagingHandler _messagingHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentQueue<Packet> _messages = new ConcurrentQueue<Packet>();
        private readonly Dictionary<SignalScoreType, Series> _seriesByScoreType = new Dictionary<SignalScoreType, Series>();

        /// <inheritdoc />
        public bool IsActive => !_cancellationTokenSource.IsCancellationRequested;

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
        /// Gets or sets the interval at which the signals are persisted
        /// </summary>
        protected TimeSpan PersistenceUpdateInterval { get; set; }

        /// <summary>
        /// Gets the signal manager instance used to manage the analysis of algorithm signals
        /// </summary>
        protected SignalManager SignalManager { get; private set; }

        /// <inheritdoc />
        public virtual void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler, IApi api)
        {
            Job = job;
            Algorithm = algorithm;
            _messagingHandler = messagingHandler;
            SignalManager = CreateSignalManager();
            algorithm.SignalsGenerated += (algo, collection) => OnSignalsGenerated(collection);

            // add charts for average signal scores
            var chart = new Chart("Alpha");
            foreach (var scoreType in ScoreTypes)
            {
                var series = new Series($"{scoreType} Score", SeriesType.Line, "%");
                chart.AddSeries(series);

                _seriesByScoreType[scoreType] = series;
            }

            Algorithm.AddChart(chart);
        }

        /// <inheritdoc />
        public virtual void ProcessSynchronousEvents()
        {
            // before updating scores, emit chart points on day changes
            if (Algorithm.UtcTime >= _nextChartUpdateAlgorithmTimeUtc)
            {
                ChartAverageSignalScores();
            }

            // update scores in line with the algo thread to ensure a consistent read of security data
            // this will manage marking signals as closed as well as performing score updates
            SignalManager.UpdateScores();
        }

        /// <inheritdoc />
        public virtual void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // run until cancelled AND we're processing messages
            while (!_cancellationTokenSource.IsCancellationRequested || !_messages.IsEmpty)
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

                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Performs asynchronous processing, including broadcasting of signals to messaging handler
        /// </summary>
        protected virtual void ProcessAsynchronousEvents()
        {
            Packet packet;
            while (_messages.TryDequeue(out packet))
            {
                _messagingHandler.Send(packet);
            }

            if (DateTime.UtcNow > _nextPersistenceUpdate)
            {
                SaveSignals();
                _nextPersistenceUpdate = DateTime.UtcNow + PersistenceUpdateInterval;
            }
        }

        /// <summary>
        /// Save signal results to persistent storage
        /// </summary>
        protected virtual void SaveSignals()
        {
            // default save all results to disk and don't remove any from memory
            // this will result in one file with all of the signals/results in it
            var signals = SignalManager.AllSignals.OrderBy(signal => signal.Score.UpdatedTimeUtc).ToList();
            var path = Path.Combine(Directory.GetCurrentDirectory(), AlgorithmId, "signal-results.json");
            Directory.CreateDirectory(new FileInfo(path).DirectoryName);
            File.WriteAllText(path, JsonConvert.SerializeObject(signals, Formatting.Indented));
        }

        /// <summary>
        /// Handles the algorithm's <see cref="IAlgorithm.SignalsGenerated"/> event
        /// and broadcasts the new signal using the messaging handler
        /// </summary>
        protected virtual void OnSignalsGenerated(SignalCollection collection)
        {
            // send message for newly created signals
            Enqueue(new SignalPacket(AlgorithmId, collection.Signals));

            SignalManager.AddSignals(collection);
        }

        /// <summary>
        /// Creates the <see cref="SignalManager"/> to manage the analysis of generated signals
        /// </summary>
        /// <returns>A new signal manager instance</returns>
        protected virtual SignalManager CreateSignalManager()
        {
            var scoreFunctionProvider = new DefaultSignalScoreFunctionProvider();
            return new SignalManager(new AlgorithmSecurityValuesProvider(Algorithm), scoreFunctionProvider, 0);
        }

        /// <summary>
        /// Enqueues a packet to be processed asynchronously
        /// </summary>
        /// <param name="packet">The packet</param>
        protected void Enqueue(Packet packet)
        {
            _messages.Enqueue(packet);
        }

        /// <inheritdoc />
        public void Exit()
        {
            _cancellationTokenSource.Cancel(false);
        }

        private void ChartAverageSignalScores()
        {
            var start = (Algorithm.UtcTime - Time.OneDay).Date;
            var end = start + Time.OneDay;

            // compute average score values for all signals with updates over the last day
            var count = 0;
            var runningTotals = ScoreTypes.ToDictionary(type => type, type => 0d);
            foreach (var signal in SignalManager.AllSignals.Where(signal => signal.Score.UpdatedTimeUtc >= start && signal.Score.UpdatedTimeUtc <= end))
            {
                count++;
                foreach (var scoreType in ScoreTypes)
                {
                    runningTotals[scoreType] += signal.Score.GetScore(scoreType);
                }
            }

            if (count > 0)
            {
                foreach (var kvp in runningTotals)
                {
                    var scoreType = kvp.Key;
                    var runningTotal = kvp.Value;
                    var average = runningTotal / count;
                    // scale the value from [0,1] to [0,100] for charting
                    _seriesByScoreType[scoreType].AddPoint(end, 100m*(decimal)average, LiveMode);
                }
            }

            _nextChartUpdateAlgorithmTimeUtc = end;
        }
    }
}