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

using System.Globalization;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a simple margining model for margining futures. Margin file contains Initial and Maintenance margins 
    /// </summary>
    public class FutureMarginModel : ISecurityMarginModel
    {
        private static readonly object DataFolderSymbolLock = new object();

        // historical database of margin requirements
        private MarginRequirementsEntry[] _marginRequirementsHistory;
        private int _marginCurrentIndex;


        /// <summary>
        /// Initializes a new instance of the <see cref="FutureMarginModel"/>
        /// </summary>
        public FutureMarginModel()
        {
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public virtual decimal GetLeverage(Security security)
        {
            return 1/GetMaintenanceMarginRequirement(security, security.Holdings.HoldingsCost);
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, futures
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public virtual void SetLeverage(Security security, decimal leverage)
        {
            // Futures are leveraged products and different leverage cannot be set by user.
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="security">The security to compute initial margin for</param>
        /// <param name="order">The order to be executed</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public virtual decimal GetInitialMarginRequiredForOrder(Security security, Order order)
        {
            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.
            var orderFees = security.FeeModel.GetOrderFee(security, order);
            var value = order.GetValue(security);
            var orderValue = value * GetInitialMarginRequirement(security, value);

            return orderValue + Math.Sign(orderValue) * orderFees;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the </returns>
        public virtual decimal GetMaintenanceMargin(Security security)
        {
            return security.Holdings.AbsoluteHoldingsCost*GetMaintenanceMarginRequirement(security, security.Holdings.HoldingsCost);
        }

        /// <summary>
        /// Gets the margin cash available for a trade
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        public virtual decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
        {
            var holdings = security.Holdings;

            if (direction == OrderDirection.Hold)
            {
                return portfolio.MarginRemaining;
            }

            //If the order is in the same direction as holdings, our remaining cash is our cash
            //In the opposite direction, our remaining cash is 2 x current value of assets + our cash
            if (holdings.IsLong)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return portfolio.MarginRemaining;

                    case OrderDirection.Sell:
                        return 
                            // portion of margin to close the existing position
                            GetMaintenanceMargin(security) +
                            // portion of margin to open the new position
                            security.Holdings.AbsoluteHoldingsValue * GetInitialMarginRequirement(security, security.Holdings.HoldingsValue) +
                            portfolio.MarginRemaining;
                }
            }
            else if (holdings.IsShort)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return
                            // portion of margin to close the existing position
                            GetMaintenanceMargin(security) +
                            // portion of margin to open the new position
                            security.Holdings.AbsoluteHoldingsValue * GetInitialMarginRequirement(security, security.Holdings.HoldingsValue) +
                            portfolio.MarginRemaining;

                    case OrderDirection.Sell:
                        return portfolio.MarginRemaining;
                }
            }

            //No holdings, return cash
            return portfolio.MarginRemaining;
        }

        /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="netLiquidationValue">The net liquidation value for the entire account</param>
        /// <param name="totalMargin">The total margin used by the account in units of base currency</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        public virtual SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal netLiquidationValue, decimal totalMargin)
        {
            // leave a buffer in default implementation
            const decimal marginBuffer = 0.10m;

            if (totalMargin <= netLiquidationValue*(1 + marginBuffer))
            {
                return null;
            }

            if (!security.Holdings.Invested)
            {
                return null;
            }

            if (security.QuoteCurrency.ConversionRate == 0m)
            {
                // check for div 0 - there's no conv rate, so we can't place an order
                return null;
            }

            // compute the amount of quote currency we need to liquidate in order to get within margin requirements
            var deltaInQuoteCurrency = (totalMargin - netLiquidationValue)/security.QuoteCurrency.ConversionRate;

            // compute the number of shares required for the order, rounding up
            var unitPriceInQuoteCurrency = security.Price * security.SymbolProperties.ContractMultiplier;
            int quantity = (int) (Math.Round(deltaInQuoteCurrency/unitPriceInQuoteCurrency, MidpointRounding.AwayFromZero)/GetMaintenanceMarginRequirement(security, security.Holdings.HoldingsValue));

            // don't try and liquidate more share than we currently hold, minimum value of 1, maximum value for absolute quantity
            quantity = Math.Max(1, Math.Min((int)security.Holdings.AbsoluteQuantity, quantity));
            if (security.Holdings.IsLong)
            {
                // adjust to a sell for long positions
                quantity *= -1;
            }

            return new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, security.LocalTime.ConvertToUtc(security.Exchange.TimeZone), "Margin Call");
        }
        
        /// <summary>
        /// The percentage of an order's absolute cost that must be held in free cash in order to place the order
        /// </summary>
        protected virtual decimal GetInitialMarginRequirement(Security security, decimal holding)
        {
            var symbol = security.Symbol;
            var date = security.GetLastData().Time.Date;
            var marginReq = GetCurrentMarginRequirements(symbol, date);

            return marginReq.InitialOvernight / holding;
        }

        /// <summary>
        /// The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call
        /// </summary>
        protected virtual decimal GetMaintenanceMarginRequirement(Security security, decimal holding)
        {
            if (security == null ||
                security.GetLastData() == null ||
                holding == 0m)
                return 0m;

            var symbol = security.Symbol;
            var date = security.GetLastData().Time.Date;
            var marginReq = GetCurrentMarginRequirements(symbol, date);

            return marginReq.MaintenanceOvernight / holding;
        }
                
        
        private MarginRequirementsEntry GetCurrentMarginRequirements (Symbol symbol, DateTime date)
        {
            if (_marginRequirementsHistory == null)
            {
                _marginRequirementsHistory = LoadMarginRequirementsHistory(symbol.Underlying.Value);
                _marginCurrentIndex = 0;
            }

            while (_marginCurrentIndex + 1 < _marginRequirementsHistory.Length && 
                _marginRequirementsHistory[_marginCurrentIndex + 1].Date <= date )
            {
                _marginCurrentIndex++;
            }

            return _marginRequirementsHistory[_marginCurrentIndex];
        }

        /// <summary>
        /// Gets the sorted list of historical margin changes produced by reading in the margin requirements 
        /// data found in /Data/symbol-margin/
        /// </summary>
        /// <returns>Sorted list of historical margin changes</returns>
        private MarginRequirementsEntry[] LoadMarginRequirementsHistory(string symbol)
        {
            lock (DataFolderSymbolLock)
            {
                var directory = Path.Combine(Globals.DataFolder, "margins");
                return FromCsvFile(Path.Combine(directory, symbol + ".csv"));
            }
        }
                
        /// <summary>
        /// Reads margin requirements file and returns a sorted list of historical margin changes
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <returns>Sorted list of historical margin changes</returns>
        private MarginRequirementsEntry[] FromCsvFile(string file)
        {
            if (!File.Exists(file))
            {
                Log.Trace("Unable to locate future margin requirements file. Defaulting to zero margin for this symbol. File: {0}" , file);

                return new[] {
                                new MarginRequirementsEntry
                                {
                                  Date = DateTime.MinValue
                                }
                            };
            }

            // skip the first header line, also skip #'s as these are comment lines

            return File.ReadLines(file)
                .Where(x => !x.StartsWith("#") && !string.IsNullOrWhiteSpace(x))
                .Skip(1)
                .Select(FromCsvLine)
                .OrderBy(x => x.Date)
                .ToArray();
        }
                
        /// <summary>
        /// Creates a new instance of <see cref="MarginRequirementsEntry"/> from the specified csv line
        /// </summary>
        /// <param name="csvLine">The csv line to be parsed</param>
        /// <returns>A new <see cref="MarginRequirementsEntry"/> for the specified csv line</returns>
        private MarginRequirementsEntry FromCsvLine(string csvLine)
        {
            var line = csvLine.Split(',');
            var date = DateTime.MinValue;

            if(!DateTime.TryParseExact(line[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Log.Trace("Couldn't parse date/time while reading future margin requirement file. Date {0}. Line: {1}", line[0], csvLine);
            }

            var initial = 0m;

            if (!decimal.TryParse(line[1], out initial))
            {
                Log.Trace("Couldn't parse Initial margin requirements while reading future margin requirement file. Date {0}. Line: {1}", line[1], csvLine);
            }

            var maintenance = 0m;

            if (!decimal.TryParse(line[2], out maintenance))
            {
                Log.Trace("Couldn't parse Maintenance margin requirements while reading future margin requirement file. Date {0}. Line: {1}", line[2], csvLine);
            }

            return new MarginRequirementsEntry()
                    {
                        Date = date,
                        InitialOvernight = initial,
                        MaintenanceOvernight = maintenance
                    };
        }


        // Private POCO class for modeling margin requirements at given date
        class MarginRequirementsEntry
        {
            /// <summary>
            /// Date of margin requirements change
            /// </summary>
            public DateTime Date;
            
            /// <summary>
            /// Initial overnight margin for the contract effective from the date of change
            /// </summary>
            public decimal InitialOvernight;

            /// <summary>
            /// Maintenance overnight margin for the contract effective from the date of change
            /// </summary>
            public decimal MaintenanceOvernight;
        }
    }
}