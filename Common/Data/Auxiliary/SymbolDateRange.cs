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

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents security identifier within a date range.
    /// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public readonly struct SymbolDateRange
    {
        /// <summary>
        /// Represents a unique security identifier.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Ticker Start Date Time in Local
        /// </summary>
        public DateTime StartDateTimeLocal { get; }

        /// <summary>
        /// Ticker End Date Time in Local
        /// </summary>
        public DateTime EndDateTimeLocal { get; }

        /// <summary>
        /// Create the instance of <see cref="SymbolDateRange"/> struct.
        /// </summary>
        /// <param name="symbol">The unique security identifier</param>
        /// <param name="startDateTimeLocal">Start Date Time Local</param>
        /// <param name="endDateTimeLocal">End Date Time Local</param>
        public SymbolDateRange(Symbol symbol, DateTime startDateTimeLocal, DateTime endDateTimeLocal)
        {
            Symbol = symbol;
            StartDateTimeLocal = startDateTimeLocal;
            EndDateTimeLocal = endDateTimeLocal;
        }
    }
#pragma warning restore CA1815
}
