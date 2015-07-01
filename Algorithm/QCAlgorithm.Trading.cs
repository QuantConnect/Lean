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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private bool _processingOrder = false;
        private int _maxOrders = 10000;

        /// <summary>
        /// Transaction Manager - Process transaction fills and order management.
        /// </summary>
        public SecurityTransactionManager Transactions { get; set; }

        /// <summary>
        /// Wait semaphore to signal the algoritm is currently processing a synchronous order.
        /// </summary>
        public bool ProcessingEvents
        {
            get { return _processingOrder; }
            set { _processingOrder = value; }
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Buy(string, double)"/>
        public OrderResponse Buy(string symbol, int quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <seealso cref="Buy(string, int)"/>
        public OrderResponse Buy(string symbol, double quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double)"/>
        public OrderResponse Buy(string symbol, decimal quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">float Quantity of the asset to trade</param>
        /// <seealso cref="Buy(string, double)"/>
        public OrderResponse Buy(string symbol, float quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Sell(string, double)"/>
        public OrderResponse Sell(string symbol, int quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>int Order Id.</returns>
        public OrderResponse Sell(string symbol, double quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>int order id</returns>
        /// <seealso cref="Sell(string, double)"/>
        public OrderResponse Sell(string symbol, float quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>Int Order Id.</returns>
        public OrderResponse Sell(string symbol, decimal quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <seealso cref="Order(string, double)"/>
        public OrderResponse Order(string symbol, double quantity)
        {
            return Order(symbol, (int) quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref="Order(string, double)"/>
        public OrderResponse Order(string symbol, decimal quantity)
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
        /// <seealso cref="MarketOrder(string, int, bool, string)"/>
        public OrderResponse Order(string symbol, int quantity, bool asynchronous = false, string tag = "")
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
        public OrderResponse MarketOrder(string symbol, int quantity, bool asynchronous = false, string tag = "")
        {
            var security = Securities[symbol];

            // check the exchange is open before sending a market order, if it's not open
            // then convert it into a market on open order
            if (!security.Exchange.ExchangeOpen)
            {
                var alternateResponse = MarketOnOpenOrder(symbol, quantity, tag);
                Debug("Converted OrderID: " + alternateResponse.OrderId + " into a MarketOnOpen order.");
                return alternateResponse;
            }

            //Initalize the Market order parameters:
            var validationResponse = PreOrderChecks(symbol, quantity, OrderType.MarketOnOpen);
            if (!validationResponse.IsProcessed)
            {
                return validationResponse;
            }

            var submitRequest = QuantConnect.Orders.MarketOrder.CreateSubmitRequest(security.Type, symbol, quantity, Time, price: security.Price, tag: tag);

            //Add the order and create a new order Id.
            var response = Transactions.SubmitOrder(submitRequest);

            //Wait for the order event to process, only if the exchange is open
            if (!asynchronous)
            {
                //Wait for the market order to fill.
                //This is processed in a parallel thread.
                Transactions.WaitForOrder(response.OrderId);
            }

            return response;
        }

        /// <summary>
        /// Market on open order implementation: Send a market order when the exchange opens
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderResponse MarketOnOpenOrder(string symbol, int quantity, string tag = "")
        {
            var response = PreOrderChecks(symbol, quantity, OrderType.MarketOnOpen);
            if (!response.IsProcessed)
            {
                return response;
            }

            var security = Securities[symbol];
            var request = QuantConnect.Orders.MarketOnOpenOrder.CreateSubmitRequest(security.Type, symbol, quantity, Time, price: security.Price, tag: tag);

            return Transactions.SubmitOrder(request);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderResponse MarketOnCloseOrder(string symbol, int quantity, string tag = "")
        {
            var response = PreOrderChecks(symbol, quantity, OrderType.MarketOnClose);
            if (!response.IsProcessed)
            {
                return response;
            }

            var security = Securities[symbol];
            var request = QuantConnect.Orders.MarketOnCloseOrder.CreateSubmitRequest(security.Type, symbol, quantity, Time, price: security.Price, tag: tag);

            return Transactions.SubmitOrder(request);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <returns>Order id</returns>
        public OrderResponse LimitOrder(string symbol, int quantity, decimal limitPrice, string tag = "")
        {
            var response = PreOrderChecks(symbol, quantity, OrderType.Limit);
            if (!response.IsProcessed)
            {
                return response;
            }

            var security = Securities[symbol];
            var request = QuantConnect.Orders.LimitOrder.CreateSubmitRequest(security.Type, symbol, quantity, Time, limitPrice, tag: tag);

            //Add the order and create a new order Id.
            return Transactions.SubmitOrder(request);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <returns>Int orderId for the new order.</returns>
        public OrderResponse StopMarketOrder(string symbol, int quantity, decimal stopPrice, string tag = "")
        {
            var response = PreOrderChecks(symbol, quantity, OrderType.StopMarket);
            if (!response.IsProcessed)
            {
                return response;
            }

            var security = Securities[symbol];
            var request = QuantConnect.Orders.StopMarketOrder.CreateSubmitRequest(security.Type, symbol, quantity, Time, stopPrice, tag: tag);

            //Add the order and create a new order Id.
            return Transactions.SubmitOrder(request);
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
        public OrderResponse StopLimitOrder(string symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag = "")
        {
            var response = PreOrderChecks(symbol, quantity, OrderType.StopLimit);
            if (!response.IsProcessed)
            {
                return response;
            }

            var security = Securities[symbol];
            var request = QuantConnect.Orders.StopLimitOrder.CreateSubmitRequest(security.Type, symbol, quantity, Time, stopPrice, limitPrice, tag: tag);

            //Add the order and create a new order Id.
            return Transactions.SubmitOrder(request);
        }

        /// <summary>
        /// Perform preorder checks to ensure we have sufficient capital, 
        /// the market is open, and we haven't exceeded maximum realistic orders per day.
        /// </summary>
        /// <returns>OrderResponse. If no error, order request is submitted.</returns>
        private OrderResponse PreOrderChecks(string symbol, int quantity, OrderType type)
        {
            var response = new OrderResponse
            {
                Type = OrderResponseType.Error,
                ErrorCode = OrderResponseErrorCode.PreOrderChecksError,
                ErrorMessage = "Unknown error"
            };

            //Ordering 0 is useless.
            if (quantity == 0 || string.IsNullOrEmpty(symbol))
            {
                response.Error(OrderResponseErrorCode.OrderQuantityZero);
                return response;
            }

            //Internals use upper case symbols.
            symbol = symbol.ToUpper();

            //If we're not tracking this symbol: throw error:
            if (!Securities.ContainsKey(symbol) && !_sentNoDataError)
            {
                _sentNoDataError = true;
                response.Error(OrderResponseErrorCode.MissingSecurity, "You haven't requested " + symbol + " data. Add this with AddSecurity() in the Initialize() Method.");
                Error(response.ErrorMessage);
                return response;
            }

            //Set a temporary price for validating order for market orders:
            var security = Securities[symbol];
            var price = security.Price;

            //Check the exchange is open before sending a market on close orders
            //Allow market orders, they'll just execute when the exchange reopens
            if (type == OrderType.MarketOnClose && !security.Exchange.ExchangeOpen)
            {
                response.Error(OrderResponseErrorCode.ExchangeNotOpen, type + " order and exchange not open.");
                Error(response.ErrorMessage);
                return response;
            }

            if (price == 0)
            {
                response.Error(OrderResponseErrorCode.SecurityPriceZero, symbol + ": asset price is $0. If using custom data make sure you've set the 'Value' property.");
                Error(response.ErrorMessage);
                return response;
            }

            if (security.Type == SecurityType.Forex)
            {
                // for forex pairs we need to verify that the conversions to USD have values as well
                string baseCurrency, quoteCurrency;
                Forex.DecomposeCurrencyPair(security.Symbol, out baseCurrency, out quoteCurrency);
                
                // verify they're in the portfolio
                Cash baseCash, quoteCash;
                if (!Portfolio.CashBook.TryGetValue(baseCurrency, out baseCash) || !Portfolio.CashBook.TryGetValue(quoteCurrency, out quoteCash))
                {
                    response.Error(OrderResponseErrorCode.ForexBaseAndQuoteCurrenciesRequired, symbol + ": requires " + baseCurrency + " and " + quoteCurrency + " in the cashbook to trade.");
                    Error(response.ErrorMessage);
                    return response;
                }
                // verify we have conversion rates for each leg of the pair back into the account currency
                if (baseCash.ConversionRate == 0m || quoteCash.ConversionRate == 0m)
                {
                    response.Error(OrderResponseErrorCode.ForexConversionRateZero, symbol + ": requires " + baseCurrency + " and " + quoteCurrency + " to have non-zero conversion rates. This can be caused by lack of data.");
                    Error(response.ErrorMessage);
                    return response;
                }
            }

            //Make sure the security has some data:
            if (!security.HasData)
            {
                response.Error(OrderResponseErrorCode.SecurityHasNoData, "There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point.");
                Error(response.ErrorMessage);
                return response;
            }

            //We've already processed too many orders: max 100 per day or the memory usage explodes
            if (Transactions.CachedOrderCount > _maxOrders)
            {
                response.Error(OrderResponseErrorCode.ExceededMaximumOrders, string.Format("You have exceeded maximum number of orders ({0}), for unlimited orders upgrade your account.", _maxOrders));
                Error(response.ErrorMessage);
                _quit = true;
                return response;
            }

            if (type == OrderType.MarketOnClose)
            {
                // must be submitted with at least 10 minutes in trading day, add buffer allow order submission
                var latestSubmissionTime = (Time.Date + security.Exchange.MarketClose).AddMinutes(-10.75);
                if (Time > latestSubmissionTime)
                {
                    // tell the user we require an 11 minute buffer, on minute data in live a user will receive the 3:49->3:50 bar at 3:50,
                    // this is already too late to submit one of these orders, so make the user do it at the 3:48->3:49 bar so it's submitted
                    // to the brokerage before 3:50.
                    response.Error(OrderResponseErrorCode.MarketOnCloseOrderTooLate, "MarketOnClose orders must be placed with at least a 11 minute buffer before market close.");
                    Error(response.ErrorMessage);
                    return response;
                }
            }

            response.Processed();

            return response;
        }

        /// <summary>
        /// Cancel order
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public OrderResponse CancelOrder(int orderId)
        {
            return Transactions.CancelOrder(orderId);
        }

        public OrderResponse UpdateOrder(MarketOrder order, int? quantity = null, string tag = null)
        {
            return Transactions.UpdateOrder(order.CreateUpdateRequest(quantity, tag));
        }

        public OrderResponse UpdateOrder(MarketOnOpenOrder order, int? quantity = null, string tag = null)
        {
            return Transactions.UpdateOrder(order.CreateUpdateRequest(quantity, tag));
        }

        public OrderResponse UpdateOrder(MarketOnCloseOrder order, int? quantity = null, string tag = null)
        {
            return Transactions.UpdateOrder(order.CreateUpdateRequest(quantity, tag));
        }

        public OrderResponse UpdateOrder(LimitOrder order, decimal? limitPrice = null, int? quantity = null, string tag = null)
        {
            return Transactions.UpdateOrder(order.CreateUpdateRequest(quantity, limitPrice, tag));
        }

        public OrderResponse UpdateOrder(StopLimitOrder order, decimal? stopPrice = null, decimal? limitPrice = null, int? quantity = null, string tag = null)
        {
            return Transactions.UpdateOrder(order.CreateUpdateRequest(quantity, stopPrice, limitPrice, tag));
        }

        public OrderResponse UpdateOrder(StopMarketOrder order, decimal? stopPrice = null, int? quantity = null, string tag = null)
        {
            return Transactions.UpdateOrder(order.CreateUpdateRequest(quantity, stopPrice, tag));
        }

        /// <summary>
        /// Liquidate all holdings. Called at the end of day for tick-strategies.
        /// </summary>
        /// <param name="symbolToLiquidate">Symbols we wish to liquidate</param>
        /// <returns>Array of order ids for liquidated symbols</returns>
        /// <seealso cref="MarketOrder"/>
        public List<int> Liquidate(string symbolToLiquidate = "")
        {
            var orderIdList = new List<int>();
            symbolToLiquidate = symbolToLiquidate.ToUpper();

            foreach (var symbol in Securities.Keys)
            {
                //Send market order to liquidate if 1, we have stock, 2, symbol matches.
                if (!Portfolio[symbol].HoldStock || (symbol != symbolToLiquidate && symbolToLiquidate != "")) continue;

                var quantity = 0;
                if (Portfolio[symbol].IsLong)
                {
                    quantity = -Portfolio[symbol].Quantity;
                }
                else
                {
                    quantity = Math.Abs(Portfolio[symbol].Quantity);
                }

                //Liquidate at market price.
                var orderResponse = Order(symbol, quantity);
                if (orderResponse.IsProcessed)
                {
                    orderIdList.Add(orderResponse.OrderId);
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
        public void SetHoldings(string symbol, double percentage, bool liquidateExistingHoldings = false)
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
        public void SetHoldings(string symbol, float percentage, bool liquidateExistingHoldings = false, string tag = "")
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
        public void SetHoldings(string symbol, int percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings);
        }

        /// <summary>
        /// Automatically place an order which will set the holdings to between 100% or -100% of *Buying Power*.
        /// E.g. SetHoldings("AAPL", 0.1); SetHoldings("IBM", -0.2); -> Sets portfolio as long 10% APPL and short 20% IBM
        /// </summary>
        /// <param name="symbol">   string Symbol indexer</param>
        /// <param name="percentage">decimal fraction of portfolio to set stock</param>
        /// <param name="liquidateExistingHoldings">bool flag to clean all existing holdings before setting new faction.</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <seealso cref="MarketOrder"/>
        public void SetHoldings(string symbol, decimal percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            //Error checks:
            if (!Portfolio.ContainsKey(symbol))
            {
                Error(symbol.ToUpper() + " not found in portfolio. Request this data when initializing the algorithm.");
                return;
            }

            //Range check values:
            if (percentage > 1) percentage = 1;
            if (percentage < -1) percentage = -1;

            //If they triggered a liquidate
            if (liquidateExistingHoldings)
            {
                foreach (var holdingSymbol in Portfolio.Keys)
                {
                    if (holdingSymbol != symbol && Portfolio[holdingSymbol].AbsoluteQuantity > 0)
                    {
                        //Go through all existing holdings [synchronously], market order the inverse quantity:
                        Order(holdingSymbol, -Portfolio[holdingSymbol].Quantity);
                    }
                }
            }

            var security = Securities[symbol];

            // compute the remaining margin for this security
            var direction = percentage > 0 ? OrderDirection.Buy : OrderDirection.Sell;

            // we need to account for the margin gained if crossing the zero line
            decimal extraMarginForClosing = 0m;
            if (security.Holdings.IsLong && direction == OrderDirection.Sell)
            {
                extraMarginForClosing = security.MarginModel.GetMaintenanceMargin(security);
            }
            else if (security.Holdings.IsShort && direction == OrderDirection.Buy)
            {
                extraMarginForClosing = security.MarginModel.GetMaintenanceMargin(security);
            }

            // compute an estimate of the buying power for this security incorporating the implied leverage
            // we don't want to apply the percentag to the required margin to bring us to zero, so we back out the 'extraMaginForClosing'
            var marginRemaining = Math.Abs(percentage)*(security.MarginModel.GetMarginRemaining(Portfolio, security, direction) - extraMarginForClosing);
            marginRemaining += extraMarginForClosing;

            //
            // Since we can't assume anything about the fee structure and the relative size of fees in
            // relation to the order size we need to perform some root finding. In general we'll only need
            // a two loops to compute a correct value. Some exotic fee structures may require more searching.
            //

            // compute the margin required for a single share
            int quantity = 1;
            var marketOrder = new MarketOrder(symbol, quantity, Time, type: security.Type) { Price = security.Price };
            var marginRequiredForSingleShare = security.MarginModel.GetInitialMarginRequiredForOrder(security, marketOrder);

            // we can't do anything if we don't have data yet
            if (security.Price == 0) return;

            // we can't even afford one more share
            if (marginRemaining < marginRequiredForSingleShare) return;

            // we want marginRequired to end up between this and marginRemaining
            var marginRequiredLowerThreshold = marginRemaining - marginRequiredForSingleShare;

            // iterate until we get a decent estimate, max out at 10 loops.
            int loops = 0;
            var marginRequired = marginRequiredForSingleShare;
            while (marginRequired > marginRemaining || marginRequired < marginRequiredLowerThreshold)
            {
                var marginPerShare = marginRequired/quantity;
                quantity = (int) Math.Truncate(marginRemaining/marginPerShare);
                marketOrder.Quantity = quantity;
                if (quantity == 0)
                {
                    // can't order anything
                    return;
                }
                marginRequired = security.MarginModel.GetInitialMarginRequiredForOrder(security, marketOrder);

                // no need to iterate longer than 10
                if (++loops > 10) break;
            }

            // nothing to change
            if (quantity == 0)
            {
                return;
            }

            // adjust for going short
            if (direction == OrderDirection.Sell)
            {
                quantity *= -1;
            }

            MarketOrder(symbol, quantity, false, tag);
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
        public OrderResponse Order(string symbol, int quantity, OrderType type, bool asynchronous = false, string tag = "")
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
        public OrderResponse Order(string symbol, decimal quantity, OrderType type)
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
        public OrderResponse Order(string symbol, int quantity, OrderType type)
        {
            return Order(symbol, quantity);
        }
    }
}
