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
using QuantConnect.Scheduling;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Projects <see cref="ScheduledEventException"/> instances
    /// </summary>
    public class ScheduledEventExceptionProjection : IExceptionProjection
    {
        /// <summary>
        /// Determines if this projection should be applied to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be projected, false otherwise</returns>
        public bool CanProject(Exception exception) => exception?.GetType() == typeof(ScheduledEventException);

        /// <summary>
        /// Project the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be projected</param>
        /// <param name="innerProjection">A projection that should be applied to the inner exception.
        /// This provides a link back allowing the inner exception to be projected using the projections
        /// configured in the exception projector. Individual implementations *may* ignore this value if
        /// required.</param>
        /// <returns>The projected exception</returns>
        public Exception Project(Exception exception, IExceptionProjection innerProjection)
        {
            var see = (ScheduledEventException) exception;

            // prepend the scheduled event name
            var message = exception.Message;
            if (!message.Contains(see.ScheduledEventName))
            {
                message = $"In Scheduled Event '{see.ScheduledEventName}', {message}";
            }

            var inner = innerProjection.Project(see.InnerException, innerProjection);
            return new ScheduledEventException(see.ScheduledEventName, message, inner);
        }
    }
}