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
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TradingApi.ModelObjects.Bitfinex.Json;

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

        enum WalletType
        {
            exchange,
            trading
        }

        //todo: trailing stop support
        private static List<OrderTypeMap> _orderTypeMap = new List<OrderTypeMap>
        {
            { new OrderTypeMap { BitfinexOrderType = _exchangeMarket, Wallet = WalletType.exchange.ToString(), OrderType = OrderType.Market } },
            { new OrderTypeMap { BitfinexOrderType = _exchangeLimit, Wallet = WalletType.exchange.ToString(), OrderType = OrderType.Limit } },
            { new OrderTypeMap { BitfinexOrderType = _exchangeStop, Wallet = WalletType.exchange.ToString(), OrderType = OrderType.StopMarket } },
            //{ new OrderTypeMap { BitfinexOrderType = "exchange trailing stop", Wallet = WalletType.exchange.ToString(), OrderType = OrderType.StopLimit } },

            { new OrderTypeMap { BitfinexOrderType = _market, Wallet = WalletType.trading.ToString(), OrderType = OrderType.Market } },
            { new OrderTypeMap { BitfinexOrderType = _limit, Wallet = WalletType.trading.ToString(), OrderType = OrderType.Limit } },
            { new OrderTypeMap { BitfinexOrderType = _stop, Wallet = WalletType.trading.ToString(), OrderType = OrderType.StopMarket } },
            //{ new OrderTypeMap { BitfinexOrderType = "trailing stop", Wallet = WalletType.trading.ToString(), OrderType = OrderType.StopLimit } },
        };
        #endregion


        /// <summary>
        /// Map exchange status
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static OrderStatus MapOrderStatus(BitfinexOrderStatusResponse response)
        {
            decimal remainingAmount;
            decimal executedAmount;
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
            var result = _orderTypeMap.Where(o => o.Wallet == Wallet && o.OrderType == orderType);

            if (result != null && result.Count() == 1)
            {
                return result.Single().BitfinexOrderType;
            }

            throw new Exception("Order type not supported: " + orderType.ToString());
        }


        /// <summary>
        /// Map exchange order type
        /// </summary>
        /// <param name="orderType"></param>
        /// <returns></returns>
        public OrderType MapOrderType(string orderType)
        {
            var result = _orderTypeMap.Where(o => o.Wallet == Wallet && o.BitfinexOrderType == orderType);

            if (result != null && result.Count() == 1)
            {
                return result.Single().OrderType;
            }

            throw new Exception("Order type not supported: " + orderType.ToString());
        }

        /// <summary>
        /// Map exchange order status
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public OrderStatus MapOrderStatus(TradeMessage msg)
        {
            if (Math.Abs(msg.FEE) != 0)
            {
                var cached = this.CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(msg.TRD_ORD_ID.ToString())).FirstOrDefault();
                if (cached.Value != null)
                {
                    if (msg.TRD_ORD_ID == cached.Value.BrokerId.Select(b => int.Parse(b)).Max(b => b))
                    {
                        return OrderStatus.Filled;
                    }
                    else
                    {
                        return OrderStatus.PartiallyFilled;
                    }
                }
                return OrderStatus.Filled;
            }

            return OrderStatus.PartiallyFilled;
        }

        /// <summary>
        /// Creates authentication hash
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="apiSecret"></param>
        /// <returns></returns>
        protected string GetHexHashSignature(string payload, string apiSecret)
        {
            HMACSHA384 hmac = new HMACSHA384(Encoding.UTF8.GetBytes(apiSecret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
