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
        private const int BacktestChartSamples = 1000;
        private static readonly IReadOnlyCollection<SignalScoreType> ScoreTypes = SignalManager.ScoreTypes;

        private DateTime _nextMessagingUpdate;
        private DateTime _nextPersistenceUpdate;
        private DateTime _nextChartSampleAlgorithmTimeUtc;
        private DateTime _lastChartSampleAlgorithmTimeUtc;

        private IMessagingHandler _messagingHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentQueue<Packet> _messages = new ConcurrentQueue<Packet>();

        private readonly Chart _assetBreakdownChart = new Chart("Alpha Asset Breakdown");
        private readonly Series _predictionCountSeries = new Series("Count", SeriesType.Line, "#");
        private readonly ConcurrentDictionary<Symbol, int> _signalCountPerSymbol = new ConcurrentDictionary<Symbol, int>();
        private readonly Dictionary<SignalScoreType, Series> _seriesByScoreType = new Dictionary<SignalScoreType, Series>();

        /// <inheritdoc />
        public bool IsActive => !_cancellationTokenSource?.IsCancellationRequested ?? false;

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
        protected TimeSpan PersistenceUpdateInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the interval at which signal updates are sent to the messaging handler
        /// </summary>
        protected TimeSpan MessagingUpdateInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets the interval at which alpha charts are updated. This is in realtion to algorithm time.
        /// </summary>
        protected TimeSpan ChartUpdateInterval { get; set; } = TimeSpan.FromMinutes(1);

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

            // chart for average scores over sample period
            var scoreChart = new Chart("Alpha");
            foreach (var scoreType in ScoreTypes)
            {
                var series = new Series($"{scoreType} Score", SeriesType.Line, "%");
                scoreChart.AddSeries(series);
                _seriesByScoreType[scoreType] = series;
            }

            // chart for prediction count over sample period
            var predictionCount = new Chart("Alpha Count");
            predictionCount.AddSeries(_predictionCountSeries);

            Algorithm.AddChart(scoreChart);
            Algorithm.AddChart(predictionCount);
            Algorithm.AddChart(_assetBreakdownChart);
        }

        /// <inheritdoc />
        public void OnAfterAlgorithmInitialized(IAlgorithm algorithm)
        {
            _lastChartSampleAlgorithmTimeUtc = algorithm.UtcTime;
            if (!LiveMode)
            {
                // space out backtesting samples evenly
                var backtestPeriod = algorithm.EndDate - algorithm.StartDate;
                ChartUpdateInterval = TimeSpan.FromTicks(backtestPeriod.Ticks / BacktestChartSamples);
            }
            else
            {
                // live mode we'll sample each minute
                ChartUpdateInterval = Time.OneMinute;
            }
        }

        /// <inheritdoc />
        public virtual void ProcessSynchronousEvents()
        {
            // before updating scores, emit chart points for the previous sample period
            if (Algorithm.UtcTime >= _nextChartSampleAlgorithmTimeUtc)
            {
                UpdateCharts();
            }

            // update scores in line with the algo thread to ensure a consistent read of security data
            // this will manage marking signals as closed as well as performing score updates
            SignalManager.UpdateScores();
        }

        private void UpdateCharts()
        {
            var updatedSignals = SignalManager.AllSignals.Where(signal =>
                signal.Score.UpdatedTimeUtc >= _lastChartSampleAlgorithmTimeUtc &&
                signal.Score.UpdatedTimeUtc <= _nextChartSampleAlgorithmTimeUtc
            )
            .ToList();

            ChartAverageSignalScores(updatedSignals, Algorithm.UtcTime);

            // compute and chart total signal count over sample period
            var totalSignals = _signalCountPerSymbol.Values.Sum();
            _predictionCountSeries.AddPoint(Algorithm.UtcTime, totalSignals, LiveMode);

            // chart asset breakdown over sample period
            foreach (var kvp in _signalCountPerSymbol)
            {
                var symbol = kvp.Key;
                var count = kvp.Value;

                Series series;
                if (!_assetBreakdownChart.Series.TryGetValue(symbol.Value, out series))
                {
                    series = new Series(symbol.Value, SeriesType.StackedArea, "#");
                    _assetBreakdownChart.Series.Add(series.Name, series);
                }

                series.AddPoint(Algorithm.UtcTime, count, LiveMode);
            }

            // reset for next sampling period
            _signalCountPerSymbol.Clear();
            _lastChartSampleAlgorithmTimeUtc = _nextChartSampleAlgorithmTimeUtc;
            _nextChartSampleAlgorithmTimeUtc = Algorithm.UtcTime + ChartUpdateInterval;
        }

        /// <inheritdoc />
        public virtual void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // run until cancelled AND we're done processing messages
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

            // persist signals at exit
            StoreSignals();
        }

        /// <inheritdoc />
        public void Exit()
        {
            // send final signal scoring updates before we exit
            _messages.Enqueue(new SignalPacket
            {
                AlgorithmId = AlgorithmId,
                Signals = SignalManager.GetUpdatedContexts().Select(context => context.Signal).ToList()
            });

            _cancellationTokenSource.Cancel(false);
        }

        /// <summary>
        /// Performs asynchronous processing, including broadcasting of signals to messaging handler
        /// </summary>
        protected void ProcessAsynchronousEvents()
        {
            Packet packet;
            while (_messages.TryDequeue(out packet))
            {
                _messagingHandler.Send(packet);
            }

            // persist generated signals to storage
            if (DateTime.UtcNow > _nextPersistenceUpdate)
            {
                StoreSignals();
                _nextPersistenceUpdate = DateTime.UtcNow + PersistenceUpdateInterval;
            }

            // push updated signals through messaging handler
            if (DateTime.UtcNow > _nextMessagingUpdate)
            {
                var signals = SignalManager.GetUpdatedContexts().Select(context => context.Signal).ToList();
                if (signals.Count > 0)
                {
                    _messages.Enqueue(new SignalPacket
                    {
                        AlgorithmId = AlgorithmId,
                        Signals = signals
                    });
                }
                _nextMessagingUpdate = DateTime.UtcNow + MessagingUpdateInterval;
            }
        }

        /// <summary>
        /// Save signal results to persistent storage
        /// </summary>
        protected virtual void StoreSignals()
        {
            // default save all results to disk and don't remove any from memory
            // this will result in one file with all of the signals/results in it
            var signals = SignalManager.AllSignals.OrderBy(signal => signal.GeneratedTimeUtc).ToList();
            if (signals.Count > 0)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), AlgorithmId, "signal-results.json");
                Directory.CreateDirectory(new FileInfo(path).DirectoryName);
                File.WriteAllText(path, JsonConvert.SerializeObject(signals, Formatting.Indented));
            }
        }

        /// <summary>
        /// Handles the algorithm's <see cref="IAlgorithm.SignalsGenerated"/> event
        /// and broadcasts the new signal using the messaging handler
        /// </summary>
        protected void OnSignalsGenerated(SignalCollection collection)
        {
            // send message for newly created signals
            Packet packet = new SignalPacket(AlgorithmId, collection.Signals);
            _messages.Enqueue(packet);

            SignalManager.AddSignals(collection);

            // aggregate signal counts per symbol
            foreach (var grouping in collection.Signals.GroupBy(signal => signal.Symbol))
            {
                _signalCountPerSymbol.AddOrUpdate(grouping.Key, 1, (sym, cnt) => cnt + grouping.Count());
            }
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
        /// Adds chart point for each signal score type with an average value of the specified period
        /// </summary>
        /// <param name="signals">The signals to chart average scores for</param>
        /// <param name="end">The analysis end time, used as time for chart points</param>
        protected void ChartAverageSignalScores(List<Signal> signals, DateTime end)
        {
            // compute average score values for all signals with updates over the last day
            var count = 0;
            var runningScoreTotals = ScoreTypes.ToDictionary(type => type, type => 0d);

            // ignore signals that haven't received scoring updates yet
            foreach (var signal in signals.Where(signal => signal.GeneratedTimeUtc != signal.Score.UpdatedTimeUtc))
            {
                count++;
                foreach (var scoreType in ScoreTypes)
                {
                    runningScoreTotals[scoreType] += signal.Score.GetScore(scoreType);
                }
            }

            if (count < 1)
            {
                return;
            }

            foreach (var kvp in runningScoreTotals)
            {
                var scoreType = kvp.Key;
                var runningTotal = kvp.Value;
                var average = runningTotal / count;
                // scale the value from [0,1] to [0,100] for charting
                _seriesByScoreType[scoreType].AddPoint(end, 100m * (decimal) average, LiveMode);
            }
        }
    }
}