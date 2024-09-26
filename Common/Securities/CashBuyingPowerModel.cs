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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a buying power model for cash accounts
    /// </summary>
    public class CashBuyingPowerModel : BuyingPowerModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CashBuyingPowerModel"/> class
        /// </summary>
        public CashBuyingPowerModel()
            : base(1m, 0m, 0m)
        {
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public override decimal GetLeverage(Security security)
        {
            // Always returns 1. Cash accounts have no leverage.
            return 1m;
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security">The security to set leverage for</param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
            if (leverage != 1)
            {
                throw new InvalidOperationException(Messages.CashBuyingPowerModel.UnsupportedLeverage);
            }
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity of shares</param>
        public override InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            return security.QuoteCurrency.ConversionRate
                * security.SymbolProperties.ContractMultiplier
                * security.Price
                * quantity;
        }

        /// <summary>
        /// Check if there is sufficient buying power to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>Returns buying power information for an order</returns>
        public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(HasSufficientBuyingPowerForOrderParameters parameters)
        {
            var baseCurrency = parameters.Security as IBaseCurrencySymbol;
            if (baseCurrency == null)
            {
                return parameters.Insufficient(Messages.CashBuyingPowerModel.UnsupportedSecurity(parameters.Security));
            }

            decimal totalQuantity;
            decimal orderQuantity;
            if (parameters.Order.Direction == OrderDirection.Buy)
            {
                // quantity available for buying in quote currency
                totalQuantity = parameters.Security.QuoteCurrency.Amount;
                orderQuantity = parameters.Order.AbsoluteQuantity * GetOrderPrice(parameters.Security, parameters.Order);
            }
            else
            {
                // quantity available for selling in base currency
                totalQuantity = baseCurrency.BaseCurrency.Amount;
                orderQuantity = parameters.Order.AbsoluteQuantity;
            }

            // calculate reserved quantity for open orders (in quote or base currency depending on direction)
            var openOrdersReservedQuantity = GetOpenOrdersReservedQuantity(parameters.Portfolio, parameters.Security, parameters.Order);

            if (parameters.Order.Direction == OrderDirection.Sell)
            {
                // can sell available and non-reserved quantities
                if (orderQuantity <= totalQuantity - openOrdersReservedQuantity)
                {
                    return parameters.Sufficient();
                }

                return parameters.Insufficient(Messages.CashBuyingPowerModel.SellOrderShortHoldingsNotSupported(totalQuantity,
                    openOrdersReservedQuantity, orderQuantity, baseCurrency));
            }

            var maximumQuantity = 0m;
            if (parameters.Order.Type == OrderType.Market)
            {
                // include existing holdings (in quote currency)
                var holdingsValue =
                    parameters.Portfolio.CashBook.Convert(baseCurrency.BaseCurrency.Amount, baseCurrency.BaseCurrency.Symbol, parameters.Security.QuoteCurrency.Symbol);

                // find a target value in account currency for buy market orders
                var targetValue =
                    parameters.Portfolio.CashBook.ConvertToAccountCurrency(totalQuantity - openOrdersReservedQuantity + holdingsValue,
                        parameters.Security.QuoteCurrency.Symbol);

                // convert the target into a percent in relation to TPV
                var targetPercent = parameters.Portfolio.TotalPortfolioValue == 0 ? 0 : targetValue / parameters.Portfolio.TotalPortfolioValue;

                // maximum quantity that can be bought (in quote currency)
                maximumQuantity =
                    GetMaximumOrderQuantityForTargetBuyingPower(
                        new GetMaximumOrderQuantityForTargetBuyingPowerParameters(parameters.Portfolio, parameters.Security, targetPercent, 0)).Quantity * GetOrderPrice(parameters.Security, parameters.Order);

                if (orderQuantity <= Math.Abs(maximumQuantity))
                {
                    return parameters.Sufficient();
                }

                return parameters.Insufficient(Messages.CashBuyingPowerModel.BuyOrderQuantityGreaterThanMaxForBuyingPower(totalQuantity,
                    maximumQuantity, openOrdersReservedQuantity, orderQuantity, baseCurrency, parameters.Security, parameters.Order));
            }

            // for limit orders, add fees to the order cost
            var orderFee = 0m;
            if (parameters.Order.Type == OrderType.Limit)
            {
                var fee = parameters.Security.FeeModel.GetOrderFee(
                    new OrderFeeParameters(parameters.Security,
                        parameters.Order)).Value;
                orderFee = parameters.Portfolio.CashBook.Convert(
                        fee.Amount,
                        fee.Currency,
                        parameters.Security.QuoteCurrency.Symbol);
            }

            maximumQuantity = totalQuantity - openOrdersReservedQuantity - orderFee;
            if (orderQuantity <= maximumQuantity)
            {
                return parameters.Sufficient();
            }

            return parameters.Insufficient(Messages.CashBuyingPowerModel.BuyOrderQuantityGreaterThanMaxForBuyingPower(totalQuantity,
                    maximumQuantity, openOrdersReservedQuantity, orderQuantity, baseCurrency, parameters.Security, parameters.Order));
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a delta in the buying power used by a security.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        public override GetMaximumOrderQuantityResult GetMaximumOrderQuantityForDeltaBuyingPower(
            GetMaximumOrderQuantityForDeltaBuyingPowerParameters parameters)
        {
            throw new NotImplementedException(Messages.CashBuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPowerNotImplemented);
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given buying power percentage.
        /// Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the target signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public override GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters)
        {
            var targetPortfolioValue = parameters.TargetBuyingPower * parameters.Portfolio.TotalPortfolioValue;
            // no shorting allowed
            if (targetPortfolioValue < 0)
            {
                return new GetMaximumOrderQuantityResult(0, Messages.CashBuyingPowerModel.ShortingNotSupported);
            }

            var baseCurrency = parameters.Security as IBaseCurrencySymbol;
            if (baseCurrency == null)
            {
                return new GetMaximumOrderQuantityResult(0, Messages.CashBuyingPowerModel.InvalidSecurity);
            }

            // if target value is zero, return amount of base currency available to sell
            if (targetPortfolioValue == 0)
            {
                return new GetMaximumOrderQuantityResult(-baseCurrency.BaseCurrency.Amount);
            }

            // convert base currency cash to account currency
            var baseCurrencyPosition = parameters.Portfolio.CashBook.ConvertToAccountCurrency(
                baseCurrency.BaseCurrency.Amount,
                baseCurrency.BaseCurrency.Symbol);

            // remove directionality, we'll work in the land of absolutes
            var targetOrderValue = Math.Abs(targetPortfolioValue - baseCurrencyPosition);
            var direction = targetPortfolioValue > baseCurrencyPosition ? OrderDirection.Buy : OrderDirection.Sell;

            // determine the unit price in terms of the account currency
            var unitPrice = direction == OrderDirection.Buy ? parameters.Security.AskPrice : parameters.Security.BidPrice;
            unitPrice *= parameters.Security.QuoteCurrency.ConversionRate * parameters.Security.SymbolProperties.ContractMultiplier;

            if (unitPrice == 0)
            {
                if (parameters.Security.QuoteCurrency.ConversionRate == 0)
                {
                    return new GetMaximumOrderQuantityResult(0,
                        Messages.CashBuyingPowerModel.NoDataInInternalCashFeedYet(parameters.Security, parameters.Portfolio));
                }

                if (parameters.Security.SymbolProperties.ContractMultiplier == 0)
                {
                    return new GetMaximumOrderQuantityResult(0, Messages.CashBuyingPowerModel.ZeroContractMultiplier(parameters.Security));
                }

                // security.Price == 0
                return new GetMaximumOrderQuantityResult(0, parameters.Security.Symbol.GetZeroPriceMessage());
            }

            // continue iterating while we do not have enough cash for the order
            decimal orderFees = 0;
            decimal currentOrderValue = 0;
            // compute the initial order quantity
            var orderQuantity = targetOrderValue / unitPrice;

            // rounding off Order Quantity to the nearest multiple of Lot Size
            orderQuantity -= orderQuantity % parameters.Security.SymbolProperties.LotSize;
            if (orderQuantity == 0)
            {
                string reason = null;
                if (!parameters.SilenceNonErrorReasons)
                {
                    reason = Messages.CashBuyingPowerModel.OrderQuantityLessThanLotSize(parameters.Security);
                }
                return new GetMaximumOrderQuantityResult(0, reason, false);
            }

            // Just in case...
            var lastOrderQuantity = 0m;
            var utcTime = parameters.Security.LocalTime.ConvertToUtc(parameters.Security.Exchange.TimeZone);
            do
            {
                // Each loop will reduce the order quantity based on the difference between
                // (cashRequired + orderFees) and targetOrderValue
                if (currentOrderValue > targetOrderValue)
                {
                    var currentOrderValuePerUnit = currentOrderValue / orderQuantity;
                    var amountOfOrdersToRemove = (currentOrderValue - targetOrderValue) / currentOrderValuePerUnit;
                    if (amountOfOrdersToRemove < parameters.Security.SymbolProperties.LotSize)
                    {
                        // we will always substract at leat 1 LotSize
                        amountOfOrdersToRemove = parameters.Security.SymbolProperties.LotSize;
                    }
                    orderQuantity -= amountOfOrdersToRemove;
                }

                // rounding off Order Quantity to the nearest multiple of Lot Size
                orderQuantity -= orderQuantity % parameters.Security.SymbolProperties.LotSize;
                if (orderQuantity <= 0)
                {
                    return new GetMaximumOrderQuantityResult(0,
                        Messages.CashBuyingPowerModel.OrderQuantityLessThanLotSize(parameters.Security) +
                        Messages.CashBuyingPowerModel.OrderQuantityLessThanLotSizeOrderDetails(targetOrderValue, orderQuantity, orderFees)
                    );
                }

                if (lastOrderQuantity == orderQuantity)
                {
                    throw new ArgumentException(Messages.CashBuyingPowerModel.FailedToConvergeOnTargetOrderValue(targetOrderValue, currentOrderValue,
                        orderQuantity, orderFees, parameters.Security));
                }
                lastOrderQuantity = orderQuantity;

                // generate the order
                var order = new MarketOrder(parameters.Security.Symbol, orderQuantity, utcTime);
                var orderValue = orderQuantity * unitPrice;

                var fees = parameters.Security.FeeModel.GetOrderFee(
                    new OrderFeeParameters(parameters.Security,
                        order)).Value;
                orderFees = parameters.Portfolio.CashBook.ConvertToAccountCurrency(fees).Amount;

                currentOrderValue = orderValue + orderFees;
            } while (currentOrderValue > targetOrderValue);

            // add directionality back in
            return new GetMaximumOrderQuantityResult((direction == OrderDirection.Sell ? -1 : 1) * orderQuantity);
        }

        /// <summary>
        /// Gets the amount of buying power reserved to maintain the specified position
        /// </summary>
        /// <param name="parameters">A parameters object containing the security</param>
        /// <returns>The reserved buying power in account currency</returns>
        public override ReservedBuyingPowerForPosition GetReservedBuyingPowerForPosition(ReservedBuyingPowerForPositionParameters parameters)
        {
            // Always returns 0. Since we're purchasing currencies outright, the position doesn't consume buying power
            return parameters.ResultInAccountCurrency(0m);
        }

        /// <summary>
        /// Gets the buying power available for a trade
        /// </summary>
        /// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
        /// <returns>The buying power available for the trade</returns>
        public override BuyingPower GetBuyingPower(BuyingPowerParameters parameters)
        {
            var security = parameters.Security;
            var portfolio = parameters.Portfolio;
            var direction = parameters.Direction;

            var baseCurrency = security as IBaseCurrencySymbol;
            if (baseCurrency == null)
            {
                return parameters.ResultInAccountCurrency(0m);
            }

            var baseCurrencyPosition = baseCurrency.BaseCurrency.Amount;
            var quoteCurrencyPosition = portfolio.CashBook[security.QuoteCurrency.Symbol].Amount;

            // determine the unit price in terms of the quote currency
            var utcTime = parameters.Security.LocalTime.ConvertToUtc(parameters.Security.Exchange.TimeZone);
            var unitPrice = new MarketOrder(security.Symbol, 1, utcTime).GetValue(security) / security.QuoteCurrency.ConversionRate;
            if (unitPrice == 0)
            {
                return parameters.ResultInAccountCurrency(0m);
            }

            // NOTE: This is returning in units of the BASE currency
            if (direction == OrderDirection.Buy)
            {
                // invert units for math, 6500USD per BTC, currency pairs aren't real fractions
                // (USD)/(BTC/USD) => 10kUSD/ (6500 USD/BTC) => 10kUSD * (1BTC/6500USD) => ~ 1.5BTC
                return parameters.Result(quoteCurrencyPosition / unitPrice, baseCurrency.BaseCurrency.Symbol);
            }

            if (direction == OrderDirection.Sell)
            {
                return parameters.Result(baseCurrencyPosition, baseCurrency.BaseCurrency.Symbol);
            }

            return parameters.ResultInAccountCurrency(0m);
        }

        private static decimal GetOrderPrice(Security security, Order order)
        {
            var orderPrice = 0m;
            switch (order.Type)
            {
                case OrderType.Market:
                    orderPrice = security.Price;
                    break;

                case OrderType.Limit:
                    orderPrice = ((LimitOrder)order).LimitPrice;
                    break;

                case OrderType.StopMarket:
                    orderPrice = ((StopMarketOrder)order).StopPrice;
                    break;

                case OrderType.StopLimit:
                    orderPrice = ((StopLimitOrder)order).LimitPrice;
                    break;

                case OrderType.LimitIfTouched:
                    orderPrice = ((LimitIfTouchedOrder)order).LimitPrice;
                    break;

                case OrderType.TrailingStop:
                    orderPrice = ((TrailingStopOrder)order).StopPrice;
                    break;

                case OrderType.TrailingStopLimit:
                    orderPrice = ((TrailingStopLimitOrder)order).LimitPrice;
                    break;
            }

            return orderPrice;
        }

        private static decimal GetOpenOrdersReservedQuantity(SecurityPortfolioManager portfolio, Security security, Order order)
        {
            var baseCurrency = security as IBaseCurrencySymbol;
            if (baseCurrency == null) return 0;

            // find the target currency for the requested direction and the securities potentially involved
            var targetCurrency = order.Direction == OrderDirection.Buy
                ? security.QuoteCurrency.Symbol
                : baseCurrency.BaseCurrency.Symbol;

            var symbolDirectionPairs = new Dictionary<Symbol, OrderDirection>();
            foreach (var portfolioSecurity in portfolio.Securities.Values)
            {
                var basePortfolioSecurity = portfolioSecurity as IBaseCurrencySymbol;
                if (basePortfolioSecurity == null) continue;

                if (basePortfolioSecurity.BaseCurrency.Symbol == targetCurrency)
                {
                    symbolDirectionPairs.Add(portfolioSecurity.Symbol, OrderDirection.Sell);
                }
                else if (portfolioSecurity.QuoteCurrency.Symbol == targetCurrency)
                {
                    symbolDirectionPairs.Add(portfolioSecurity.Symbol, OrderDirection.Buy);
                }
            }

            // fetch open orders with matching symbol/side
            var openOrders = portfolio.Transactions.GetOpenOrders(x =>
                {
                    OrderDirection dir;
                    return symbolDirectionPairs.TryGetValue(x.Symbol, out dir) &&
                           // same direction of our order
                           dir == x.Direction &&
                           // don't count our current order
                           x.Id != order.Id &&
                           // only count working orders
                           (x.Type == OrderType.Limit || x.Type == OrderType.StopMarket);
                }
            );

            // calculate reserved quantity for selected orders
            var openOrdersReservedQuantity = 0m;
            foreach (var openOrder in openOrders)
            {
                var orderSecurity = portfolio.Securities[openOrder.Symbol];
                var orderBaseCurrency = orderSecurity as IBaseCurrencySymbol;

                if (orderBaseCurrency != null)
                {
                    // convert order value to target currency
                    var quantityInTargetCurrency = openOrder.AbsoluteQuantity;
                    if (orderSecurity.QuoteCurrency.Symbol == targetCurrency)
                    {
                        quantityInTargetCurrency *= GetOrderPrice(security, openOrder);
                    }

                    openOrdersReservedQuantity += quantityInTargetCurrency;
                }
            }

            return openOrdersReservedQuantity;
        }
    }
}
