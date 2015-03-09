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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Securities 
{
    /// <summary>
    /// Portfolio manager class groups popular properties and makes them accessible through one interface.
    /// It also provide indexing by the vehicle symbol to get the Security.Holding objects.
    /// </summary>
    public class SecurityPortfolioManager : IDictionary<string, SecurityHolding> 
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        /// <summary>
        /// Local access to the securities collection for the portfolio summation.
        /// </summary>
        public SecurityManager Securities;

        /// <summary>
        /// Local access to the transactions collection for the portfolio summation and updates.
        /// </summary>
        public SecurityTransactionManager Transactions;
        
        //Record keeping variables
        private decimal _cash = 100000;
        private decimal _lastTradeProfit = 0;
        private decimal _profit = 0;

        /******************************************************** 
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// Initialise security portfolio manager.
        /// </summary>
        public SecurityPortfolioManager(SecurityManager securityManager, SecurityTransactionManager transactions) 
        {
            Securities = securityManager;
            Transactions = transactions;
        }

        /******************************************************** 
        * DICTIONARY IMPLEMENTATION
        *********************************************************/
        /// <summary>
        /// Add a new securities string-security to the portfolio.
        /// </summary>
        /// <param name="symbol">Symbol of dictionary</param>
        /// <param name="holding">SecurityHoldings object</param>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public void Add(string symbol, SecurityHolding holding) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager. To add a new asset add the required data during initialization."); }

        /// <summary>
        /// Add a new securities key value pair to the portfolio.
        /// </summary>
        /// <param name="pair">Key value pair of dictionary</param>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public void Add(KeyValuePair<string, SecurityHolding> pair) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager. To add a new asset add the required data during initialization."); }

        /// <summary>
        /// Clear the portfolio of securities objects.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public void Clear() { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager and cannot be cleared."); }

        /// <summary>
        /// Remove this keyvalue pair from the portfolio.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <param name="pair">Key value pair of dictionary</param>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public bool Remove(KeyValuePair<string, SecurityHolding> pair) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager and objects cannot be removed."); }

        /// <summary>
        /// Remove this symbol from the portfolio.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <param name="symbol">Symbol of dictionary</param>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public bool Remove(string symbol) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager and objects cannot be removed."); }

        /// <summary>
        /// Check if the portfolio contains this symbol string.
        /// </summary>
        /// <param name="symbol">String search symbol for the security</param>
        /// <returns>Boolean true if portfolio contains this symbol</returns>
        public bool ContainsKey(string symbol)
        {
            return Securities.ContainsKey(symbol);
        }

        /// <summary>
        /// Check if the key-value pair is in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        /// <param name="pair">Pair we're searching for</param>
        /// <returns>True if we have this object</returns>
        public bool Contains(KeyValuePair<string, SecurityHolding> pair)
        {
            return Securities.ContainsKey(pair.Key);
        }

        /// <summary>
        /// Count the securities objects in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        public int Count
        {
            get
            {
                return Securities.Count;
            }
        }

        /// <summary>
        /// Check if the underlying securities array is read only.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        public bool IsReadOnly
        {
            get
            {
                return Securities.IsReadOnly;
            }
        }

        /// <summary>
        /// Copy contents of the portfolio collection to a new destination.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        /// <param name="array">Destination array</param>
        /// <param name="index">Position in array to start copying</param>
        public void CopyTo(KeyValuePair<string, SecurityHolding>[] array, int index)
        {
            array = new KeyValuePair<string, SecurityHolding>[Securities.Count];
            var i = 0;
            foreach (var asset in Securities)
            {
                if (i >= index)
                {
                    array[i] = new KeyValuePair<string, SecurityHolding>(asset.Key, asset.Value.Holdings);
                }
                i++;
            }
        }

        /// <summary>
        /// Symbol keys collection of the underlying assets in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying securities key symbols</remarks>
        public ICollection<string> Keys
        {
            get
            {
                return Securities.Keys;
            }
        }

        /// <summary>
        /// Collection of securities objects in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying securities values collection</remarks>
        public ICollection<SecurityHolding> Values
        {
            get
            {
                return (from asset in Securities.Values
                        select asset.Holdings).ToList();
            }
        }

        /// <summary>
        /// Attempt to get the value of the securities holding class if this symbol exists.
        /// </summary>
        /// <param name="symbol">String search symbol</param>
        /// <param name="holding">Holdings object of this security</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Boolean true if successful locating and setting the holdings object</returns>
        public bool TryGetValue(string symbol, out SecurityHolding holding)
        {
            Security security;
            var success = Securities.TryGetValue(symbol, out security);
            holding = success ? security.Holdings : null;
            return success;
        }

        /// <summary>
        /// Get the enumerator for the underlying securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerable key value pair</returns>
        IEnumerator<KeyValuePair<string, SecurityHolding>> IEnumerable<KeyValuePair<string, SecurityHolding>>.GetEnumerator()
        {
            return Securities.GetInternalPortfolioCollection().GetEnumerator();
        }

        /// <summary>
        /// Get the enumerator for the underlying securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Securities.GetInternalPortfolioCollection().GetEnumerator();
        }

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Cash allocated to this company, from which we can find the buying power available.
        /// When Equity turns profit available cash increases, generating a positive feed back 
        /// for successful Security.
        /// </summary>
        public decimal Cash 
        {
            get
            {
                return _cash;
            }
        }

        /// <summary>
        /// Absolute value of cash discounted from our total cash by the holdings we own.
        /// </summary>
        /// <remarks>When account has leverage the actual cash removed is a fraction of the purchase price according to the leverage</remarks>
        public decimal TotalUnleveredAbsoluteHoldingsCost 
        {
            get 
            {
                //Sum of unlevered cost of holdings
                return (from position in Securities.Values
                        select position.Holdings.UnleveredAbsoluteHoldingsCost).Sum();
            }
        }


        /// <summary>
        /// Absolute sum the individual items in portfolio.
        /// </summary>
        public decimal TotalHoldingsValue
        {
            get
            {
                //Sum sum of holdings
                return (from position in Securities.Values
                        select position.Holdings.AbsoluteHoldingsValue).Sum();
            }
        }

        /// <summary>
        /// Boolean flag indicating we have any holdings in the portfolio.
        /// </summary>
        /// <remarks>Assumes no asset can have $0 price and uses the sum of total holdings value</remarks>
        /// <seealso cref="Invested"/>
        public bool HoldStock 
        {
            get 
            {
                return TotalHoldingsValue > 0;
            }
        }


        /// <summary>
        /// Alias for HoldStock. Check if we have and holdings.
        /// </summary>
        /// <seealso cref="HoldStock"/>
        public bool Invested 
        {
            get 
            {
                return HoldStock;
            }
        }

        /// <summary>
        /// Get the total unrealised profit in our portfolio from the individual security unrealized profits.
        /// </summary>
        public decimal TotalUnrealisedProfit 
        {
            get 
            {
                return (from position in Securities.Values
                               select position.Holdings.UnrealizedProfit).Sum();
            }
        }


        /// <summary>
        /// Get the total unrealised profit in our portfolio from the individual security unrealized profits.
        /// </summary>
        /// <remarks>Added alias for American spelling</remarks>
        public decimal TotalUnrealizedProfit 
        {
            get 
            {
                return TotalUnrealisedProfit;
            }
        }

        /// <summary>
        /// Total portfolio value if we sold all holdings at current market rates.
        /// </summary>
        /// <remarks>Cash + TotalUnrealisedProfit + TotalUnleveredAbsoluteHoldingsCost</remarks>
        /// <seealso cref="Cash"/>
        /// <seealso cref="TotalUnrealizedProfit"/>
        /// <seealso cref="TotalUnleveredAbsoluteHoldingsCost"/>
        public decimal TotalPortfolioValue 
        {
            get 
            {
                return Cash + TotalUnrealisedProfit + TotalUnleveredAbsoluteHoldingsCost;
            }
        }

        /// <summary>
        /// Total fees paid during the algorithm operation across all securities in portfolio.
        /// </summary>
        public decimal TotalFees 
        {
            get 
            {
                return (from position in Securities.Values
                        select position.Holdings.TotalFees).Sum();
            }
        }

        /// <summary>
        /// Sum of all gross profit across all securities in portfolio.
        /// </summary>
        public decimal TotalProfit 
        {
            get 
            {
                return (from position in Securities.Values
                        select position.Holdings.Profit).Sum();
            }
        }

        /// <summary>
        /// Total sale volume since the start of algorithm operations.
        /// </summary>
        public decimal TotalSaleVolume 
        {
            get 
            {
                return (from position in Securities.Values
                        select position.Holdings.TotalSaleVolume).Sum();
            }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Indexer for the PortfolioManager class to access the underlying security holdings objects.
        /// </summary>
        /// <param name="symbol">Search string symbol as indexer</param>
        /// <returns>SecurityHolding class from the algorithm securities</returns>
        public SecurityHolding this [string symbol] 
        {
            get 
            {
                return Securities[symbol].Holdings;
            }
            set 
            {
                Securities[symbol].Holdings = value;
            }
        }

        /// <summary>
        /// Set the cash this algorithm is to manage.
        /// </summary>
        /// <param name="cash">Decimal cash value of portfolio</param>
        public void SetCash(decimal cash) 
        {
            _cash = cash;
        }

        /// <summary>
        /// The total buying power remaining after factoring in leverage.
        /// </summary>
        /// <remarks>
        ///     Because each security has its own leverage the buying power is a function of security.
        ///     Similarly the desired trade direction can impact the buying power available
        /// </remarks>
        /// <returns>Decimal total buying power for this symbol</returns>
        public virtual decimal GetFreeCash(string symbol, OrderDirection direction = OrderDirection.Hold) 
        {
            //Each asset has different leverage values, so affects our cash position in different ways.
            var holdings = Securities[symbol].Holdings;

            if (direction == OrderDirection.Hold) return Cash;
            //Log.Debug("SecurityPortfolioManager.GetFreeCash(): Direction: " + direction.ToString());


            //If the order is in the same direction as holdings, our remaining cash is our cash
            //In the opposite direction, our remaining cash is 2 x current value of assets + our cash
            if (Securities[symbol].Holdings.IsLong)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return Cash;
                    case OrderDirection.Sell:
                        return (holdings.UnrealizedProfit + holdings.UnleveredAbsoluteHoldingsCost) * 2 + Cash;
                }
            }
            else if (Securities[symbol].Holdings.IsShort)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return (holdings.UnrealizedProfit + holdings.UnleveredAbsoluteHoldingsCost) * 2 + Cash;
                    case OrderDirection.Sell:
                        return Cash;
                }
            }

            //No holdings, return cash
            return Cash;
        }



        /// <summary>
        /// Calculate the new average price after processing a partial/complete order fill event. 
        /// </summary>
        /// <remarks>
        ///     For purchasing stocks from zero holdings, the new average price is the sale price.
        ///     When simply partially reducing holdings the average price remains the same.
        ///     When crossing zero holdings the average price becomes the trade price in the new side of zero.
        /// </remarks>
        public virtual void ProcessFill(OrderEvent fill) 
        {
            //Get the required information from the vehicle this order will affect
            var symbol = fill.Symbol;
            var vehicle = Securities[symbol];
            var isLong = vehicle.Holdings.IsLong;
            var isShort = vehicle.Holdings.IsShort;
            var closedPosition = false;
            //Make local decimals to avoid any rounding errors from int multiplication
            var quantityHoldings = (decimal)vehicle.Holdings.Quantity;
            var absoluteHoldingsQuantity = vehicle.Holdings.AbsoluteQuantity;
            var averageHoldingsPrice = vehicle.Holdings.AveragePrice;
            var leverage = vehicle.Leverage;

            try
            {
                //Update the Vehicle approximate total sales volume.
                vehicle.Holdings.AddNewSale(fill.FillPrice * Convert.ToDecimal(fill.AbsoluteFillQuantity));

                //Get the Fee for this Order - Update the Portfolio Cash Balance: Remove Transacion Fees.
                var feeThisOrder = Math.Abs(Securities[symbol].Model.GetOrderFee(fill.AbsoluteFillQuantity, fill.FillPrice));
                vehicle.Holdings.AddNewFee(feeThisOrder);
                _cash -= feeThisOrder;

                
                //Calculate & Update the Last Trade Profit
                if (isLong && fill.Direction == OrderDirection.Sell) 
                {
                    //Closing up a long position
                    if (quantityHoldings >= fill.AbsoluteFillQuantity) 
                    {
                        //Closing up towards Zero.
                        _lastTradeProfit = (fill.FillPrice - averageHoldingsPrice) * fill.AbsoluteFillQuantity;
                        
                        //New cash += profitLoss + costOfAsset/leverage.
                        _cash += _lastTradeProfit + ((averageHoldingsPrice * fill.AbsoluteFillQuantity) / leverage);
                    } 
                    else 
                    {
                        //Closing up to Neg/Short Position (selling more than we have) - Only calc profit on the stock we have to sell.
                        _lastTradeProfit = (fill.FillPrice - averageHoldingsPrice) * quantityHoldings;

                        //New cash += profitLoss + costOfAsset/leverage.
                        _cash += _lastTradeProfit + ((averageHoldingsPrice * quantityHoldings) / leverage);
                    }
                    closedPosition = true;
                }
                else if (isShort && fill.Direction == OrderDirection.Buy)
                {
                    //Closing up a short position.
                    if (absoluteHoldingsQuantity >= fill.FillQuantity) 
                    {
                        //Reducing the stock we have, and enough stock on hand to process order.
                        _lastTradeProfit = (averageHoldingsPrice - fill.FillPrice) * fill.AbsoluteFillQuantity;

                        //New cash += profitLoss + costOfAsset/leverage.
                        _cash += _lastTradeProfit + ((averageHoldingsPrice * fill.AbsoluteFillQuantity) / leverage);
                    }
                    else 
                    {
                        //Increasing stock holdings, short to positive through zero, but only calc profit on stock we Buy.
                        _lastTradeProfit = (averageHoldingsPrice - fill.FillPrice) * absoluteHoldingsQuantity;

                        //New cash += profitLoss + costOfAsset/leverage.
                        _cash += _lastTradeProfit + ((averageHoldingsPrice * absoluteHoldingsQuantity) / leverage);
                    }
                    closedPosition = true;
                }


                if (closedPosition)
                {
                    //Update Vehicle Profit Tracking:
                    _profit += _lastTradeProfit;
                    vehicle.Holdings.AddNewProfit(_lastTradeProfit);
                    vehicle.Holdings.SetLastTradeProfit(_lastTradeProfit);
                    AddTransactionRecord(vehicle.Time, _lastTradeProfit - 2 * feeThisOrder);
                }


                //UPDATE HOLDINGS QUANTITY, AVG PRICE:
                //Currently NO holdings. The order is ALL our holdings.
                if (quantityHoldings == 0) 
                {
                    //First transaction just subtract order from cash and set our holdings:
                    averageHoldingsPrice = fill.FillPrice;
                    quantityHoldings = fill.FillQuantity;
                    _cash -= (fill.FillPrice * Convert.ToDecimal(fill.AbsoluteFillQuantity)) / leverage;
                }
                else if (isLong) 
                {
                    //If we're currently LONG on the stock.
                    switch (fill.Direction) 
                    {
                        case OrderDirection.Buy:
                            //Update the Holding Average Price: Total Value / Total Quantity:
                            averageHoldingsPrice = ((averageHoldingsPrice * quantityHoldings) + (fill.FillQuantity * fill.FillPrice)) / (quantityHoldings + (decimal)fill.FillQuantity);
                            //Add the new quantity:
                            quantityHoldings += fill.FillQuantity;
                            //Subtract this order from cash:
                            _cash -= (fill.FillPrice * Convert.ToDecimal(fill.AbsoluteFillQuantity)) / leverage;
                            break;

                        case OrderDirection.Sell:
                            quantityHoldings += fill.FillQuantity; //+ a short = a subtraction
                            if (quantityHoldings < 0) 
                            {
                                //If we've now passed through zero from selling stock: new avg price:
                                averageHoldingsPrice = fill.FillPrice;
                                _cash -= (fill.FillPrice * Math.Abs(quantityHoldings)) / leverage;
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
                                _cash -= (fill.FillPrice * Math.Abs(quantityHoldings)) / leverage;
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
                            averageHoldingsPrice = ((averageHoldingsPrice * quantityHoldings) + (Convert.ToDecimal(fill.FillQuantity) * fill.FillPrice)) / (quantityHoldings + (decimal)fill.FillQuantity);
                            quantityHoldings += fill.FillQuantity;
                            _cash -= (fill.FillPrice * Convert.ToDecimal(fill.AbsoluteFillQuantity)) / leverage;
                            break;
                    }
                }
            } 
            catch( Exception err )
            {
                Log.Error("SecurityPortfolioManager.ProcessFill(orderEvent): " + err.Message);
            }
            
            //Set the results back to the vehicle.
            vehicle.Holdings.SetHoldings(averageHoldingsPrice, Convert.ToInt32(quantityHoldings));
        } // End Process Fill


        /// <summary>
        /// Scan the portfolio and the updated data for a potential margin call situation which may get the holdings below zero! 
        /// If there is a margin call, liquidate the portfolio immediately before the portfolio gets sub zero.
        /// </summary>
        /// <returns>True for a margin call on the holdings.</returns>
        public bool ScanForMarginCall()
        {
            // TODO.
            return false;
        }


        /// <summary>
        /// Record the transaction value and time in a list to later be processed for statistics creation.
        /// </summary>
        /// <remarks>
        /// Bit of a hack -- but using datetime as dictionary key is dangerous as you can process multiple orders within a second.
        /// For the accounting / statistics generating purposes its not really critical to know the precise time, so just add a millisecond while there's an identical key.
        /// </remarks>
        /// <param name="time">Time of order processed </param>
        /// <param name="transactionProfitLoss">Profit Loss.</param>
        private void AddTransactionRecord(DateTime time, decimal transactionProfitLoss)
        {
            var clone = time;
            while (Transactions.TransactionRecord.ContainsKey(clone))
            {
                clone = clone.AddMilliseconds(1);
            }
            Transactions.TransactionRecord.Add(clone, transactionProfitLoss);
        }

    } //End Algorithm Portfolio Class
} // End QC Namespace
