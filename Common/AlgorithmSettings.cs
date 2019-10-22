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
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect
{
    /// <summary>
    /// This class includes user settings for the algorithm which can be changed in the <see cref="IAlgorithm.Initialize"/> method
    /// </summary>
    public class AlgorithmSettings : IAlgorithmSettings
    {
        /// <summary>
        /// The absolute maximum valid total portfolio value target percentage
        /// </summary>
        /// <remarks>This setting is currently being used to filter out undesired target percent values,
        /// caused by the <see cref="IPortfolioConstructionModel"/> implementation being used.
        /// For example rounding errors, math operations</remarks>
        public decimal MaxAbsolutePortfolioTargetPercentage { get; set; }

        /// <summary>
        /// The absolute minimum valid total portfolio value target percentage
        /// </summary>
        /// <remarks>This setting is currently being used to filter out undesired target percent values,
        /// caused by the <see cref="IPortfolioConstructionModel"/> implementation being used.
        /// For example rounding errors, math operations</remarks>
        public decimal MinAbsolutePortfolioTargetPercentage { get; set; }

        /// <summary>
        /// Gets/sets the maximum number of concurrent market data subscriptions available
        /// </summary>
        /// <remarks>
        /// All securities added with <see cref="IAlgorithm.AddSecurity"/> are counted as one,
        /// with the exception of options and futures where every single contract in a chain counts as one.
        /// </remarks>
        public int DataSubscriptionLimit { get; set; }

        /// <summary>
        /// Gets/sets the SetHoldings buffers value.
        /// The buffer is used for orders not to be rejected due to volatility when using SetHoldings and CalculateOrderQuantity
        /// </summary>
        public decimal FreePortfolioValue { get; set; }

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
        /// Initializes a new instance of the <see cref="AlgorithmSettings"/> class
        /// </summary>
        public AlgorithmSettings()
        {
            // default is unlimited
            DataSubscriptionLimit = int.MaxValue;
            LiquidateEnabled = true;
            FreePortfolioValue = 250;
            FreePortfolioValuePercentage = 0.0025m;
            StalePriceTimeSpan = Time.OneHour;
            MaxAbsolutePortfolioTargetPercentage = 1000000000;
            MinAbsolutePortfolioTargetPercentage = 0.0000000001m;
        }
    }
}
