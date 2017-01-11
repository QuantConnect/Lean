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
using QuantConnect.Data;
using System.IO;
using QuantConnect.Lean.Engine.DataFeeds.Transport;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default file provider functionality that does not attempt to retrieve any data
    /// </summary>
    public class DefaultDataFileProvider : IDataFileProvider
    {
        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public IStreamReader Fetch(Symbol symbol, SubscriptionDataSource source, DateTime date, Resolution resolution, TickType tickType)
        {
            string entryName = null; // default to all entries
            var file = source.Source;
            var hashIndex = source.Source.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = source.Source.Substring(hashIndex + 1);
                file = source.Source.Substring(0, hashIndex);
            }

            if (!File.Exists(file))
            {
                return null;
            }

            // handles zip or text files
            return new LocalFileSubscriptionStreamReader(file, entryName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }
    }
}
