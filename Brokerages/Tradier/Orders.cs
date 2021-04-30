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
    /// Order parent class for deserialization
    /// </summary>
    public class TradierOrdersContainer
    {
        /// Orders Contents:
        [JsonProperty(PropertyName = "orders")]
        [JsonConverter(typeof(NullStringValueConverter<TradierOrders>))]
        public TradierOrders Orders;

        /// Constructor: Orders parent:
        public TradierOrdersContainer()
        { }
    }

    /// <summary>
    /// Order container class
    /// </summary>
    public class TradierOrders
    {
        /// Array of user account details:
        [JsonProperty(PropertyName = "order")]
        [JsonConverter(typeof(SingleValueListConverter<TradierOrder>))]
        public List<TradierOrder> Orders = new List<TradierOrder>();

        /// Null Constructor:
        public TradierOrders()
        { }
    }

    /// <summary>
    /// Intraday or pending order for user
    /// </summary>
    public class TradierOrder
    {
        /// Unique order id.
        [JsonProperty(PropertyName = "id")]
        public long Id;

        /// Market, Limit Order etc.
        [JsonProperty(PropertyName = "type")]
        public TradierOrderType Type;

        /// Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;

        /// Option symbol
        [JsonProperty(PropertyName = "option_symbol")]
        public string OptionSymbol;

        ///Long short.
        [JsonProperty(PropertyName = "side")]
        public TradierOrderDirection Direction;

        /// Quantity
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity;

        /// Status of the order (filled, canceled, open, expired, rejected, pending, partially_filled, submitted).
        [JsonProperty(PropertyName = "status")]
        public TradierOrderStatus Status;

        /// Duration of the order (day, gtc)
        [JsonProperty(PropertyName = "duration")]
        public TradierOrderDuration Duration;

        /// Percentage of gain or loss on the position.
        [JsonProperty(PropertyName = "price")]
        public decimal Price;

        /// Average fill price
        [JsonProperty(PropertyName = "avg_fill_price")]
        public decimal AverageFillPrice;

        /// Quantity executed
        [JsonProperty(PropertyName = "exec_quantity")]
        public decimal QuantityExecuted;

        /// Last fill price
        [JsonProperty(PropertyName = "last_fill_price")]
        public decimal LastFillPrice;

        /// Last amount filled
        [JsonProperty(PropertyName = "last_fill_quantity")]
        public decimal LastFillQuantity;

        /// Quantity Remaining in Order.
        [JsonProperty(PropertyName = "remaining_quantity")]
        public decimal RemainingQuantity;

        /// Date order was created.
        [JsonProperty(PropertyName = "create_date")]
        public DateTime CreatedDate;

        /// Date order was created.
        [JsonProperty(PropertyName = "transaction_date")]
        public DateTime TransactionDate;

        ///Classification of order (equity, option, multileg, combo)
        [JsonProperty(PropertyName = "class")]
        public TradierOrderClass Class;

        ///The number of legs
        [JsonProperty(PropertyName = "num_legs")]
        public int NumberOfLegs;

        /// Numberof legs in order
        [JsonProperty(PropertyName = "leg")]
        public List<TradierOrderLeg> Legs;

        /// Closed position trade summary
        public TradierOrder()
        { }
    }

    /// <summary>
    /// Detailed order parent class
    /// </summary>
    public class TradierOrderDetailedContainer
    {
        /// Details of the order
        [JsonProperty(PropertyName = "order")]
        public TradierOrderDetailed DetailedOrder;
    }


    /// <summary>
    /// Deserialization wrapper for order response:
    /// </summary>
    public class TradierOrderResponse
    {
        /// Tradier Order information
        [JsonProperty(PropertyName = "order")]
        public TradierOrderResponseOrder Order = new TradierOrderResponseOrder();

        /// Errors in request
        [JsonProperty(PropertyName = "errors")]
        public TradierOrderResponseError Errors = new TradierOrderResponseError();
    }

    /// <summary>
    /// Errors result from an order request.
    /// </summary>
    public class TradierOrderResponseError
    {
        /// List of errors
        [JsonProperty(PropertyName = "error")]
        [JsonConverter(typeof(SingleValueListConverter<string>))]
        public List<string> Errors;
    }

    /// <summary>
    /// Order response when purchasing equity.
    /// </summary>
    public class TradierOrderResponseOrder
    {
        /// id or order response
        [JsonProperty(PropertyName = "id")]
        public long Id;

        /// Partner id - me
        [JsonProperty(PropertyName = "partner_id")]
        public string PartnerId;

        /// Status of order
        [JsonProperty(PropertyName = "status")]
        public string Status;
    }

    /// <summary>
    /// Detailed order type.
    /// </summary>
    public class TradierOrderDetailed : TradierOrder
    {
        /// Order exchange
        [JsonProperty(PropertyName = "exch")]
        public string Exchange;

        /// Executed Exchange
        [JsonProperty(PropertyName = "exec_exch")]
        public string ExecutionExchange;

        /// Option type
        [JsonProperty(PropertyName = "option_type")]
        public TradierOptionType OptionType;

        /// Expiration date
        [JsonProperty(PropertyName = "expiration_date")]
        public DateTime OptionExpirationDate;

        /// Stop Price
        [JsonProperty(PropertyName = "stop_price")]
        public decimal StopPrice;
    }

    /// <summary>
    /// Leg of a tradier order:
    /// </summary>
    public class TradierOrderLeg
    {
        /// Date order was created.
        [JsonProperty(PropertyName = "type")]
        public TradierOrderType Type;

        /// Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;

        ///Long short.
        [JsonProperty(PropertyName = "side")]
        public TradierOrderDirection Direction;

        /// Quantity
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity;

        /// Status of the order (filled, canceled, open, expired, rejected, pending, partially_filled, submitted).
        [JsonProperty(PropertyName = "status")]
        public TradierOrderStatus Status;

        /// Duration of the order (day, gtc)
        [JsonProperty(PropertyName = "duration")]
        public TradierOrderDuration Duration;

        /// Percentage of gain or loss on the position.
        [JsonProperty(PropertyName = "price")]
        public decimal Price;

        /// Average fill price
        [JsonProperty(PropertyName = "avg_fill_price")]
        public decimal AverageFillPrice;

        /// Quantity executed
        [JsonProperty(PropertyName = "exec_quantity")]
        public decimal QuantityExecuted;

        /// Last fill price
        [JsonProperty(PropertyName = "last_fill_price")]
        public decimal LastFillPrice;

        /// Last amount filled
        [JsonProperty(PropertyName = "last_fill_quantity")]
        public decimal LastFillQuantity;

        /// Quantity Remaining in Order.
        [JsonProperty(PropertyName = "remaining_quantity")]
        public decimal RemainingQuantity;

        /// Date order was created.
        [JsonProperty(PropertyName = "create_date")]
        public DateTime CreatedDate;

        /// Date order was created.
        [JsonProperty(PropertyName = "transaction_date")]
        public DateTime TransacionDate;

        /// Constructor
        public TradierOrderLeg()
        { }
    }

}
