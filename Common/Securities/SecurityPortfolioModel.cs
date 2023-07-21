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
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a default implementation of <see cref="ISecurityPortfolioModel"/> that simply
    /// applies the fills to the algorithm's portfolio. This implementation is intended to
    /// handle all security types.
    /// </summary>
    public class SecurityPortfolioModel : ISecurityPortfolioModel
    {
        /// <summary>
        /// Performs application of an OrderEvent to the portfolio
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The fill's security</param>
        /// <param name="fill">The order event fill object to be applied</param>
        public virtual void ProcessFill(SecurityPortfolioManager portfolio, Security security, OrderEvent fill)
        {
            var quoteCash = security.QuoteCurrency;

            //Get the required information from the vehicle this order will affect
            var isLong = security.Holdings.IsLong;
            var isShort = security.Holdings.IsShort;
            var closedPosition = false;
            //Make local decimals to avoid any rounding errors from int multiplication
            var quantityHoldings = (decimal)security.Holdings.Quantity;
            var absoluteHoldingsQuantity = security.Holdings.AbsoluteQuantity;
            var averageHoldingsPrice = security.Holdings.AveragePrice;

            try
            {
                // apply sales value to holdings in the account currency
                var saleValue = security.Holdings.GetQuantityValue(fill.AbsoluteFillQuantity, fill.FillPrice).InAccountCurrency;
                security.Holdings.AddNewSale(saleValue);

                // subtract transaction fees from the portfolio
                var feeInAccountCurrency = 0m;
                if (fill.OrderFee != OrderFee.Zero
                    // this is for user friendliness because some
                    // Security types default to use 0 USD ConstantFeeModel
                    && fill.OrderFee.Value.Amount != 0)
                {
                    var feeThisOrder = fill.OrderFee.Value;
                    feeInAccountCurrency = portfolio.CashBook.ConvertToAccountCurrency(feeThisOrder).Amount;
                    security.Holdings.AddNewFee(feeInAccountCurrency);

                    fill.OrderFee.ApplyToPortfolio(portfolio, fill);
                }

                // apply the funds using the current settlement model
                // we dont adjust funds for futures and CFDs: it is zero upfront payment derivative (margin applies though)
                // We do however apply funds for futures options, pay/gained premium, since they affect our cash balance the moment they are purchased/sold.
                if (security.Type != SecurityType.Future && security.Type != SecurityType.Cfd && security.Type != SecurityType.CryptoFuture)
                {
                    security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, fill.UtcTime, new CashAmount(-fill.FillQuantity * fill.FillPrice * security.SymbolProperties.ContractMultiplier, quoteCash.Symbol), fill));
                }
                if (security.Type == SecurityType.Forex || security.Type == SecurityType.Crypto)
                {
                    // model forex fills as currency swaps
                    var forex = (IBaseCurrencySymbol) security;
                    security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, fill.UtcTime, new CashAmount(fill.FillQuantity, forex.BaseCurrency.Symbol), fill));
                }

                // did we close or open a position further?
                closedPosition = isLong && fill.Direction == OrderDirection.Sell
                             || isShort && fill.Direction == OrderDirection.Buy;

                // calculate the last trade profit
                if (closedPosition)
                {
                    // profit = (closed sale value - cost)*conversion to account currency
                    // closed sale value = quantity closed * fill price       BUYs are deemed negative cash flow
                    // cost = quantity closed * average holdings price        SELLS are deemed positive cash flow
                    var absoluteQuantityClosed = Math.Min(fill.AbsoluteFillQuantity, absoluteHoldingsQuantity);
                    var quantityClosed = Math.Sign(-fill.FillQuantity) * absoluteQuantityClosed;
                    var closedCost = security.Holdings.GetQuantityValue(quantityClosed, averageHoldingsPrice);
                    var closedSaleValueInQuoteCurrency = security.Holdings.GetQuantityValue(quantityClosed, fill.FillPrice);

                    var lastTradeProfit = closedSaleValueInQuoteCurrency.Amount - closedCost.Amount;
                    var lastTradeProfitInAccountCurrency = closedSaleValueInQuoteCurrency.InAccountCurrency - closedCost.InAccountCurrency;

                    // Reflect account cash adjustment for futures/CFD position
                    if (security.Type == SecurityType.Future || security.Type == SecurityType.Cfd || security.Type == SecurityType.CryptoFuture)
                    {
                        security.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, security, fill.UtcTime, new CashAmount(lastTradeProfit, closedCost.Cash.Symbol), fill));
                    }

                    //Update Vehicle Profit Tracking:
                    security.Holdings.AddNewProfit(lastTradeProfitInAccountCurrency);
                    security.Holdings.SetLastTradeProfit(lastTradeProfitInAccountCurrency);
                    var transactionProfitLoss = lastTradeProfitInAccountCurrency - 2 * feeInAccountCurrency;
                    portfolio.AddTransactionRecord(
                        security.LocalTime.ConvertToUtc(security.Exchange.TimeZone),
                        transactionProfitLoss,
                        fill.IsWin(security, transactionProfitLoss));
                }

                //UPDATE HOLDINGS QUANTITY, AVG PRICE:
                //Currently NO holdings. The order is ALL our holdings.
                if (quantityHoldings == 0)
                {
                    //First transaction just subtract order from cash and set our holdings:
                    averageHoldingsPrice = fill.FillPrice;
                    quantityHoldings = fill.FillQuantity;
                }
                else if (isLong)
                {
                    //If we're currently LONG on the stock.
                    switch (fill.Direction)
                    {
                        case OrderDirection.Buy:
                            //Update the Holding Average Price: Total Value / Total Quantity:
                            averageHoldingsPrice = ((averageHoldingsPrice*quantityHoldings) + (fill.FillQuantity*fill.FillPrice))/(quantityHoldings + fill.FillQuantity);
                            //Add the new quantity:
                            quantityHoldings += fill.FillQuantity;
                            break;

                        case OrderDirection.Sell:
                            quantityHoldings += fill.FillQuantity; //+ a short = a subtraction
                            if (quantityHoldings < 0)
                            {
                                //If we've now passed through zero from selling stock: new avg price:
                                averageHoldingsPrice = fill.FillPrice;
                            }
                            else if (quantityHoldings == 0)
                            {
                                averageHoldingsPrice = 0;
                            }
                            break;
                    }
                }
                else if (isShort)
                {
                    //We're currently SHORTING the stock: What is the new position now?
                    switch (fill.Direction)
                    {
                        case OrderDirection.Buy:
                            //Buying when we're shorting moves to close position:
                            quantityHoldings += fill.FillQuantity;
                            if (quantityHoldings > 0)
                            {
                                //If we were short but passed through zero, new average price is what we paid. The short position was closed.
                                averageHoldingsPrice = fill.FillPrice;
                            }
                            else if (quantityHoldings == 0)
                            {
                                averageHoldingsPrice = 0;
                            }
                            break;

                        case OrderDirection.Sell:
                            //We are increasing a Short position:
                            //E.g.  -100 @ $5, adding -100 @ $10: Avg: $7.5
                            //      dAvg = (-500 + -1000) / -200 = 7.5
                            averageHoldingsPrice = ((averageHoldingsPrice*quantityHoldings) + (fill.FillQuantity*fill.FillPrice))/(quantityHoldings + fill.FillQuantity);
                            quantityHoldings += fill.FillQuantity;
                            break;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            //Set the results back to the vehicle.
            security.Holdings.SetHoldings(averageHoldingsPrice, quantityHoldings);
        }
    }
}
