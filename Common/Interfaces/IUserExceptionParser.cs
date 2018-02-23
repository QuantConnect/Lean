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

using QuantConnect.Exceptions;
using System;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// <see cref="IExceptionParser"/> interface. Parser that creates an <see cref="LegibleException"/> from an <see cref="Exception"/>.
    /// </summary>
    public interface IExceptionParser
    {
        /// <summary>
        /// Parses an <see cref="Exception"/> object into an <see cref="LegibleException"/> one
        /// </summary>
        /// <param name="exception"><see cref="Exception"/> object to parse into an <see cref="LegibleException"/> one.</param>
        /// <returns>Parsed exception</returns>
        Exception Parse(Exception exception);
    }
}