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
        private readonly decimal _forexCommissionRate;
        private readonly decimal _forexMinimumOrderFee;

        // option commission function takes number of contracts and the size of the option premium and returns total commission
        private readonly Dictionary<string, Func<decimal, decimal, CashAmount>> _optionFee =
            new Dictionary<string, Func<decimal, decimal, CashAmount>>();

        #pragma warning disable CS1570
        /// <summary>
        /// Reference at https://www.interactivebrokers.com/en/index.php?f=commission&p=futures1
        /// </summary>
        #pragma warning restore CS1570
        private readonly Dictionary<string, Func<Security, CashAmount>> _futureFee =
            //                                                               IB fee + exchange fee
            new()
            {
                { Market.USA, UnitedStatesFutureFees },
                { Market.HKFE, HongKongFutureFees }
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmediateFillModel"/>
        /// </summary>
        /// <param name="monthlyForexTradeAmountInUSDollars">Monthly FX dollar volume traded</param>
        /// <param name="monthlyOptionsTradeAmountInContracts">Monthly options contracts traded</param>
        public InteractiveBrokersFeeModel(decimal monthlyForexTradeAmountInUSDollars = 0, decimal monthlyOptionsTradeAmountInContracts = 0)
        {
            ProcessForexRateSchedule(monthlyForexTradeAmountInUSDollars, out _forexCommissionRate, out _forexMinimumOrderFee);
            Func<decimal, decimal, CashAmount> optionsCommissionFunc;
            ProcessOptionsRateSchedule(monthlyOptionsTradeAmountInContracts, out optionsCommissionFunc);
            // only USA for now
            _optionFee.Add(Market.USA, optionsCommissionFunc);
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
                    // get the total order value in the account currency
                    var totalOrderValue = order.GetValue(security);
                    var fee = Math.Abs(_forexCommissionRate*totalOrderValue);
                    feeResult = Math.Max(_forexMinimumOrderFee, fee);
                    // IB Forex fees are all in USD
                    feeCurrency = Currencies.USD;
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    Func<decimal, decimal, CashAmount> optionsCommissionFunc;
                    if (!_optionFee.TryGetValue(market, out optionsCommissionFunc))
                    {
                        throw new KeyNotFoundException(Messages.InteractiveBrokersFeeModel.UnexpectedOptionMarket(market));
                    }
                    // applying commission function to the order
                    var optionFee = optionsCommissionFunc(quantity, GetPotentialOrderPrice(order, security));
                    feeResult = optionFee.Amount;
                    feeCurrency = optionFee.Currency;
                    break;

                case SecurityType.Future:
                case SecurityType.FutureOption:
                    // The futures options fee model is exactly the same as futures' fees on IB.
                    if (market == Market.Globex || market == Market.NYMEX
                        || market == Market.CBOT || market == Market.ICE
                        || market == Market.CFE || market == Market.COMEX
                        || market == Market.CME || market == Market.NYSELIFFE)
                    {
                        // just in case...
                        market = Market.USA;
                    }

                    if (!_futureFee.TryGetValue(market, out var feeRatePerContractFunc))
                    {
                        throw new KeyNotFoundException(Messages.InteractiveBrokersFeeModel.UnexpectedFutureMarket(market));
                    }

                    var feeRatePerContract = feeRatePerContractFunc(security);
                    feeResult = quantity * feeRatePerContract.Amount;
                    feeCurrency = feeRatePerContract.Currency;
                    break;

                case SecurityType.Equity:
                    EquityFee equityFee;
                    switch (market)
                    {
                        case Market.USA:
                            equityFee = new EquityFee(Currencies.USD, feePerShare: 0.005m, minimumFee: 1, maximumFeeRate: 0.005m);
                            break;
                        case Market.India:
                            equityFee = new EquityFee(Currencies.INR, feePerShare: 0.01m, minimumFee: 6, maximumFeeRate: 20);
                            break;
                        default:
                            throw new KeyNotFoundException(Messages.InteractiveBrokersFeeModel.UnexpectedEquityMarket(market));
                    }
                    var tradeValue = Math.Abs(order.GetValue(security));

                    //Per share fees
                    var tradeFee = equityFee.FeePerShare * quantity;

                    //Maximum Per Order: equityFee.MaximumFeeRate
                    //Minimum per order. $equityFee.MinimumFee
                    var maximumPerOrder = equityFee.MaximumFeeRate * tradeValue;
                    if (tradeFee < equityFee.MinimumFee)
                    {
                        tradeFee = equityFee.MinimumFee;
                    }
                    else if (tradeFee > maximumPerOrder)
                    {
                        tradeFee = maximumPerOrder;
                    }

                    feeCurrency = equityFee.Currency;
                    //Always return a positive fee.
                    feeResult = Math.Abs(tradeFee);
                    break;

                case SecurityType.Cfd:
                    var value = Math.Abs(order.GetValue(security));
                    feeResult = 0.00002m * value; // 0.002%
                    feeCurrency = security.QuoteCurrency.Symbol;

                    var minimumFee = security.QuoteCurrency.Symbol switch
                    {
                        "JPY" => 40.0m,
                        "HKD" => 10.0m,
                        _ => 1.0m
                    };
                    feeResult = Math.Max(feeResult, minimumFee);
                    break;

                default:
                    // unsupported security type
                    throw new ArgumentException(Messages.FeeModel.UnsupportedSecurityType(security));
            }

            return new OrderFee(new CashAmount(
                feeResult,
                feeCurrency));
        }

        /// <summary>
        /// Approximates the order's price based on the order type
        /// </summary>
        protected static decimal GetPotentialOrderPrice(Order order, Security security)
        {
            decimal price = 0;
            switch (order.Type)
            {
                case OrderType.TrailingStop:
                    price = (order as TrailingStopOrder).StopPrice;
                    break;
                case OrderType.StopMarket:
                    price = (order as StopMarketOrder).StopPrice;
                    break;
                case OrderType.ComboMarket:
                case OrderType.MarketOnOpen:
                case OrderType.MarketOnClose:
                case OrderType.Market:
                    decimal securityPrice;
                    if (order.Direction == OrderDirection.Buy)
                    {
                        price = security.BidPrice;
                    }
                    else
                    {
                        price = security.AskPrice;
                    }
                    break;
                case OrderType.ComboLimit:
                    price = (order as ComboLimitOrder).GroupOrderManager.LimitPrice;
                    break;
                case OrderType.ComboLegLimit:
                    price = (order as ComboLegLimitOrder).LimitPrice;
                    break;
                case OrderType.StopLimit:
                    price = (order as StopLimitOrder).LimitPrice;
                    break;
                case OrderType.LimitIfTouched:
                    price = (order as LimitIfTouchedOrder).LimitPrice;
                    break;
                case OrderType.Limit:
                    price = (order as LimitOrder).LimitPrice;
                    break;
            }

            return price;
        }

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume
        /// </summary>
        private static void ProcessForexRateSchedule(decimal monthlyForexTradeAmountInUSDollars, out decimal commissionRate, out decimal minimumOrderFee)
        {
            const decimal bp = 0.0001m;
            if (monthlyForexTradeAmountInUSDollars <= 1000000000)      // 1 billion
            {
                commissionRate = 0.20m * bp;
                minimumOrderFee = 2.00m;
            }
            else if (monthlyForexTradeAmountInUSDollars <= 2000000000) // 2 billion
            {
                commissionRate = 0.15m * bp;
                minimumOrderFee = 1.50m;
            }
            else if (monthlyForexTradeAmountInUSDollars <= 5000000000) // 5 billion
            {
                commissionRate = 0.10m * bp;
                minimumOrderFee = 1.25m;
            }
            else
            {
                commissionRate = 0.08m * bp;
                minimumOrderFee = 1.00m;
            }
        }

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume
        /// </summary>
        private static void ProcessOptionsRateSchedule(decimal monthlyOptionsTradeAmountInContracts, out Func<decimal, decimal, CashAmount> optionsCommissionFunc)
        {
            if (monthlyOptionsTradeAmountInContracts <= 10000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.1m ?
                                            0.65m :
                                            (0.05m <= premium && premium < 0.1m ? 0.5m : 0.25m);
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
            else if (monthlyOptionsTradeAmountInContracts <= 50000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.05m ? 0.5m : 0.25m;
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
            else if (monthlyOptionsTradeAmountInContracts <= 100000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = 0.25m;
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
            else
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = 0.15m;
                    return new CashAmount(Math.Max(orderSize * commissionRate, 1.0m), Currencies.USD);
                };
            }
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
                    exchangeFees = _usaFuturesExchangeFees;
                    symbol = security.Symbol.ID.Symbol;
                    break;
                case SecurityType.FutureOption:
                    fees = _usaFutureOptionsFees;
                    exchangeFees = _usaFutureOptionsExchangeFees;
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
        /// See https://www.hkex.com.hk/Services/Rules-and-Forms-and-Fees/Fees/Listed-Derivatives/Trading/Transaction?sc_lang=en
        /// </summary>
        private static CashAmount HongKongFutureFees(Security security)
        {
            if (security.Symbol.ID.Symbol.Equals("HSI", StringComparison.InvariantCultureIgnoreCase))
            {
                // IB fee + exchange fee
                return new CashAmount(30 + 10, Currencies.HKD);
            }

            decimal ibFeePerContract;
            switch (security.QuoteCurrency.Symbol)
            {
                case Currencies.CNH:
                    ibFeePerContract = 13;
                    break;
                case Currencies.HKD:
                    ibFeePerContract = 20;
                    break;
                case Currencies.USD:
                    ibFeePerContract = 2.40m;
                    break;
                default:
                    throw new ArgumentException(Messages.InteractiveBrokersFeeModel.HongKongFutureFeesUnexpectedQuoteCurrency(security));
            }

            // let's add a 50% extra charge for exchange fees
            return new CashAmount(ibFeePerContract * 1.5m, security.QuoteCurrency.Symbol);
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

        private static readonly Dictionary<string, decimal> _usaFuturesExchangeFees = new()
        {
            // E-mini Futures
            { "ES", 1.28m }, { "NQ", 1.28m }, { "YM", 1.28m }, { "RTY", 1.28m }, { "EMD", 1.28m },
            // Micro E-mini Futures
            { "MYM", 0.30m }, { "M2K", 0.30m }, { "MES", 0.30m }, { "MNQ", 0.30m }, { "2YY", 0.30m }, { "5YY", 0.30m }, { "10Y", 0.30m },
            { "30Y", 0.30m }, { "MCL", 0.30m }, { "MGC", 0.30m }, { "SIL", 0.30m },
            // Cryptocurrency Futures
            { "BTC", 6m }, { "MBT", 2.5m }, { "ETH", 4m }, { "MET", 0.20m },
            // E-mini FX (currencies) Futures
            { "E7", 0.85m }, { "J7", 0.85m },
            // Micro E-mini FX (currencies) Futures
            { "M6E", 0.24m }, { "M6A", 0.24m }, { "M6B", 0.24m }, { "MCD", 0.24m }, { "MJY", 0.24m }, { "MSF", 0.24m }, { "M6J", 0.24m },
            { "MIR", 0.24m }, { "M6C", 0.24m }, { "M6S", 0.24m }, { "MNH", 0.24m },
        };

        private static readonly Dictionary<string, decimal> _usaFutureOptionsExchangeFees = new()
        {
            // E-mini Future Options
            { "ES", 0.55m }, { "NQ", 0.55m }, { "YM", 0.55m }, { "RTY", 0.55m }, { "EMD", 0.55m },
            // Micro E-mini Future Options
            { "MYM", 0.20m }, { "M2K", 0.20m }, { "MES", 0.20m }, { "MNQ", 0.20m }, { "2YY", 0.20m }, { "5YY", 0.20m }, { "10Y", 0.20m },
            { "30Y", 0.20m }, { "MCL", 0.20m }, { "MGC", 0.20m }, { "SIL", 0.20m },
            // Cryptocurrency Future Options
            { "BTC", 5m }, { "MBT", 2.5m }, { "ETH", 4m }, { "MET", 0.20m },
        };

        /// <summary>
        /// Helper class to handle IB Equity fees
        /// </summary>
        private class EquityFee
        {
            public string Currency { get; }
            public decimal FeePerShare { get; }
            public decimal MinimumFee { get; }
            public decimal MaximumFeeRate { get; }

            public EquityFee(string currency,
                decimal feePerShare,
                decimal minimumFee,
                decimal maximumFeeRate)
            {
                Currency = currency;
                FeePerShare = feePerShare;
                MinimumFee = minimumFee;
                MaximumFeeRate = maximumFeeRate;
            }
        }
    }
}
