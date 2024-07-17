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
 *
*/

using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
#pragma warning disable CA1010, CA1710 // Generic interface should also be implemented
#pragma warning disable CA1710 // Identifiers should have correct suffix

    /// <summary>
    /// Represents derivative symbols universe used in filtering.
    /// </summary>
    public interface IDerivativeSecurityFilterUniverse : IEnumerable
    {
    }

#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1010 // Generic interface should also be implemented

    /// <summary>
    /// Represents derivative symbols universe used in filtering.
    /// </summary>
    public interface IDerivativeSecurityFilterUniverse<T> : IDerivativeSecurityFilterUniverse, IEnumerable<T>
        where T : ISymbol
    {
    }
}
