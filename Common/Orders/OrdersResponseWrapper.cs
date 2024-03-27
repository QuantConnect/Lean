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

using Newtonsoft.Json;
using QuantConnect.Api;
using System.Collections.Generic;
using QuantConnect.Orders.Serialization;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Collection container for a list of orders for a project
    /// </summary>
    public class OrdersResponseWrapper : RestResponse
    {
        /// <summary>
        /// Returns the total order collection length, not only the amount we are sending here
        /// </summary>
        [JsonProperty(PropertyName = "length")]
        public int Length { get; set; }

        /// <summary>
        /// Collection of summarized Orders objects
        /// </summary>
        [JsonProperty(PropertyName = "orders")]
        public List<ApiOrderResponse> Orders { get; set; } = new();
    }

    /// <summary>
    /// Api order and order events reponse
    /// </summary>
    [JsonConverter(typeof(ReadOrdersResponseJsonConverter))]
    public class ApiOrderResponse
    {
        /// <summary>
        /// The symbol associated with this order
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The order
        /// </summary>
        public Order Order { get; set; }

        /// <summary>
        /// The order events
        /// </summary>
        public List<SerializedOrderEvent> Events { get; set; }

        public ApiOrderResponse()
        {
        }

        public ApiOrderResponse(Order order, List<SerializedOrderEvent> events, Symbol symbol)
        {
            Order = order;
            Events = events;
            Symbol = symbol;
        }
    }
}
