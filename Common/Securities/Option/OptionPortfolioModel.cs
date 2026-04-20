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
            if (fill.Ticket.OrderType == OrderType.OptionExercise)
            {
                base.ProcessFill(portfolio, portfolio.Securities[fill.Symbol], fill);
                // Update the order event message with the P&L
                UpdateExerciseOrderEventMessage(security, fill);
            }
            else
            {
                // we delegate the call to the base class (default behavior)
                base.ProcessFill(portfolio, security, fill);
            }
        }

        private static void UpdateExerciseOrderEventMessage(Security security, OrderEvent fill)
        {
            var lastTradeProfit = security.Holdings.LastTradeProfit;
            var message = "";
            if (lastTradeProfit >= 0)
            {
                message += $". Profit: +{lastTradeProfit.ToStringInvariant()}";
            }
            else
            {
                message += $". Loss: {lastTradeProfit.ToStringInvariant()}";
            }
            fill.Message = fill.Message + message;
        }

        /// <summary>
        /// Helper method to determine the close trade profit
        /// </summary>
        /// <remarks>For SettlementType.Cash we apply funds and add in the result to the profit</remarks>
        protected override ConvertibleCashAmount ProcessCloseTradeProfit(SecurityPortfolioManager portfolio, Security security, OrderEvent fill)
        {
            var baseResult = base.ProcessCloseTradeProfit(portfolio, security, fill);

            var ticket = fill.Ticket;
            if (ticket.OrderType == OrderType.OptionExercise && security.Symbol.SecurityType.IsOption())
            {
                var option = (Option)security;
                if (option.ExerciseSettlement == SettlementType.Cash)
                {
                    var underlying = option.Underlying;
                    var optionQuantity = fill.Ticket.Quantity;
                    var cashQuantity = -option.GetIntrinsicValue(underlying.Close) * option.ContractUnitOfTrade * optionQuantity;
                    if (cashQuantity != decimal.Zero)
                    {
                        security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, fill.UtcTime, new CashAmount(cashQuantity, option.QuoteCurrency.Symbol), fill));
                        return new ConvertibleCashAmount(cashQuantity + baseResult.Amount, option.QuoteCurrency);
                    }
                }
            }
            return baseResult;
        }
    }
}
