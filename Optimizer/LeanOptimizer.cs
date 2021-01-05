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
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Base Lean optimizer class in charge of handling an optimization job packet
    /// </summary>
    public abstract class LeanOptimizer : IDisposable
    {
        private readonly int _optimizationUpdateInterval = Config.GetInt("optimization-update-interval", 10);

        private DateTime _startedAt = DateTime.UtcNow;

        private DateTime _lastUpdate;
        private int _failedBacktest;
        private int _completedBacktest;
        private volatile bool _disposed;

        /// <summary>
        /// The total completed backtests count
        /// </summary>
        protected int CompletedBacktests => _failedBacktest + _completedBacktest;

        /// <summary>
        /// Lock to update optimization status
        /// </summary>
        private object _statusLock = new object();

        /// <summary>
        /// The current optimization status
        /// </summary>
        protected OptimizationStatus Status { get; private set; } = OptimizationStatus.New;

        /// <summary>
        /// The optimization target
        /// </summary>
        protected readonly Target OptimizationTarget;

        /// <summary>
        /// Collection holding <see cref="ParameterSet"/> for each backtest id we are waiting to finish
        /// </summary>
        protected readonly ConcurrentDictionary<string, ParameterSet> RunningParameterSetForBacktest;

        /// <summary>
        /// Collection holding <see cref="ParameterSet"/> for each backtest id we are waiting to launch
        /// </summary>
        /// <remarks>We can't launch 1 million backtests at the same time</remarks>
        protected readonly ConcurrentQueue<ParameterSet> PendingParameterSet;

        /// <summary>
        /// The optimization strategy being used
        /// </summary>
        protected readonly IOptimizationStrategy Strategy;

        /// <summary>
        /// The optimization packet
        /// </summary>
        protected readonly OptimizationNodePacket NodePacket;

        /// <summary>
        /// Indicates whether optimizer was disposed
        /// </summary>
        protected bool Disposed => _disposed;

        /// <summary>
        /// Event triggered when the optimization work ended
        /// </summary>
        public event EventHandler<OptimizationResult> Ended;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="nodePacket">The optimization node packet to handle</param>
        protected LeanOptimizer(OptimizationNodePacket nodePacket)
        {
            if (nodePacket.OptimizationParameters.IsNullOrEmpty())
            {
                throw new ArgumentException("Cannot start an optimization job with no parameter to optimize");
            }

            if (string.IsNullOrEmpty(nodePacket.Criterion?.Target))
            {
                throw new ArgumentException("Cannot start an optimization job with no target to optimize");
            }

            NodePacket = nodePacket;
            OptimizationTarget = NodePacket.Criterion;
            OptimizationTarget.Reached += (s, e) =>
            {
                // we've reached the optimization target
                TriggerOnEndEvent();
            };

            Strategy = (IOptimizationStrategy)Activator.CreateInstance(Type.GetType(NodePacket.OptimizationStrategy));

            RunningParameterSetForBacktest = new ConcurrentDictionary<string, ParameterSet>();
            PendingParameterSet = new ConcurrentQueue<ParameterSet>();

            Strategy.Initialize(OptimizationTarget, nodePacket.Constraints, NodePacket.OptimizationParameters, NodePacket.OptimizationStrategySettings);

            Strategy.NewParameterSet += (s, parameterSet) =>
            {
                if (parameterSet == null)
                {
                    // shouldn't happen
                    Log.Error($"Strategy.NewParameterSet({GetLogDetails()}): generated a null {nameof(ParameterSet)} instance");
                    return;
                }
                LaunchLeanForParameterSet(parameterSet);
            };
        }

        /// <summary>
        /// Starts the optimization
        /// </summary>
        public virtual void Start()
        {
            lock (RunningParameterSetForBacktest)
            {
                Strategy.PushNewResults(OptimizationResult.Initial);

                // if after we started there are no running parameter sets means we have failed to start
                if (RunningParameterSetForBacktest.Count == 0)
                {
                    throw new InvalidOperationException($"LeanOptimizer.Start({GetLogDetails()}): failed to start");
                }
                Log.Trace($"LeanOptimizer.Start({GetLogDetails()}): start ended. Waiting on {RunningParameterSetForBacktest.Count + PendingParameterSet.Count} backtests");
            }

            SetOptimizationStatus(OptimizationStatus.Running);
            ProcessUpdate(forceSend: true);
        }

        /// <summary>
        /// Triggers the optimization job end event
        /// </summary>
        protected virtual void TriggerOnEndEvent()
        {
            if (_disposed)
            {
                return;
            }
            SetOptimizationStatus(OptimizationStatus.Completed);

            var result = Strategy.Solution;
            if (result != null)
            {
                var constraint = NodePacket.Constraints != null ? $"Constraints: ({string.Join(",", NodePacket.Constraints)})" : string.Empty;
                Log.Trace($"LeanOptimizer.TriggerOnEndEvent({GetLogDetails()}): Optimization has ended. " +
                    $"Result for {OptimizationTarget}: was reached using ParameterSet: ({result.ParameterSet}) backtestId '{result.BacktestId}'. " +
                    $"{constraint}");
            }
            else
            {
                Log.Trace($"LeanOptimizer.TriggerOnEndEvent({GetLogDetails()}): Optimization has ended. Result was not reached");
            }

            // we clean up before we send an update so that the runtime stats are updated
            CleanUpRunningInstance();
            ProcessUpdate(forceSend: true);

            Ended?.Invoke(this, result);
        }

        /// <summary>
        /// Handles starting Lean for a given parameter set
        /// </summary>
        /// <param name="parameterSet">The parameter set for the backtest to run</param>
        /// <returns>The new unique backtest id</returns>
        protected abstract string RunLean(ParameterSet parameterSet);

        /// <summary>
        /// Handles a new backtest json result matching a requested backtest id
        /// </summary>
        /// <param name="jsonBacktestResult">The backtest json result</param>
        /// <param name="backtestId">The associated backtest id</param>
        protected virtual void NewResult(string jsonBacktestResult, string backtestId)
        {
            lock (RunningParameterSetForBacktest)
            {
                ParameterSet parameterSet;

                // we take a lock so that there is no race condition with launching Lean adding the new backtest id and receiving the backtest result for that id
                // before it's even in the collection 'ParameterSetForBacktest'

                if (!RunningParameterSetForBacktest.TryRemove(backtestId, out parameterSet))
                {
                    Interlocked.Increment(ref _failedBacktest);
                    Log.Error(
                        $"LeanOptimizer.NewResult({GetLogDetails()}): Optimization compute job with id '{backtestId}' was not found");
                    return;
                }

                // we got a new result if there are any pending parameterSet to run we can now trigger 1
                // we do this before 'Strategy.PushNewResults' so FIFO is respected
                if (PendingParameterSet.Count > 0)
                {
                    ParameterSet pendingParameterSet;
                    PendingParameterSet.TryDequeue(out pendingParameterSet);
                    LaunchLeanForParameterSet(pendingParameterSet);
                }

                var result = new OptimizationResult(null, parameterSet, backtestId);
                if (string.IsNullOrEmpty(jsonBacktestResult))
                {
                    Interlocked.Increment(ref _failedBacktest);
                    Log.Error(
                        $"LeanOptimizer.NewResult({GetLogDetails()}): Got null/empty backtest result for backtest id '{backtestId}'");
                }
                else
                {
                    Interlocked.Increment(ref _completedBacktest);
                    result = new OptimizationResult(jsonBacktestResult, parameterSet, backtestId);
                }

                // always notify the strategy
                Strategy.PushNewResults(result);

                // strategy could of added more
                if (RunningParameterSetForBacktest.Count == 0)
                {
                    TriggerOnEndEvent();
                }
                else
                {
                    ProcessUpdate();
                }
            }
        }

        /// <summary>
        /// Disposes of any resources
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            CleanUpRunningInstance();
        }

        /// <summary>
        /// Returns the current optimization status and strategy estimates
        /// </summary>
        public int GetCurrentEstimate()
        {
            return Strategy.GetTotalBacktestEstimate();
        }

        /// <summary>
        /// Get the current runtime statistics
        /// </summary>
        public Dictionary<string, string> GetRuntimeStatistics()
        {
            var completedCount = _completedBacktest;
            var totalEndedCount = completedCount + _failedBacktest;
            var runtime = DateTime.UtcNow - _startedAt;
            var result = new Dictionary<string, string>
            {
                { "Completed", $"{completedCount}"},
                { "Failed", $"{_failedBacktest}"},
                { "Running", $"{RunningParameterSetForBacktest.Count}"},
                { "In Queue", $"{PendingParameterSet.Count}"},
                { "Average Length", $"{(totalEndedCount > 0 ? new TimeSpan(runtime.Ticks / totalEndedCount) : TimeSpan.Zero).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)}"},
                { "Total Runtime", $"{runtime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)}" }
            };

            return result;
        }

        /// <summary>
        /// Helper method to have pretty more informative logs
        /// </summary>
        protected string GetLogDetails()
        {
            if (NodePacket.UserId == 0)
            {
                return $"OID {NodePacket.OptimizationId}";
            }
            return $"UI {NodePacket.UserId} PID {NodePacket.ProjectId} OID {NodePacket.OptimizationId} S {Status}";
        }

        /// <summary>
        /// Handles stopping Lean process
        /// </summary>
        /// <param name="backtestId">Specified backtest id</param>
        protected abstract void AbortLean(string backtestId);

        /// <summary>
        /// Sends an update of the current optimization status to the user
        /// </summary>
        protected abstract void SendUpdate();

        /// <summary>
        /// Sets the current optimization status
        /// </summary>
        /// <param name="optimizationStatus">The new optimization status</param>
        protected virtual void SetOptimizationStatus(OptimizationStatus optimizationStatus)
        {
            lock (_statusLock)
            {
                // we never come back from an aborted/ended status
                if (Status != OptimizationStatus.Aborted && Status != OptimizationStatus.Completed)
                {
                    Status = optimizationStatus;
                }
            }
        }

        /// <summary>
        /// Clean up any pending or running lean instance
        /// </summary>
        private void CleanUpRunningInstance()
        {
            PendingParameterSet.Clear();

            lock (RunningParameterSetForBacktest)
            {
                foreach (var backtestId in RunningParameterSetForBacktest.Keys)
                {
                    ParameterSet parameterSet;
                    if (RunningParameterSetForBacktest.TryRemove(backtestId, out parameterSet))
                    {
                        Interlocked.Increment(ref _failedBacktest);
                        try
                        {
                            AbortLean(backtestId);
                        }
                        catch
                        {
                            // pass
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Will determine if it's right time to trigger an update call
        /// </summary>
        /// <param name="forceSend">True will force send, skipping interval, useful on start and end</param>
        private void ProcessUpdate(bool forceSend = false)
        {
            if (!forceSend && Status == OptimizationStatus.New)
            {
                // don't send any update until we finish the Start(), will be creating a bunch of backtests don't want to send partial/multiple updates
                return;
            }

            try
            {
                var now = DateTime.UtcNow;
                if (forceSend || (now - _lastUpdate > TimeSpan.FromSeconds(_optimizationUpdateInterval)))
                {
                    _lastUpdate = now;
                    Log.Debug($"LeanOptimizer.ProcessUpdate({GetLogDetails()}): start sending update...");

                    SendUpdate();

                    Log.Debug($"LeanOptimizer.ProcessUpdate({GetLogDetails()}): finished sending update successfully.");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to send status update");
            }
        }

        private void LaunchLeanForParameterSet(ParameterSet parameterSet)
        {
            if (_disposed || Status == OptimizationStatus.Completed || Status == OptimizationStatus.Aborted)
            {
                return;
            }

            lock (RunningParameterSetForBacktest)
            {
                if (NodePacket.MaximumConcurrentBacktests != 0 && RunningParameterSetForBacktest.Count >= NodePacket.MaximumConcurrentBacktests)
                {
                    // we hit the limit on the concurrent backtests
                    PendingParameterSet.Enqueue(parameterSet);
                    return;
                }

                try
                {
                    var backtestId = RunLean(parameterSet);

                    if (!string.IsNullOrEmpty(backtestId))
                    {
                        Log.Trace($"LeanOptimizer.LaunchLeanForParameterSet({GetLogDetails()}): launched backtest '{backtestId}'");
                        RunningParameterSetForBacktest.TryAdd(backtestId, parameterSet);
                    }
                    else
                    {
                        Interlocked.Increment(ref _failedBacktest);
                        // always notify the strategy
                        Strategy.PushNewResults(new OptimizationResult(null, parameterSet, backtestId));
                        Log.Error($"LeanOptimizer.LaunchLeanForParameterSet({GetLogDetails()}): Initial/null optimization compute job could not be placed into the queue");
                    }

                    ProcessUpdate();
                }
                catch (Exception ex)
                {
                    Log.Error($"LeanOptimizer.LaunchLeanForParameterSet({GetLogDetails()}): Error encountered while placing optimization message into the queue: {ex.Message}");
                }
            }
        }
    }
}
