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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Empty class for deserializing positions held.
    /// </summary>
    public class TradierPositionsContainer
    {
        /// Positions Class
        [JsonProperty(PropertyName = "positions")]
        [JsonConverter(typeof(NullStringValueConverter<TradierPositions>))]
        public TradierPositions TradierPositions;

        /// Default Constructor:
        public TradierPositionsContainer()
        { }
    }

    /// <summary>
    /// Position array container.
    /// </summary>
    public class TradierPositions 
    { 
        /// Positions Class List
        [JsonProperty(PropertyName = "position")]
        [JsonConverter(typeof(SingleValueListConverter<TradierPosition>))]
        public List<TradierPosition> Positions;

        /// Default Constructor for JSON
        public TradierPositions()
        { }
    }


    /// <summary>
    /// Individual Tradier position model.
    /// </summary>
    public class TradierPosition
    { 
        /// Position Id
        [JsonProperty(PropertyName = "id")]
        public long Id;

        /// Postion Date Acquired,
        [JsonProperty(PropertyName = "date_acquired")]
        public DateTime DateAcquired;

        /// Position Quantity
        [JsonProperty(PropertyName = "quantity")]
        public long Quantity;

        /// Position Cost:
        [JsonProperty(PropertyName = "cost_basis")]
        public decimal CostBasis;

        ///Position Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;
    }

}
