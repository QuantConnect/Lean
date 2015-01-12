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
        public SecurityTransactionManager Transactions
        {
            get;
            set;
        }

        /// <summary>
        /// Wait semaphore to signal the algoritm is currently processing a synchronous order.
        /// </summary>
        public bool ProcessingOrder
        {
            get
            {
                return _processingOrder;
            }
            set
            {
                _processingOrder = value;
            }
        }

        /// <summary>
        /// Accessor for filled orders dictionary
        /// </summary>
        public ConcurrentDictionary<int, Order> Orders
        {
            get
            {
                return Transactions.Orders;
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Buy(string symbol, int quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">double Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Buy(string symbol, double quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">decimal Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Buy(string symbol, decimal quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Buy Stock (Alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">float Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Buy(string symbol, float quantity)
        {
            return Order(symbol, quantity);
        }

        /// <summary>
        /// Sell stock (alias of Order)
        /// </summary>
        /// <param name="symbol">string Symbol of the asset to trade</param>
        /// <param name="quantity">int Quantity of the asset to trade</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
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
        /// <seealso cref="Order(string, double, OrderType)"/>
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
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Order(string symbol, double quantity, OrderType type = OrderType.Market)
        {
            return Order(symbol, (int)quantity, type);
        }

        /// <summary>
        /// Issue an order/trade for asset: Alias wrapper for Order(string, int);
        /// </summary>
        /// <remarks></remarks>
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Order(string symbol, decimal quantity, OrderType type = OrderType.Market)
        {
            return Order(symbol, (int)quantity, type);
        }

        /// <summary>
        /// Submit a new order for quantity of symbol using type order.
        /// </summary>
        /// <param name="type">Buy/Sell Limit or Market Order Type.</param>
        /// <param name="symbol">Symbol of the MarketType Required.</param>
        /// <param name="quantity">Number of shares to request.</param>
        /// <param name="asynchronous">Send the order asynchrously (false). Otherwise we'll block until it fills</param>
        /// <param name="tag">Place a custom order property or tag (e.g. indicator data).</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
        public int Order(string symbol, int quantity, OrderType type = OrderType.Market, bool asynchronous = false, string tag = "")
        {
            //Add an order to the transacion manager class:
            var orderId = -1;
            decimal price = 0;

            //Ordering 0 is useless.
            if (quantity == 0 || symbol == null || symbol == "")
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
            }

            //Set a temporary price for validating order for market orders:
            price = Securities[symbol].Price;
            if (price == 0)
            {
                Error("Asset price is $0. If using custom data make sure you've set the 'Value' property.");
                return -1;
            }

            //Check the exchange is open before sending a market order.
            if (type == OrderType.Market && !Securities[symbol].Exchange.ExchangeOpen)
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

            //Add the order and create a new order Id.
            orderId = Transactions.AddOrder(new Order(symbol, quantity, type, Time, price, tag));

            //Wait for the order event to process:
            //Enqueue means send to order queue but don't wait for response:
            if (!asynchronous && type == OrderType.Market)
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
        /// Liquidate all holdings. Called at the end of day for tick-strategies.
        /// </summary>
        /// <param name="symbolToLiquidate">Symbols we wish to liquidate</param>
        /// <returns>Array of order ids for liquidated symbols</returns>
        /// <seealso cref="Order(string, double, OrderType)"/>
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
                orderIdList.Add(Order(symbol, quantity, OrderType.Market));
            }
            return orderIdList;
        }


        /// <summary>
        /// Alias for SetHoldings to avoid the M-decimal errors.
        /// </summary>
        /// <param name="symbol">string symbol we wish to hold</param>
        /// <param name="percentage">double percentage of holdings desired</param>
        /// <param name="liquidateExistingHoldings">liquidate existing holdings if neccessary to hold this stock</param>
        /// <seealso cref="Order(string, double, OrderType)"/>
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
        /// <seealso cref="Order(string, double, OrderType)"/>
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
        /// <seealso cref="Order(string, double, OrderType)"/>
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
        /// <seealso cref="Order(string, double, OrderType)"/>
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
            var total = Portfolio.TotalHoldingsValue + Portfolio.Cash * Securities[symbol].Leverage;

            //2. Difference between our target % and our current holdings: (relative +- number).
            var deltaValue = (total * percentage) - Portfolio[symbol].HoldingsValue;

            var deltaQuantity = 0m;

            //Potential divide by zero error for zero prices assets.
            if (Math.Abs(Securities[symbol].Price) > 0)
            {
                //3. Now rebalance the symbol requested:
                deltaQuantity = Math.Round(deltaValue / Securities[symbol].Price);
            }

            //Determine if we need to place an order:
            if (Math.Abs(deltaQuantity) > 0)
            {
                Order(symbol, (int)deltaQuantity, OrderType.Market, false, tag);
            }
        }

    } // End Partial Algorithm Template - Trading..

} // End QC Namespace
