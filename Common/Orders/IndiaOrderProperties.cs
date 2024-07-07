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

using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Contains additional properties and settings for an order submitted to Indian Brokerages
    /// </summary>
    public class IndiaOrderProperties : OrderProperties
    {
        /// <summary>
        /// India product type
        /// </summary>
        public string ProductType { get; }

        /// <summary>
        /// Define the India Order type that we are targeting (MIS/CNC/NRML).
        /// </summary>
        public enum IndiaProductType
        {
            /// <summary>
            /// Margin Intraday Square Off (0)
            /// </summary>
            MIS,

            /// <summary>
            /// Cash and Carry (1)
            /// </summary>
            CNC,

            /// <summary>
            /// Normal (2)
            /// </summary>
            NRML
        }

        /// <summary>
        /// Initialize a new OrderProperties for <see cref="IndiaOrderProperties"/>
        /// </summary>
        /// <param name="exchange">Exchange value, nse/bse etc</param>
        public IndiaOrderProperties(Exchange exchange)
            : base(exchange) { }

        /// <summary>
        /// Initialize a new OrderProperties for <see cref="IndiaOrderProperties"/>
        /// </summary>
        /// <param name="exchange">Exchange value, nse/bse etc</param>
        /// <param name="productType">ProductType value, MIS/CNC/NRML etc</param>
        public IndiaOrderProperties(Exchange exchange, IndiaProductType productType)
            : base(exchange)
        {
            ProductType = productType.ToString();
        }

        /// <summary>
        /// Returns a new instance clone of this object
        /// </summary>
        public override IOrderProperties Clone()
        {
            return (IndiaOrderProperties)MemberwiseClone();
        }
    }
}
