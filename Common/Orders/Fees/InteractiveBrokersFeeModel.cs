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
using QuantConnect.Orders.Fills;
using System.Collections.Generic;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the default implementation of <see cref="IFeeModel"/>
    /// </summary>
    public class InteractiveBrokersFeeModel : FeeModel
    {
        private const decimal CryptoMinimumOrderFee = 1.75m;
        private readonly decimal _forexCommissionRate;
        private readonly decimal _forexMinimumOrderFee;
        private readonly decimal _cryptoCommissionRate;

        // option commission function takes number of contracts and the size of the option premium and returns total commission
        private readonly Dictionary<string, Func<decimal, decimal, CashAmount>> _optionFee =
            new Dictionary<string, Func<decimal, decimal, CashAmount>>();

        #pragma warning disable CS1570
        /// <summary>
        /// Reference at https://www.interactivebrokers.com/en/index.php?f=commission&p=futures1
        /// </summary>
        #pragma warning restore CS1570
        private readonly Dictionary<string, Func<Security, CashAmount>> _futureFee =
            // IB fee + exchange fee
            new()
            {
                { Market.USA, UnitedStatesFutureFees },
                { Market.HKFE, InteractiveBrokersFeeHelper.HongKongFutureFees },
                { Market.EUREX, InteractiveBrokersFeeHelper.EUREXFutureFees }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateFillModel"/>
        /// </summary>
        /// <param name="monthlyForexTradeAmountInUSDollars">Monthly FX dollar volume traded</param>
        /// <param name="monthlyOptionsTradeAmountInContracts">Monthly options contracts traded</param>
        /// <param name="monthlyCryptoTradeAmountInUSDollars">Monthly Crypto dollar volume traded (in USD)</param>
        public InteractiveBrokersFeeModel(decimal monthlyForexTradeAmountInUSDollars = 0, decimal monthlyOptionsTradeAmountInContracts = 0, decimal monthlyCryptoTradeAmountInUSDollars = 0)
        {
            InteractiveBrokersFeeHelper.ProcessForexRateSchedule(monthlyForexTradeAmountInUSDollars, out _forexCommissionRate, out _forexMinimumOrderFee);
            Func<decimal, decimal, CashAmount> optionsCommissionFunc;
            InteractiveBrokersFeeHelper.ProcessOptionsRateSchedule(monthlyOptionsTradeAmountInContracts, out optionsCommissionFunc);
            // only USA for now
            _optionFee.Add(Market.USA, optionsCommissionFunc);
            InteractiveBrokersFeeHelper.ProcessCryptoRateSchedule(monthlyCryptoTradeAmountInUSDollars, out _cryptoCommissionRate);
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

                // For Futures Options, contracts are charged the standard commission at expiration of the contract.
                // Read more here: https://www1.interactivebrokers.com/en/index.php?f=14718#trading-related-fees
                if (optionOrder.Symbol.ID.SecurityType == SecurityType.Option)
                {
                    return OrderFee.Zero;
                }
            }

            var quantity = order.AbsoluteQuantity;
            decimal feeResult;
            string feeCurrency;
            var market = security.Symbol.ID.Market;
            switch (security.Type)
            {
                case SecurityType.Forex:
                    InteractiveBrokersFeeHelper.CalculateForexFee(security, order, _forexCommissionRate, _forexMinimumOrderFee, out feeResult, out feeCurrency);
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    InteractiveBrokersFeeHelper.CalculateOptionFee(security, order, quantity, market, _optionFee, out feeResult, out feeCurrency);
                    break;

                case SecurityType.Future:
                case SecurityType.FutureOption:
                    InteractiveBrokersFeeHelper.CalculateFutureFopFee(security, quantity, market, _futureFee, out feeResult, out feeCurrency);
                    break;

                case SecurityType.Equity:
                    var tradeValue = Math.Abs(order.GetValue(security));
                    InteractiveBrokersFeeHelper.CalculateEquityFee(quantity, tradeValue, market, 0.005m, 1m, out feeResult, out feeCurrency);
                    break;

                case SecurityType.Cfd:
                    InteractiveBrokersFeeHelper.CalculateCfdFee(security, order, out feeResult, out feeCurrency);
                    break;
                    
                case SecurityType.Crypto:
                    InteractiveBrokersFeeHelper.CalculateCryptoFee(security, order, _cryptoCommissionRate, CryptoMinimumOrderFee, out feeResult, out feeCurrency);
                    break;

                default:
                    // unsupported security type
                    throw new ArgumentException(Messages.FeeModel.UnsupportedSecurityType(security));
            }

            return new OrderFee(new CashAmount(
                feeResult,
                feeCurrency));
        }

        private static CashAmount UnitedStatesFutureFees(Security security)
        {
            IDictionary<string, decimal> fees, exchangeFees;
            decimal ibFeePerContract, exchangeFeePerContract;
            string symbol;

            switch (security.Symbol.SecurityType)
            {
                case SecurityType.Future:
                    fees = _usaFuturesFees;
                    exchangeFees = InteractiveBrokersFeeHelper.UsaFuturesExchangeFees;
                    symbol = security.Symbol.ID.Symbol;
                    break;
                case SecurityType.FutureOption:
                    fees = _usaFutureOptionsFees;
                    exchangeFees = InteractiveBrokersFeeHelper.UsaFutureOptionsExchangeFees;
                    symbol = security.Symbol.Underlying.ID.Symbol;
                    break;
                default:
                    throw new ArgumentException(Messages.InteractiveBrokersFeeModel.UnitedStatesFutureFeesUnsupportedSecurityType(security));
            }

            if (!fees.TryGetValue(symbol, out ibFeePerContract))
            {
                ibFeePerContract = 0.85m;
            }

            if (!exchangeFees.TryGetValue(symbol, out exchangeFeePerContract))
            {
                exchangeFeePerContract = 1.60m;
            }

            // Add exchange fees + IBKR regulatory fee (0.02)
            return new CashAmount(ibFeePerContract + exchangeFeePerContract + 0.02m, Currencies.USD);
        }

        /// <summary>
        /// Reference at https://www.interactivebrokers.com/en/pricing/commissions-futures.php?re=amer
        /// </summary>
        private static readonly Dictionary<string, decimal> _usaFuturesFees = new()
        {
            // Micro E-mini Futures
            { "MYM", 0.25m }, { "M2K", 0.25m }, { "MES", 0.25m }, { "MNQ", 0.25m }, { "2YY", 0.25m }, { "5YY", 0.25m }, { "10Y", 0.25m },
            { "30Y", 0.25m }, { "MCL", 0.25m }, { "MGC", 0.25m }, { "SIL", 0.25m },
            // Cryptocurrency Futures
            { "BTC", 5m }, { "MBT", 2.25m }, { "ETH", 3m }, { "MET", 0.20m },
            // E-mini FX (currencies) Futures
            { "E7", 0.50m }, { "J7", 0.50m },
            // Micro E-mini FX (currencies) Futures
            { "M6E", 0.15m }, { "M6A", 0.15m }, { "M6B", 0.15m }, { "MCD", 0.15m }, { "MJY", 0.15m }, { "MSF", 0.15m }, { "M6J", 0.15m },
            { "MIR", 0.15m }, { "M6C", 0.15m }, { "M6S", 0.15m }, { "MNH", 0.15m },
        };

        private static readonly Dictionary<string, decimal> _usaFutureOptionsFees = new()
        {
            // Micro E-mini Future Options
            { "MYM", 0.25m }, { "M2K", 0.25m }, { "MES", 0.25m }, { "MNQ", 0.25m }, { "2YY", 0.25m }, { "5YY", 0.25m }, { "10Y", 0.25m },
            { "30Y", 0.25m }, { "MCL", 0.25m }, { "MGC", 0.25m }, { "SIL", 0.25m },
            // Cryptocurrency Future Options
            { "BTC", 5m }, { "MBT", 1.25m }, { "ETH", 3m }, { "MET", 0.10m },
        };
    }
}
