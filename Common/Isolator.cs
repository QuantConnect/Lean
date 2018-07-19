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
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public bool ExecuteWithTimeLimit(TimeSpan timeSpan, Func<string> withinCustomLimits, Action codeBlock, long memoryCap = 1024)
        {
            // default to always within custom limits
            withinCustomLimits = withinCustomLimits ?? (() => null);

            var message = "";
            var emaPeriod = 60d;
            var memoryUsed = 0L;
            var end = DateTime.Now + timeSpan;
            var memoryLogger = DateTime.Now + TimeSpan.FromMinutes(1);

            //Convert to bytes
            memoryCap *= 1024 * 1024;
            var spikeLimit = memoryCap*2;

            //Launch task
            var task = Task.Factory.StartNew(codeBlock, CancellationTokenSource.Token);

            while (!task.IsCompleted && DateTime.Now < end)
            {
                // if over 80% allocation force GC then sample
                var sample = OS.ApplicationMemoryUsed * 1024 * 1024;
                if (memoryUsed > memoryCap * 0.8)
                {
                    GC.Collect();
                }

                // find the EMA of the memory used to prevent spikes killing stategy
                memoryUsed = Convert.ToInt64((emaPeriod-1)/emaPeriod * memoryUsed + (1/emaPeriod)*sample);

                // if the rolling EMA > cap; or the spike is more than 2x the allocation.
                if (memoryUsed > memoryCap || sample > spikeLimit)
                {
                    message = "Execution Security Error: Memory Usage Maxed Out - " + PrettyFormatRam(memoryCap) + "MB max, with last sample of " + PrettyFormatRam((long)sample) + "MB.";
                    break;
                }

                if (DateTime.Now > memoryLogger)
                {
                    if (memoryUsed > memoryCap * 0.8)
                    {
                        Log.Error("Execution Security Error: Memory usage over 80% capacity. Sampled at {0}", sample);
                    }
                    Log.Trace("{0} Isolator.ExecuteWithTimeLimit(): Used: {1} Sample: {2}", DateTime.Now.ToString("u"), PrettyFormatRam(memoryUsed), PrettyFormatRam((long)sample));
                    memoryLogger = DateTime.Now.AddMinutes(1);
                }

                // check to see if we're within other custom limits defined by the caller
                var possibleMessage = withinCustomLimits();
                if (!string.IsNullOrEmpty(possibleMessage))
                {
                    message = possibleMessage;
                    break;
                }

                Thread.Sleep(1000);
            }

            if (task.IsCompleted == false && message == "")
            {
                message = "Execution Security Error: Operation timed out - " + timeSpan.TotalMinutes + " minutes max. Check for recursive loops.";
                Log.Trace("Isolator.ExecuteWithTimeLimit(): " + message);
            }

            if (message != "")
            {
                CancellationTokenSource.Cancel();
                Log.Error("Security.ExecuteWithTimeLimit(): " + message);
                throw new Exception(message);
            }
            return task.IsCompleted;
        }

        /// <summary>
        /// Execute a code block with a maximum limit on time and memory.
        /// </summary>
        /// <param name="timeSpan">Timeout in timespan</param>
        /// <param name="codeBlock">Action codeblock to execute</param>
        /// <param name="memoryCap">Maximum memory allocation, default 1024Mb</param>
        /// <returns>True if algorithm exited successfully, false if cancelled because it exceeded limits.</returns>
        public bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock, long memoryCap)
        {
            return ExecuteWithTimeLimit(timeSpan, null, codeBlock, memoryCap);
        }

        /// <summary>
        /// Convert the bytes to a MB in double format for string display
        /// </summary>
        /// <param name="ramInBytes"></param>
        /// <returns></returns>
        private static double PrettyFormatRam(long ramInBytes)
        {
            return Math.Round(Convert.ToDouble(ramInBytes/(1024*1024)));
        }
    }
}
