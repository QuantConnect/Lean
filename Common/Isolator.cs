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
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Isolator class - create a new instance of the algorithm and ensure it doesn't
    /// exceed memory or time execution limits.
    /// </summary>
    public class Isolator
    {
        /// <summary>
        /// Algo cancellation controls - cancel source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource
        {
            get; private set;
        }

        /// <summary>
        /// Algo cancellation controls - cancellation token for algorithm thread.
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return CancellationTokenSource.Token; }
        }

        /// <summary>
        /// Check if this task isolator is cancelled, and exit the analysis
        /// </summary>
        public bool IsCancellationRequested
        {
            get { return CancellationTokenSource.IsCancellationRequested; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Isolator"/> class
        /// </summary>
        public Isolator()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Execute a code block with a maximum limit on time and memory.
        /// </summary>
        /// <param name="timeSpan">Timeout in timespan</param>
        /// <param name="withinCustomLimits">Function used to determine if the codeBlock is within custom limits, such as with algorithm manager
        /// timing individual time loops, return a non-null and non-empty string with a message indicating the error/reason for stoppage</param>
        /// <param name="codeBlock">Action codeblock to execute</param>
        /// <param name="memoryCap">Maximum memory allocation, default 1024Mb</param>
        /// <param name="sleepIntervalMillis">Sleep interval between each check in ms</param>
        /// <param name="workerThread">The worker thread instance that will execute the provided action, if null
        /// will use a <see cref="Task"/></param>
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public bool ExecuteWithTimeLimit(TimeSpan timeSpan, Func<IsolatorLimitResult> withinCustomLimits, Action codeBlock, long memoryCap = 1024, int sleepIntervalMillis = 1000, WorkerThread workerThread = null)
        {
            workerThread?.Add(codeBlock);

            var task = workerThread == null
                //Launch task
                ? Task.Factory.StartNew(codeBlock, CancellationTokenSource.Token)
                // wrapper task so we can reuse MonitorTask
                : Task.Factory.StartNew(() => workerThread.FinishedWorkItem.WaitOne(), CancellationTokenSource.Token);
            try
            {
                return MonitorTask(task, timeSpan, withinCustomLimits, memoryCap, sleepIntervalMillis);
            }
            catch (Exception)
            {
                if (!task.IsCompleted)
                {
                    // lets free the wrapper task even if the worker thread didn't finish
                    workerThread?.FinishedWorkItem.Set();
                }
                throw;
            }
        }

        private bool MonitorTask(Task task,
            TimeSpan timeSpan,
            Func<IsolatorLimitResult> withinCustomLimits,
            long memoryCap = 1024,
            int sleepIntervalMillis = 1000)
        {
            // default to always within custom limits
            withinCustomLimits = withinCustomLimits ?? (() => new IsolatorLimitResult(TimeSpan.Zero, string.Empty));

            var message = "";
            var emaPeriod = 60d;
            var memoryUsed = 0L;
            var end = DateTime.Now + timeSpan;
            var memoryLogger = DateTime.Now + TimeSpan.FromMinutes(1);
            var isolatorLimitResult = new IsolatorLimitResult(TimeSpan.Zero, string.Empty);

            //Convert to bytes
            memoryCap *= 1024 * 1024;
            var spikeLimit = memoryCap*2;

            // give some granularity to the sleep interval if >= 1000ms
            var sleepGranularity = sleepIntervalMillis >= 1000 ? 5 : 1;
            var granularSleepIntervalMillis = sleepIntervalMillis / sleepGranularity;

            while (!task.IsCompleted && DateTime.Now < end)
            {
                // if over 80% allocation force GC then sample
                var sample = Convert.ToDouble(GC.GetTotalMemory(memoryUsed > memoryCap * 0.8));

                // find the EMA of the memory used to prevent spikes killing stategy
                memoryUsed = Convert.ToInt64((emaPeriod-1)/emaPeriod * memoryUsed + (1/emaPeriod)*sample);

                // if the rolling EMA > cap; or the spike is more than 2x the allocation.
                if (memoryUsed > memoryCap || sample > spikeLimit)
                {
                    message = $"Execution Security Error: Memory Usage Maxed Out - {PrettyFormatRam(memoryCap)}MB max, " +
                              $"with last sample of {PrettyFormatRam((long) sample)}MB.";
                    break;
                }

                if (DateTime.Now > memoryLogger)
                {
                    if (memoryUsed > memoryCap * 0.8)
                    {
                        Log.Error(Invariant($"Execution Security Error: Memory usage over 80% capacity. Sampled at {sample}"));
                    }

                    Log.Trace("Isolator.ExecuteWithTimeLimit(): " +
                              $"Used: {PrettyFormatRam(memoryUsed)}, " +
                              $"Sample: {PrettyFormatRam((long)sample)}, " +
                              $"App: {PrettyFormatRam(OS.ApplicationMemoryUsed * 1024 * 1024)}, " +
                              Invariant($"CurrentTimeStepElapsed: {isolatorLimitResult.CurrentTimeStepElapsed:mm':'ss'.'fff}"));

                    memoryLogger = DateTime.Now.AddMinutes(1);
                }

                // check to see if we're within other custom limits defined by the caller
                isolatorLimitResult = withinCustomLimits();
                if (!isolatorLimitResult.IsWithinCustomLimits)
                {
                    message = isolatorLimitResult.ErrorMessage;
                    break;
                }

                // for loop to give the sleep intervals some granularity
                for (int i = 0; i < sleepGranularity; i++)
                {
                    Thread.Sleep(granularSleepIntervalMillis);
                    if (task.IsCompleted)
                    {
                        break;
                    }
                }
            }

            if (task.IsCompleted == false && message == "")
            {
                message = $"Execution Security Error: Operation timed out - {timeSpan.TotalMinutes.ToStringInvariant()} minutes max. Check for recursive loops.";
                Log.Trace($"Isolator.ExecuteWithTimeLimit(): {message}");
            }

            if (message != "")
            {
                CancellationTokenSource.Cancel();
                Log.Error($"Security.ExecuteWithTimeLimit(): {message}");
                throw new TimeoutException(message);
            }
            return task.IsCompleted;
        }

        /// <summary>
        /// Execute a code block with a maximum limit on time and memory.
        /// </summary>
        /// <param name="timeSpan">Timeout in timespan</param>
        /// <param name="codeBlock">Action codeblock to execute</param>
        /// <param name="memoryCap">Maximum memory allocation, default 1024Mb</param>
        /// <param name="sleepIntervalMillis">Sleep interval between each check in ms</param>
        /// <param name="workerThread">The worker thread instance that will execute the provided action, if null
        /// will use a <see cref="Task"/></param>
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock, long memoryCap, int sleepIntervalMillis = 1000, WorkerThread workerThread = null)
        {
            return ExecuteWithTimeLimit(timeSpan, null, codeBlock, memoryCap, sleepIntervalMillis, workerThread);
        }

        /// <summary>
        /// Convert the bytes to a MB in double format for string display
        /// </summary>
        /// <param name="ramInBytes"></param>
        /// <returns></returns>
        private static string PrettyFormatRam(long ramInBytes)
        {
            return Math.Round(Convert.ToDouble(ramInBytes/(1024*1024))).ToStringInvariant();
        }
    }
}
