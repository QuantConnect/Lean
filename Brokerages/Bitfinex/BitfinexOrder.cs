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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Bitfinex order object with additional fields
    /// </summary>
    public class BitfinexOrder : MarketOrder
    {

        /// <summary>
        /// Amount executed
        /// </summary>
        public decimal ExecutedAmount { get; set; }

        /// <summary>
        /// Amount left to be executed
        /// </summary>
        public decimal RemainingAmount { get; set; }

        /// <summary>
        /// Full amount of trade
        /// </summary>
        public decimal OriginalAmount { get; set; }

    }
}
