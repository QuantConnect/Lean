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
using System.Collections.Generic;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Helper class to supplement fee calculation in <see cref="InteractiveBrokersFeeModel"/> and <see cref="InteractiveBrokersTieredFeeModel"/> 
    /// </summary>
    internal static class InteractiveBrokersFeeHelper
    {
        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume of Equities (in shares)
        /// </summary>
        /// <remarks>https://www.interactivebrokers.com/en/pricing/commissions-stocks.php?re=amer</remarks>
        internal static void ProcessEquityRateSchedule(decimal monthlyEquityTradeVolume, out decimal commissionRate)
        {
            commissionRate = 0.0005m;
            if (monthlyEquityTradeVolume <= 300000)
            {
                commissionRate = 0.0035m;
            }
            else if (monthlyEquityTradeVolume <= 3000000)
            {
                commissionRate = 0.002m;
            }
            else if (monthlyEquityTradeVolume <= 20000000)
            {
                commissionRate = 0.0015m;
            }
            else if (monthlyEquityTradeVolume <= 100000000)
            {
                commissionRate = 0.001m;
            }
        }

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume of Futures (in contracts)
        /// </summary>
        /// <remarks>https://www.interactivebrokers.com/en/pricing/commissions-futures.php?re=amer</remarks>
        internal static void ProcessFutureRateSchedule(decimal monthlyFutureTradeVolume, out int commissionTier)
        {
            commissionTier = 3;
            if (monthlyFutureTradeVolume <= 1000)
            {
                commissionTier = 0;
            }
            else if (monthlyFutureTradeVolume <= 10000)
            {
                commissionTier = 1;
            }
            else if (monthlyFutureTradeVolume <= 20000)
            {
                commissionTier = 2;
            }
        }

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume of forex
        /// </summary>
        /// <remarks>https://www.interactivebrokers.com/en/pricing/commissions-spot-currencies.php?re=amer</remarks>
        internal static void ProcessForexRateSchedule(decimal monthlyForexTradeAmountInUSDollars, out decimal commissionRate, out decimal minimumOrderFee)
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
        /// Determines which tier an account falls into based on the monthly trading volume of options
        /// </summary>
        /// <remarks>https://www.interactivebrokers.com/en/pricing/commissions-options.php?re=amer</remarks>
        internal static void ProcessOptionsRateSchedule(decimal monthlyOptionsTradeAmountInContracts, out Func<decimal, decimal, CashAmount> optionsCommissionFunc)
        {
            if (monthlyOptionsTradeAmountInContracts <= 10000)
            {
                optionsCommissionFunc = (orderSize, premium) =>
                {
                    var commissionRate = premium >= 0.1m ? 0.65m : (0.05m <= premium && premium < 0.1m ? 0.5m : 0.25m);
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

        /// <summary>
        /// Determines which tier an account falls into based on the monthly trading volume of cryptos
        /// </summary>
        /// <remarks>https://www.interactivebrokers.com/en/pricing/commissions-cryptocurrencies.php?re=amer</remarks>
        internal static void ProcessCryptoRateSchedule(decimal monthlyCryptoTradeAmountInUSDollars, out decimal commissionRate)
        {
            if (monthlyCryptoTradeAmountInUSDollars <= 100000)
            {
                commissionRate = 0.0018m;
            }
            else if (monthlyCryptoTradeAmountInUSDollars <= 1000000)
            {
                commissionRate = 0.0015m;
            }
            else
            {
                commissionRate = 0.0012m;
            }
        }

        /// <summary>
        /// Calculate the transaction fee of a Forex order
        /// </summary>
        /// <returns>The traded value of the transaction</returns>
        internal static decimal CalculateForexFee(Security security, Order order, decimal forexCommissionRate, 
            decimal forexMinimumOrderFee, out decimal fee, out string currency)
        {
            // get the total order value in the account currency
            var totalOrderValue = Math.Abs(order.GetValue(security));
            var baseFee = forexCommissionRate*totalOrderValue;

            fee = Math.Max(forexMinimumOrderFee, baseFee);
            // IB Forex fees are all in USD
            currency = Currencies.USD;

            return totalOrderValue;
        }

        /// <summary>
        /// Calculate the transaction fee of an Option order
        /// </summary>
        /// <returns>The traded value of the transaction</returns>
        internal static decimal CalculateOptionFee(Security security, Order order, decimal quantity, string market,
            Dictionary<string, Func<decimal, decimal, CashAmount>> feeRef, out decimal fee, out string currency)
        {
            Func<decimal, decimal, CashAmount> optionsCommissionFunc;
            if (!feeRef.TryGetValue(market, out optionsCommissionFunc))
            {
                throw new KeyNotFoundException(Messages.InteractiveBrokersFeeModel.UnexpectedOptionMarket(market));
            }
            // applying commission function to the order
            var orderPrice = GetPotentialOrderPrice(order, security);
            var optionFee = optionsCommissionFunc(quantity, orderPrice);

            fee = optionFee.Amount;
            currency = optionFee.Currency;

            return orderPrice * quantity;
        }

        /// <summary>
        /// Calculate the transaction fee of a Future or FOP order
        /// </summary>
        internal static void CalculateFutureFopFee(Security security, Order order, decimal quantity, string market,
            Dictionary<string, Func<Security, CashAmount>> feeRef, out decimal fee, out string currency)
        {
            // The futures options fee model is exactly the same as futures' fees on IB.
            if (market == Market.Globex || market == Market.NYMEX
                || market == Market.CBOT || market == Market.ICE
                || market == Market.CFE || market == Market.COMEX
                || market == Market.CME || market == Market.NYSELIFFE)
            {
                // just in case...
                market = Market.USA;
            }

            if (!feeRef.TryGetValue(market, out var feeRatePerContractFunc))
            {
                throw new KeyNotFoundException(Messages.InteractiveBrokersFeeModel.UnexpectedFutureMarket(market));
            }

            var feeRatePerContract = feeRatePerContractFunc(security);
            fee = quantity * feeRatePerContract.Amount;
            currency = feeRatePerContract.Currency;
        }

        /// <summary>
        /// Calculate the transaction fee of an Equity order
        /// </summary>
        /// <returns>Commission part of the transaction cost</returns>
        internal static decimal CalculateEquityFee(Security security, Order order, decimal quantity, decimal tradeValue, string market,
            decimal usFeeRate, decimal usMinimumFee, out decimal fee, out string currency)
        {
            EquityFee equityFee;
            switch (market)
            {
                case Market.USA:
                    equityFee = new EquityFee(Currencies.USD, feePerShare: usFeeRate, minimumFee: usMinimumFee, maximumFeeRate: 0.01m);
                    break;
                case Market.India:
                    equityFee = new EquityFee(Currencies.INR, feePerShare: 0.01m, minimumFee: 6, maximumFeeRate: 20);
                    break;
                default:
                    throw new KeyNotFoundException(Messages.InteractiveBrokersFeeModel.UnexpectedEquityMarket(market));
            }

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

            currency = equityFee.Currency;
            //Always return a positive fee.
            fee = Math.Abs(tradeFee);

            return tradeFee;
        }

        /// <summary>
        /// Calculate the transaction fee of a Cfd order
        /// </summary>
        internal static void CalculateCfdFee(Security security, Order order, out decimal fee, out string currency)
        {
            var value = Math.Abs(order.GetValue(security));
            fee = 0.00002m * value; // 0.002%
            currency = security.QuoteCurrency.Symbol;

            var minimumFee = security.QuoteCurrency.Symbol switch
            {
                "JPY" => 40.0m,
                "HKD" => 10.0m,
                _ => 1.0m
            };
            fee = Math.Max(fee, minimumFee);
        }

        /// <summary>
        /// Calculate the transaction fee of a Crypto order
        /// </summary>
        /// <returns>The traded value of the transaction</returns>
        internal static decimal CalculateCryptoFee(Security security, Order order, decimal cryptoCommissionRate, 
            decimal cryptoMinimumOrderFee, out decimal fee, out string currency)
        {
            // get the total trade value in the USD
            var totalTradeValue = order.GetValue(security);
            var cryptoFee = Math.Abs(cryptoCommissionRate*totalTradeValue);
            // 1% maximum fee
            fee = Math.Max(Math.Min(totalTradeValue * 0.01m, cryptoMinimumOrderFee), cryptoFee);
            // IB Crypto fees are all in USD
            currency = Currencies.USD;

            return totalTradeValue;
        }

        /// <summary>
        /// See https://www.hkex.com.hk/Services/Rules-and-Forms-and-Fees/Fees/Listed-Derivatives/Trading/Transaction?sc_lang=en
        /// </summary>
        internal static CashAmount HongKongFutureFees(Security security)
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

        internal static CashAmount EUREXFutureFees(Security security)
        {
            IDictionary<string, decimal> fees, exchangeFees;
            decimal ibFeePerContract, exchangeFeePerContract;
            string symbol;

            switch (security.Symbol.SecurityType)
            {
                case SecurityType.Future:
                    fees = _eurexFuturesFees;
                    exchangeFees = _eurexFuturesExchangeFees;
                    symbol = security.Symbol.ID.Symbol;
                    break;
                default:
                    throw new ArgumentException(Messages.InteractiveBrokersFeeModel.EUREXFutureFeesUnsupportedSecurityType(security));
            }

            if (!fees.TryGetValue(symbol, out ibFeePerContract))
            {
                ibFeePerContract = 1.00m;
            }

            if (!exchangeFees.TryGetValue(symbol, out exchangeFeePerContract))
            {
                exchangeFeePerContract = 0.00m;
            }

            // Add exchange fees + IBKR regulatory fee (0.02)
            return new CashAmount(ibFeePerContract + exchangeFeePerContract + 0.02m, Currencies.EUR);
        }
        
        /// <summary>
        /// Get the exchange fees of an Equity trade.
        /// </summary>
        /// <remarks>Refer to https://www.interactivebrokers.com/en/pricing/commissions-stocks.php, section United States - Third Party Fees.</remarks>
        internal static decimal GetEquityExchangeFee(Order order, Exchange exchange, decimal tradeValue, decimal commission)
        {
            var pennyStock = order.Price < 1m;

            switch (order.Type, pennyStock)
            {
                case (OrderType.MarketOnOpen, true):
                    if (exchange == Exchange.AMEX)
                    {
                        return order.AbsoluteQuantity * 0.0005m;
                    }
                    else if (exchange == Exchange.ARCA)
                    {
                        return tradeValue * 0.001m;
                    }
                    else if (exchange == Exchange.BATS)
                    {
                        return order.AbsoluteQuantity * 0.00075m;
                    }
                    else if (exchange == Exchange.NYSE)
                    {
                        return tradeValue * 0.0030m + commission * 0.000175m;
                    }
                    return tradeValue * 0.0030m;

                case (OrderType.MarketOnOpen, false):
                    if (exchange == Exchange.AMEX)
                    {
                        return order.AbsoluteQuantity * 0.0005m;
                    }
                    else if (exchange == Exchange.ARCA)
                    {
                        return order.AbsoluteQuantity * 0.0015m;
                    }
                    else if (exchange == Exchange.BATS)
                    {
                        return order.AbsoluteQuantity * 0.00075m;
                    }
                    else if (exchange == Exchange.NYSE)
                    {
                        return order.AbsoluteQuantity * 0.0010m + commission * 0.000175m;
                    }
                    return order.AbsoluteQuantity * 0.0015m;
                    
                case (OrderType.MarketOnClose, true):
                    if (exchange == Exchange.AMEX)
                    {
                        return order.AbsoluteQuantity * 0.0005m;
                    }
                    else if (exchange == Exchange.ARCA)
                    {
                        return tradeValue * 0.001m;
                    }
                    else if (exchange == Exchange.BATS)
                    {
                        return order.AbsoluteQuantity * 0.0010m;
                    }
                    else if (exchange == Exchange.NYSE)
                    {
                        return tradeValue * 0.0030m + commission * 0.000175m;
                    }
                    return tradeValue * 0.0030m;

                case (OrderType.MarketOnClose, false):
                    if (exchange == Exchange.AMEX)
                    {
                        return order.AbsoluteQuantity * 0.0005m;
                    }
                    else if (exchange == Exchange.ARCA)
                    {
                        return order.AbsoluteQuantity * 0.0012m;
                    }
                    else if (exchange == Exchange.BATS)
                    {
                        return order.AbsoluteQuantity * 0.0010m;
                    }
                    else if (exchange == Exchange.NYSE)
                    {
                        return order.AbsoluteQuantity * 0.0010m + commission * 0.000175m;
                    }
                    return order.AbsoluteQuantity * 0.0015m;

                case (OrderType.Market, true):
                case (OrderType.Limit, true):
                case (OrderType.LimitIfTouched, true):
                case (OrderType.StopMarket, true):
                case (OrderType.StopLimit, true):
                case (OrderType.TrailingStop, true):
                    if (exchange == Exchange.AMEX)
                    {
                        return tradeValue * 0.0025m;
                    }
                    else if (exchange == Exchange.NYSE)
                    {
                        return tradeValue * 0.0030m + commission * 0.000175m;
                    }
                    return tradeValue * 0.0030m;

                default:
                    if (exchange == Exchange.NYSE)
                    {
                        return order.AbsoluteQuantity * 0.0030m + commission * 0.000175m;
                    }
                    return order.AbsoluteQuantity * 0.0030m;
            }
        }

        /// <summary>
        /// Approximates the order's price based on the order type
        /// </summary>
        internal static decimal GetPotentialOrderPrice(Order order, Security security)
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

        internal static readonly Dictionary<string, decimal> UsaFuturesExchangeFees = new()
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

        internal static readonly Dictionary<string, decimal> UsaFutureOptionsExchangeFees = new()
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
        /// Reference at https://www.interactivebrokers.com/en/pricing/commissions-futures-europe.php?re=europe
        /// </summary>
        private static readonly Dictionary<string, decimal> _eurexFuturesFees = new()
        {
            // Futures
            { "FESX", 1.00m },
        };

        private static readonly Dictionary<string, decimal> _eurexFuturesExchangeFees = new()
        {
            // Futures
            { "FESX", 0.00m },
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
