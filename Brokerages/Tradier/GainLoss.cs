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
    /// Gain loss parent class for deserialization
    /// </summary>
    public class TradierGainLossContainer
    {
        /// Profit Loss
        [JsonProperty(PropertyName = "gainloss")]
        public TradierGainLossClosed GainLossClosed;

        /// Null Constructor
        public TradierGainLossContainer()
        { }
    }

    /// <summary>
    /// Gain loss class
    /// </summary>
    public class TradierGainLossClosed
    {
        /// Array of user account details:
        [JsonProperty(PropertyName = "closed_position")]
        [JsonConverter(typeof(SingleValueListConverter<TradierGainLoss>))]
        public List<TradierGainLoss> ClosedPositions = new List<TradierGainLoss>();
    }

    /// <summary>
    /// Account only settings for a tradier user:
    /// </summary>
    public class TradierGainLoss 
    {
        /// Date the position was closed.
        [JsonProperty(PropertyName = "close_date")]
        public DateTime CloseDate;

        /// Date the position was opened
        [JsonProperty(PropertyName = "open_date")]
        public DateTime OpenDate;

        /// Total cost of the order.
        [JsonProperty(PropertyName = "cost")]
        public decimal Cost;

        /// Gain or loss on the position.
        [JsonProperty(PropertyName = "gain_loss")]
        public decimal GainLoss;

        /// Percentage of gain or loss on the position.
        [JsonProperty(PropertyName = "gain_loss_percent")]
        public decimal GainLossPercentage;

        /// Total amount received for the order.
        [JsonProperty(PropertyName = "proceeds")]
        public decimal Proceeds;

        /// Number of shares/contracts
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity;

        /// Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;

        /// Number of shares/contracts
        [JsonProperty(PropertyName = "term")]
        public decimal Term;

        /// <summary>
        /// Closed position trade summary
        /// </summary>
        public TradierGainLoss() 
        { }
    }

}
