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
        /// Initial Overnight margin requirement for the contract effective from the date of change
        /// </summary>
        public decimal InitialOvernightMarginRequirement => GetCurrentMarginRequirements(_security)?.InitialOvernight ?? 0m;

        /// <summary>
        /// Maintenance Overnight margin requirement for the contract effective from the date of change
        /// </summary>
        public decimal MaintenanceOvernightMarginRequirement => GetCurrentMarginRequirements(_security)?.MaintenanceOvernight ?? 0m;

        /// <summary>
        /// Initial Intraday margin for the contract effective from the date of change
        /// </summary>
        public decimal InitialIntradayMarginRequirement => GetCurrentMarginRequirements(_security)?.InitialIntraday ?? 0m;

        /// <summary>
        /// Maintenance Intraday margin requirement for the contract effective from the date of change
        /// </summary>
        public decimal MaintenanceIntradayMarginRequirement => GetCurrentMarginRequirements(_security)?.MaintenanceIntraday ?? 0m;

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
        public override decimal GetLeverage(Security security)
        {
            return 1;
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
        /// Get the maximum market order quantity to obtain a position with a given buying power percentage.
        /// Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the target signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public override GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(
            GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters)
        {
            if (Math.Abs(parameters.TargetBuyingPower) > 1)
            {
                throw new InvalidOperationException(
                    "Futures do not allow specifying a leveraged target, since they are traded using margin which already is leveraged. " +
                    $"Possible target buying power goes from -1 to 1, target provided is: {parameters.TargetBuyingPower}");
            }
            return base.GetMaximumOrderQuantityForTargetBuyingPower(parameters);
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

            var orderMargin = GetInitialMarginRequirement(parameters.Security, parameters.Order.Quantity);

            return orderMargin + Math.Sign(orderMargin) * feesInAccountCurrency;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the </returns>
        protected override decimal GetMaintenanceMargin(Security security)
        {
            if (security?.GetLastData() == null || security.Holdings.HoldingsCost == 0m)
                return 0m;

            var marginReq = GetCurrentMarginRequirements(security);

            if (security.Exchange.ExchangeOpen)
            {
                return marginReq.MaintenanceIntraday * security.Holdings.AbsoluteQuantity;
            }

            // margin is per contract
            return marginReq.MaintenanceOvernight * security.Holdings.AbsoluteQuantity;
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        protected override decimal GetInitialMarginRequirement(Security security, decimal quantity)
        {
            if (security?.GetLastData() == null || quantity == 0m)
                return 0m;

            var marginReq = GetCurrentMarginRequirements(security);

            if (security.Exchange.ExchangeOpen)
            {
                return marginReq.InitialIntraday * quantity;
            }

            // margin is per contract
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
                Log.Trace($"Couldn't parse date/time while reading future margin requirement file. Line: {csvLine}");
            }

            decimal initialOvernight;
            if (!decimal.TryParse(line[1], out initialOvernight))
            {
                Log.Trace($"Couldn't parse Initial Overnight margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            decimal maintenanceOvernight;
            if (!decimal.TryParse(line[2], out maintenanceOvernight))
            {
                Log.Trace($"Couldn't parse Maintenance Overnight margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            // default value, if present in file we try to parse
            decimal initialIntraday = initialOvernight * 0.4m;
            if (line.Length >= 4
                && !decimal.TryParse(line[3], out initialIntraday))
            {
                Log.Trace($"Couldn't parse Initial Intraday margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            // default value, if present in file we try to parse
            decimal maintenanceIntraday = maintenanceOvernight * 0.4m;
            if (line.Length >= 5
                && !decimal.TryParse(line[4], out maintenanceIntraday))
            {
                Log.Trace($"Couldn't parse Maintenance Intraday margin requirements while reading future margin requirement file. Line: {csvLine}");
            }

            return new MarginRequirementsEntry
            {
                Date = date,
                InitialOvernight = initialOvernight,
                MaintenanceOvernight = maintenanceOvernight,
                InitialIntraday = initialIntraday,
                MaintenanceIntraday = maintenanceIntraday
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

            /// <summary>
            /// Initial intraday margin for the contract effective from the date of change
            /// </summary>
            public decimal InitialIntraday;

            /// <summary>
            /// Maintenance intraday margin for the contract effective from the date of change
            /// </summary>
            public decimal MaintenanceIntraday;
        }
    }
}