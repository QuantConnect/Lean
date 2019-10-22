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
using QuantConnect.Logging;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Provides an implementation of <see cref="IIsolatorLimitResultProvider"/> that tracks the algorithm
    /// manager's time loops and enforces a maximum amount of time that each time loop may take to execute.
    /// The isolator uses the result provided by <see cref="IsWithinLimit"/> to determine if it should
    /// terminate the algorithm for violation of the imposed limits.
    /// </summary>
    public class AlgorithmTimeLimitManager : IIsolatorLimitResultProvider
    {
        private volatile bool _failed;
        private volatile bool _stopped;
        private long _additionalMinutes;

        private DateTime _currentTimeStepTime;
        private readonly TimeSpan _timeLoopMaximum;

        /// <summary>
        /// Gets the additional time bucket which is responsible for tracking additional time requested
        /// for processing via long-running scheduled events. In LEAN, we use the <see cref="LeakyBucket"/>
        /// </summary>
        public ITokenBucket AdditionalTimeBucket { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AlgorithmTimeLimitManager"/> to manage the
        /// creation of <see cref="IsolatorLimitResult"/> instances as it pertains to the
        /// algorithm manager's time loop
        /// </summary>
        /// <param name="additionalTimeBucket">Provides a bucket of additional time that can be requested to be
        /// spent to give execution time for things such as training scheduled events</param>
        /// <param name="timeLoopMaximum">Specifies the maximum amount of time the algorithm is permitted to
        /// spend in a single time loop. This value can be overriden if certain actions are taken by the
        /// algorithm, such as invoking the training methods.</param>
        public AlgorithmTimeLimitManager(ITokenBucket additionalTimeBucket, TimeSpan timeLoopMaximum)
        {
            _timeLoopMaximum = timeLoopMaximum;
            AdditionalTimeBucket = additionalTimeBucket;
        }

        /// <summary>
        /// Invoked by the algorithm at the start of each time loop. This resets the current time step
        /// elapsed time.
        /// </summary>
        /// <remarks>
        /// This class is the result of a mechanical refactor with the intention of preserving all existing
        /// behavior, including setting the <code>_currentTimeStepTime</code> to <see cref="DateTime.MinValue"/>
        /// </remarks>
        public void StartNewTimeStep()
        {
            if (_stopped)
            {
                throw new InvalidOperationException("The AlgorithmTimeLimitManager may not be stopped and restarted.");
            }

            // maintains existing implementation behavior to reset the time to min value and then
            // when the isolator pings IsWithinLimit, invocation of CurrentTimeStepElapsed will cause
            // it to update to the current time. This was done as a performance improvement and moved
            // accessing DateTime.UtcNow from the algorithm manager thread to the isolator thread
            _currentTimeStepTime = DateTime.MinValue;
            Interlocked.Exchange(ref _additionalMinutes, 0L);
        }

        /// <summary>
        /// Stops this instance from tracking the algorithm manager's time loop elapsed time.
        /// This is invoked at the end of the algorithm to prevent the isolator from terminating
        /// the algorithm during final clean up and shutdown.
        /// </summary>
        internal void StopEnforcingTimeLimit()
        {
            _stopped = true;
        }

        /// <summary>
        /// Determines whether or not the algorithm time loop is considered within the limits
        /// </summary>
        public IsolatorLimitResult IsWithinLimit()
        {
            TimeSpan currentTimeStepElapsed;
            var message = IsOutOfTime(out currentTimeStepElapsed) ? GetErrorMessage(currentTimeStepElapsed) : string.Empty;
            return new IsolatorLimitResult(currentTimeStepElapsed, message);
        }

        /// <summary>
        /// Requests additional time to continue executing the current time step.
        /// At time of writing, this is intended to be used to provide training scheduled events
        /// additional time to allow complex training models time to execute while also preventing
        /// abuse by enforcing certain control parameters set via the job packet.
        ///
        /// Each time this method is invoked, this time limit manager will increase the allowable
        /// execution time by the specified number of whole minutes
        /// </summary>
        public void RequestAdditionalTime(int minutes)
        {
            if (!TryRequestAdditionalTime(minutes))
            {
                _failed = true;
                Log.Debug($"AlgorithmTimeLimitManager.RequestAdditionalTime({minutes}): Failed to acquire additional time. Marking failed.");
            }
        }

        /// <summary>
        /// Attempts to requests additional time to continue executing the current time step.
        /// At time of writing, this is intended to be used to provide training scheduled events
        /// additional time to allow complex training models time to execute while also preventing
        /// abuse by enforcing certain control parameters set via the job packet.
        ///
        /// Each time this method is invoked, this time limit manager will increase the allowable
        /// execution time by the specified number of whole minutes
        /// </summary>
        public bool TryRequestAdditionalTime(int minutes)
        {
            Log.Debug($"AlgorithmTimeLimitManager.TryRequestAdditionalTime({minutes}): Requesting additional time. Available: {AdditionalTimeBucket.AvailableTokens}");

            // safely attempts to consume from the bucket, returning false if insufficient resources available
            if (AdditionalTimeBucket.TryConsume(minutes))
            {
                var newValue = Interlocked.Add(ref _additionalMinutes, minutes);
                Log.Debug($"AlgorithmTimeLimitManager.TryRequestAdditionalTime({minutes}): Success: AdditionalMinutes: {newValue}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether or not the algorithm should be terminated due to exceeding the time limits
        /// </summary>
        private bool IsOutOfTime(out TimeSpan currentTimeStepElapsed)
        {
            if (_stopped)
            {
                currentTimeStepElapsed = TimeSpan.Zero;
                return false;
            }

            currentTimeStepElapsed = GetCurrentTimeStepElapsed();
            if (_failed)
            {
                return true;
            }

            var additionalMinutes = TimeSpan.FromMinutes(Interlocked.Read(ref _additionalMinutes));
            return currentTimeStepElapsed > _timeLoopMaximum.Add(additionalMinutes);
        }

        /// <summary>
        /// Gets the current amount of time that has elapsed since the beginning of the
        /// most recent algorithm manager time loop
        /// </summary>
        private TimeSpan GetCurrentTimeStepElapsed()
        {
            if (_currentTimeStepTime == DateTime.MinValue)
            {
                _currentTimeStepTime = DateTime.UtcNow;
                return TimeSpan.Zero;
            }

            return DateTime.UtcNow - _currentTimeStepTime;
        }

        private string GetErrorMessage(TimeSpan currentTimeStepElapsed)
        {
            var message = $"Algorithm took longer than {_timeLoopMaximum.TotalMinutes} minutes on a single time loop.";

            var minutesAboveStandardLimit = _additionalMinutes - (int) _timeLoopMaximum.TotalMinutes;
            if (minutesAboveStandardLimit > 0)
            {
                message = $"{message} An additional {minutesAboveStandardLimit} minutes were also allocated and consumed.";
            }

            message = $"{message} CurrentTimeStepElapsed: {currentTimeStepElapsed.TotalMinutes:0.0} minutes";

            return message;
        }
    }
}