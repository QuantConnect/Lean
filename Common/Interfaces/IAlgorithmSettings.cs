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
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{
    /// <summary>
    ///  User settings for the algorithm which can be changed in the <see cref="IAlgorithm.Initialize"/> method
    /// </summary>
    public interface IAlgorithmSettings
    {
        /// <summary>
        /// The absolute maximum valid total portfolio value target percentage
        /// </summary>
        /// <remarks>This setting is currently being used to filter out undesired target percent values,
        /// caused by the <see cref="IPortfolioConstructionModel"/> implementation being used.
        /// For example rounding errors, math operations</remarks>
        decimal MaxAbsolutePortfolioTargetPercentage { get; set; }

        /// <summary>
        /// The absolute minimum valid total portfolio value target percentage
        /// </summary>
        /// <remarks>This setting is currently being used to filter out undesired target percent values,
        /// caused by the <see cref="IPortfolioConstructionModel"/> implementation being used.
        /// For example rounding errors, math operations</remarks>
        decimal MinAbsolutePortfolioTargetPercentage { get; set; }

        /// <summary>
        /// Gets/sets the SetHoldings buffers value.
        /// The buffer is used for orders not to be rejected due to volatility when using SetHoldings and CalculateOrderQuantity
        /// </summary>
        decimal FreePortfolioValue { get; set; }

        /// <summary>
        /// Gets/sets the SetHoldings buffers value percentage.
        /// This percentage will be used to set the <see cref="FreePortfolioValue"/>
        /// based on the <see cref="SecurityPortfolioManager.TotalPortfolioValue"/>
        /// </summary>
        decimal FreePortfolioValuePercentage { get; set; }

        /// <summary>
        /// Gets/sets if Liquidate() is enabled
        /// </summary>
        bool LiquidateEnabled { get; set; }

        /// <summary>
        /// Gets/sets the maximum number of concurrent market data subscriptions available
        /// </summary>
        /// <remarks>
        /// All securities added with <see cref="IAlgorithm.AddSecurity"/> are counted as one,
        /// with the exception of options and futures where every single contract in a chain counts as one.
        /// </remarks>
        int DataSubscriptionLimit { get; set; }

        /// <summary>
        /// Gets the minimum time span elapsed to consider a market fill price as stale (defaults to one hour)
        /// </summary>
        TimeSpan StalePriceTimeSpan { get; set; }
    }
}
