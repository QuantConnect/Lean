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

using QuantConnect.Logging;
using QuantConnect.Orders;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Provides an implementation of <see cref="ISecurityPortfolioModel"/> for options that supports
    /// default fills as well as option exercising.
    /// </summary>
    public class OptionPortfolioModel : SecurityPortfolioModel
    {
        /// <summary>
        /// Performs application of an OrderEvent to the portfolio
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">Option security</param>
        /// <param name="fill">The order event fill object to be applied</param>
        public override void ProcessFill(SecurityPortfolioManager portfolio, Security security, OrderEvent fill)
        {
            var order = portfolio.Transactions.GetOrderById(fill.OrderId);
            if (order == null)
            {
                Log.Error(Invariant($"OptionPortfolioModel.ProcessFill(): Unable to locate Order with id {fill.OrderId}"));
                return;
            }

            if (order.Type == OrderType.OptionExercise)
            {
                ProcessExerciseFill(portfolio, security, order, fill);
            }
            else
            {
                // we delegate the call to the base class (default behavior)
                base.ProcessFill(portfolio, security, fill);
            }
        }

        /// <summary>
        /// Processes exercise/assignment event to the portfolio
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">Option security</param>
        /// <param name="order">The order object to be applied</param>
        /// <param name="fill">The order event fill object to be applied</param>
        public void ProcessExerciseFill(SecurityPortfolioManager portfolio, Security security, Order order, OrderEvent fill)
        {
            var exerciseOrder = (OptionExerciseOrder)order;
            var option = (Option)portfolio.Securities[exerciseOrder.Symbol];
            var underlying = option.Underlying;
            var cashQuote = option.QuoteCurrency;
            var optionQuantity = order.Quantity;
            var processSecurity = portfolio.Securities[fill.Symbol];

            // depending on option settlement terms we either add underlying to the account or add cash equivalent
            // we then remove the exercised contracts from our option position
            switch (option.ExerciseSettlement)
            {
                case SettlementType.PhysicalDelivery:

                    base.ProcessFill(portfolio, processSecurity, fill);
                    break;

                case SettlementType.Cash:

                    var cashQuantity = -option.GetIntrinsicValue(underlying.Close) * option.ContractUnitOfTrade * optionQuantity;

                    // we add cash equivalent to portfolio
                    option.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, option, fill.UtcTime, new CashAmount(cashQuantity, cashQuote.Symbol), fill));

                    base.ProcessFill(portfolio, processSecurity, fill);
                    break;
            }
        }
    }
}
