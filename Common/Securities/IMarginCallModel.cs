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

using System.Collections.Generic;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for picking which orders should be executed during a margin call
    /// </summary>
    public interface IMarginCallModel
    {
        /// <summary>
        /// Scan the portfolio and the updated data for a potential margin call situation which may get the holdings below zero!
        /// If there is a margin call, liquidate the portfolio immediately before the portfolio gets sub zero.
        /// </summary>
        /// <param name="issueMarginCallWarning">Set to true if a warning should be issued to the algorithm</param>
        /// <returns>True for a margin call on the holdings.</returns>
        List<SubmitOrderRequest> GetMarginCallOrders(out bool issueMarginCallWarning);

            /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The totl margin used by the account in units of base currency</param>
        /// <param name="maintenanceMarginRequirement">The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin, decimal maintenanceMarginRequirement);

        /// <summary>
        /// Executes synchronous orders to bring the account within margin requirements.
        /// </summary>
        /// <param name="generatedMarginCallOrders">These are the margin call orders that were generated
        /// by individual security margin models.</param>
        /// <returns>The list of orders that were actually executed</returns>
        List<OrderTicket> ExecuteMarginCall(IEnumerable<SubmitOrderRequest> generatedMarginCallOrders);
    }

    /// <summary>
    /// Provides access to a null implementation for <see cref="IMarginCallModel"/>
    /// </summary>
    public static class MarginCallModel
    {
        /// <summary>
        /// Gets an instance of <see cref="IMarginCallModel"/> that will always
        /// return an empty list of executed orders.
        /// </summary>
        public static readonly IMarginCallModel Null = new NullMarginCallModel();

        private sealed class NullMarginCallModel : IMarginCallModel
        {
            public List<SubmitOrderRequest> GetMarginCallOrders(out bool issueMarginCallWarning)
            {
                issueMarginCallWarning = false;
                return new List<SubmitOrderRequest>();
            }

            public SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin,
                decimal maintenanceMarginRequirement)
            {
                return null;
            }

            public List<OrderTicket> ExecuteMarginCall(IEnumerable<SubmitOrderRequest> generatedMarginCallOrders)
            {
                return new List<OrderTicket>();
            }
        }
    }
}
