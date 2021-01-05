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

using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders.Slippage
{
    /// <summary>
    /// Represents a slippage model that is calculated by multiplying the price impact constant
    /// by the square of the ratio of the order to the total volume.
    /// </summary>
    public class VolumeShareSlippageModel : ISlippageModel
    {
        private readonly decimal _priceImpact;
        private readonly decimal _volumeLimit;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeShareSlippageModel"/> class
        /// </summary>
        /// <param name="volumeLimit"></param>
        /// <param name="priceImpact">Defines how large of an impact the order will have on the price calculation</param>
        public VolumeShareSlippageModel(decimal volumeLimit = 0.025m, decimal priceImpact = 0.1m)
        {
            _priceImpact = priceImpact;
            _volumeLimit = volumeLimit;
        }

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            var lastData = asset.GetLastData();
            if (lastData == null) return 0;

            var barVolume = 0m;
            var slippagePercent = _volumeLimit * _volumeLimit * _priceImpact;

            switch (lastData.DataType)
            {
                case MarketDataType.TradeBar:
                    barVolume = ((TradeBar)lastData).Volume;
                    break;
                case MarketDataType.QuoteBar:
                    barVolume = order.Direction == OrderDirection.Buy
                        ? ((QuoteBar)lastData).LastBidSize
                        : ((QuoteBar)lastData).LastAskSize;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"VolumeShareSlippageModel.GetSlippageApproximation: Cannot use this model with market data type {lastData.GetType()}"
                    );
            }

            // If volume is zero or negative, we use the maximum slippage percentage since the impact of any quantity is infinite
            // In FX/CFD case, we issue a warning and return zero slippage
            if (barVolume <= 0)
            {
                var securityType = asset.Symbol.ID.SecurityType;
                if (securityType == SecurityType.Cfd || securityType == SecurityType.Forex || securityType == SecurityType.Crypto)
                {
                    Log.Error(Invariant($"VolumeShareSlippageModel.GetSlippageApproximation: {securityType} security type often ") +
                        "does not report volume. If you intend to model slippage beyond the spread, please consider another model."
                    );
                    return 0;
                }

                Log.Error("VolumeShareSlippageModel.GetSlippageApproximation: Bar volume cannot be zero or negative. " +
                    Invariant($"Volume: {barVolume}. Using maximum slippage percentage of {slippagePercent}")
                );
            }
            else
            {
                // Ratio of the order to the total volume
                var volumeShare = Math.Min(order.AbsoluteQuantity / barVolume, _volumeLimit);

                slippagePercent = volumeShare * volumeShare * _priceImpact;
            }

            return slippagePercent * lastData.Value;
        }
    }
}