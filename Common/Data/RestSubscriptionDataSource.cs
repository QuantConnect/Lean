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
    /// Represents the source location for a rest call subscription
    /// </summary>
    public class RestSubscriptionDataSource : WebSubscriptionDataSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestSubscriptionDataSource"/> class.
        /// </summary>
        public RestSubscriptionDataSource(string source, bool isLiveMode, IEnumerable<KeyValuePair<string, string>> headers = null, bool returnsACollection = false)
            : base(_ => new Transport.RestSubscriptionStreamReader(source, headers, isLiveMode), source, returnsACollection ? FileFormat.Collection : FileFormat.Csv)
        {
        }
    }
}
