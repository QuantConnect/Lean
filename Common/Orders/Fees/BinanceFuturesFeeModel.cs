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

using QuantConnect.Util;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models Binance Futures order fees
    /// </summary>
    public class BinanceFuturesFeeModel : BinanceFeeModel
    {
        /// <summary>
        /// Tier 1 USDT maker fees
        /// https://www.binance.com/en/fee/futureFee
        /// </summary>
        public const decimal MakerTier1USDTFee = 0.0002m;

        /// <summary>
        /// Tier 1 USDT taker fees
        /// https://www.binance.com/en/fee/futureFee
        /// </summary>
        public const decimal TakerTier1USDTFee = 0.0004m;

        /// <summary>
        /// Tier 1 BUSD maker fees
        /// https://www.binance.com/en/fee/futureFee
        /// </summary>
        public const decimal MakerTier1BUSDFee = 0.00012m;

        /// <summary>
        /// Tier 1 BUSD taker fees
        /// https://www.binance.com/en/fee/futureFee
        /// </summary>
        public const decimal TakerTier1BUSDFee = 0.00036m;

        private decimal _makerUsdtFee;
        private decimal _takerUsdtFee;
        private decimal _makerBusdFee;
        private decimal _takerBusdFee;

        /// <summary>
        /// Creates Binance Futures fee model setting fees values
        /// </summary>
        /// <param name="mUsdtFee">Maker fee value for USDT pair contracts</param>
        /// <param name="tUsdtFee">Taker fee value for USDT pair contracts</param>
        /// <param name="mBusdFee">Maker fee value for BUSD pair contracts</param>
        /// <param name="tBusdFee">Taker fee value for BUSD pair contracts</param>
        public BinanceFuturesFeeModel(
            decimal mUsdtFee = MakerTier1USDTFee,
            decimal tUsdtFee = TakerTier1USDTFee,
            decimal mBusdFee = MakerTier1BUSDFee,
            decimal tBusdFee = TakerTier1BUSDFee
        )
            : base(-1, -1)
        {
            _makerUsdtFee = mUsdtFee;
            _takerUsdtFee = tUsdtFee;
            _makerBusdFee = mBusdFee;
            _takerBusdFee = tBusdFee;
        }

        protected override decimal GetFee(Order order)
        {
            CurrencyPairUtil.DecomposeCurrencyPair(order.Symbol, out var _, out var quoteCurrency);
            var makerFee = _makerUsdtFee;
            var takerFee = _takerUsdtFee;
            if (quoteCurrency == "BUSD")
            {
                makerFee = _makerBusdFee;
                takerFee = _takerBusdFee;
            }

            return GetFee(order, makerFee, takerFee);
        }
    }
}
