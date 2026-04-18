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

using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Webull.
    /// </summary>
    /// <see href="https://www.webull.com/pricing"/>
    /// <remarks>
    /// Equity and standard options trades are commission-free on Webull.
    /// Index options carry a flat $0.50 Webull contract fee plus a variable exchange proprietary fee
    /// that depends on the underlying index symbol and the option's market price.
    /// Cryptocurrency trades carry a 0.6% fee on the notional trade value.
    /// </remarks>
    public class WebullFeeModel : FeeModel
    {
        /// <summary>
        /// Webull contract fee applied to every index option contract, regardless of underlying.
        /// </summary>
        private const decimal _webullIndexOptionContractFee = 0.50m;

        /// <summary>
        /// Exchange proprietary fee for SPX options priced below $1.00.
        /// </summary>
        private const decimal _spxExchangeFeeBelow1 = 0.57m;

        /// <summary>
        /// Exchange proprietary fee for SPX options priced at or above $1.00.
        /// </summary>
        private const decimal _spxExchangeFeeAbove1 = 0.66m;

        /// <summary>
        /// Exchange proprietary fee for SPXW options priced below $1.00.
        /// </summary>
        private const decimal _spxwExchangeFeeBelow1 = 0.50m;

        /// <summary>
        /// Exchange proprietary fee for SPXW options priced at or above $1.00.
        /// </summary>
        private const decimal _spxwExchangeFeeAbove1 = 0.59m;

        /// <summary>
        /// VIX/VIXW exchange fee tier 1: option price at or below $0.10.
        /// </summary>
        private const decimal _vixExchangeFeeTier1 = 0.10m;

        /// <summary>
        /// VIX/VIXW exchange fee tier 2: option price between $0.11 and $0.99.
        /// </summary>
        private const decimal _vixExchangeFeeTier2 = 0.25m;

        /// <summary>
        /// VIX/VIXW exchange fee tier 3: option price between $1.00 and $1.99.
        /// </summary>
        private const decimal _vixExchangeFeeTier3 = 0.40m;

        /// <summary>
        /// VIX/VIXW exchange fee tier 4: option price at or above $2.00.
        /// </summary>
        private const decimal _vixExchangeFeeTier4 = 0.45m;

        /// <summary>
        /// XSP exchange fee for orders with fewer than 10 contracts.
        /// </summary>
        private const decimal _xspExchangeFeeSmall = 0.00m;

        /// <summary>
        /// XSP exchange fee for orders with 10 or more contracts.
        /// </summary>
        private const decimal _xspExchangeFeeLarge = 0.07m;

        /// <summary>
        /// DJX flat exchange proprietary fee per contract.
        /// </summary>
        private const decimal _djxExchangeFee = 0.18m;

        /// <summary>
        /// NDX/NDXP exchange fee for single-leg orders with premium below $25.
        /// </summary>
        private const decimal _ndxSingleLegFeeBelow25 = 0.50m;

        /// <summary>
        /// NDX/NDXP exchange fee for single-leg orders with premium at or above $25.
        /// </summary>
        private const decimal _ndxSingleLegFeeAbove25 = 0.75m;

        /// <summary>
        /// NDX/NDXP exchange fee for multi-leg orders with premium below $25.
        /// </summary>
        private const decimal _ndxMultiLegFeeBelow25 = 0.65m;

        /// <summary>
        /// NDX/NDXP exchange fee for multi-leg orders with premium at or above $25.
        /// </summary>
        private const decimal _ndxMultiLegFeeAbove25 = 0.90m;

        /// <summary>
        /// Crypto fee rate applied as a percentage of the notional trade value (0.6%).
        /// </summary>
        private const decimal _cryptoFeeRate = 0.006m;

        /// <summary>
        /// Gets the order fee for a given security and order.
        /// </summary>
        /// <param name="parameters">The parameters including the security and order details.</param>
        /// <returns>
        /// <see cref="OrderFee.Zero"/> for equity and standard options;
        /// a per-contract fee for index options;
        /// a percentage-of-notional fee for crypto.
        /// </returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            switch (parameters.Security.Type)
            {
                case SecurityType.IndexOption:
                    return new OrderFee(new CashAmount(GetIndexOptionFee(parameters), Currencies.USD));
                case SecurityType.Crypto:
                    var notional = parameters.Order.AbsoluteQuantity * parameters.Security.Price;
                    return new OrderFee(new CashAmount(notional * _cryptoFeeRate, Currencies.USD));
                default:
                    // Equity and Option are commission-free on Webull.
                    return OrderFee.Zero;
            }
        }

        /// <summary>
        /// Calculates the total per-contract fee for an index option order.
        /// The total fee = (exchange proprietary fee + Webull contract fee) × quantity.
        /// </summary>
        /// <param name="parameters">Order fee parameters containing the security and order.</param>
        /// <returns>Total fee amount in USD.</returns>
        private static decimal GetIndexOptionFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;
            var quantity = order.AbsoluteQuantity;
            var price = security.Price;
            var underlying = security.Symbol.Underlying?.Value?.ToUpperInvariant() ?? string.Empty;
            var isMultiLeg = order.Type == OrderType.ComboMarket
                || order.Type == OrderType.ComboLimit
                || order.Type == OrderType.ComboLegLimit;

            var exchangeFee = GetIndexOptionExchangeFee(underlying, price, quantity, isMultiLeg);
            return quantity * (exchangeFee + _webullIndexOptionContractFee);
        }

        /// <summary>
        /// Returns the exchange proprietary fee per contract for an index option, based on
        /// the underlying ticker, the option's current price, order quantity, and leg type.
        /// </summary>
        /// <param name="underlying">Uppercase underlying ticker (e.g. "SPX", "VIX").</param>
        /// <param name="price">Current market price of the option.</param>
        /// <param name="quantity">Absolute number of contracts in the order.</param>
        /// <param name="isMultiLeg">True when the order is a combo/multi-leg order.</param>
        /// <returns>Exchange fee per contract in USD.</returns>
        private static decimal GetIndexOptionExchangeFee(string underlying, decimal price, decimal quantity, bool isMultiLeg)
        {
            switch (underlying)
            {
                case "SPX":
                    return price < 1m ? _spxExchangeFeeBelow1 : _spxExchangeFeeAbove1;

                case "SPXW":
                    return price < 1m ? _spxwExchangeFeeBelow1 : _spxwExchangeFeeAbove1;

                case "VIX":
                case "VIXW":
                    return GetVixExchangeFee(price);

                case "XSP":
                    return quantity < 10m ? _xspExchangeFeeSmall : _xspExchangeFeeLarge;

                case "DJX":
                    return _djxExchangeFee;

                case "NDX":
                case "NDXP":
                    return GetNdxExchangeFee(price, isMultiLeg);

                default:
                    return 0m;
            }
        }

        /// <summary>
        /// Returns the VIX/VIXW exchange fee for a simple order based on the option price tier.
        /// </summary>
        private static decimal GetVixExchangeFee(decimal price)
        {
            if (price <= 0.10m)
            {
                return _vixExchangeFeeTier1;
            }

            if (price <= 0.99m)
            {
                return _vixExchangeFeeTier2;
            }

            if (price <= 1.99m)
            {
                return _vixExchangeFeeTier3;
            }

            return _vixExchangeFeeTier4;
        }

        /// <summary>
        /// Returns the NDX/NDXP exchange fee per contract based on premium tier and order leg type.
        /// </summary>
        private static decimal GetNdxExchangeFee(decimal price, bool isMultiLeg)
        {
            if (isMultiLeg)
            {
                return price < 25m ? _ndxMultiLegFeeBelow25 : _ndxMultiLegFeeAbove25;
            }

            return price < 25m ? _ndxSingleLegFeeBelow25 : _ndxSingleLegFeeAbove25;
        }
    }
}
