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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a version of <see cref="SecurityMarginModel"/> which does not issue margin call orders
    /// </summary>
    public class NoMarginCallMarginModel : SecurityMarginModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoMarginCallMarginModel"/>. This margining model does not issue margin call orders.
        /// </summary>
        /// <param name="leverage">The leverage</param>
        public NoMarginCallMarginModel(decimal leverage)
            : base(leverage)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoMarginCallMarginModel"/>. This margining model does not issue margin call orders.
        /// </summary>
        /// <param name="initialMarginRequirement">The percentage of an order's absolute
        /// cost that must be held in free cash in order to place the order</param>
        /// <param name="maintenanceMarginRequirement">The percentage of the holding's absolute
        /// cost that must be held in free cash in order to avoid a margin call</param>
        public NoMarginCallMarginModel(decimal initialMarginRequirement, decimal maintenanceMarginRequirement)
            : base(initialMarginRequirement, maintenanceMarginRequirement)
        {

        }

        /// <summary>
        /// Prevents margin call orders to be issued
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The total margin used by the account in units of base currency</param>
        /// <returns>Null</returns>
        public override SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin)
        {
            return null;
        }
    }
}