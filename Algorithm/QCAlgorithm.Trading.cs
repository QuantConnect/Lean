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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private int _maxOrders = 10000;

        /// <summary>
        /// Transaction Manager - Process transaction fills and order management.
        /// </summary>
        public SecurityTransactionManager Transactions { get; set; }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, double)"/>
        public OrderTicket Buy(Symbol symbol, int quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, int)"/>
        public OrderTicket Buy(Symbol symbol, double quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <seealso cref="Order(Symbol, double)"/>
        public OrderTicket Buy(Symbol symbol, decimal quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">float Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, double)"/>
        public OrderTicket Buy(Symbol symbol, float quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Sell(Symbol, double)"/>
        public OrderTicket Sell(Symbol symbol, int quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>int Order Id.</returns>
        public OrderTicket Sell(Symbol symbol, double quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>int order id</returns>
        /// <seealso cref="Sell(Symbol, double)"/>
        public OrderTicket Sell(Symbol symbol, float quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>Int Order Id.</returns>
        public OrderTicket Sell(Symbol symbol, decimal quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <seealso cref="Order(Symbol, decimal)"/>
        public OrderTicket Order(Symbol symbol, double quantity)
        {
            return Order(symbol, (int) quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref="Order(Symbol, double)"/>
        public OrderTicket Order(Symbol symbol, decimal quantity)
        {
            return Order(symbol, (int) quantity);
        }

        /// <summary>
        /// Wrapper for market order method: submit a new order for quantity of symbol using type order.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <seealso cref="MarketOrder(Symbol, int, bool, string)"/>
        public OrderTicket Order(Symbol symbol, int quantity, bool asynchronous = false, string tag = "")
        {
            return MarketOrder(symbol, quantity, asynchronous, tag);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>int Order id</returns>
        public OrderTicket MarketOrder(Symbol symbol, int quantity, bool asynchronous = false, string tag = "")
        {
            var security = Securities[symbol];

            // check the exchange is open before sending a market order, if it's not open
            // then convert it into a market on open order
            if (!security.Exchange.ExchangeOpen)
            {
                var mooTicket = MarketOnOpenOrder(security.Symbol, quantity, tag);
                var anyNonDailySubscriptions = security.Subscriptions.Any(x => x.Resolution != Resolution.Daily);
                if (mooTicket.SubmitRequest.Response.IsSuccess && !anyNonDailySubscriptions)
                {
                    Debug("Converted OrderID: " + mooTicket.OrderId + " into a MarketOnOpen order.");
                }   
                return mooTicket;
            }

            var request = CreateSubmitOrderRequest(OrderType.Market, security, quantity, tag);

            //Initialize the Market order parameters:
            var preOrderCheckResponse = PreOrderChecks(request);
            if (preOrderCheckResponse.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, preOrderCheckResponse);
            }

            //Add the order and create a new order Id.
            var ticket = Transactions.AddOrder(request);

            // Wait for the order event to process, only if the exchange is open
            if (!asynchronous)
            {
                Transactions.WaitForOrder(ticket.OrderId);
            }

            return ticket;
        }

        /// <summary>
        /// Market on open order implementation: Send a market order when the exchange opens
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderTicket MarketOnOpenOrder(Symbol symbol, int quantity, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.MarketOnOpen, security, quantity, tag);
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderTicket MarketOnCloseOrder(Symbol symbol, int quantity, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.MarketOnClose, security, quantity, tag);
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <returns>Order id</returns>
        public OrderTicket LimitOrder(Symbol symbol, int quantity, decimal limitPrice, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.Limit, security, quantity, tag, limitPrice: limitPrice);
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <returns>Int orderId for the new order.</returns>
        public OrderTicket StopMarketOrder(Symbol symbol, int quantity, decimal stopPrice, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.StopMarket, security, quantity, tag, stopPrice: stopPrice);
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Send a stop limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="stopPrice">Stop price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <returns>Order id</returns>
        public OrderTicket StopLimitOrder(Symbol symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.StopLimit, security, quantity, tag, stopPrice: stopPrice, limitPrice: limitPrice);
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            //Add the order and create a new order Id.
            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Perform preorder checks to ensure we have sufficient capital, 
        /// the market is open, and we haven't exceeded maximum realistic orders per day.
        /// </summary>
        /// <returns>OrderResponse. If no error, order request is submitted.</returns>
        private OrderResponse PreOrderChecks(SubmitOrderRequest request)
        {
            var response = PreOrderChecksImpl(request);
            if (response.IsError)
            {
                Error(response.ErrorMessage);
            }
            return response;
        }

        /// <summary>
        /// Perform preorder checks to ensure we have sufficient capital, 
        /// the market is open, and we haven't exceeded maximum realistic orders per day.
        /// </summary>
        /// <returns>OrderResponse. If no error, order request is submitted.</returns>
        private OrderResponse PreOrderChecksImpl(SubmitOrderRequest request)
        {
            //Most order methods use security objects; so this isn't really used. 
            // todo: Left here for now but should review 
            Security security;
            if (!Securities.TryGetValue(request.Symbol, out security))
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.MissingSecurity, "You haven't requested " + request.Symbol.ToString() + " data. Add this with AddSecurity() in the Initialize() Method.");
            }

            //Ordering 0 is useless.
            if (request.Quantity == 0 || request.Symbol == null || request.Symbol == QuantConnect.Symbol.Empty || Math.Abs(request.Quantity) < security.SymbolProperties.LotSize)
            {
                return OrderResponse.ZeroQuantity(request);
            }

            if (!security.IsTradable)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.NonTradableSecurity, "The security with symbol '" + request.Symbol.ToString() + "' is marked as non-tradable.");
            }

            var price = security.Price;

            //Check the exchange is open before sending a market on close orders
            if (request.OrderType == OrderType.MarketOnClose && !security.Exchange.ExchangeOpen)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.ExchangeNotOpen, request.OrderType + " order and exchange not open.");
            }
            
            if (price == 0)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.SecurityPriceZero, request.Symbol.ToString() + ": asset price is $0. If using custom data make sure you've set the 'Value' property.");
            }

            // check quote currency existence/conversion rate on all orders
            Cash quoteCash;
            var quoteCurrency = security.QuoteCurrency.Symbol;
            if (!Portfolio.CashBook.TryGetValue(quoteCurrency, out quoteCash))
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.QuoteCurrencyRequired, request.Symbol.Value + ": requires " + quoteCurrency + " in the cashbook to trade.");
            }
            if (security.QuoteCurrency.ConversionRate == 0m)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.ConversionRateZero, request.Symbol.Value + ": requires " + quoteCurrency + " to have a non-zero conversion rate. This can be caused by lack of data.");
            }
            
            // need to also check base currency existence/conversion rate on forex orders
            if (security.Type == SecurityType.Forex)
            {
                Cash baseCash;
                var baseCurrency = ((Forex) security).BaseCurrencySymbol;
                if (!Portfolio.CashBook.TryGetValue(baseCurrency, out baseCash))
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.ForexBaseAndQuoteCurrenciesRequired, request.Symbol.Value + ": requires " + baseCurrency + " and " + quoteCurrency + " in the cashbook to trade.");
                }
                if (baseCash.ConversionRate == 0m)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.ForexConversionRateZero, request.Symbol.Value + ": requires " + baseCurrency + " and " + quoteCurrency + " to have non-zero conversion rates. This can be caused by lack of data.");
                }
            }
            
            //Make sure the security has some data:
            if (!security.HasData)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.SecurityHasNoData, "There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point.");
            }
            
            //We've already processed too many orders: max 100 per day or the memory usage explodes
            if (Transactions.OrdersCount > _maxOrders)
            {
                Status = AlgorithmStatus.Stopped;
                return OrderResponse.Error(request, OrderResponseErrorCode.ExceededMaximumOrders, string.Format("You have exceeded maximum number of orders ({0}), for unlimited orders upgrade your account.", _maxOrders));
            }
            
            if (request.OrderType == OrderType.MarketOnClose)
            {
                var nextMarketClose = security.Exchange.Hours.GetNextMarketClose(security.LocalTime, false);
                // must be submitted with at least 10 minutes in trading day, add buffer allow order submission
                var latestSubmissionTime = nextMarketClose.AddMinutes(-15.50);
                if (!security.Exchange.ExchangeOpen || Time > latestSubmissionTime)
                {
                    // tell the user we require a 16 minute buffer, on minute data in live a user will receive the 3:44->3:45 bar at 3:45,
                    // this is already too late to submit one of these orders, so make the user do it at the 3:43->3:44 bar so it's submitted
                    // to the brokerage before 3:45.
                    return OrderResponse.Error(request, OrderResponseErrorCode.MarketOnCloseOrderTooLate, "MarketOnClose orders must be placed with at least a 16 minute buffer before market close.");
                }
            }

            // passes all initial order checks
            return OrderResponse.Success(request);
        }

        /// <summary>
        /// Liquidate all holdings and cancel open orders. Called at the end of day for tick-strategies.
        /// </summary>
        /// <param name="symbolToLiquidate">Symbols we wish to liquidate</param>
        /// <returns>Array of order ids for liquidated symbols</returns>
        /// <seealso cref="MarketOrder"/>
        public List<int> Liquidate(Symbol symbolToLiquidate = null)
        {
            var orderIdList = new List<int>();
            symbolToLiquidate = symbolToLiquidate ?? QuantConnect.Symbol.Empty;

            foreach (var symbol in Securities.Keys.OrderBy(x => x.Value))
            {
                // symbol not matching, do nothing
                if (symbol != symbolToLiquidate && symbolToLiquidate != QuantConnect.Symbol.Empty) 
                    continue;

                // get open orders
                var orders = Transactions.GetOpenOrders(symbol);

                // get quantity in portfolio
                var quantity = Portfolio[symbol].Quantity;

                // if there is only one open market order that would close the position, do nothing
                if (orders.Count == 1 && quantity != 0 && orders[0].Quantity == -quantity && orders[0].Type == OrderType.Market)
                    continue;

                // cancel all open orders
                var marketOrdersQuantity = 0m;
                foreach (var order in orders)
                {
                    if (order.Type == OrderType.Market)
                    {
                        // pending market order
                        var ticket = Transactions.GetOrderTicket(order.Id);
                        if (ticket != null)
                        {
                            // get remaining quantity
                            marketOrdersQuantity += ticket.Quantity - ticket.QuantityFilled;
                        }
                    }
                    else
                    {
                        Transactions.CancelOrder(order.Id);
                    }
                }

                // Liquidate at market price
                if (quantity != 0)
                {
                    // calculate quantity for closing market order
                    var ticket = Order(symbol, -quantity - marketOrdersQuantity);
                    if (ticket.Status == OrderStatus.Filled)
                    {
                        orderIdList.Add(ticket.OrderId);
                    }
                }
            }

            return orderIdList;
        }

        /// <summary>
        /// Maximum number of orders for the algorithm
        /// </summary>
        /// <param name="max"></param>
        public void SetMaximumOrders(int max)
        {
            if (!_locked)
            {
                _maxOrders = max;
            }
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">double percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">liquidate existing holdings if neccessary to hold this stock</param>
        /// <seealso cref="MarketOrder"/>
        public void SetHoldings(Symbol symbol, double percentage, bool liquidateExistingHoldings = false)
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings);
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">float percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">bool liquidate existing holdings if neccessary to hold this stock</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <seealso cref="MarketOrder"/>
        public void SetHoldings(Symbol symbol, float percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings, tag);
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">float percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">bool liquidate existing holdings if neccessary to hold this stock</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <seealso cref="MarketOrder"/>
        public void SetHoldings(Symbol symbol, int percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings, tag);
        }

        /// <summary>
        /// Automatically place an order which will set the holdings to between 100% or -100% of *PORTFOLIO VALUE*.
        /// E.g. SetHoldings("AAPL", 0.1); SetHoldings("IBM", -0.2); -> Sets portfolio as long 10% APPL and short 20% IBM
        /// E.g. SetHoldings("AAPL", 2); -> Sets apple to 2x leveraged with all our cash.
        /// </summary>
        /// <param name="symbol">Symbol indexer</param>
        /// <param name="percentage">decimal fraction of portfolio to set stock</param>
        /// <param name="liquidateExistingHoldings">bool flag to clean all existing holdings before setting new faction.</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <seealso cref="MarketOrder"/>
        public void SetHoldings(Symbol symbol, decimal percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            //Initialize Requirements:
            Security security;
            if (!Securities.TryGetValue(symbol, out security))
            {
                Error(symbol.ToString() + " not found in portfolio. Request this data when initializing the algorithm.");
                return;
            }

            //If they triggered a liquidate
            if (liquidateExistingHoldings)
            {
                foreach (var kvp in Portfolio)
                {
                    var holdingSymbol = kvp.Key;
                    var holdings = kvp.Value;
                    if (holdingSymbol != symbol && holdings.AbsoluteQuantity > 0)
                    {
                        //Go through all existing holdings [synchronously], market order the inverse quantity:
                        Order(holdingSymbol, -holdings.Quantity, false, tag);
                    }
                }
            }

            //Only place trade if we've got > 1 share to order.
            var quantity = CalculateOrderQuantity(symbol, percentage);
            if (Math.Abs(quantity) > 0)
            {
                MarketOrder(symbol, quantity, false, tag);
            }
        }

        /// <summary>
        /// Calculate the order quantity to achieve target-percent holdings.
        /// </summary>
        /// <param name="symbol">Security object we're asking for</param>
        /// <param name="target">Target percentag holdings</param>
        /// <returns>Order quantity to achieve this percentage</returns>
        public int CalculateOrderQuantity(Symbol symbol, double target)
        {
            return CalculateOrderQuantity(symbol, (decimal)target);
        }

        /// <summary>
        /// Calculate the order quantity to achieve target-percent holdings.
        /// </summary>
        /// <param name="symbol">Security object we're asking for</param>
        /// <param name="target">Target percentag holdings, this is an unlevered value, so 
        /// if you have 2x leverage and request 100% holdings, it will utilize half of the 
        /// available margin</param>
        /// <returns>Order quantity to achieve this percentage</returns>
        public int CalculateOrderQuantity(Symbol symbol, decimal target)
        {
            var security = Securities[symbol];
            var price = security.Price;

            // can't order it if we don't have data
            if (price == 0) return 0;

            // if targeting zero, simply return the negative of the quantity
            if (target == 0) return -security.Holdings.Quantity;

            // this is the value in dollars that we want our holdings to have
            var targetPortfolioValue = target*Portfolio.TotalPortfolioValue;
            var quantity = security.Holdings.Quantity;
            var currentHoldingsValue = price*quantity;

            // remove directionality, we'll work in the land of absolutes
            var targetOrderValue = Math.Abs(targetPortfolioValue - currentHoldingsValue);
            var direction = targetPortfolioValue > currentHoldingsValue ? OrderDirection.Buy : OrderDirection.Sell;

            // determine the unit price in terms of the account currency
            var unitPrice = new MarketOrder(symbol, 1, UtcTime).GetValue(security);

            // calculate the total margin available
            var marginRemaining = Portfolio.GetMarginRemaining(symbol, direction);
            if (marginRemaining <= 0) return 0;

            // continue iterating while we do not have enough margin for the order
            decimal marginRequired;
            decimal orderValue;
            decimal orderFees;
            var feeToPriceRatio = 0;

            // compute the initial order quantity
            var orderQuantity = (int)(targetOrderValue / unitPrice);
            var iterations = 0;

            do
            {
                // decrease the order quantity
                if (iterations > 0)
                {
                    // if fees are high relative to price, we reduce the order quantity faster
                    if (feeToPriceRatio > 0)
                        orderQuantity -= feeToPriceRatio;
                    else
                        orderQuantity--;
                }

                // generate the order
                var order = new MarketOrder(security.Symbol, orderQuantity, UtcTime);
                orderValue = order.GetValue(security);
                orderFees = security.FeeModel.GetOrderFee(security, order);
                feeToPriceRatio = (int)(orderFees / unitPrice);

                // calculate the margin required for the order
                marginRequired = security.MarginModel.GetInitialMarginRequiredForOrder(security, order);

                iterations++;

            } while (orderQuantity > 0 && (marginRequired > marginRemaining || orderValue + orderFees > targetOrderValue));

            //Rounding off Order Quantity to the nearest multiple of Lot Size
            if (orderQuantity % Convert.ToInt32(security.SymbolProperties.LotSize) != 0)
            {
                orderQuantity = orderQuantity - (orderQuantity % Convert.ToInt32(security.SymbolProperties.LotSize));
            }

            // add directionality back in
            return (direction == OrderDirection.Sell ? -1 : 1) * orderQuantity;
        }

        /// <summary>
        /// Obsolete implementation of Order method accepting a OrderType. This was deprecated since it 
        /// was impossible to generate other orders via this method. Any calls to this method will always default to a Market Order.
        /// </summary>
        /// <param name="symbol">Symbol we want to purchase</param>
        /// <param name="quantity">Quantity to buy, + is long, - short.</param>
        /// <param name="type">Order Type</param>
        /// <param name="asynchronous">Don't wait for the response, just submit order and move on.</param>
        /// <param name="tag">Custom data for this order</param>
        /// <returns>Integer Order ID.</returns>
        [Obsolete("This Order method has been made obsolete, use Order(string, int, bool, string) method instead. Calls to the obsolete method will only generate market orders.")]
        public OrderTicket Order(Symbol symbol, int quantity, OrderType type, bool asynchronous = false, string tag = "")
        {
            return Order(symbol, quantity, asynchronous, tag);
        }

        /// <summary>
        /// Obsolete method for placing orders. 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="type"></param>
        [Obsolete("This Order method has been made obsolete, use the specialized Order helper methods instead. Calls to the obsolete method will only generate market orders.")]
        public OrderTicket Order(Symbol symbol, decimal quantity, OrderType type)
        {
            return Order(symbol, (int)quantity);
        }

        /// <summary>
        /// Obsolete method for placing orders.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="type"></param>
        [Obsolete("This Order method has been made obsolete, use the specialized Order helper methods instead. Calls to the obsolete method will only generate market orders.")]
        public OrderTicket Order(Symbol symbol, int quantity, OrderType type)
        {
            return Order(symbol, quantity);
        }

        private SubmitOrderRequest CreateSubmitOrderRequest(OrderType orderType, Security security, int quantity, string tag, decimal stopPrice = 0m, decimal limitPrice = 0m)
        {
            return new SubmitOrderRequest(orderType, security.Type, security.Symbol, quantity, stopPrice, limitPrice, UtcTime, tag);
        }
    }
}
