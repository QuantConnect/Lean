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
using System.Linq;
using QuantConnect.Util;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using System.Collections.Concurrent;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Base Lean optimizer class in charge of handling an optimization job packet
    /// </summary>
    public abstract class LeanOptimizer : IDisposable
    {
        private string _jsonEscapedTarget;

        /// <summary>
        /// The optimization target
        /// </summary>
        protected readonly string OptimizationTarget;

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
        /// Event triggered when the optimization work ended
        /// </summary>
        public event EventHandler Ended;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="nodePacket">The optimization node packet to handle</param>
        protected LeanOptimizer(OptimizationNodePacket nodePacket)
        {
            if (nodePacket.OptimizationParameters.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Cannot start an optimization job with no parameter to optimize");
            }

            OptimizationTarget = nodePacket.Criterion["target"];
            if (!OptimizationTarget.Contains("."))
            {
                // default path
                OptimizationTarget = $"Statistics.{OptimizationTarget}";
            }
            // escape empty space in json path
            _jsonEscapedTarget = string.Join(".", OptimizationTarget.Split('.').Select(s => $"['{s}']"));

            NodePacket = nodePacket;

            Strategy = (IOptimizationStrategy)Activator.CreateInstance(Type.GetType(NodePacket.OptimizationStrategy));

            RunningParameterSetForBacktest = new ConcurrentDictionary<string, ParameterSet>();
            PendingParameterSet = new ConcurrentQueue<ParameterSet>();

            Strategy.Initialize(NodePacket.Criterion["extremum"] == "max"
                    ? new Maximization() as Extremum
                    : new Minimization(),
                NodePacket.OptimizationParameters);

            Strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                if (parameterSet == null)
                {
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
            Strategy.PushNewResults(OptimizationResult.Empty);
        }

        /// <summary>
        /// Triggers the optimization job end event
        /// </summary>
        protected virtual void TriggerOnEndEvent(EventArgs eventArgs)
        {
            var result = Strategy.Solution;
            if (result != null)
            {
                Log.Trace($"LeanOptimizer.TriggerOnEndEvent({GetLogDetails()}): Optimization has ended. " +
                    $"Result for {OptimizationTarget}: {result.Target} was reached using ParameterSet: ({result.ParameterSet})");
            }
            else
            {
                Log.Trace($"LeanOptimizer.TriggerOnEndEvent({GetLogDetails()}): Optimization has ended. Result was not reached");
            }

            Ended?.Invoke(this, eventArgs);
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
            ParameterSet parameterSet;

            // we take a lock so that there is no race condition with launching Lean adding the new backtest id and receiving the backtest result for that id
            // before it's even in the collection 'ParameterSetForBacktest'
            lock (RunningParameterSetForBacktest)
            {
                if (!RunningParameterSetForBacktest.TryRemove(backtestId, out parameterSet))
                {
                    Log.Error($"LeanOptimizer.NewResult({GetLogDetails()}): Optimization compute job with id '{backtestId}' was not found");
                    return;
                }
            }

            // we got a new result if there are any pending parameterSet to run we can now trigger 1
            // we do this before 'Strategy.PushNewResults' so FIFO is respected
            if (PendingParameterSet.Count > 0)
            {
                ParameterSet pendingParameterSet;
                PendingParameterSet.TryDequeue(out pendingParameterSet);
                LaunchLeanForParameterSet(pendingParameterSet);
            }

            var result = new OptimizationResult(null, parameterSet);
            if (string.IsNullOrEmpty(jsonBacktestResult))
            {
                Log.Error($"LeanOptimizer.NewResult({GetLogDetails()}): Got null/empty backtest result for backtest id '{backtestId}'");
            }
            else
            {
                try
                {
                    var value = JObject.Parse(jsonBacktestResult).SelectToken(_jsonEscapedTarget).Value<decimal>();
                    result = new OptimizationResult(value, parameterSet);
                }
                catch (Exception e)
                {
                    Log.Error($"LeanOptimizer.NewResult({GetLogDetails()}): Failed to get optimization target '{backtestId}'. Exception: {e}");
                }
            }
            // always notify the strategy
            Strategy.PushNewResults(result);

            if (!RunningParameterSetForBacktest.Any())
            {
                // TODO: could send winning backtest id/result?
                TriggerOnEndEvent(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Disposes of any resources
        /// </summary>
        public abstract void Dispose();


        private void LaunchLeanForParameterSet(ParameterSet parameterSet)
        {
            if (NodePacket.MaximumConcurrentBacktests != 0 && RunningParameterSetForBacktest.Count > NodePacket.MaximumConcurrentBacktests)
            {
                // we hit the limit on the concurrent backtests
                PendingParameterSet.Enqueue(parameterSet);
                return;
            }

            lock (RunningParameterSetForBacktest)
            {
                try
                {
                    var backtestId = RunLean(parameterSet);

                    if (!string.IsNullOrEmpty(backtestId))
                    {
                        RunningParameterSetForBacktest.TryAdd(backtestId, parameterSet);
                    }
                    else
                    {
                        Log.Error($"LeanOptimizer.LaunchLeanForParameterSet({GetLogDetails()}): Empty/null optimization compute job could not be placed into the queue");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"LeanOptimizer.LaunchLeanForParameterSet({GetLogDetails()}): Error encountered while placing optimization message into the queue: {ex.Message}");
                }
            }
        }

        protected string GetLogDetails()
        {
            if (NodePacket.UserId == 0)
            {
                return $"OID {NodePacket.OptimizationId}";
            }
            return $"UI {NodePacket.UserId} PID {NodePacket.ProjectId} OID {NodePacket.OptimizationId}";
        }
    }
}
