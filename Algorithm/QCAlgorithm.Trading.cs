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
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using static QuantConnect.StringExtensions;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private int _maxOrders = 10000;
        private bool _isMarketOnOpenOrderWarningSent;
        private bool _isMarketOnOpenOrderRestrictedForFuturesWarningSent;
        private bool _isGtdTfiForMooAndMocOrdersValidationWarningSent;
        private bool _isOptionsOrderOnStockSplitWarningSent;

        /// <summary>
        /// Transaction Manager - Process transaction fills and order management.
        /// </summary>
        [DocumentationAttribute(TradingAndOrders)]
        public SecurityTransactionManager Transactions { get; set; }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, double)"/>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Buy(Symbol symbol, int quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, decimal)"/>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Buy(Symbol symbol, double quantity)
        {
            return Order(symbol, Math.Abs(quantity).SafeDecimalCast());
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <seealso cref="Order(Symbol, int)"/>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Buy(Symbol symbol, decimal quantity)
        {
            return Order(symbol, Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">float Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, decimal)"/>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Buy(Symbol symbol, float quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity));
        }


        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Sell(Symbol, decimal)"/>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Sell(Symbol symbol, int quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Sell(Symbol symbol, double quantity)
        {
            return Order(symbol, Math.Abs(quantity).SafeDecimalCast() * -1m);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Sell(Symbol symbol, float quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity) * -1m);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Sell(Symbol symbol, decimal quantity)
        {
            return Order(symbol, Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <param name="symbol">Symbol to order</param>
        /// <param name="quantity">Quantity to order</param>
        /// <seealso cref="Order(Symbol, decimal)"/>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, double quantity)
        {
            return Order(symbol, quantity.SafeDecimalCast());
        }

        /// <summary>
        /// Issue an order/trade for asset
        /// </summary>
        /// <param name="symbol">Symbol to order</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, int quantity)
        {
            return MarketOrder(symbol, (decimal)quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset
        /// </summary>
        /// <param name="symbol">Symbol to order</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, decimal quantity)
        {
            return MarketOrder(symbol, quantity);
        }

        /// <summary>
        /// Wrapper for market order method: submit a new order for quantity of symbol using type order.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOrder(symbol, quantity, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOrder(Symbol symbol, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOrder(symbol, (decimal)quantity, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOrder(Symbol symbol, double quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOrder(symbol, quantity.SafeDecimalCast(), asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOrder(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            return MarketOrder(security, quantity, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="security">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOrder(Security security, decimal quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            // check the exchange is open before sending a market order, if it's not open then convert it into a market on open order.
            // For futures and FOPs, market orders can be submitted on extended hours, so we let them through.
            if ((security.Type != SecurityType.Future && security.Type != SecurityType.FutureOption) && !security.Exchange.ExchangeOpen)
            {
                var mooTicket = MarketOnOpenOrder(security.Symbol, quantity, tag, orderProperties);
                if (!_isMarketOnOpenOrderWarningSent)
                {
                    var anyNonDailySubscriptions = security.Subscriptions.Any(x => x.Resolution != Resolution.Daily);
                    if (mooTicket.SubmitRequest.Response.IsSuccess && !anyNonDailySubscriptions)
                    {
                        Debug("Warning: all market orders sent using daily data, or market orders sent after hours are automatically converted into MarketOnOpen orders.");
                        _isMarketOnOpenOrderWarningSent = true;
                    }
                }
                return mooTicket;
            }

            var request = CreateSubmitOrderRequest(OrderType.Market, security, quantity, tag, orderProperties ?? DefaultOrderProperties?.Clone());

            //Add the order and create a new order Id.
            var ticket = SubmitOrderRequest(request);

            // Wait for the order event to process, only if the exchange is open and the order is valid
            if (ticket.Status != OrderStatus.Invalid && !asynchronous)
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
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOnOpenOrder(Symbol symbol, double quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOnOpenOrder(symbol, quantity.SafeDecimalCast(), tag, orderProperties);
        }

        /// <summary>
        /// Market on open order implementation: Send a market order when the exchange opens
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOnOpenOrder(Symbol symbol, int quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOnOpenOrder(symbol, (decimal)quantity, tag, orderProperties);
        }

        /// <summary>
        /// Market on open order implementation: Send a market order when the exchange opens
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOnOpenOrder(Symbol symbol, decimal quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            var properties = orderProperties ?? DefaultOrderProperties?.Clone();
            InvalidateGoodTilDateTimeInForce(properties);

            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.MarketOnOpen, security, quantity, tag, properties);

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOnCloseOrder(Symbol symbol, int quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOnCloseOrder(symbol, (decimal)quantity, tag, orderProperties);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOnCloseOrder(Symbol symbol, double quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            return MarketOnCloseOrder(symbol, quantity.SafeDecimalCast(), tag, orderProperties);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket MarketOnCloseOrder(Symbol symbol, decimal quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            var properties = orderProperties ?? DefaultOrderProperties?.Clone();
            InvalidateGoodTilDateTimeInForce(properties);

            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.MarketOnClose, security, quantity, tag, properties);

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket LimitOrder(Symbol symbol, int quantity, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return LimitOrder(symbol, (decimal)quantity, limitPrice, tag, orderProperties);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket LimitOrder(Symbol symbol, double quantity, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return LimitOrder(symbol, quantity.SafeDecimalCast(), limitPrice, tag, orderProperties);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket LimitOrder(Symbol symbol, decimal quantity, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.Limit, security, quantity, tag, limitPrice: limitPrice, properties: orderProperties ?? DefaultOrderProperties?.Clone());

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket StopMarketOrder(Symbol symbol, int quantity, decimal stopPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return StopMarketOrder(symbol, (decimal)quantity, stopPrice, tag, orderProperties);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket StopMarketOrder(Symbol symbol, double quantity, decimal stopPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return StopMarketOrder(symbol, quantity.SafeDecimalCast(), stopPrice, tag, orderProperties);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket StopMarketOrder(Symbol symbol, decimal quantity, decimal stopPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.StopMarket, security, quantity, tag, stopPrice: stopPrice, properties: orderProperties ?? DefaultOrderProperties?.Clone());

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Create a trailing stop order and return the newly created order id; or negative if the order is invalid.
        /// It will calculate the stop price using the trailing amount and the current market price.
        /// </summary>
        /// <param name="symbol">Trading asset symbol</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket TrailingStopOrder(Symbol symbol, int quantity, decimal trailingAmount, bool trailingAsPercentage,
            string tag = "", IOrderProperties orderProperties = null)
        {
            return TrailingStopOrder(symbol, (decimal)quantity, trailingAmount, trailingAsPercentage, tag, orderProperties);
        }

        /// <summary>
        /// Create a trailing stop order and return the newly created order id; or negative if the order is invalid.
        /// It will calculate the stop price using the trailing amount and the current market price.
        /// </summary>
        /// <param name="symbol">Trading asset symbol</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket TrailingStopOrder(Symbol symbol, double quantity, decimal trailingAmount, bool trailingAsPercentage,
            string tag = "", IOrderProperties orderProperties = null)
        {
            return TrailingStopOrder(symbol, quantity.SafeDecimalCast(), trailingAmount, trailingAsPercentage, tag, orderProperties);
        }

        /// <summary>
        /// Create a trailing stop order and return the newly created order id; or negative if the order is invalid.
        /// It will calculate the stop price using the trailing amount and the current market price.
        /// </summary>
        /// <param name="symbol">Trading asset symbol</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket TrailingStopOrder(Symbol symbol, decimal quantity, decimal trailingAmount, bool trailingAsPercentage,
            string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            var stopPrice = Orders.TrailingStopOrder.CalculateStopPrice(security.Price, trailingAmount, trailingAsPercentage,
                quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell);
            return TrailingStopOrder(symbol, quantity, stopPrice, trailingAmount, trailingAsPercentage, tag, orderProperties);
        }

        /// <summary>
        /// Create a trailing stop order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">Trading asset symbol</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Initial stop price at which the order should be triggered</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket TrailingStopOrder(Symbol symbol, int quantity, decimal stopPrice, decimal trailingAmount, bool trailingAsPercentage,
            string tag = "", IOrderProperties orderProperties = null)
        {
            return TrailingStopOrder(symbol, (decimal)quantity, stopPrice, trailingAmount, trailingAsPercentage, tag, orderProperties);
        }

        /// <summary>
        /// Create a trailing stop order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">Trading asset symbol</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Initial stop price at which the order should be triggered</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket TrailingStopOrder(Symbol symbol, double quantity, decimal stopPrice, decimal trailingAmount, bool trailingAsPercentage,
            string tag = "", IOrderProperties orderProperties = null)
        {
            return TrailingStopOrder(symbol, quantity.SafeDecimalCast(), stopPrice, trailingAmount, trailingAsPercentage, tag, orderProperties);
        }

        /// <summary>
        /// Create a trailing stop order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">Trading asset symbol</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Initial stop price at which the order should be triggered</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket TrailingStopOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal trailingAmount, bool trailingAsPercentage,
            string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(
                OrderType.TrailingStop,
                security,
                quantity,
                tag,
                stopPrice: stopPrice,
                trailingAmount: trailingAmount,
                trailingAsPercentage: trailingAsPercentage,
                properties: orderProperties ?? DefaultOrderProperties?.Clone());

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Send a stop limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="stopPrice">Stop price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket StopLimitOrder(Symbol symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return StopLimitOrder(symbol, (decimal)quantity, stopPrice, limitPrice, tag, orderProperties);
        }

        /// <summary>
        /// Send a stop limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="stopPrice">Stop price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket StopLimitOrder(Symbol symbol, double quantity, decimal stopPrice, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return StopLimitOrder(symbol, quantity.SafeDecimalCast(), stopPrice, limitPrice, tag, orderProperties);
        }

        /// <summary>
        /// Send a stop limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="stopPrice">Stop price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket StopLimitOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.StopLimit, security, quantity, tag, stopPrice: stopPrice, limitPrice: limitPrice, properties: orderProperties ?? DefaultOrderProperties?.Clone());

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Send a limit if touched order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="triggerPrice">Trigger price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket LimitIfTouchedOrder(Symbol symbol, int quantity, decimal triggerPrice, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return LimitIfTouchedOrder(symbol, (decimal)quantity, triggerPrice, limitPrice, tag, orderProperties);
        }

        /// <summary>
        /// Send a limit if touched order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="triggerPrice">Trigger price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket LimitIfTouchedOrder(Symbol symbol, double quantity, decimal triggerPrice, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            return LimitIfTouchedOrder(symbol, quantity.SafeDecimalCast(), triggerPrice, limitPrice, tag, orderProperties);
        }

        /// <summary>
        /// Send a limit if touched order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="triggerPrice">Trigger price for this order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket LimitIfTouchedOrder(Symbol symbol, decimal quantity, decimal triggerPrice, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.LimitIfTouched, security, quantity, tag, triggerPrice: triggerPrice, limitPrice: limitPrice, properties: orderProperties ?? DefaultOrderProperties?.Clone());

            return SubmitOrderRequest(request);
        }

        /// <summary>
        /// Send an exercise order to the transaction handler
        /// </summary>
        /// <param name="optionSymbol">String symbol for the option position</param>
        /// <param name="quantity">Quantity of options contracts</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket ExerciseOption(Symbol optionSymbol, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            var option = (Option)Securities[optionSymbol];

            // SubmitOrderRequest.Quantity indicates the change in holdings quantity, therefore manual exercise quantities must be negative
            // PreOrderChecksImpl confirms that we don't hold a short position, so we're lenient here and accept +/- quantity values
            var request = CreateSubmitOrderRequest(OrderType.OptionExercise, option, -Math.Abs(quantity), tag, orderProperties ?? DefaultOrderProperties?.Clone());

            //Initialize the exercise order parameters
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

        // Support for option strategies trading

        /// <summary>
        /// Buy Option Strategy (Alias of Order)
        /// </summary>
        /// <param name="strategy">Specification of the strategy to trade</param>
        /// <param name="quantity">Quantity of the strategy to trade</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>Sequence of order tickets</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Buy(OptionStrategy strategy, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Order(strategy, Math.Abs(quantity), asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Sell Option Strategy (alias of Order)
        /// </summary>
        /// <param name="strategy">Specification of the strategy to trade</param>
        /// <param name="quantity">Quantity of the strategy to trade</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>Sequence of order tickets</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Sell(OptionStrategy strategy, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Order(strategy, Math.Abs(quantity) * -1, asynchronous, tag, orderProperties);
        }

        /// <summary>
        ///  Issue an order/trade for buying/selling an option strategy
        /// </summary>
        /// <param name="strategy">Specification of the strategy to trade</param>
        /// <param name="quantity">Quantity of the strategy to trade</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>Sequence of order tickets</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Order(OptionStrategy strategy, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return GenerateOptionStrategyOrders(strategy, quantity, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Issue a combo market order/trade for multiple assets
        /// </summary>
        /// <param name="legs">The list of legs the order consists of</param>
        /// <param name="quantity">The total quantity for the order</param>
        /// <param name="asynchronous">Send the order asynchronously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>Sequence of order tickets, one for each leg</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> ComboMarketOrder(List<Leg> legs, int quantity, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return SubmitComboOrder(legs, quantity, 0, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Issue a combo leg limit order/trade for multiple assets, each having its own limit price.
        /// </summary>
        /// <param name="legs">The list of legs the order consists of</param>
        /// <param name="quantity">The total quantity for the order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>Sequence of order tickets, one for each leg</returns>
        /// <exception cref="ArgumentException">If not every leg has a defined limit price</exception>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> ComboLegLimitOrder(List<Leg> legs, int quantity, string tag = "", IOrderProperties orderProperties = null)
        {
            if (legs.Any(x => x.OrderPrice == null || x.OrderPrice == 0))
            {
                throw new ArgumentException("ComboLegLimitOrder requires a limit price for each leg");
            }

            return SubmitComboOrder(legs, quantity, 0, asynchronous: true, tag, orderProperties);
        }

        /// <summary>
        /// Issue a combo limit order/trade for multiple assets.
        /// A single limit price is defined for the combo order and will fill only if the sum of the assets price compares properly to the limit price, depending on the direction.
        /// </summary>
        /// <param name="legs">The list of legs the order consists of</param>
        /// <param name="quantity">The total quantity for the order</param>
        /// <param name="limitPrice">The compound limit price to use for a ComboLimit order. This limit price will compared to the sum of the assets price in order to fill the order.</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>Sequence of order tickets, one for each leg</returns>
        /// <exception cref="ArgumentException">If the order type is neither ComboMarket, ComboLimit nor ComboLegLimit</exception>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> ComboLimitOrder(List<Leg> legs, int quantity, decimal limitPrice, string tag = "", IOrderProperties orderProperties = null)
        {
            if (limitPrice == 0)
            {
                throw new ArgumentException("ComboLimitOrder requires a limit price");
            }

            if (legs.Any(x => x.OrderPrice != null && x.OrderPrice != 0))
            {
                throw new ArgumentException("ComboLimitOrder does not support limit prices for individual legs");
            }

            return SubmitComboOrder(legs, quantity, limitPrice, asynchronous: true, tag, orderProperties);
        }

        private List<OrderTicket> GenerateOptionStrategyOrders(OptionStrategy strategy, int strategyQuantity, bool asynchronous, string tag, IOrderProperties orderProperties)
        {
            // if the option strategy canonical is set let's use it to make sure we target the right option, for example SPXW for SPX underlying,
            // it could be null if the user created the option strategy manually and just set the underlying, in which case we use the default option target by using 'null'
            var targetOption = strategy.CanonicalOption != null ? strategy.CanonicalOption.Canonical.ID.Symbol : null;

            // setting up the tag text for all orders of one strategy
            tag ??= $"{strategy.Name} ({strategyQuantity.ToStringInvariant()})";

            var legs = new List<Leg>(strategy.UnderlyingLegs);

            // WHY: the option strategy doesn't specify the option style (and in consequence the symbol), so we figure it out at runtime
            foreach (var optionLeg in strategy.OptionLegs)
            {
                Leg leg = null;
                // search for both american/european style -- much better than looping through all securities
                foreach (var optionStyle in new[] { OptionStyle.American, OptionStyle.European })
                {
                    var option = QuantConnect.Symbol.CreateOption(strategy.Underlying, targetOption, strategy.Underlying.ID.Market, optionStyle, optionLeg.Right, optionLeg.Strike, optionLeg.Expiration);
                    if (Securities.ContainsKey(option))
                    {
                        // we found it, we add it a break/stop searching
                        leg = new Leg { Symbol = option, OrderPrice = optionLeg.OrderPrice, Quantity = optionLeg.Quantity };
                        break;
                    }
                }

                if (leg == null)
                {
                    throw new InvalidOperationException("Couldn't find the option contract in algorithm securities list. " +
                        Invariant($"Underlying: {strategy.Underlying}, option {optionLeg.Right}, strike {optionLeg.Strike}, ") +
                        Invariant($"expiration: {optionLeg.Expiration}")
                    );
                }
                legs.Add(leg);
            }

            return SubmitComboOrder(legs, strategyQuantity, 0, asynchronous, tag, orderProperties);
        }

        private List<OrderTicket> SubmitComboOrder(List<Leg> legs, decimal quantity, decimal limitPrice, bool asynchronous, string tag, IOrderProperties orderProperties)
        {
            CheckComboOrderSizing(legs, quantity);

            var orderType = OrderType.ComboMarket;
            if (limitPrice != 0)
            {
                orderType = OrderType.ComboLimit;
            }

            // we create a unique Id so the algorithm and the brokerage can relate the combo orders with each other
            var groupOrderManager = new GroupOrderManager(Transactions.GetIncrementGroupOrderManagerId(), legs.Count, quantity, limitPrice);

            List<OrderTicket> orderTickets = new(capacity: legs.Count);
            List<SubmitOrderRequest> submitRequests = new(capacity: legs.Count);
            foreach (var leg in legs)
            {
                var security = Securities[leg.Symbol];

                if (leg.OrderPrice.HasValue)
                {
                    // limit price per leg!
                    limitPrice = leg.OrderPrice.Value;
                    orderType = OrderType.ComboLegLimit;
                }
                var request = CreateSubmitOrderRequest(
                    orderType,
                    security,
                    ((decimal)leg.Quantity).GetOrderLegGroupQuantity(groupOrderManager),
                    tag,
                    orderProperties ?? DefaultOrderProperties?.Clone(),
                    groupOrderManager: groupOrderManager,
                    limitPrice: limitPrice);

                // we execture pre order checks for all requests before submitting, so that if anything fails we are not left with half submitted combo orders
                var response = PreOrderChecks(request);
                if (response.IsError)
                {
                    orderTickets.Add(OrderTicket.InvalidSubmitRequest(Transactions, request, response));
                    return orderTickets;
                }

                submitRequests.Add(request);
            }

            foreach (var request in submitRequests)
            {
                //Add the order and create a new order Id.
                orderTickets.Add(Transactions.AddOrder(request));
            }

            // Wait for the order event to process, only if the exchange is open
            if (!asynchronous)
            {
                foreach (var ticket in orderTickets)
                {
                    if (ticket.Status.IsOpen())
                    {
                        Transactions.WaitForOrder(ticket.OrderId);
                    }
                }
            }

            return orderTickets;
        }

        /// <summary>
        /// Will submit an order request to the algorithm
        /// </summary>
        /// <param name="request">The request to submit</param>
        /// <remarks>Will run order prechecks, which include making sure the algorithm is not warming up, security is added and has data among others</remarks>
        /// <returns>The order ticket</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket SubmitOrderRequest(SubmitOrderRequest request)
        {
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            //Add the order and create a new order Id.
            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Perform pre-order checks to ensure we have sufficient capital,
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
        /// Perform pre-order checks to ensure we have sufficient capital,
        /// the market is open, and we haven't exceeded maximum realistic orders per day.
        /// </summary>
        /// <returns>OrderResponse. If no error, order request is submitted.</returns>
        private OrderResponse PreOrderChecksImpl(SubmitOrderRequest request)
        {
            if (IsWarmingUp)
            {
                return OrderResponse.WarmingUp(request);
            }

            //Most order methods use security objects; so this isn't really used.
            // todo: Left here for now but should review
            Security security;
            if (!Securities.TryGetValue(request.Symbol, out security))
            {
                return OrderResponse.MissingSecurity(request);
            }

            //Ordering 0 is useless.
            if (request.Quantity == 0)
            {
                return OrderResponse.ZeroQuantity(request);
            }

            if (Math.Abs(request.Quantity) < security.SymbolProperties.LotSize)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.OrderQuantityLessThanLotSize,
                    Invariant($"Unable to {request.OrderRequestType.ToLower()} order with id {request.OrderId} which ") +
                    Invariant($"quantity ({Math.Abs(request.Quantity)}) is less than lot ") +
                    Invariant($"size ({security.SymbolProperties.LotSize}).")
                );
            }

            if (!security.IsTradable)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.NonTradableSecurity,
                    $"The security with symbol '{request.Symbol}' is marked as non-tradable."
                );
            }

            var price = security.Price;

            //Check the exchange is open before sending a exercise orders
            if (request.OrderType == OrderType.OptionExercise && !security.Exchange.ExchangeOpen)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.ExchangeNotOpen,
                    $"{request.OrderType} order and exchange not open."
                );
            }

            //Check the exchange is open before sending a market on open order for futures
            if ((security.Type == SecurityType.Future || security.Type == SecurityType.FutureOption) && request.OrderType == OrderType.MarketOnOpen)
            {
                if (!_isMarketOnOpenOrderRestrictedForFuturesWarningSent)
                {
                    Debug("Warning: Market-On-Open orders are not allowed for futures and future options. Consider using limit orders during extended market hours.");
                    _isMarketOnOpenOrderRestrictedForFuturesWarningSent = true;
                }

                return OrderResponse.Error(request, OrderResponseErrorCode.ExchangeNotOpen,
                    $"{request.OrderType} orders not supported for {security.Type}."
                );
            }

            if (price == 0)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.SecurityPriceZero, request.Symbol.GetZeroPriceMessage());
            }

            // check quote currency existence/conversion rate on all orders
            var quoteCurrency = security.QuoteCurrency.Symbol;
            if (!Portfolio.CashBook.TryGetValue(quoteCurrency, out var quoteCash))
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.QuoteCurrencyRequired,
                    $"{request.Symbol.Value}: requires {quoteCurrency} in the cashbook to trade."
                );
            }
            if (security.QuoteCurrency.ConversionRate == 0m)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.ConversionRateZero,
                    $"{request.Symbol.Value}: requires {quoteCurrency} to have a non-zero conversion rate. This can be caused by lack of data."
                );
            }

            // need to also check base currency existence/conversion rate on forex orders
            if (security.Type == SecurityType.Forex || security.Type == SecurityType.Crypto)
            {
                var baseCurrency = ((IBaseCurrencySymbol)security).BaseCurrency.Symbol;
                if (!Portfolio.CashBook.TryGetValue(baseCurrency, out var baseCash))
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.ForexBaseAndQuoteCurrenciesRequired,
                        $"{request.Symbol.Value}: requires {baseCurrency} and {quoteCurrency} in the cashbook to trade."
                    );
                }
                if (baseCash.ConversionRate == 0m)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.ForexConversionRateZero,
                        $"{request.Symbol.Value}: requires {baseCurrency} and {quoteCurrency} to have non-zero conversion rates. This can be caused by lack of data."
                    );
                }
            }

            //Make sure the security has some data:
            if (!security.HasData)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.SecurityHasNoData,
                    "There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point."
                );
            }

            // We've already processed too many orders: max 10k
            if (!LiveMode && Transactions.OrdersCount > _maxOrders)
            {
                Status = AlgorithmStatus.Stopped;
                return OrderResponse.Error(request, OrderResponseErrorCode.ExceededMaximumOrders,
                    Invariant($"You have exceeded maximum number of orders ({_maxOrders}), for unlimited orders upgrade your account.")
                );
            }

            if (request.OrderType == OrderType.OptionExercise)
            {
                if (!security.Type.IsOption())
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.NonExercisableSecurity,
                        $"The security with symbol '{request.Symbol}' is not exercisable."
                    );
                }

                if ((security as Option).Style == OptionStyle.European && UtcTime.Date < security.Symbol.ID.Date.ConvertToUtc(security.Exchange.TimeZone).Date)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.EuropeanOptionNotExpiredOnExercise,
                        $"Cannot exercise European style option with symbol '{request.Symbol}' before its expiration date."
                    );
                }

                if (security.Holdings.IsShort)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.UnsupportedRequestType,
                        $"The security with symbol '{request.Symbol}' has a short option position. Only long option positions are exercisable."
                    );
                }

                if (Math.Abs(request.Quantity) > security.Holdings.Quantity)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.UnsupportedRequestType,
                        $"Cannot exercise more contracts of '{request.Symbol}' than is currently available in the portfolio. "
                    );
                }
            }

            if (request.OrderType == OrderType.MarketOnOpen)
            {
                if (security.Exchange.Hours.IsMarketAlwaysOpen)
                {
                    throw new InvalidOperationException($"Market never closes for this symbol {security.Symbol}, can no submit a {nameof(OrderType.MarketOnOpen)} order.");
                }

                if (security.Exchange.Hours.IsOpen(security.LocalTime, false))
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.MarketOnOpenNotAllowedDuringRegularHours, $"Cannot submit a {nameof(OrderType.MarketOnOpen)} order while the market is open.");
                }
            }
            else if (request.OrderType == OrderType.MarketOnClose)
            {
                if (security.Exchange.Hours.IsMarketAlwaysOpen)
                {
                    throw new InvalidOperationException($"Market never closes for this symbol {security.Symbol}, can no submit a {nameof(OrderType.MarketOnClose)} order.");
                }

                var nextMarketClose = security.Exchange.Hours.GetNextMarketClose(security.LocalTime, false);

                // Enforce MarketOnClose submission buffer
                var latestSubmissionTimeUtc = nextMarketClose
                    .ConvertToUtc(security.Exchange.TimeZone)
                    .Subtract(Orders.MarketOnCloseOrder.SubmissionTimeBuffer);
                if (UtcTime > latestSubmissionTimeUtc)
                {
                    // Tell user the required buffer on these orders, also inform them it can be changed for special cases.
                    // Default buffer is 15.5 minutes because with minute data a user will receive the 3:44->3:45 bar at 3:45,
                    // if the latest time is 3:45 it is already too late to submit one of these orders
                    return OrderResponse.Error(request, OrderResponseErrorCode.MarketOnCloseOrderTooLate,
                        $"MarketOnClose orders must be placed within {Orders.MarketOnCloseOrder.SubmissionTimeBuffer} before market close." +
                        " Override this TimeSpan buffer by setting Orders.MarketOnCloseOrder.SubmissionTimeBuffer in QCAlgorithm.Initialize()."
                    );
                }
            }

            if (request.OrderType == OrderType.ComboMarket && request.LimitPrice != 0)
            {
                // just in case some validation
                throw new ArgumentException("Can not set a limit price using market combo orders");
            }

            // Check for splits. Option are selected before the security price is split-adjusted, so in this time step
            // we don't allow option orders to make sure they are properly filtered using the right security price.
            if (request.SecurityType.IsOption() &&
                CurrentSlice != null &&
                CurrentSlice.Splits.Count > 0 &&
                CurrentSlice.Splits.TryGetValue(request.Symbol.Underlying, out _))
            {
                if (!_isOptionsOrderOnStockSplitWarningSent)
                {
                    Debug("Warning: Options orders are not allowed when a split occurred for its underlying stock");
                    _isOptionsOrderOnStockSplitWarningSent = true;
                }

                return OrderResponse.Error(request, OrderResponseErrorCode.OptionOrderOnStockSplit,
                    "Options orders are not allowed when a split occurred for its underlying stock");
            }

            // passes all initial order checks
            return OrderResponse.Success(request);
        }

        /// <summary>
        /// Liquidate your portfolio holdings
        /// </summary>
        /// <param name="symbol">Specific asset to liquidate, defaults to all</param>
        /// <param name="asynchronous">Flag to indicate if the symbols should be liquidated asynchronously</param>
        /// <param name="tag">Custom tag to know who is calling this</param>
        /// <param name="orderProperties">Order properties to use</param>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Liquidate(Symbol symbol = null, bool asynchronous = false, string tag = null, IOrderProperties orderProperties = null)
        {
            IEnumerable<Symbol> toLiquidate;
            if (symbol != null)
            {
                toLiquidate = Securities.ContainsKey(symbol)
                    ? new[] { symbol } : Enumerable.Empty<Symbol>();
            }
            else
            {
                toLiquidate = Securities.Keys.OrderBy(x => x.Value);
            }

            return Liquidate(toLiquidate, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Liquidate your portfolio holdings
        /// </summary>
        /// <param name="symbols">List of symbols to liquidate, defaults to all</param>
        /// <param name="asynchronous">Flag to indicate if the symbols should be liquidated asynchronously</param>
        /// <param name="tag">Custom tag to know who is calling this</param>
        /// <param name="orderProperties">Order properties to use</param>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Liquidate(IEnumerable<Symbol> symbols, bool asynchronous = false, string tag = null, IOrderProperties orderProperties = null)
        {
            var orderTickets = new List<OrderTicket>();
            if (!Settings.LiquidateEnabled)
            {
                Debug("Liquidate() is currently disabled by settings. To re-enable please set 'Settings.LiquidateEnabled' to true");
                return orderTickets;
            }

            tag ??= "Liquidated";
            foreach (var symbolToLiquidate in symbols)
            {
                // get open orders
                var orders = Transactions.GetOpenOrders(symbolToLiquidate);

                // get quantity in portfolio
                var quantity = 0m;
                var holdings = Portfolio[symbolToLiquidate];
                if (holdings.Invested)
                {
                    // invested flag might filter some quantity that's less than lot size
                    quantity = holdings.Quantity;
                }

                // if there is only one open market order that would close the position, do nothing
                if (orders.Count == 1 && quantity != 0 && orders[0].Quantity == -quantity && orders[0].Type == OrderType.Market)
                {
                    continue;
                }

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
                        Transactions.CancelOrder(order.Id, tag);
                    }
                }

                // Liquidate at market price
                if (quantity != 0)
                {
                    // calculate quantity for closing market order
                    var ticket = Order(symbolToLiquidate, -quantity - marketOrdersQuantity, asynchronous: asynchronous, tag: tag, orderProperties: orderProperties);
                    orderTickets.Add(ticket);
                }
            }

            return orderTickets;
        }

        /// <summary>
        /// Liquidate all holdings and cancel open orders. Called at the end of day for tick-strategies.
        /// </summary>
        /// <param name="symbolToLiquidate">Symbol we wish to liquidate</param>
        /// <param name="tag">Custom tag to know who is calling this.</param>
        /// <returns>Array of order ids for liquidated symbols</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [Obsolete($"This method is obsolete, please use Liquidate(symbol: symbolToLiquidate, tag: tag) method")]
        public List<int> Liquidate(Symbol symbolToLiquidate, string tag)
        {
            return Liquidate(symbol: symbolToLiquidate, tag: tag).Select(x => x.OrderId).ToList();
        }

        /// <summary>
        /// Maximum number of orders for the algorithm
        /// </summary>
        /// <param name="max"></param>
        [DocumentationAttribute(TradingAndOrders)]
        public void SetMaximumOrders(int max)
        {
            if (!_locked)
            {
                _maxOrders = max;
            }
        }

        /// <summary>
        /// Sets holdings for a collection of targets.
        /// The implementation will order the provided targets executing first those that
        /// reduce a position, freeing margin.
        /// </summary>
        /// <param name="targets">The portfolio desired quantities as percentages</param>
        /// <param name="liquidateExistingHoldings">True will liquidate existing holdings</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>A list of order tickets.</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> SetHoldings(List<PortfolioTarget> targets, bool liquidateExistingHoldings = false, string tag = null, IOrderProperties orderProperties = null)
        {
            List<OrderTicket> orderTickets = null;
            //If they triggered a liquidate
            if (liquidateExistingHoldings)
            {
                orderTickets = Liquidate(GetSymbolsToLiquidate(targets.Select(t => t.Symbol)), tag: tag, orderProperties: orderProperties);
            }
            orderTickets ??= new List<OrderTicket>();

            foreach (var portfolioTarget in targets
                // we need to create targets with quantities for OrderTargetsByMarginImpact
                .Select(target => new PortfolioTarget(target.Symbol, CalculateOrderQuantity(target.Symbol, target.Quantity)))
                .OrderTargetsByMarginImpact(this, targetIsDelta: true))
            {
                var tickets = SetHoldingsImpl(portfolioTarget.Symbol, portfolioTarget.Quantity, false, tag, orderProperties);
                orderTickets.AddRange(tickets);
            }
            return orderTickets;
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">double percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">liquidate existing holdings if necessary to hold this stock</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>A list of order tickets.</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> SetHoldings(Symbol symbol, double percentage, bool liquidateExistingHoldings = false, string tag = null, IOrderProperties orderProperties = null)
        {
            return SetHoldings(symbol, percentage.SafeDecimalCast(), liquidateExistingHoldings, tag, orderProperties);
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">float percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">bool liquidate existing holdings if necessary to hold this stock</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>A list of order tickets.</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> SetHoldings(Symbol symbol, float percentage, bool liquidateExistingHoldings = false, string tag = null, IOrderProperties orderProperties = null)
        {
            return SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings, tag, orderProperties);
        }

        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">float percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">bool liquidate existing holdings if necessary to hold this stock</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>A list of order tickets.</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> SetHoldings(Symbol symbol, int percentage, bool liquidateExistingHoldings = false, string tag = null, IOrderProperties orderProperties = null)
        {
            return SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings, tag, orderProperties);
        }

        /// <summary>
        /// Automatically place a market order which will set the holdings to between 100% or -100% of *PORTFOLIO VALUE*.
        /// E.g. SetHoldings("AAPL", 0.1); SetHoldings("IBM", -0.2); -> Sets portfolio as long 10% APPL and short 20% IBM
        /// E.g. SetHoldings("AAPL", 2); -> Sets apple to 2x leveraged with all our cash.
        /// If the market is closed, place a market on open order.
        /// </summary>
        /// <param name="symbol">Symbol indexer</param>
        /// <param name="percentage">decimal fraction of portfolio to set stock</param>
        /// <param name="liquidateExistingHoldings">bool flag to clean all existing holdings before setting new faction.</param>
        /// <param name="tag">Tag the order with a short string.</param>
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>A list of order tickets.</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol, decimal, bool, string, IOrderProperties)"/>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> SetHoldings(Symbol symbol, decimal percentage, bool liquidateExistingHoldings = false, string tag = null, IOrderProperties orderProperties = null)
        {
            return SetHoldingsImpl(symbol, CalculateOrderQuantity(symbol, percentage), liquidateExistingHoldings, tag, orderProperties);
        }

        /// <summary>
        /// Set holdings implementation, which uses order quantities (delta) not percentage nor target final quantity
        /// </summary>
        private List<OrderTicket> SetHoldingsImpl(Symbol symbol, decimal orderQuantity, bool liquidateExistingHoldings = false, string tag = null, IOrderProperties orderProperties = null)
        {
            List<OrderTicket> orderTickets = null;
            //If they triggered a liquidate
            if (liquidateExistingHoldings)
            {
                orderTickets = Liquidate(GetSymbolsToLiquidate([symbol]), tag: tag, orderProperties: orderProperties);
            }

            orderTickets ??= new List<OrderTicket>();
            tag ??= "";
            //Calculate total unfilled quantity for open market orders
            var marketOrdersQuantity = Transactions.GetOpenOrderTickets(
                    ticket => ticket.Symbol == symbol
                              && (ticket.OrderType == OrderType.Market
                                  || ticket.OrderType == OrderType.MarketOnOpen))
                .Aggregate(0m, (d, ticket) => d + ticket.Quantity - ticket.QuantityFilled);

            //Only place trade if we've got > 1 share to order.
            var quantity = orderQuantity - marketOrdersQuantity;
            if (Math.Abs(quantity) > 0)
            {
                Security security;
                if (!Securities.TryGetValue(symbol, out security))
                {
                    Error($"{symbol} not found in portfolio. Request this data when initializing the algorithm.");
                    return orderTickets;
                }

                //Check whether the exchange is open to send a market order. If not, send a market on open order instead
                OrderTicket ticket;
                if (security.Exchange.ExchangeOpen)
                {
                    ticket = MarketOrder(symbol, quantity, false, tag, orderProperties);
                }
                else
                {
                    ticket = MarketOnOpenOrder(symbol, quantity, tag, orderProperties);
                }
                orderTickets.Add(ticket);
            }
            return orderTickets;
        }

        /// <summary>
        /// Returns the symbols in the portfolio to be liquidated, excluding the provided symbols.
        /// </summary>
        /// <param name="symbols">The list of symbols to exclude from liquidation.</param>
        /// <returns>A list of symbols to liquidate.</returns>
        private List<Symbol> GetSymbolsToLiquidate(IEnumerable<Symbol> symbols)
        {
            var targetSymbols = new HashSet<Symbol>(symbols);
            var symbolsToLiquidate = Portfolio.Keys
                .Where(symbol => !targetSymbols.Contains(symbol))
                .OrderBy(symbol => symbol.Value)
                .ToList();
            return symbolsToLiquidate;
        }

        /// <summary>
        /// Calculate the order quantity to achieve target-percent holdings.
        /// </summary>
        /// <param name="symbol">Security object we're asking for</param>
        /// <param name="target">Target percentage holdings</param>
        /// <returns>Order quantity to achieve this percentage</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public decimal CalculateOrderQuantity(Symbol symbol, double target)
        {
            return CalculateOrderQuantity(symbol, target.SafeDecimalCast());
        }

        /// <summary>
        /// Calculate the order quantity to achieve target-percent holdings.
        /// </summary>
        /// <param name="symbol">Security object we're asking for</param>
        /// <param name="target">Target percentage holdings, this is an unleveraged value, so
        /// if you have 2x leverage and request 100% holdings, it will utilize half of the
        /// available margin</param>
        /// <returns>Order quantity to achieve this percentage</returns>
        [DocumentationAttribute(TradingAndOrders)]
        public decimal CalculateOrderQuantity(Symbol symbol, decimal target)
        {
            var percent = PortfolioTarget.Percent(this, symbol, target, true);

            if (percent == null)
            {
                return 0;
            }
            return percent.Quantity;
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
        /// <param name="orderProperties">The order properties to use. Defaults to <see cref="DefaultOrderProperties"/></param>
        /// <returns>The order ticket instance.</returns>
        [Obsolete("This Order method has been made obsolete, use Order(string, int, bool, string) method instead. Calls to the obsolete method will only generate market orders.")]
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, int quantity, OrderType type, bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
        {
            return Order(symbol, quantity, asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Obsolete method for placing orders.
        /// </summary>
        /// <param name="symbol">Symbol we want to order</param>
        /// <param name="quantity">The quantity to order</param>
        /// <param name="type">The order type</param>
        /// <returns>The order ticket instance.</returns>
        [Obsolete("This Order method has been made obsolete, use the specialized Order helper methods instead. Calls to the obsolete method will only generate market orders.")]
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, decimal quantity, OrderType type)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Obsolete method for placing orders.
        /// </summary>
        /// <param name="symbol">Symbol we want to order</param>
        /// <param name="quantity">The quantity to order</param>
        /// <param name="type">The order type</param>
        /// <returns>The order ticket instance.</returns>
        [Obsolete("This Order method has been made obsolete, use the specialized Order helper methods instead. Calls to the obsolete method will only generate market orders.")]
        [DocumentationAttribute(TradingAndOrders)]
        public OrderTicket Order(Symbol symbol, int quantity, OrderType type)
        {
            return Order(symbol, (decimal)quantity);
        }

        /// <summary>
        /// Determines if the exchange for the specified symbol is open at the current time.
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>True if the exchange is considered open at the current time, false otherwise</returns>
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        public bool IsMarketOpen(Symbol symbol)
        {
            if (Securities.TryGetValue(symbol, out var security))
            {
                return security.IsMarketOpen(false);
            }
            return symbol.IsMarketOpen(UtcTime, false);
        }

        private SubmitOrderRequest CreateSubmitOrderRequest(OrderType orderType, Security security, decimal quantity, string tag,
            IOrderProperties properties, decimal stopPrice = 0m, decimal limitPrice = 0m, decimal triggerPrice = 0m, decimal trailingAmount = 0m,
            bool trailingAsPercentage = false, GroupOrderManager groupOrderManager = null)
        {
            return new SubmitOrderRequest(orderType, security.Type, security.Symbol, quantity, stopPrice, limitPrice, triggerPrice, trailingAmount,
                trailingAsPercentage, UtcTime, tag, properties, groupOrderManager);
        }

        private static void CheckComboOrderSizing(List<Leg> legs, decimal quantity)
        {
            var greatestsCommonDivisor = Math.Abs(legs.Select(leg => leg.Quantity).GreatestCommonDivisor());

            if (greatestsCommonDivisor != 1)
            {
                throw new ArgumentException(
                    "The global combo quantity should be used to increase or reduce the size of the order, " +
                    "while the leg quantities should be used to specify the ratio of the order. " +
                    "The combo order quantities should be reduced " +
                    $"from {quantity}x({string.Join(", ", legs.Select(leg => $"{leg.Quantity} {leg.Symbol}"))}) " +
                    $"to {quantity * greatestsCommonDivisor}x({string.Join(", ", legs.Select(leg => $"{leg.Quantity / greatestsCommonDivisor} {leg.Symbol}"))}).");
            }
        }

        /// <summary>
        /// Resets the time-in-force to the default <see cref="TimeInForce.GoodTilCanceled" /> if the given one is a <see cref="GoodTilDateTimeInForce"/>.
        /// This is required for MOO and MOC orders, for which GTD is not supported.
        /// </summary>
        private void InvalidateGoodTilDateTimeInForce(IOrderProperties orderProperties)
        {
            if (orderProperties.TimeInForce as GoodTilDateTimeInForce != null)
            {
                // Good-Til-Date(GTD) Time-In-Force is not supported for MOO and MOC orders
                orderProperties.TimeInForce = TimeInForce.GoodTilCanceled;

                if (!_isGtdTfiForMooAndMocOrdersValidationWarningSent)
                {
                    Debug("Warning: Good-Til-Date Time-In-Force is not supported for MOO and MOC orders. " +
                        "The time-in-force will be reset to Good-Til-Canceled (GTC).");
                    _isGtdTfiForMooAndMocOrdersValidationWarningSent = true;
                }
            }
        }
    }
}
