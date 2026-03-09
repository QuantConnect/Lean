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
using System.IO;
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Represents a simple margin model for margin futures. Margin file contains Initial and Maintenance margins
    /// </summary>
    public class FutureMarginModel : SecurityMarginModel
    {
        private static IDataProvider _dataProvider;
        private static readonly object _locker = new();
        private static Dictionary<string, MarginRequirementsEntry[]> _marginRequirementsCache = new();

        // historical database of margin requirements
        private int _marginCurrentIndex;

        private readonly Security _security;

        /// <summary>
        /// True will enable usage of intraday margins.
        /// </summary>
        /// <remarks>Disabled by default. Note that intraday margins are less than overnight margins
        /// and could lead to margin calls</remarks>
        public bool EnableIntradayMargins { get; set; }

        /// <summary>
        /// Initial Overnight margin requirement for the contract effective from the date of change
        /// </summary>
        public virtual decimal InitialOvernightMarginRequirement => GetCurrentMarginRequirements(_security)?.InitialOvernight ?? 0m;

        /// <summary>
        /// Maintenance Overnight margin requirement for the contract effective from the date of change
        /// </summary>
        public virtual decimal MaintenanceOvernightMarginRequirement => GetCurrentMarginRequirements(_security)?.MaintenanceOvernight ?? 0m;

        /// <summary>
        /// Initial Intraday margin for the contract effective from the date of change
        /// </summary>
        public virtual decimal InitialIntradayMarginRequirement => GetCurrentMarginRequirements(_security)?.InitialIntraday ?? 0m;

        /// <summary>
        /// Maintenance Intraday margin requirement for the contract effective from the date of change
        /// </summary>
        public virtual decimal MaintenanceIntradayMarginRequirement => GetCurrentMarginRequirements(_security)?.MaintenanceIntraday ?? 0m;

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
        public override InitialMargin GetInitialMarginRequiredForOrder(
            InitialMarginRequiredForOrderParameters parameters
            )
        {
            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.

            var fees = parameters.Security.FeeModel.GetOrderFee(
                new OrderFeeParameters(parameters.Security,
                    parameters.Order)).Value;
            var feesInAccountCurrency = parameters.CurrencyConverter.
                ConvertToAccountCurrency(fees).Amount;

            var orderMargin = this.GetInitialMarginRequirement(parameters.Security, parameters.Order.Quantity);

            return new InitialMargin(orderMargin + Math.Sign(orderMargin) * feesInAccountCurrency);
        }

        /// <summary>
        /// Gets the margin currently allotted to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the </returns>
        public override MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            if (parameters.Quantity == 0m)
            {
                return 0m;
            }

            var security = parameters.Security;
            var marginReq = GetCurrentMarginRequirements(security);
            if (marginReq == null)
            {
                return 0m;
            }

            if (EnableIntradayMargins
                && security.Exchange.ExchangeOpen
                && !security.Exchange.ClosingSoon)
            {
                return marginReq.MaintenanceIntraday * parameters.AbsoluteQuantity * security.QuoteCurrency.ConversionRate;
            }

            // margin is per contract
            return marginReq.MaintenanceOvernight * parameters.AbsoluteQuantity * security.QuoteCurrency.ConversionRate;
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        public override InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            if (quantity == 0m)
            {
                return InitialMargin.Zero;
            }

            var marginReq = GetCurrentMarginRequirements(security);
            if (marginReq == null)
            {
                return InitialMargin.Zero;
            }

            if (EnableIntradayMargins
                && security.Exchange.ExchangeOpen
                && !security.Exchange.ClosingSoon)
            {
                return new InitialMargin(marginReq.InitialIntraday * quantity * security.QuoteCurrency.ConversionRate);
            }

            // margin is per contract
            return new InitialMargin(marginReq.InitialOvernight * quantity * security.QuoteCurrency.ConversionRate);
        }

        private MarginRequirementsEntry GetCurrentMarginRequirements(Security security)
        {
            var lastData = security?.GetLastData();
            if (lastData == null)
            {
                return null;
            }

            var marginRequirementsHistory = LoadMarginRequirementsHistory(security.Symbol);
            var date = lastData.Time.Date;

            while (_marginCurrentIndex + 1 < marginRequirementsHistory.Length &&
                marginRequirementsHistory[_marginCurrentIndex + 1].Date <= date)
            {
                _marginCurrentIndex++;
            }

            return marginRequirementsHistory[_marginCurrentIndex];
        }

        /// <summary>
        /// Gets the sorted list of historical margin changes produced by reading in the margin requirements
        /// data found in /Data/symbol-margin/
        /// </summary>
        /// <returns>Sorted list of historical margin changes</returns>
        private static MarginRequirementsEntry[] LoadMarginRequirementsHistory(Symbol symbol)
        {
            if (!_marginRequirementsCache.TryGetValue(symbol.ID.Symbol, out var marginRequirementsEntries))
            {
                lock (_locker)
                {
                    if (!_marginRequirementsCache.TryGetValue(symbol.ID.Symbol, out marginRequirementsEntries))
                    {
                        Dictionary<string, MarginRequirementsEntry[]> marginRequirementsCache = new(_marginRequirementsCache)
                        {
                            [symbol.ID.Symbol] = marginRequirementsEntries = FromCsvFile(symbol)
                        };
                        // we change the reference so we can read without a lock
                        _marginRequirementsCache = marginRequirementsCache;
                    }
                }
            }
            return marginRequirementsEntries;
        }

        /// <summary>
        /// Reads margin requirements file and returns a sorted list of historical margin changes
        /// </summary>
        /// <param name="symbol">The symbol to fetch margin requirements for</param>
        /// <returns>Sorted list of historical margin changes</returns>
        private static MarginRequirementsEntry[] FromCsvFile(Symbol symbol)
        {
            var file = Path.Combine(Globals.DataFolder,
                                    symbol.SecurityType.ToLower(),
                                    symbol.ID.Market.ToLowerInvariant(),
                                    "margins", symbol.ID.Symbol + ".csv");

            if(_dataProvider == null)
            {
                ClearMarginCache();
                _dataProvider = Composer.Instance.GetPart<IDataProvider>();
            }

            // skip the first header line, also skip #'s as these are comment lines
            var marginRequirementsEntries = _dataProvider.ReadLines(file)
                .Where(x => !x.StartsWith("#") && !string.IsNullOrWhiteSpace(x))
                .Skip(1)
                .Select(MarginRequirementsEntry.Create)
                .OrderBy(x => x.Date)
                .ToArray();

            if (marginRequirementsEntries.Length == 0)
            {
                Log.Error($"FutureMarginModel.FromCsvFile(): Unable to locate future margin requirements file. Defaulting to zero margin for this symbol. File: {file}");

                marginRequirementsEntries = new[] {
                    new MarginRequirementsEntry
                    {
                        Date = DateTime.MinValue
                    }
                };
            }
            return marginRequirementsEntries;
        }

        /// <summary>
        /// For live deployments we don't want to have stale margin requirements to we refresh them every day
        /// </summary>
        private static void ClearMarginCache()
        {
            Task.Delay(Time.OneDay).ContinueWith((_) =>
            {
                lock (_locker)
                {
                    _marginRequirementsCache = new();
                }
                ClearMarginCache();
            });
        }
    }
}
