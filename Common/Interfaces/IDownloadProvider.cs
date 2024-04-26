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
using System.Collections.Generic;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Wrapper on the API for downloading data for an algorithm.
    /// </summary>
    public interface IDownloadProvider
    {
        /// <summary>
        /// Method for downloading data for an algorithm
        /// </summary>
        /// <param name="address">Source URL to download from</param>
        /// <param name="headers">Headers to pass to the site</param>
        /// <param name="userName">Username for basic authentication</param>
        /// <param name="password">Password for basic authentication</param>
        /// <returns>String contents of file</returns>
        string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password);

        /// <summary>
        /// Method for downloading data for an algorithm that can be read from a stream
        /// </summary>
        /// <param name="address">Source URL to download from</param>
        /// <param name="headers">Headers to pass to the site</param>
        /// <param name="userName">Username for basic authentication</param>
        /// <param name="password">Password for basic authentication</param>
        /// <returns>String contents of file</returns>
        byte[] DownloadBytes(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password);
    }
}
