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
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Bitfinex.Rest;
using QuantConnect.Logging;
using QuantConnect.Orders;
using RestSharp;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexBrokerage
    {
        #region Declarations

        private class OrderTypeMap
        {
            public string BitfinexOrderType { get; set; }
            public string Wallet { get; set; }
            public OrderType OrderType { get; set; }
        }

        private enum WalletType
        {
            exchange,
            trading
        }

        //todo: trailing stop support
        private static readonly List<OrderTypeMap> _orderTypeMap = new List<OrderTypeMap>
        {
            new OrderTypeMap { BitfinexOrderType = ExchangeMarket, Wallet = WalletType.exchange.ToString(), OrderType = OrderType.Market },
            new OrderTypeMap { BitfinexOrderType = ExchangeLimit, Wallet = WalletType.exchange.ToString(), OrderType = OrderType.Limit },
            new OrderTypeMap { BitfinexOrderType = ExchangeStop, Wallet = WalletType.exchange.ToString(), OrderType = OrderType.StopMarket },
            //{ new OrderTypeMap { BitfinexOrderType = "exchange trailing stop", Wallet = WalletType.exchange.ToString(), OrderType = OrderType.StopLimit } },

            new OrderTypeMap { BitfinexOrderType = Market, Wallet = WalletType.trading.ToString(), OrderType = OrderType.Market },
            new OrderTypeMap { BitfinexOrderType = Limit, Wallet = WalletType.trading.ToString(), OrderType = OrderType.Limit },
            new OrderTypeMap { BitfinexOrderType = Stop, Wallet = WalletType.trading.ToString(), OrderType = OrderType.StopMarket }
            //{ new OrderTypeMap { BitfinexOrderType = "trailing stop", Wallet = WalletType.trading.ToString(), OrderType = OrderType.StopLimit } },
        };

        #endregion

        /// <summary>
        /// Map exchange status
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static OrderStatus MapOrderStatus(OrderStatusResponse response)
        {
            decimal remainingAmount;
            decimal executedAmount = 0;
            if (response.IsCancelled)
            {
                return OrderStatus.Canceled;
            }
            else if (decimal.TryParse(response.RemainingAmount, out remainingAmount) && remainingAmount > 0
                     && decimal.TryParse(response.ExecutedAmount, out executedAmount) && executedAmount > 0)
            {
                return OrderStatus.PartiallyFilled;
            }
            else if (response.IsLive)
            {
                return OrderStatus.Submitted;
            }

            return OrderStatus.Invalid;
        }


        /// <summary>
        /// Map exchange order type
        /// </summary>
        /// <param name="orderType"></param>
        /// <returns></returns>
        public string MapOrderType(OrderType orderType)
        {
            var result = _orderTypeMap.Where(o => o.Wallet == _wallet && o.OrderType == orderType);

            if (result != null && result.Count() == 1)
            {
                return result.Single().BitfinexOrderType;
            }

            throw new Exception("Order type not supported: " + orderType);
        }


        /// <summary>
        /// Map exchange order type
        /// </summary>
        /// <param name="orderType"></param>
        /// <returns></returns>
        public OrderType MapOrderType(string orderType)
        {
            var result = _orderTypeMap.Where(o => o.Wallet == _wallet && o.BitfinexOrderType == orderType);

            if (result != null && result.Count() == 1)
            {
                return result.Single().OrderType;
            }

            throw new Exception("Order type not supported: " + orderType);
        }

        /// <summary>
        /// Creates authentication hash
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="apiSecret"></param>
        /// <returns></returns>
        public static string GetHexHashSignature(string payload, string apiSecret)
        {
            var hmac = new HMACSHA384(Encoding.UTF8.GetBytes(apiSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static decimal GetPrice(Order order)
        {
            if (order is StopMarketOrder)
            {
                return ((StopMarketOrder)order).StopPrice;
            }
            else if (order is LimitOrder)
            {
                return ((LimitOrder)order).LimitPrice;
            }

            return order.Price;
        }

        /// <summary>
        /// Determines whether or not the specified order will bring us across the zero line for holdings
        /// </summary>
        private static bool OrderCrossesZero(Order order, decimal quantity)
        {
            if (quantity > 0 && order.Quantity < 0)
            {
                return quantity + order.Quantity < 0;
            }
            else if (quantity < 0 && order.Quantity > 0)
            {
                return quantity + order.Quantity > 0;
            }
            return false;
        }

        private static void CheckForError(IRestResponse response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.BadRequest:
                    var errorMsgObj = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    Log.Trace("BitfinexBrokerage.CheckForError(): " + errorMsgObj.Message);
                    break;
                default:
                    Log.Trace("BitfinexBrokerage.CheckForError(): " + response.StatusCode + " - " + response.Content);
                    break;
            }
        }
    }
}