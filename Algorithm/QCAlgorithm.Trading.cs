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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private int _maxOrders = 10000;
        private bool _isMarketOnOpenOrderWarningSent = false;

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
            return Order(symbol, (decimal)Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <seealso cref="Buy(Symbol, decimal)"/>
        public OrderTicket Buy(Symbol symbol, double quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity));
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <seealso cref="Order(Symbol, int)"/>
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
        public OrderTicket Sell(Symbol symbol, int quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>int Order Id.</returns>
        public OrderTicket Sell(Symbol symbol, double quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity) * -1);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>int order id</returns>
        public OrderTicket Sell(Symbol symbol, float quantity)
        {
            return Order(symbol, (decimal)Math.Abs(quantity) * -1m);
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
            return Order(symbol, (decimal)quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset
        /// </summary>
        /// <remarks></remarks>
        public OrderTicket Order(Symbol symbol, int quantity)
        {
            return MarketOrder(symbol, (decimal)quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset
        /// </summary>
        /// <remarks></remarks>
        public OrderTicket Order(Symbol symbol, decimal quantity)
        {
            return MarketOrder(symbol, quantity);
        }

        /// <summary>
        /// Wrapper for market order method: submit a new order for quantity of symbol using type order.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <seealso cref="MarketOrder(Symbol, decimal, bool, string)"/>
        public OrderTicket Order(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "")
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
            return MarketOrder(symbol, (decimal)quantity, asynchronous, tag);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>int Order id</returns>
        public OrderTicket MarketOrder(Symbol symbol, double quantity, bool asynchronous = false, string tag = "")
        {
            return MarketOrder(symbol, (decimal)quantity, asynchronous, tag);
        }

        /// <summary>
        /// Market order implementation: Send a market order and wait for it to be filled.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>int Order id</returns>
        public OrderTicket MarketOrder(Symbol symbol, decimal quantity, bool asynchronous = false, string tag = "")
        {
            var security = Securities[symbol];

            // check the exchange is open before sending a market order, if it's not open
            // then convert it into a market on open order
            if (!security.Exchange.ExchangeOpen)
            {
                var mooTicket = MarketOnOpenOrder(security.Symbol, quantity, tag);
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

            var request = CreateSubmitOrderRequest(OrderType.Market, security, quantity, tag, DefaultOrderProperties?.Clone());

            // If warming up, do not submit
            if (IsWarmingUp)
            {
                return OrderTicket.InvalidWarmingUp(Transactions, request);
            }

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
        public OrderTicket MarketOnOpenOrder(Symbol symbol, double quantity, string tag = "")
        {
            return MarketOnOpenOrder(symbol, (decimal)quantity, tag);
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
            return MarketOnOpenOrder(symbol, (decimal)quantity, tag);
        }

        /// <summary>
        /// Market on open order implementation: Send a market order when the exchange opens
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderTicket MarketOnOpenOrder(Symbol symbol, decimal quantity, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.MarketOnOpen, security, quantity, tag, DefaultOrderProperties?.Clone());
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
            return MarketOnCloseOrder(symbol, (decimal)quantity, tag);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderTicket MarketOnCloseOrder(Symbol symbol, double quantity, string tag = "")
        {
            return MarketOnCloseOrder(symbol, (decimal)quantity, tag);
        }

        /// <summary>
        /// Market on close order implementation: Send a market order when the exchange closes
        /// </summary>
        /// <param name="symbol">The symbol to be ordered</param>
        /// <param name="quantity">The number of shares to required</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <returns>The order ID</returns>
        public OrderTicket MarketOnCloseOrder(Symbol symbol, decimal quantity, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.MarketOnClose, security, quantity, tag, DefaultOrderProperties?.Clone());
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
            return LimitOrder(symbol, (decimal)quantity, limitPrice, tag);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <returns>Order id</returns>
        public OrderTicket LimitOrder(Symbol symbol, double quantity, decimal limitPrice, string tag = "")
        {
            return LimitOrder(symbol, (decimal)quantity, limitPrice, tag);
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <returns>Order id</returns>
        public OrderTicket LimitOrder(Symbol symbol, decimal quantity, decimal limitPrice, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.Limit, security, quantity, tag, limitPrice: limitPrice, properties: DefaultOrderProperties?.Clone());
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
            return StopMarketOrder(symbol, (decimal)quantity, stopPrice, tag);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <returns>Int orderId for the new order.</returns>
        public OrderTicket StopMarketOrder(Symbol symbol, double quantity, decimal stopPrice, string tag = "")
        {
            return StopMarketOrder(symbol, (decimal)quantity, stopPrice, tag);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <returns>Int orderId for the new order.</returns>
        public OrderTicket StopMarketOrder(Symbol symbol, decimal quantity, decimal stopPrice, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.StopMarket, security, quantity, tag, stopPrice: stopPrice, properties: DefaultOrderProperties?.Clone());
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
            return StopLimitOrder(symbol, (decimal)quantity, stopPrice, limitPrice, tag);
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
        public OrderTicket StopLimitOrder(Symbol symbol, double quantity, decimal stopPrice, decimal limitPrice, string tag = "")
        {
            return StopLimitOrder(symbol, (decimal)quantity, stopPrice, limitPrice, tag);
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
        public OrderTicket StopLimitOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, string tag = "")
        {
            var security = Securities[symbol];
            var request = CreateSubmitOrderRequest(OrderType.StopLimit, security, quantity, tag, stopPrice: stopPrice, limitPrice: limitPrice, properties: DefaultOrderProperties?.Clone());
            var response = PreOrderChecks(request);
            if (response.IsError)
            {
                return OrderTicket.InvalidSubmitRequest(Transactions, request, response);
            }

            //Add the order and create a new order Id.
            return Transactions.AddOrder(request);
        }

        /// <summary>
        /// Send an exercise order to the transaction handler
        /// </summary>
        /// <param name="optionSymbol">String symbol for the option position</param>
        /// <param name="quantity">Quantity of options contracts</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">String tag for the order (optional)</param>
        public OrderTicket ExerciseOption(Symbol optionSymbol, int quantity, bool asynchronous = false, string tag = "")
        {
            var option = (Option)Securities[optionSymbol];

            var request = CreateSubmitOrderRequest(OrderType.OptionExercise, option, quantity, tag, DefaultOrderProperties?.Clone());

            // If warming up, do not submit
            if (IsWarmingUp)
            {
                return OrderTicket.InvalidWarmingUp(Transactions, request);
            }

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
        /// <returns>Sequence of order ids</returns>
        public IEnumerable<OrderTicket> Buy(OptionStrategy strategy, int quantity)
        {
            return Order(strategy, Math.Abs(quantity));
        }

        /// <summary>
        /// Sell Option Strategy (alias of Order)
        /// </summary>
        /// <param name="strategy">Specification of the strategy to trade</param>
        /// <param name="quantity">Quantity of the strategy to trade</param>
        /// <returns>Sequence of order ids</returns>
        public IEnumerable<OrderTicket> Sell(OptionStrategy strategy, int quantity)
        {
            return Order(strategy, Math.Abs(quantity) * -1);
        }

        /// <summary>
        ///  Issue an order/trade for buying/selling an option strategy
        /// </summary>
        /// <param name="strategy">Specification of the strategy to trade</param>
        /// <param name="quantity">Quantity of the strategy to trade</param>
        /// <returns>Sequence of order ids</returns>
        public IEnumerable<OrderTicket> Order(OptionStrategy strategy, int quantity)
        {
            return GenerateOrders(strategy, quantity);
        }

        private IEnumerable<OrderTicket> GenerateOrders(OptionStrategy strategy, int strategyQuantity)
        {
            var orders = new List<OrderTicket>();

            // setting up the tag text for all orders of one strategy
            var strategyTag = $"{strategy.Name} ({strategyQuantity.ToStringInvariant()})";

            // walking through all option legs and issuing orders
            if (strategy.OptionLegs != null)
            {
                foreach (var optionLeg in strategy.OptionLegs)
                {
                    var optionSeq = Securities.Where(kv => kv.Key.Underlying == strategy.Underlying &&
                                                            kv.Key.ID.OptionRight == optionLeg.Right &&
                                                            kv.Key.ID.Date == optionLeg.Expiration &&
                                                            kv.Key.ID.StrikePrice == optionLeg.Strike);

                    if (optionSeq.Count() != 1)
                    {
                        throw new InvalidOperationException("Couldn't find the option contract in algorithm securities list. " +
                            Invariant($"Underlying: {strategy.Underlying}, option {optionLeg.Right}, strike {optionLeg.Strike}, ") +
                            Invariant($"expiration: {optionLeg.Expiration}"));
                    }

                    var option = optionSeq.First().Key;

                    switch (optionLeg.OrderType)
                    {
                        case OrderType.Market:
                            var marketOrder = MarketOrder(option, optionLeg.Quantity * strategyQuantity, tag: strategyTag);
                            orders.Add(marketOrder);
                            break;
                        case OrderType.Limit:
                            var limitOrder = LimitOrder(option, optionLeg.Quantity * strategyQuantity, optionLeg.OrderPrice, tag: strategyTag);
                            orders.Add(limitOrder);
                            break;
                        default:
                            throw new InvalidOperationException("Order type is not supported in option strategy: " + optionLeg.OrderType.ToString());
                    }
                }
            }

            // walking through all underlying legs and issuing orders
            if (strategy.UnderlyingLegs != null)
            {
                foreach (var underlyingLeg in strategy.UnderlyingLegs)
                {
                    if (!Securities.ContainsKey(strategy.Underlying))
                    {
                        var error = $"Couldn't find the option contract underlying in algorithm securities list. Underlying: {strategy.Underlying}";
                        throw new InvalidOperationException(error);
                    }

                    switch (underlyingLeg.OrderType)
                    {
                        case OrderType.Market:
                            var marketOrder = MarketOrder(strategy.Underlying, underlyingLeg.Quantity * strategyQuantity, tag: strategyTag);
                            orders.Add(marketOrder);
                            break;
                        case OrderType.Limit:
                            var limitOrder = LimitOrder(strategy.Underlying, underlyingLeg.Quantity * strategyQuantity, underlyingLeg.OrderPrice, tag: strategyTag);
                            orders.Add(limitOrder);
                            break;
                        default:
                            throw new InvalidOperationException("Order type is not supported in option strategy: " + underlyingLeg.OrderType.ToString());
                    }
                }
            }
            return orders;
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
            if (IsWarmingUp)
            {
                return OrderResponse.WarmingUp(request);
            }

            //Most order methods use security objects; so this isn't really used.
            // todo: Left here for now but should review
            Security security;
            if (!Securities.TryGetValue(request.Symbol, out security))
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.MissingSecurity, "You haven't requested " + request.Symbol.ToString() + " data. Add this with AddSecurity() in the Initialize() Method.");
            }

            //Ordering 0 is useless.
            if (request.Quantity == 0)
            {
                return OrderResponse.ZeroQuantity(request);
            }

            if (Math.Abs(request.Quantity) < security.SymbolProperties.LotSize)
            {
                return OrderResponse.Error(request, OrderResponseErrorCode.OrderQuantityLessThanLoteSize,
                    Invariant($"Unable to {request.OrderRequestType.ToLower()} order with id {request.OrderId} which ") +
                    Invariant($"quantity ({Math.Abs(request.Quantity)}) is less than lot ") +
                    Invariant($"size ({security.SymbolProperties.LotSize}).")
                );
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

            //Check the exchange is open before sending a exercise orders
            if (request.OrderType == OrderType.OptionExercise && !security.Exchange.ExchangeOpen)
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
            if (security.Type == SecurityType.Forex || security.Type == SecurityType.Crypto)
            {
                Cash baseCash;
                var baseCurrency = ((IBaseCurrencySymbol)security).BaseCurrencySymbol;
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

            // We've already processed too many orders: max 10k
            if (!LiveMode && Transactions.OrdersCount > _maxOrders)
            {
                Status = AlgorithmStatus.Stopped;
                return OrderResponse.Error(request, OrderResponseErrorCode.ExceededMaximumOrders,
                    $"You have exceeded maximum number of orders ({_maxOrders.ToStringInvariant()}), for unlimited orders upgrade your account."
                );
            }

            if (request.OrderType == OrderType.OptionExercise)
            {
                if (security.Type != SecurityType.Option)
                    return OrderResponse.Error(request, OrderResponseErrorCode.NonExercisableSecurity, "The security with symbol '" + request.Symbol.ToString() + "' is not exercisable.");

                if (security.Holdings.IsShort)
                    return OrderResponse.Error(request, OrderResponseErrorCode.UnsupportedRequestType, "The security with symbol '" + request.Symbol.ToString() + "' has a short option position. Only long option positions are exercisable.");

                if (request.Quantity > security.Holdings.Quantity)
                    return OrderResponse.Error(request, OrderResponseErrorCode.UnsupportedRequestType, "Cannot exercise more contracts of '" + request.Symbol.ToString() + "' than is currently available in the portfolio. ");

                if (request.Quantity <= 0.0m)
                    OrderResponse.ZeroQuantity(request);
            }

            if (request.OrderType == OrderType.MarketOnClose)
            {
                var nextMarketClose = security.Exchange.Hours.GetNextMarketClose(security.LocalTime, false);
                // must be submitted with at least 10 minutes in trading day, add buffer allow order submission
                var latestSubmissionTime = nextMarketClose.Subtract(Orders.MarketOnCloseOrder.DefaultSubmissionTimeBuffer);
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
        /// <param name="tag">Custom tag to know who is calling this.</param>
        /// <returns>Array of order ids for liquidated symbols</returns>
        /// <seealso cref="MarketOrder(QuantConnect.Symbol,decimal,bool,string)"/>
        public List<int> Liquidate(Symbol symbolToLiquidate = null, string tag = "Liquidated")
        {
            var orderIdList = new List<int>();
            if (!Settings.LiquidateEnabled)
            {
                Debug("Liquidate() is currently disabled by settings. To re-enable please set 'Settings.LiquidateEnabled' to true");
                return orderIdList;
            }

            IEnumerable<Symbol> toLiquidate;
            if (symbolToLiquidate != null)
            {
                toLiquidate = Securities.ContainsKey(symbolToLiquidate)
                    ? new[] { symbolToLiquidate } : Enumerable.Empty<Symbol>();
            }
            else
            {
                toLiquidate = Securities.Keys.OrderBy(x => x.Value);
            }


            foreach (var symbol in toLiquidate)
            {
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
                        Transactions.CancelOrder(order.Id, tag);
                    }
                }

                // Liquidate at market price
                if (quantity != 0)
                {
                    // calculate quantity for closing market order
                    var ticket = Order(symbol, -quantity - marketOrdersQuantity, tag: tag);
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
        /// <seealso cref="MarketOrder(QuantConnect.Symbol,decimal,bool,string)"/>
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
        /// <seealso cref="MarketOrder(QuantConnect.Symbol,decimal,bool,string)"/>
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
        /// <seealso cref="MarketOrder(QuantConnect.Symbol,decimal,bool,string)"/>
        public void SetHoldings(Symbol symbol, int percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            SetHoldings(symbol, (decimal)percentage, liquidateExistingHoldings, tag);
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
        /// <seealso cref="MarketOrder(QuantConnect.Symbol,decimal,bool,string)"/>
        public void SetHoldings(Symbol symbol, decimal percentage, bool liquidateExistingHoldings = false, string tag = "")
        {
            //Initialize Requirements:
            Security security;
            if (!Securities.TryGetValue(symbol, out security))
            {
                Error($"{symbol} not found in portfolio. Request this data when initializing the algorithm.");
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
                        var liquidationQuantity = CalculateOrderQuantity(holdingSymbol, 0m);
                        Order(holdingSymbol, liquidationQuantity, false, tag);
                    }
                }
            }

            //Calculate total unfilled quantity for open market orders
            var marketOrdersQuantity =
                (from order in Transactions.GetOpenOrders(symbol)
                 where order.Type == OrderType.Market
                 select Transactions.GetOrderTicket(order.Id)
                 into ticket
                 where ticket != null
                 select ticket.Quantity - ticket.QuantityFilled).Sum();

            //Only place trade if we've got > 1 share to order.
            var quantity = CalculateOrderQuantity(symbol, percentage) - marketOrdersQuantity;
            if (Math.Abs(quantity) > 0)
            {
                //Check whether the exchange is open to send a market order. If not, send a market on open order instead
                if (security.Exchange.ExchangeOpen)
                {
                    MarketOrder(symbol, quantity, false, tag);
                }
                else
                {
                    MarketOnOpenOrder(symbol, quantity, tag);
                }
            }
        }

        /// <summary>
        /// Calculate the order quantity to achieve target-percent holdings.
        /// </summary>
        /// <param name="symbol">Security object we're asking for</param>
        /// <param name="target">Target percentag holdings</param>
        /// <returns>Order quantity to achieve this percentage</returns>
        public decimal CalculateOrderQuantity(Symbol symbol, double target)
        {
            return CalculateOrderQuantity(symbol, (decimal)target);
        }

        /// <summary>
        /// Calculate the order quantity to achieve target-percent holdings.
        /// </summary>
        /// <param name="symbol">Security object we're asking for</param>
        /// <param name="target">Target percentage holdings, this is an unlevered value, so
        /// if you have 2x leverage and request 100% holdings, it will utilize half of the
        /// available margin</param>
        /// <returns>Order quantity to achieve this percentage</returns>
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
            return Order(symbol, quantity);
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
            return Order(symbol, (decimal)quantity);
        }

        /// <summary>
        /// Determines if the exchange for the specified symbol is open at the current time.
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>True if the exchange is considered open at the current time, false otherwise</returns>
        public bool IsMarketOpen(Symbol symbol)
        {
            var exchangeHours = MarketHoursDatabase
                .FromDataFolder()
                .GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var time = UtcTime.ConvertFromUtc(exchangeHours.TimeZone);

            return exchangeHours.IsOpen(time, false);
        }

        private SubmitOrderRequest CreateSubmitOrderRequest(OrderType orderType, Security security, decimal quantity, string tag, IOrderProperties properties, decimal stopPrice = 0m, decimal limitPrice = 0m)
        {
            return new SubmitOrderRequest(orderType, security.Type, security.Symbol, quantity, stopPrice, limitPrice, UtcTime, tag, properties);
        }
    }
}
