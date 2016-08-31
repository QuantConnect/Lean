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
using QuantConnect.Logging;
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
            var order = portfolio.Transactions.GetOrderById(fill.OrderId);
            if (order == null)
            {
                Log.Error("OptionPortfolioModel.ProcessFill(): Unable to locate Order with id " + fill.OrderId);
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
        /// Processes exercise event to the portfolio
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">Option security</param>
        /// <param name="order">The order object to be applied</param>
        /// <param name="fill">The order event fill object to be applied</param>
        public void ProcessExerciseFill(SecurityPortfolioManager portfolio, Security security, Order order, OrderEvent fill)
        {
            if (order.Type == OrderType.OptionExercise)
            {
                // option exercise adds several changes to portfolio

                // we first prepare parameters of the fill events
                var exerciseOrder = (OptionExerciseOrder)order;

                var option = (Option)portfolio.Securities[exerciseOrder.Symbol];
                var optionQuantity = order.Quantity;

                var underlying = portfolio.Securities[exerciseOrder.Symbol.Underlying];
                var exercisePrice = fill.FillPrice;
                var exerciseQuantity = option.Symbol.ID.OptionRight == OptionRight.Call ? Math.Abs(fill.FillQuantity) : -Math.Abs(fill.FillQuantity);
                var exerciseDirection = option.Symbol.ID.OptionRight == OptionRight.Call ? OrderDirection.Buy : OrderDirection.Sell;

                var cashQuote = option.QuoteCurrency;

                if (!option.Holdings.IsLong)
                {
                    Log.Error("OptionPortfolioModel.ProcessExerciseFill(): Invalid order event direction. Option holding is not long ");
                    return;
                }

                var addUnderlyingEvent = new OrderEvent(fill.OrderId,
                                underlying.Symbol,
                                fill.UtcTime,
                                OrderStatus.Filled,
                                exerciseDirection,
                                exercisePrice,
                                exerciseQuantity,
                                fill.OrderFee,
                                fill.Message);

                var optionRemoveEvent = new OrderEvent(fill.OrderId,
                                option.Symbol,
                                fill.UtcTime,
                                OrderStatus.Filled,
                                OrderDirection.Sell,
                                0.0m,
                                -optionQuantity,
                                0.0m,
                                "Adjusting(or removing) the exercised option");

                // depending on option settlement terms we either add underlying to the account or add cash equivalent 
                // we then remove the exercised contracts from our option position
                switch (option.ExerciseSettlement)
                {
                    case SettlementType.PhysicalDelivery:

                        // we add underlying to portfolio
                        base.ProcessFill(portfolio, underlying, addUnderlyingEvent);

                        // we adjust our option position by removing exercised contracts
                        base.ProcessFill(portfolio, option, optionRemoveEvent);

                        break;
                    case SettlementType.Cash:

                        var cashQuantity = option.GetIntrinsicValue(underlying.Close) * option.ContractUnitOfTrade * option.QuoteCurrency.ConversionRate * optionQuantity;

                        // we add cash equivalent to portfolio
                        option.SettlementModel.ApplyFunds(portfolio, option, fill.UtcTime, cashQuote.Symbol, cashQuantity);

                        // we adjust our option position by removing exercised contracts
                        base.ProcessFill(portfolio, option, optionRemoveEvent);

                        break;
                }
            }
        }

        /// <summary>
        /// Processes assignment event to the portfolio
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">Option security</param>
        /// <param name="order">The order object to be applied</param>
        /// <param name="fill">The order event fill object to be applied</param>
        public void ProcessAssignmentFill(SecurityPortfolioManager portfolio, Security security, Order order, OrderEvent fill)
        {
            if (order.Type == OrderType.OptionExercise)
            {
                // option exercise adds several changes to portfolio

                // we first prepare parameters of the fill events
                var exerciseOrder = (OptionExerciseOrder)order;

                var option = (Option)portfolio.Securities[exerciseOrder.Symbol];
                var optionQuantity = order.Quantity;

                var underlying = portfolio.Securities[exerciseOrder.Symbol.Underlying];
                var exercisePrice = fill.FillPrice;
                var exerciseQuantity = option.Symbol.ID.OptionRight == OptionRight.Call ? -Math.Abs(fill.FillQuantity) : Math.Abs(fill.FillQuantity);
                var exerciseDirection = option.Symbol.ID.OptionRight == OptionRight.Call ? OrderDirection.Sell : OrderDirection.Buy;

                var cashQuote = option.QuoteCurrency;

                if (!option.Holdings.IsShort)
                {
                    Log.Error("OptionPortfolioModel.ProcessAssignmentFill(): Invalid order event direction. Option holding is not short ");
                    return;
                }

                var addUnderlyingEvent = new OrderEvent(fill.OrderId,
                                underlying.Symbol,
                                fill.UtcTime,
                                OrderStatus.Filled,
                                exerciseDirection,
                                exercisePrice,
                                exerciseQuantity,
                                fill.OrderFee,
                                fill.Message);

                var optionRemoveEvent = new OrderEvent(fill.OrderId,
                                option.Symbol,
                                fill.UtcTime,
                                OrderStatus.Filled,
                                OrderDirection.Buy,
                                0.0m,
                                -optionQuantity,
                                0.0m,
                                "Adjusting(or removing) the assigned option");

                // depending on option settlement terms we either add underlying to the account or add cash equivalent 
                // we then remove the exercised contracts from our option position
                switch (option.ExerciseSettlement)
                {
                    case SettlementType.PhysicalDelivery:

                        // we add underlying to portfolio
                        base.ProcessFill(portfolio, underlying, addUnderlyingEvent);

                        // we adjust our option position by removing exercised contracts
                        base.ProcessFill(portfolio, option, optionRemoveEvent);

                        break;

                    case SettlementType.Cash:

                        var cashQuantity = option.GetIntrinsicValue(underlying.Close) * option.ContractUnitOfTrade * option.QuoteCurrency.ConversionRate * optionQuantity;

                        // we add cash equivalent to portfolio
                        option.SettlementModel.ApplyFunds(portfolio, option, fill.UtcTime, cashQuote.Symbol, cashQuantity);

                        // we adjust our option position by removing exercised contracts
                        base.ProcessFill(portfolio, option, optionRemoveEvent);

                        break;
                }
            }
        }
    }
}
