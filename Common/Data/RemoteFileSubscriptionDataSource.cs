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

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents the remote file source location for a subscription
    /// </summary>
    public class RemoteFileSubscriptionDataSource : SubscriptionDataSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteFileSubscriptionDataSource"/> class.
        /// </summary>
        public RemoteFileSubscriptionDataSource(string source, IEnumerable<KeyValuePair<string, string>> headers = null, FileFormat fileFormat = FileFormat.Csv)
#pragma warning disable CS0618 // Type or member is obsolete
            : base(source, SubscriptionTransportMedium.RemoteFile, fileFormat, (dataCacheProvider) => new Transport.RemoteFileSubscriptionStreamReader(dataCacheProvider, source, headers))
#pragma warning restore CS0618 // Type or member is obsolete
        {
        }
    }
}
