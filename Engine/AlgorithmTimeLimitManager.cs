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
        private volatile bool _stopped;
        private DateTime _currentTimeStepTime;
        private readonly TimeSpan _timeLoopMaximum;

        /// <summary>
        /// Initializes a new instance of <see cref="AlgorithmTimeLimitManager"/> to manage the
        /// creation of <see cref="IsolatorLimitResult"/> instances as it pertains to the
        /// algorithm manager's time loop
        /// </summary>
        /// <param name="timeLoopMaximum">Specifies the maximum amount of time the algorithm is permitted to
        /// spend in a single time loop. This value can be overriden if certain actions are taken by the
        /// algorithm, such as invoking the training methods.</param>
        public AlgorithmTimeLimitManager(TimeSpan timeLoopMaximum)
        {
            _timeLoopMaximum = timeLoopMaximum;
        }

        /// <summary>
        /// Gets the current amount of time that has elapsed since the beginning of the
        /// most recent algorithm manager time loop
        /// </summary>
        public TimeSpan CurrentTimeStepElapsed
        {
            get
            {
                if (_currentTimeStepTime == DateTime.MinValue)
                {
                    _currentTimeStepTime = DateTime.UtcNow;
                    return TimeSpan.Zero;
                }

                return DateTime.UtcNow - _currentTimeStepTime;
            }
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
            // maintains existing implementation behavior to reset the time to min value and then
            // when the isolator pings IsWithinLimit, invocation of CurrentTimeStepElapsed will cause
            // it to update to the current time. IIRC, this was done to avoid a potential race
            _currentTimeStepTime = DateTime.MinValue;
        }

        /// <summary>
        /// Stops this instance from tracking the algorithm manager's time loop elapsed time.
        /// This is invoked at the end of the algorithm to prevent the isolator from terminating
        /// the algorithm during final clean up and shutdown.
        /// </summary>
        public void StopEnforcingTimeLimit()
        {
            _stopped = true;
        }

        /// <summary>
        /// Determines whether or not the algorithm time loop is considered within the limits
        /// </summary>
        public IsolatorLimitResult IsWithinLimit()
        {
            if (_stopped)
            {
                return new IsolatorLimitResult(TimeSpan.Zero, string.Empty);
            }

            var message = string.Empty;
            if (CurrentTimeStepElapsed > _timeLoopMaximum)
            {
                message = $"Algorithm took longer than {_timeLoopMaximum.TotalMinutes} minutes on a single time loop.";
            }

            return new IsolatorLimitResult(CurrentTimeStepElapsed, message);
        }
    }
}