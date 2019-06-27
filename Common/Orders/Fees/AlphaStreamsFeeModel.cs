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
using System.Collections.Generic;
using QLNet;
using QuantConnect.Data.Fundamental;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    public class AlphaStreamsFeeModel : FeeModel
    {

        private readonly decimal _forexCommissionRate = 0.000002m;
        private decimal _liborRate;

        private readonly Dictionary<string, EquityFee> _equityFee =
            new Dictionary<string, EquityFee>();

        private readonly Dictionary<string, CashAmount> _futureFee =
            //                                                               Commission plus clearing fee
            new Dictionary<string, CashAmount> { { Market.USA, new CashAmount(0.4m + 0.1m, "USD") } };

        private readonly Dictionary<string, CashAmount> _optionFee =
            //                                                               Commission plus clearing fee
            new Dictionary<string, CashAmount> { { Market.USA, new CashAmount(0.4m + 0.1m, "USD") } };

        public AlphaStreamsFeeModel(decimal liborRate = 0.024m)
        {
            _liborRate = Math.Abs(liborRate);
            _equityFee.Add(Market.USA, new EquityFee("USD", 0.004m + _liborRate, 0));
        }
        /// <summary>
        /// Gets the order fee associated with the specified order. This returns the cost
        /// of the transaction in the account currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            // Option exercise for equity options is free of charge
            if (order.Type == OrderType.OptionExercise)
            {
                var optionOrder = (OptionExerciseOrder)order;

                if (optionOrder.Symbol.ID.SecurityType == SecurityType.Option &&
                    optionOrder.Symbol.ID.Underlying.SecurityType == SecurityType.Equity)
                {
                    return OrderFee.Zero;
                }
            }

            decimal feeResult;
            string feeCurrency;
            var market = security.Symbol.ID.Market;
            switch (security.Type)
            {
                case SecurityType.Forex:
                    // get the total order value in the account currency
                    var totalOrderValue = order.GetValue(security);
                    var fee = Math.Abs(_forexCommissionRate * totalOrderValue);
                    feeResult = Math.Max(0, fee);
                    // Forex fees are all in USD
                    feeCurrency = Currencies.USD;
                    break;

                case SecurityType.Option:
                    CashAmount optionsFeeRatePerContract;
                    if (!_optionFee.TryGetValue(market, out optionsFeeRatePerContract))
                    {
                        throw new Exception($"AlphaStreamsFeeModel(): unexpected future Market {market}");
                    }
                    feeResult = order.AbsoluteQuantity * optionsFeeRatePerContract.Amount;
                    feeCurrency = optionsFeeRatePerContract.Currency;
                    break;

                case SecurityType.Future:
                    if (market == Market.Globex || market == Market.NYMEX
                        || market == Market.CBOT || market == Market.ICE
                        || market == Market.CBOE || market == Market.NSE)
                    {
                        // just in case...
                        market = Market.USA;
                    }

                    CashAmount futuresFeeRatePerContract;
                    if (!_futureFee.TryGetValue(market, out futuresFeeRatePerContract))
                    {
                        throw new Exception($"AlphaStreamsFeeModel(): unexpected future Market {market}");
                    }
                    feeResult = order.AbsoluteQuantity * futuresFeeRatePerContract.Amount;
                    feeCurrency = futuresFeeRatePerContract.Currency;
                    break;

                case SecurityType.Equity:
                    EquityFee equityFee;
                    if (!_equityFee.TryGetValue(market, out equityFee))
                    {
                        throw new Exception($"AlphaStreamsFeeModel(): unexpected equity Market {market}");
                    }
                    
                    //Per trade notional value fees
                    var tradeFee = equityFee.FeePerTrade * order.GetValue(security);

                    if (tradeFee < equityFee.MinimumFee)
                    {
                        tradeFee = equityFee.MinimumFee;
                    }
                    
                    feeCurrency = equityFee.Currency;
                    //Always return a positive fee.
                    feeResult = Math.Abs(tradeFee);
                    break;

                default:
                    // unsupported security type
                    throw new ArgumentException($"Unsupported security type: {security.Type}");
            }

            return new OrderFee(new CashAmount(
                feeResult,
                feeCurrency));
        }

        /// <summary>
        /// Helper class to handle IB Equity fees
        /// </summary>
        private class EquityFee
        {
            public string Currency { get; }
            public decimal FeePerTrade { get; }
            public decimal MinimumFee { get; }
            public decimal LiborRate { get; }

            public EquityFee(string currency,
                decimal feePerTrade,
                decimal minimumFee)
            {
                Currency = currency;
                FeePerTrade = feePerTrade;
                MinimumFee = minimumFee;
            }
        }
    }
}
