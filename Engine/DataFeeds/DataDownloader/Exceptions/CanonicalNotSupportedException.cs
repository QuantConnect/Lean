/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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

namespace QuantConnect.Lean.Engine.DataFeeds.DataDownloader.Exceptions
{
    /// <summary>
    /// Exception thrown when a data downloader does not support canonical symbol resolution.
    /// </summary>
    public class CanonicalNotSupportedException : NotSupportedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CanonicalNotSupportedException"/> class
        /// with a specified canonical symbol and data provider name.
        /// </summary>
        /// <param name="symbol">The canonical symbol that is not supported.</param>
        /// <param name="providerName">The name of the data provider that does not support canonical symbols.</param>
        public CanonicalNotSupportedException(Symbol symbol, string providerName)
            : base($"The '{providerName}' data downloader does not support canonical symbol '{symbol}'. Use a concrete contract symbol or wrap with CanonicalDataDownloaderDecorator.")
        {
        }
    }
}
