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

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models Binance Coin Futures order fees
    /// </summary>
    public class BinanceCoinFuturesFeeModel : BinanceFeeModel
    {
        /// <summary>
        /// Tier 1 maker fees
        /// https://www.binance.com/en/fee/deliveryFee
        /// </summary>
        public new const decimal MakerTier1Fee = 0.0001m;

        /// <summary>
        /// Tier 1 taker fees
        /// https://www.binance.com/en/fee/deliveryFee
        /// </summary>
        public new const decimal TakerTier1Fee = 0.0005m;

        /// <summary>
        /// Creates Binance Coin Futures fee model setting fees values
        /// </summary>
        /// <param name="mFee">Maker fee value</param>
        /// <param name="tFee">Taker fee value</param>
        public BinanceCoinFuturesFeeModel(
            decimal mFee = MakerTier1Fee,
            decimal tFee = TakerTier1Fee
        )
            : base(mFee, tFee) { }
    }
}
