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
using System.ComponentModel.Composition;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Fetches a remote file for a security using a Symbol, Resolution and DateTime
    /// Used to get remote files when they are not found locally
    /// </summary>
    [InheritedExport(typeof(IDataFileProvider))]
    public interface IDataFileProvider
    {
        /// <summary>
        /// Gets and downloads the remote file
        /// </summary>
        /// <param name="symbol">Symbol of the security</param>
        /// <param name="resolution">Resolution of the data requested</param>
        /// <param name="date">DateTime of the data requested</param>
        /// <returns>Bool indicating whether the remote file was fetched correctly</returns>
        bool Fetch(Symbol symbol, Resolution resolution, DateTime date);
    }
}
