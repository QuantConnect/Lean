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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Manager of a group of orders
    /// </summary>
    public class GroupOrderManager
    {
        /// <summary>
        /// The unique order group Id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public long Id { get; }

        /// <summary>
        /// The group order quantity
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity { get; internal set; }

        /// <summary>
        /// The total order count associated with this order group
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count { get; }

        /// <summary>
        /// The limit price associated with this order group if any
        /// </summary>
        [JsonProperty(PropertyName = "limitPrice")]
        public decimal LimitPrice { get; set; }

        /// <summary>
        /// The order Ids in this group
        /// </summary>
        /// <remarks>In live trading we process orders in a dedicated thread so we need to be thread safe</remarks>
        [JsonProperty(PropertyName = "orderIds")]
        public HashSet<int> OrderIds { get; }

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        [JsonProperty(PropertyName = "direction")]
        public OrderDirection Direction
        {
            get
            {
                if (Quantity > 0)
                {
                    return OrderDirection.Buy;
                }
                if (Quantity < 0)
                {
                    return OrderDirection.Sell;
                }
                return OrderDirection.Hold;
            }
        }

        /// <summary>
        /// Get the absolute quantity for this combo order
        /// </summary>
        [JsonIgnore]
        public decimal AbsoluteQuantity => Math.Abs(Quantity);

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public GroupOrderManager()
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="id">This order group unique Id</param>
        /// <param name="legCount">The order leg count</param>
        /// <param name="quantity">The group order quantity</param>
        /// <param name="limitPrice">The limit price associated with this order group if any</param>
        public GroupOrderManager(int id, int legCount, decimal quantity, decimal limitPrice = 0)
        {
            Id = id;
            Count = legCount;
            Quantity = quantity;
            LimitPrice = limitPrice;
            OrderIds = new (capacity: legCount);
        }
    }
}
