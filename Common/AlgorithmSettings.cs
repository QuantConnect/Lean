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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Orders.Fills;
using QuantConnect.Configuration;

namespace QuantConnect
{
    /// <summary>
    /// This class includes user settings for the algorithm which can be changed in the <see cref="IAlgorithm.Initialize"/> method
    /// </summary>
    public class AlgorithmSettings : IAlgorithmSettings
    {
        private static TimeSpan _defaultDatabasesRefreshPeriod =
            TimeSpan.TryParse(Config.Get("databases-refresh-period", "1.00:00:00"), out var refreshPeriod) ? refreshPeriod : Time.OneDay;

        // We default this to true so that we don't terminate live algorithms when the
        // brokerage account has existing holdings for an asset that is not supported by Lean.
        // Users can override this on initialization so that the algorithm is not terminated when
        // placing orders for assets without a correct definition or mapping.
        private static bool _defaultIgnoreUnknownAssetHoldings = Config.GetBool("ignore-unknown-asset-holdings", true);

        /// <summary>
        /// Gets whether or not WarmUpIndicator is allowed to warm up indicators
        /// </summary>
        public bool AutomaticIndicatorWarmUp { get; set; }

        /// <summary>
        /// True if should rebalance portfolio on security changes. True by default
        /// </summary>
        public bool? RebalancePortfolioOnSecurityChanges { get; set; }

        /// <summary>
        /// True if should rebalance portfolio on new insights or expiration of insights. True by default
        /// </summary>
        public bool? RebalancePortfolioOnInsightChanges { get; set; }

        /// <summary>
        /// The absolute maximum valid total portfolio value target percentage
        /// </summary>
        /// <remarks>This setting is currently being used to filter out undesired target percent values,
        /// caused by the IPortfolioConstructionModel implementation being used.
        /// For example rounding errors, math operations</remarks>
        public decimal MaxAbsolutePortfolioTargetPercentage { get; set; }

        /// <summary>
        /// The absolute minimum valid total portfolio value target percentage
        /// </summary>
        /// <remarks>This setting is currently being used to filter out undesired target percent values,
        /// caused by the IPortfolioConstructionModel implementation being used.
        /// For example rounding errors, math operations</remarks>
        public decimal MinAbsolutePortfolioTargetPercentage { get; set; }

        /// <summary>
        /// Configurable minimum order margin portfolio percentage to ignore bad orders, orders with unrealistic small sizes
        /// </summary>
        /// <remarks>Default value is 0.1% of the portfolio value. This setting is useful to avoid small trading noise when using SetHoldings</remarks>
        public decimal MinimumOrderMarginPortfolioPercentage { get; set; }

        /// <summary>
        /// Gets/sets the maximum number of concurrent market data subscriptions available
        /// </summary>
        /// <remarks>
        /// All securities added with <see cref="IAlgorithm.AddSecurity"/> are counted as one,
        /// with the exception of options and futures where every single contract in a chain counts as one.
        /// </remarks>
        [Obsolete("This property is deprecated. Please observe data subscription limits set by your brokerage to avoid runtime errors.")]
        public int DataSubscriptionLimit { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets/sets the SetHoldings buffers value.
        /// The buffer is used for orders not to be rejected due to volatility when using SetHoldings and CalculateOrderQuantity
        /// </summary>
        public decimal? FreePortfolioValue { get; set; }

        /// <summary>
        /// Gets/sets the SetHoldings buffers value percentage.
        /// This percentage will be used to set the <see cref="FreePortfolioValue"/>
        /// based on the <see cref="SecurityPortfolioManager.TotalPortfolioValue"/>
        /// </summary>
        public decimal FreePortfolioValuePercentage { get; set; }

        /// <summary>
        /// Gets/sets if Liquidate() is enabled
        /// </summary>
        public bool LiquidateEnabled { get; set; }

        /// <summary>
        /// Gets/sets the minimum time span elapsed to consider a market fill price as stale (defaults to one hour)
        /// </summary>
        /// <remarks>
        /// In the default fill models, a warning message will be added to market order fills
        /// if this time span (or more) has elapsed since the price was last updated.
        /// </remarks>
        /// <seealso cref="FillModel"/>
        /// <seealso cref="ImmediateFillModel"/>
        public TimeSpan StalePriceTimeSpan { get; set; }

        /// <summary>
        /// The warmup resolution to use if any
        /// </summary>
        /// <remarks>This allows improving the warmup speed by setting it to a lower resolution than the one added in the algorithm</remarks>
        public Resolution? WarmupResolution { get; set; }

        /// <summary>
        /// The warmup resolution to use if any
        /// </summary>
        /// <remarks>This allows improving the warmup speed by setting it to a lower resolution than the one added in the algorithm.
        /// Pass through version to be user friendly</remarks>
        public Resolution? WarmUpResolution
        {
            get
            {
                return WarmupResolution;
            }
            set
            {
                WarmupResolution = value;
            }
        }

        /// <summary>
        /// Number of trading days per year for this Algorithm's portfolio statistics.
        /// </summary>
        /// <remarks>Effect on
        /// <see cref="Statistics.PortfolioStatistics.AnnualVariance"/>,
        /// <seealso cref="Statistics.PortfolioStatistics.AnnualStandardDeviation"/>,
        /// <seealso cref="Statistics.PortfolioStatistics.SharpeRatio"/>,
        /// <seealso cref="Statistics.PortfolioStatistics.SortinoRatio"/>,
        /// <seealso cref="Statistics.PortfolioStatistics.TrackingError"/>,
        /// <seealso cref="Statistics.PortfolioStatistics.InformationRatio"/>.
        /// </remarks>
        public int? TradingDaysPerYear { get; set; }

        /// <summary>
        /// True if daily strict end times are enabled
        /// </summary>
        public bool DailyPreciseEndTime { get; set; }

        /// <summary>
        /// True if extended market hours should be used for daily consolidation, when extended market hours is enabled
        /// </summary>
        public bool DailyConsolidationUseExtendedMarketHours { get; set; }

        /// <summary>
        /// Gets the time span used to refresh the market hours and symbol properties databases
        /// </summary>
        public TimeSpan DatabasesRefreshPeriod { get; set; }

        /// <summary>
        /// Determines whether to terminate the algorithm when an asset holding is not supported by Lean or the brokerage.
        /// Defaults to true, meaning that the algorithm will not be terminated if an asset holding is not supported.
        /// </summary>
        public bool IgnoreUnknownAssetHoldings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmSettings"/> class
        /// </summary>
        public AlgorithmSettings()
        {
            LiquidateEnabled = true;
            DailyPreciseEndTime = true;
            FreePortfolioValuePercentage = 0.0025m;
            // Because the free portfolio value has a trailing behavior by default, let's add a default minimum order margin portfolio percentage
            // to avoid tiny trades when rebalancing, defaulting to 0.1% of the TPV
            MinimumOrderMarginPortfolioPercentage = 0.001m;
            StalePriceTimeSpan = Time.OneHour;
            MaxAbsolutePortfolioTargetPercentage = 1000000000;
            MinAbsolutePortfolioTargetPercentage = 0.0000000001m;
            DatabasesRefreshPeriod = _defaultDatabasesRefreshPeriod;
            IgnoreUnknownAssetHoldings = _defaultIgnoreUnknownAssetHoldings;
        }
    }
}
