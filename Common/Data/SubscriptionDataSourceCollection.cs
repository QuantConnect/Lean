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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Data
{
    /// <summary>
    /// Subscription data source - which is actually returns a collection of data points with each GetSource call.
    /// </summary>
    /// <remarks>
    ///     Often data sources might return an array per request for efficiency e.g. a JSON request returning last 100 data points.
    /// </remarks>
    public class SubscriptionDataSourceCollection : SubscriptionDataSource
    {
        /// <summary>
        /// Identifies where to get the subscription's data from
        /// </summary>
        public virtual IEnumerable<string> Explode(string source, SubscriptionDataConfig config, DateTime date, bool isLive)
        {

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };
            var jsonArray = JsonConvert.DeserializeObject<List<JObject>>(source, jsonSerializerSettings);
            
            foreach (var item in jsonArray)
            {
                yield return item.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionDataSourceCollection"/> class.
        /// </summary>
        /// <param name="source">The subscription's data source location</param>
        /// <param name="transportMedium">The transport medium to be used to retrieve the subscription's data from the source</param>
        public SubscriptionDataSourceCollection(string source, SubscriptionTransportMedium transportMedium, FileFormat format) 
            : base (source, transportMedium, format)
        {

        }
    }
}