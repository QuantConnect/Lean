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
#pragma warning disable 1591

namespace QuantConnect.Brokerages.Ccxt.Messages
{
    public class Order
    {
        public string Id { get; set; }
        public string ClientOrderId { get; set; }
        public DateTime? Datetime { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public string TimeInForce { get; set; }
        public bool? PostOnly { get; set; }
        public string Side { get; set; }
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal? Cost { get; set; }
        public decimal? Average { get; set; }
        public decimal? Filled { get; set; }
        public decimal? Remaining { get; set; }
        public string Status { get; set; }
        public TradeFee Fee { get; set; }
        public List<Trade> Trades { get; set; }
    }
}
