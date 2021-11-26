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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Specifies the data in an order to be updated
    /// </summary>
    public class UpdateOrderFields
    {
        /// <summary>
        /// Specify to update the quantity of the order
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Specify to update the limit price of the order
        /// </summary>
        public decimal? LimitPrice { get; set; }

        /// <summary>
        /// Specify to update the stop price of the order
        /// </summary>
        public decimal? StopPrice { get; set; }
        
        /// <summary>
        /// Specify to update the trigger price of the order
        /// </summary>
        public decimal? TriggerPrice { get; set; }
        
        /// <summary>
        /// Specify to update the order's tag
        /// </summary>
        public string Tag { get; set; }
    }
}