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
 *
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.AlgorithmFactory;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Tradier;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.Setup
{
    /// <summary>
    /// Setup the algorithm for live trading with Tradier Brokerage! Get the brokerage cash, portfolio and setup algorithm internal state.
    /// </summary>
    public class TradierSetupHandler : ISetupHandler
    {
        /******************************************************** 
        * PRIVATE VARIABLES
        *********************************************************/
        private TradierBrokerage _tradier;

        /******************************************************** 
        * PUBLIC PROPERTIES
        *********************************************************/
        /// <summary>
        /// Internal errors list from running the setup proceedures.
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Maximum runtime of the algorithm in seconds.
        /// </summary>
        /// <remarks>Maximum runtime is a formula based on the number and resolution of symbols requested, and the days backtesting</remarks>
        public TimeSpan MaximumRuntime { get; private set; }

        /// <summary>
        /// Starting capital according to the users initialize routine.
        /// </summary>
        /// <remarks>Set from the user code.</remarks>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public decimal StartingCapital { get; private set; }

        /// <summary>
        /// Start date for analysis loops to search for data.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(DateTime)"/>
        public DateTime StartingDate { get; private set; }

        /// <summary>
        /// Maximum number of orders for this live paper trading algorithm. (int.MaxValue)
        /// </summary>
        /// <remarks>For live trading its almost impossible to limit the order number</remarks>
        public int MaxOrders { get; private set; }

        /******************************************************** 
        * PUBLIC CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Setup the algorithm data, cash, job start end date etc:
        /// </summary>
        public TradierSetupHandler()
        {
            StartingCapital = 0;
            MaxOrders = int.MaxValue;
            StartingDate = new DateTime(1998, 01, 01);
            MaximumRuntime = TimeSpan.FromDays(365 * 10);
            Errors = new List<string>();
        }

        /******************************************************** 
        * PUBLIC METHODS
        *********************************************************/
        /// <summary>
        /// Creates a new algorithm instance. Checks configuration for a specific type name, and if present will
        /// force it to find that one
        /// </summary>
        /// <param name="assemblyPath">Physical path of the algorithm dll.</param>
        /// <returns>Algorithm instance</returns>
        public IAlgorithm CreateAlgorithmInstance(string assemblyPath)
        {
            string error;
            IAlgorithm algorithm;

            // limit load times to 10 seconds and force the assembly to have exactly one derived type
            var loader = new Loader(TimeSpan.FromSeconds(10), names => names.SingleOrDefault());
            bool complete = loader.TryCreateAlgorithmInstanceWithIsolator(assemblyPath, out algorithm, out error);
            if (!complete) throw new Exception(error + " Try re-building algorithm.");

            return algorithm;
        }

        /// <summary>
        /// Primary entry point to setup a new algorithm
        /// </summary>
        /// <param name="algorithm">Algorithm instance</param>
        /// <param name="brokerage">New brokerage output instance</param>
        /// <param name="baseJob">Algorithm job task</param>
        /// <returns>True on successfully setting up the algorithm state, or false on error.</returns>
        public bool Setup(IAlgorithm algorithm, out IBrokerage brokerage, AlgorithmNodePacket baseJob)
        {
            //-> Initialize:
            var initializeComplete = false;
            var job = baseJob as LiveNodePacket;
            var portfolioResolution = PortfolioResolution(algorithm.Securities);

            //-> Connect to Tradier:
            _tradier = new TradierBrokerage(job.AccountId);
            //_tradier = (Tradier)brokerage;
            _tradier.SetTokens(job.UserId, job.AccessToken, job.RefreshToken, job.IssuedAt, job.LifeTime);
            brokerage = _tradier;

            // -> Refresh the session immediately, buy us 24 hours:
            if (!_tradier.RefreshSession())
            {
                Errors.Add("Failed to refresh access token. Please login again.");
                return false;
            }

            //-> Setup any user specific code:
            try
            {
                algorithm.Initialize();
            }
            catch (Exception err)
            {
                Errors.Add("Failed to initialize user algorithm, Initialize() returned error - " + err.Message);
                return false;
            }
            
            Log.Trace("TradierSetupHandler.Setup(): Algorithm initialized");

            //-> Strip any FOREX Symbols:
            var symbols = algorithm.Securities.Keys.ToList();
            foreach(var symbol in symbols)
            {
                if (algorithm.Securities[symbol].Type == SecurityType.Forex)
                {
                    algorithm.Securities.Remove(symbol);
                }
            }

            //-> Fetch the orders on the account:
            var orders = _tradier.FetchOrders();
            foreach (var order in orders)
            {
                //Ignore option orders for now.
                if (order.Class != TradierOrderClass.Equity) continue;

                var qcPrice = order.Price;
                var qcQuantity = order.Quantity;
                var qcType = OrderType.Limit;
                var qcStatus = OrderStatus.None;

                // Get the order type:
                switch (order.Type) 
                { 
                    case TradierOrderType.Market:
                        qcType = OrderType.Market;
                        break;
                    case TradierOrderType.Limit:
                        qcType = OrderType.Limit;
                        break;
                    case TradierOrderType.StopMarket:
                        qcType = OrderType.StopMarket;
                        break;
                }

                // Convert order direction to a quantity
                switch (order.Direction)
                { 
                    case TradierOrderDirection.Buy:
                    case TradierOrderDirection.BuyToCover:
                        break;

                    case TradierOrderDirection.Sell:
                    case TradierOrderDirection.SellShort:
                        qcQuantity *= -1; //Invert quantity.
                        break;
                }

                //Set the QC Order Status Flag:
                switch (order.Status)
                {
                    case TradierOrderStatus.Canceled:
                        qcStatus = OrderStatus.Canceled;
                        break;
                    case TradierOrderStatus.Filled:
                        qcStatus = OrderStatus.Filled;
                        break;
                    case TradierOrderStatus.Open:
                    case TradierOrderStatus.Submitted:
                    case TradierOrderStatus.Pending:
                        qcStatus = OrderStatus.Submitted;
                        break;
                    case TradierOrderStatus.PartiallyFilled:
                        qcStatus = OrderStatus.PartiallyFilled;
                        break;
                    case TradierOrderStatus.Rejected:
                        qcStatus = OrderStatus.Invalid;
                        break;
                }

                //Create the new qcOrder
                var qcOrder = new Order(order.Symbol, algorithm.Securities[order.Symbol].Type, Convert.ToInt32((decimal) qcQuantity), qcType, order.CreatedDate, qcPrice);
                //Set Status for Order:
                qcOrder.Status = qcStatus;

                //Create any fill information:
                var fill = new OrderEvent(qcOrder, "Pre-existing Tradier Order");
                fill.FillPrice = order.AverageFillPrice;
                fill.FillQuantity = Convert.ToInt32((decimal) order.QuantityExecuted);
                var fillList = new List<OrderEvent>() { fill };
                
                //Get a unique qc-id: set to fill 
                var qcid = algorithm.Transactions.GetIncrementOrderId();
                order.Id = qcid; fill.OrderId = qcid; qcOrder.Id = qcid;

                //Add the order to our internal records:
                algorithm.Transactions.Orders.AddOrUpdate<int, Order>(Convert.ToInt32((long) order.Id), qcOrder);

                //Add the fill quantity to the list:
                algorithm.Transactions.OrderEvents.AddOrUpdate<int, List<OrderEvent>>(Convert.ToInt32((long) order.Id), fillList);

                //If we don't have this symbol, add it manually:
                if (!algorithm.Portfolio.ContainsKey(order.Symbol))
                {
                    algorithm.AddSecurity(SecurityType.Equity, order.Symbol, portfolioResolution, true, 1, false);
                }
            }

            //-> Retrieve/Set Tradier Portfolio Positions:
            var positions = _tradier.Positions();
            foreach (var position in positions)
            {
                //We can't support options.
                if (position.Symbol.Length >= 10)
                {
                    continue;
                }

                //If we don't have this symbol, add it manually:
                if (!algorithm.Portfolio.ContainsKey(position.Symbol))
                {
                    algorithm.AddSecurity(SecurityType.Equity, position.Symbol, portfolioResolution, true, 1, false);
                }
                //Once we have the symbol, set the holdings:
                var avgPrice = Math.Round(position.CostBasis / Convert.ToDecimal((long) position.Quantity), 4);
                algorithm.Portfolio[position.Symbol].SetHoldings(avgPrice, (int)position.Quantity);
                Log.Trace("TradierSetupHandler.Setup(): Portfolio security added to algorithm: " + position.Symbol + " with " + position.Quantity + " shares at " + avgPrice.ToString("C"));
            }


            //-> Retrieve/Set Tradier Cash Positions:
            var balanceFound = false;

            //HACK:
            //balanceFound = true;
            //algorithm.Portfolio.SetCash(100000);
            //_startingCapital = 100000;

            var balance = _tradier.Balance();
            if (balance != null)
            {
                if (balance.AccountNumber == job.AccountId)
                {
                    //Set the cash in this account:
                    var cash = balance.TotalCash - balance.OptionRequirement;
                    algorithm.Portfolio.SetCash(cash);
                    StartingCapital = cash;
                    balanceFound = true;
                    Log.Trace("TradierSetupHandler.Setup(): Free Cash: " + cash.ToString("C"));
                    Log.Trace("TradierSetupHandler.Setup(): Total Cash: " + balance.TotalCash.ToString("C"));
                }

                //Set the leverage on all the securities:
                switch (balance.Type)
                { 
                    //Maximum 1x Leverage
                    case TradierAccountType.Cash:
                        foreach (var security in algorithm.Securities.Values)
                        {
                            if (security.Type == SecurityType.Equity)
                            {
                                security.SetLeverage(1m);
                            }
                        }
                        break;

                    //Maximum 2x Leverage
                    case TradierAccountType.Margin:
                        foreach (var security in algorithm.Securities.Values)
                        {
                            if (security.Type == SecurityType.Equity && security.Leverage > 2)
                            {
                                security.SetLeverage(2m);
                            }   
                        }
                        break;

                    case TradierAccountType.DayTrader:
                        //Do nothing, let the user set their own leverage:
                        foreach (var security in algorithm.Securities.Values)
                        {
                            if (security.Type == SecurityType.Equity && security.Leverage > 4)
                            {
                                security.SetLeverage(4m);
                            }   
                        }
                        break;
                }
            }

            // Maximum number of orders or the algorithm
            MaxOrders = int.MaxValue;

            if (!balanceFound)
            {
                Errors.Add("Could not get the account cash balance");
            }

            if (Errors.Count == 0)
            {
                initializeComplete = true;
            }
            return initializeComplete;
        }

        /// <summary>
        /// Get the lowest resolution of the portfolio manager.
        /// </summary>
        /// <param name="securities">List of securities we're scanning.</param>
        /// <returns>Resolution frequency of desired updates</returns>
        private static Resolution PortfolioResolution(SecurityManager securities)
        { 
            var resolution = Resolution.Minute;
            //Go through the portfolio, find the lowest common resolution:
            foreach (var asset in securities.Values)
            {
                //Enum comparison of resolution int values:
                if ((int)asset.Resolution < (int)resolution)
                {
                    resolution = asset.Resolution;
                }
            }
            return resolution;
        }

        /// <summary>
        /// Error handlers in event of a brokerage error.
        /// </summary>
        /// <param name="results">Result handler for sending results on error.</param>
        /// <param name="brokerage">Brokerage instance firing the errors</param>
        /// <returns>Boolean true on successfully setting up local algorithm</returns>
        public bool SetupErrorHandler(IResultHandler results, IBrokerage brokerage)
        {
            //Setup handler for access token error.
            brokerage.AddErrorHander("Access Token expired", () =>
            {
                results.RuntimeError("Brokerage access token has expired. In general this should not happen, please contact support@quantconnect.com");
            });
            brokerage.AddErrorHander("Invalid Access Token", () =>
            {
                results.RuntimeError("Access token is invalid. In general this should not happen, please contact support@quantconnect.com");
            });
            return true;
        }

    } // End Result Handler Thread:

} // End Namespace
