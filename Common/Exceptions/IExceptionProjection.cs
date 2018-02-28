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
using QuantConnect.Interfaces;

namespace QuantConnect.Exceptions
{
    /// <summary>
    /// Defines an exception projection. Projections are invoked on <see cref="IAlgorithm.RunTimeError"/>
    /// </summary>
    public interface IExceptionProjection
    {
        /// <summary>
        /// Determines if this projection should be applied to the specified exception.
        /// </summary>
        /// <param name="exception">The exception to check</param>
        /// <returns>True if the exception can be projected, false otherwise</returns>
        bool CanProject(Exception exception);

        /// <summary>
        /// Project the specified exception into a new exception
        /// </summary>
        /// <param name="exception">The exception to be projected</param>
        /// <param name="innerProjection">A projection that should be applied to the inner exception.
        /// This provides a link back allowing the inner exception to be projected using the projections
        /// configured in the exception projector. Individual implementations *may* ignore this value if
        /// required.</param>
        /// <returns>The projected exception</returns>
        Exception Project(Exception exception, IExceptionProjection innerProjection);
    }
}