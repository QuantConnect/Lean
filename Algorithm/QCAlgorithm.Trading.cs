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
/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm
{
    /********************************************************
    * CLASS DEFINITIONS
    *********************************************************/

    public partial class QCAlgorithm
    {
        /********************************************************
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private bool _processingOrder = false;

        /********************************************************
        * CLASS PUBLIC PROPERTIES
        *********************************************************/

        /// <summary>
        /// Transaction Manager - Process transaction fills and order management.
        /// </summary>
        public SecurityTransactionManager Transactions { get; set; }

        /// <summary>
        /// Wait semaphore to signal the algoritm is currently processing a synchronous order.
        /// </summary>
        public bool ProcessingOrder
        {
            get { return _processingOrder; }
            set { _processingOrder = value; }
        }

        /// <summary>
        /// Accessor for filled orders dictionary
        /// </summary>
        public ConcurrentDictionary<int, Order> Orders
        {
            get { return Transactions.Orders; }
        }

        /********************************************************
        * CLASS METHODS
        *********************************************************/

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double)"/>
        public int Buy(string symbol, int quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double)"/>
        public int Buy(string symbol, double quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double)"/>
        public int Buy(string symbol, decimal quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">float Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double)"/>
        public int Buy(string symbol, float quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double)"/>
        public int Sell(string symbol, int quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to order</param>
        /// <returns>int Order Id.</returns>
        public int Sell(string symbol, double quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>int order id</returns>
        /// <seealso cref="Order(string, double)"/>
        public int Sell(string symbol, float quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">String symbol to sell</param>
        /// <param name="quantity">Quantity to sell</param>
        /// <returns>Int Order Id.</returns>
        public int Sell(string symbol, decimal quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <seealso cref="Order(string, double)"/>
        public int Order(string symbol, double quantity)
        {
            return Order(symbol, (int)quantity);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref="Order(string, double)"/>
        public int Order(string symbol, decimal quantity)
        {
            return Order(symbol, (int)quantity);
        }

        /// <summary>
        /// Wrapper for market order method: submit a new order for quantity of symbol using type order.
        /// </summary>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <seealso cref="MarketOrder(string, int, bool, string)"/>
        public int Order(string symbol, int quantity, bool asynchronous = false, string tag = "")
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
        public int MarketOrder(string symbol, int quantity, bool asynchronous = false, string tag = "")
        {
            //Initalize the Market order parameters:
            var error = PreOrderChecks(symbol, quantity, OrderType.Market);
            if (error < 0)
            {
                return error;
            }

            var order = new MarketOrder(symbol, quantity, Time, tag, Securities[symbol].Type);

            //Set the rough price of the order for buying power calculations
            order.Price = Securities[symbol].Price;

            //Add the order and create a new order Id.
            var orderId = Transactions.AddOrder(order);

            //Wait for the order event to process:
            //Enqueue means send to order queue but don't wait for response:
            if (!asynchronous)
            {
                //Wait for the market order to fill.
                //This is processed in a parallel thread.
                while (!Transactions.Orders.ContainsKey(orderId) ||
                       (Transactions.Orders[orderId].Status != OrderStatus.Filled &&
                        Transactions.Orders[orderId].Status != OrderStatus.Invalid &&
                        Transactions.Orders[orderId].Status != OrderStatus.Canceled) || _processingOrder)
                {
                    Thread.Sleep(1);
                }
            }

            return orderId;
        }

        /// <summary>
        /// Send a limit order to the transaction handler:
        /// </summary>
        /// <param name="symbol">String symbol for the asset</param>
        /// <param name="quantity">Quantity of shares for limit order</param>
        /// <param name="limitPrice">Limit price to fill this order</param>
        /// <param name="tag">String tag for the order (optional)</param>
        /// <returns>Order id</returns>
        public int LimitOrder(string symbol, int quantity, decimal limitPrice, string tag = "")
        {
            var error = PreOrderChecks(symbol, quantity, OrderType.Limit);
            if (error < 0)
            {
                return error;
            }

            var order = new LimitOrder(symbol, quantity, limitPrice, Time, tag, Securities[symbol].Type);

            //Add the order and create a new order Id.
            return Transactions.AddOrder(order);
        }

        /// <summary>
        /// Create a stop market order and return the newly created order id; or negative if the order is invalid
        /// </summary>
        /// <param name="symbol">String symbol for the asset we're trading</param>
        /// <param name="quantity">Quantity to be traded</param>
        /// <param name="stopPrice">Price to fill the stop order</param>
        /// <param name="tag">Optional string data tag for the order</param>
        /// <returns>Int orderId for the new order.</returns>
        public int StopMarketOrder(string symbol, int quantity, decimal stopPrice, string tag = "")
        {
            var error = PreOrderChecks(symbol, quantity, OrderType.StopMarket);
            if (error < 0)
            {
                return error;
            }

            var order = new StopMarketOrder(symbol, quantity, stopPrice, Time, tag, Securities[symbol].Type);

            //Add the order and create a new order Id.
            return Transactions.AddOrder(order);
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
        public int StopLimitOrder(string symbol, int quantity, decimal stopPrice, decimal limitPrice, string tag = "")
        {
            var error = PreOrderChecks(symbol, quantity, OrderType.StopLimit);
            if (error < 0)
            {
                return error;
            }

            var order = new StopLimitOrder(symbol, quantity, stopPrice, limitPrice, Time, tag, Securities[symbol].Type);

            //Add the order and create a new order Id.
            return Transactions.AddOrder(order);
        }

        /// <summary>
        /// Perform preorder checks to ensure we have sufficient capital,
        /// the market is open, and we haven't exceeded maximum realistic orders per day.
        /// </summary>
        /// <returns>Negative order errors or zero for pass.</returns>
        private int PreOrderChecks(string symbol, int quantity, OrderType type)
        {
            //Ordering 0 is useless.
            if (quantity == 0 || string.IsNullOrEmpty(symbol))
            {
                return -1;
            }

            //Internals use upper case symbols.
            symbol = symbol.ToUpper();

            //If we're not tracking this symbol: throw error:
            if (!Securities.ContainsKey(symbol) && !_sentNoDataError)
            {
                _sentNoDataError = true;
                Error("You haven't requested " + symbol + " data. Add this with AddSecurity() in the Initialize() Method.");
                return -1;
            }

            //Set a temporary price for validating order for market orders:
            var security = Securities[symbol];
            var price = security.Price;

            if (price == 0)
            {
                Error("Asset price is $0. If using custom data make sure you've set the 'Value' property.");
                return -1;
            }

            //Make sure the security has some data:
            if (!security.HasData)
            {
                Error("There is no data for this symbol yet, please check the security.HasData flag to ensure there is at least one data point.");
                return -1;
            }

            //Check the exchange is open before sending a market order.
            if (type == OrderType.Market && !security.Exchange.ExchangeOpen)
            {
                Error("Market order and exchange not open");
                return -3;
            }

            //We've already processed too many orders: max 100 per day or the memory usage explodes
            if (Orders.Count > (_endDate - _startDate).TotalDays * 100)
            {
                Error("You have exceeded 100 orders per day");
                return -5;
            }

            return 0;
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
                orderIdList.Add(Order(symbol, quantity));
            }

            return orderIdList;
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
                Debug(symbol.ToUpper() + " not found in portfolio. Request this data when initializing the algorithm.");
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

            //1. To set a fraction of whole, we need to know the whole: Cash * Leverage for remaining buying power:
            var security = Securities[symbol];
            var total = Portfolio.TotalHoldingsValue + Portfolio.Cash * security.Leverage;

            //2. Difference between our target % and our current holdings: (relative +- number).
            var deltaValue = (total * percentage) - Portfolio[symbol].HoldingsValue;

            //3. Calculate the rough first pass of quantity: avoid Potential divide by zero error for zero prices assets.
            var deltaQuantity = 0m;
            if (Math.Abs(Securities[symbol].Price) > 0)
            {
                //3. Now rebalance the symbol requested:
                deltaQuantity = Math.Round(deltaValue / Securities[symbol].Price);
            }

            //4. Determine if we need to place an order:
            if (Math.Abs(deltaQuantity) > 0)
            {
                //5. Calculate accurate quantity factoring in fees:
                var projectedFees = security.Model.GetOrderFee(deltaQuantity, security.Price);

                //5.1 Long Short Constant Multiplier:
                var direction = (deltaQuantity > 0) ? 1 : -1;

                //5.2 Multiply fees by leverage because each share's cash impact is only value/leverage. Changing quantity linearly won't work.
                var feesCashImpact = (projectedFees * direction * security.Leverage);

                //5.3 Adjust the target quantity down by percentage of fees: 
                // e.g. Target Quantity = 1000, fees = 10, value = 1000
                // newQuantity = 1000 * 99% == $990 max possible given projected fees.
                // e.g. Target Quantity = -1000, fees = 10, value = -1000
                // newQuantity = -1000 * 99% == -$990 max possible given projected fees.
                deltaQuantity = Math.Floor((deltaValue - feesCashImpact) / security.Price);

                MarketOrder(symbol, (int)deltaQuantity, false, tag);
            }
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
        public int Order(string symbol, int quantity, OrderType type, bool asynchronous = false, string tag = "")
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
        public int Order(string symbol, decimal quantity, OrderType type)
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
        public int Order(string symbol, int quantity, OrderType type)
        {
            return Order(symbol, quantity);
        }

    } // End Partial Algorithm Template - Trading..

} // End QC Namespace
