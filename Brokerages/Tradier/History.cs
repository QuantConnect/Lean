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
    /// Tradier deserialization container for history
    /// </summary>
    public class TradierEventContainer
    {
        /// Event Contents:
        [JsonProperty(PropertyName = "history")]
        public TradierEvents TradierEvents;

        /// Default constructor for json serialization
        public TradierEventContainer()
        { }
    }

    /// <summary>
    /// Events array container.
    /// </summary>
    public class TradierEvents 
    { 
        /// Events List:
        [JsonProperty(PropertyName = "event")]
        [JsonConverter(typeof(SingleValueListConverter<TradierEvent>))]
        public List<TradierEvent> Events;

        /// Default Constructor for JSON
        public TradierEvents()
        { }
    }

    /// <summary>
    /// Tradier event model:
    /// </summary>
    public class TradierEvent
    { 
        /// Tradier Event: Amount
        [JsonProperty(PropertyName = "amount")]
        public decimal Amount;

        /// Tradier Event: Date
        [JsonProperty(PropertyName = "date")]
        public DateTime Date;

        /// Tradier Event: Type
        [JsonProperty(PropertyName = "type")]
        public TradierEventType Type;

        /// Tradier Event: TradeEvent
        [JsonProperty(PropertyName = "trade")]
        public TradierTradeEvent TradeEvent;

        /// Tradier Event: Journal Event
        [JsonProperty(PropertyName = "journal")]
        public TradierJournalEvent JournalEvent;

        /// Tradier Event: Option Event
        [JsonProperty(PropertyName = "option")]
        public TradierOptionEvent OptionEvent;

        /// Tradier Event: Dividend Event
        [JsonProperty(PropertyName = "dividend")]
        public TradierOptionEvent DividendEvent;
    }

    /// <summary>
    /// Common base class for events detail information:
    /// </summary>
    public class TradierEventDetail 
    {
        /// Tradier Event: Description
        [JsonProperty(PropertyName = "description")]
        public string Description;

        /// Tradier Event: Quantity
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity;
        
        /// Empty Constructor
        public TradierEventDetail()
        {  }
    }

    /// <summary>
    /// Trade event in history for tradier:
    /// </summary>
    public class TradierTradeEvent : TradierEventDetail
    {
        /// Tradier Event: Comission
        [JsonProperty(PropertyName = "commission")]
        public decimal Commission;

        /// Tradier Event: Price
        [JsonProperty(PropertyName = "price")]
        public decimal Price;

        /// Tradier Event: Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;

        /// Tradier Event: Trade Type
        [JsonProperty(PropertyName = "trade_type")]
        public TradierTradeType TradeType;

        /// Empty constructor
        public TradierTradeEvent()
        { }
    }

    /// <summary>
    /// Journal event in history:
    /// </summary>
    public class TradierJournalEvent : TradierEventDetail
    {
        ///
        public TradierJournalEvent() 
        { }
    }

    /// <summary>
    /// Dividend event in history:
    /// </summary>
    public class TradierDividendEvent : TradierEventDetail
    {
        ///
        public TradierDividendEvent()
        { }
    }

    /// <summary>
    /// Option event record in history:
    /// </summary>
    public class TradierOptionEvent : TradierEventDetail
    {
        ///
        [JsonProperty(PropertyName = "option_type")]
        public TradierOptionStatus Type;
        ///
        public TradierOptionEvent()
        { }
    }
    
}
