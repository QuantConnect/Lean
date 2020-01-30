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
using System.IO;
using System.Linq;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Represents a simple margin model for margin futures. Margin file contains Initial and Maintenance margins
    /// </summary>
    public class FutureMarginModel : SecurityMarginModel
    {
        private static readonly object DataFolderSymbolLock = new object();

        // historical database of margin requirements
        private MarginRequirementsEntry[] _marginRequirementsHistory;
        private int _marginCurrentIndex;

        private readonly Security _security;

        /// <summary>
        /// Initial margin requirement for the contract effective from the date of change
        /// </summary>
        public decimal InitialMarginRequirement => GetCurrentMarginRequirements(_security)?.InitialOvernight ?? 0m;

        /// <summary>
        /// Maintenance margin requirement for the contract effective from the date of change
        /// </summary>
        public decimal MaintenanceMarginRequirement => GetCurrentMarginRequirements(_security)?.MaintenanceOvernight ?? 0m;

        /// <summary>
        /// Initializes a new instance of the <see cref="FutureMarginModel"/>
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required unused buying power for the account.</param>
        /// <param name="security">The security that this model belongs to</param>
        public FutureMarginModel(decimal requiredFreeBuyingPowerPercent = 0, Security security = null)
        {
            RequiredFreeBuyingPowerPercent = requiredFreeBuyingPowerPercent;
            _security = security;
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        /// <remarks>Calculated using the maintenance margin requirement and current security holdings.
        /// For no holdings will return 1.</remarks>
        public override decimal GetLeverage(Security security)
        {
            var marginRequirement = GetMaintenanceMarginRequirement(security);
            return marginRequirement == 0 ? 1m : 1 / marginRequirement;
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, futures
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
            // Futures are leveraged products and different leverage cannot be set by user.
            throw new InvalidOperationException("Futures are leveraged products and different leverage cannot be set by user");
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        protected override decimal GetInitialMarginRequiredForOrder(
            InitialMarginRequiredForOrderParameters parameters)
        {
            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.

            var fees = parameters.Security.FeeModel.GetOrderFee(
                new OrderFeeParameters(parameters.Security,
                    parameters.Order)).Value;
            var feesInAccountCurrency = parameters.CurrencyConverter.
                ConvertToAccountCurrency(fees).Amount;

            var marginRequired = GetInitialMarginRequirement(parameters.Security, parameters.Order.AbsoluteQuantity);

            return (marginRequired + feesInAccountCurrency) * Math.Sign(parameters.Order.Quantity);
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding. Always positive
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the current position</returns>
        protected override decimal GetMaintenanceMargin(Security security)
        {
            if (security?.GetLastData() == null || security.Holdings.AbsoluteQuantity == 0m)
                return 0m;

            var marginReq = GetCurrentMarginRequirements(security);

            // margin is per contract
            return marginReq.MaintenanceOvernight * security.Holdings.AbsoluteQuantity;
        }

        /// <summary>
        /// Gets the margin cash available for a trade
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        protected override decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
        {
            var result = portfolio.MarginRemaining;

            if (direction != OrderDirection.Hold)
            {
                var holdings = security.Holdings;
                //If the order is in the same direction as holdings, our remaining cash is our cash
                //In the opposite direction, our remaining cash is 2 x current value of assets + our cash
                if (holdings.IsLong)
                {
                    switch (direction)
                    {
                        case OrderDirection.Sell:
                            result +=
                                // portion of margin to close the existing position
                                GetMaintenanceMargin(security) +
                                // portion of margin to open the new position
                                GetInitialMarginRequirement(security, security.Holdings.AbsoluteQuantity);
                            break;
                    }
                }
                else if (holdings.IsShort)
                {
                    switch (direction)
                    {
                        case OrderDirection.Buy:
                            result +=
                                // portion of margin to close the existing position
                                GetMaintenanceMargin(security) +
                                // portion of margin to open the new position
                                GetInitialMarginRequirement(security, security.Holdings.AbsoluteQuantity);
                            break;
                    }
                }
            }

            result -= portfolio.TotalPortfolioValue * RequiredFreeBuyingPowerPercent;
            return result < 0 ? 0 : result;
        }

        /// <summary>
        /// Gets the initial margin required for a given order
        /// </summary>
        /// <param name="order">The desired order</param>
        /// <param name="security">The target security</param>
        /// <returns>Returns the margin required</returns>
        /// <remarks>This method is required to allow the <see cref="FutureMarginModel"/>
        /// to factor in margin requirements since the leveraged used depends on it and is
        /// independent of the user provided target. For this reason targets above absolute 1
        /// do no make sense and are not allowed. <see cref="GetMaximumOrderQuantityForTargetValue"/>
        /// Note that for example equities, factor leverage into the target portfolio value</remarks>
        protected sealed override decimal GetNormalizedInitialMarginForOrder(Order order, Security security)
        {
            // we use Quantity here and not AbsoluteQuantity on purpose to get the sign
            return GetInitialMarginRequirement(security, order.Quantity);
        }

        /// <summary>
        /// The percentage of an order's absolute cost that must be held in free cash in order to place the order
        /// </summary>
        protected override decimal GetInitialMarginRequirement(Security security)
        {
            // this method shouldn't be called since we override all methods where it's used
            throw new InvalidOperationException("This method shouldn't be called for Futures." +
                                                " See 'protected decimal GetInitialMarginRequirement(Security security, decimal absoluteQuantity)'");
        }

        /// <summary>
        /// The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call
        /// </summary>
        public override decimal GetMaintenanceMarginRequirement(Security security)
        {
            if (security.Holdings.HoldingsValue == 0)
            {
                return 0;
            }
            // we have to express the maintenance margin as a percentage of the holdings value
            return Math.Abs(GetMaintenanceMargin(security) / security.Holdings.HoldingsValue);
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given value in account currency.
        /// Will not take into account buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the target percentage holdings</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public override GetMaximumOrderQuantityForTargetValueResult GetMaximumOrderQuantityForTargetValue(
            GetMaximumOrderQuantityForTargetValueParameters parameters)
        {
            if (Math.Abs(parameters.Target) > 1)
            {
                throw new InvalidOperationException(
                    "Futures do not allow specifying a leveraged target, since they are traded using margin which already is leveraged. " +
                    $"Possible target values go from -1 to 1, target provided is: {parameters.Target}");
            }
            return base.GetMaximumOrderQuantityForTargetValue(parameters);
        }

        /// <summary>
        /// The percentage of an order's absolute cost that must be held in free cash in order to place the order
        /// </summary>
        /// <remarks>This method is protected for testing</remarks>
        protected decimal GetInitialMarginRequirement(Security security, decimal quantity)
        {
            if (security?.GetLastData() == null || quantity == 0m)
                return 0m;

            var marginReq = GetCurrentMarginRequirements(security);

            // margin is per contract and margin requirement is a percentage of orders holdings value
            return marginReq.InitialOvernight * quantity;
        }

        private MarginRequirementsEntry GetCurrentMarginRequirements(Security security)
        {
            if (security?.GetLastData() == null)
                return null;

            if (_marginRequirementsHistory == null)
            {
                _marginRequirementsHistory = LoadMarginRequirementsHistory(security.Symbol);
                _marginCurrentIndex = 0;
            }

            var date = security.GetLastData().Time.Date;

            while (_marginCurrentIndex + 1 < _marginRequirementsHistory.Length &&
                _marginRequirementsHistory[_marginCurrentIndex + 1].Date <= date)
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
        private MarginRequirementsEntry[] LoadMarginRequirementsHistory(Symbol symbol)
        {
            var directory = Path.Combine(Globals.DataFolder,
                symbol.SecurityType.ToLower(),
                symbol.ID.Market.ToLowerInvariant(),
                "margins");

            return FromCsvFile(Path.Combine(directory, symbol.ID.Symbol + ".csv"));
        }

        /// <summary>
        /// Reads margin requirements file and returns a sorted list of historical margin changes
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <returns>Sorted list of historical margin changes</returns>
        private MarginRequirementsEntry[] FromCsvFile(string file)
        {
            lock (DataFolderSymbolLock)
            {
                if (!File.Exists(file))
                {
                    Log.Trace($"Unable to locate future margin requirements file. Defaulting to zero margin for this symbol. File: {file}");

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
        }

        /// <summary>
        /// Creates a new instance of <see cref="MarginRequirementsEntry"/> from the specified csv line
        /// </summary>
        /// <param name="csvLine">The csv line to be parsed</param>
        /// <returns>A new <see cref="MarginRequirementsEntry"/> for the specified csv line</returns>
        private MarginRequirementsEntry FromCsvLine(string csvLine)
        {
            var line = csvLine.Split(',');
            DateTime date;

            if (!DateTime.TryParseExact(line[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Log.Trace($"Couldn't parse date/time while reading future margin requirement file. Date {line[0]}. Line: {csvLine}");
            }

            decimal initial;
            if (!decimal.TryParse(line[1], out initial))
            {
                Log.Trace($"Couldn't parse Initial margin requirements while reading future margin requirement file. Date {line[1]}. Line: {csvLine}");
            }

            decimal maintenance;
            if (!decimal.TryParse(line[2], out maintenance))
            {
                Log.Trace($"Couldn't parse Maintenance margin requirements while reading future margin requirement file. Date {line[2]}. Line: {csvLine}");
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