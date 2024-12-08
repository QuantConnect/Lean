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
using QuantConnect.Securities.Equity;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the implementation of <see cref="IFeeModel"/> for Interactive Brokers Tiered Fee Structure
    /// </summary>
    public class InteractiveBrokersTieredFeeModel : FeeModel
    {
        private const decimal EquityMinimumOrderFee = 0.35m;
        private const decimal CryptoMinimumOrderFee = 1.75m;
        private decimal _equityCommissionRate;
        private int _futureCommissionTier;
        private decimal _forexCommissionRate;
        private decimal _forexMinimumOrderFee;
        private decimal _cryptoCommissionRate;
        #pragma warning disable CS1570
        /// <summary>
        /// Reference at https://www.interactivebrokers.com/en/index.php?f=commission&p=futures1
        /// </summary>
        #pragma warning restore CS1570
        private Dictionary<string, Func<Security, CashAmount>> _futureFee;
        // option commission function takes number of contracts and the size of the option premium and returns total commission
        private readonly Dictionary<string, Func<decimal, decimal, CashAmount>> _optionFee =
            new Dictionary<string, Func<decimal, decimal, CashAmount>>();
        private Dictionary<SecurityType, decimal> _monthlyTradeVolume;
        private DateTime _lastOrderTime = DateTime.MinValue;
        // List of Option exchanges susceptible to pay ORF regulatory fee.
        private static readonly List<string> _optionExchangesOrfFee = new() { Market.CBOE, Market.USA };

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveBrokersTieredFeeModel"/>
        /// </summary>
        /// <param name="monthlyEquityTradeVolume">Monthly Equity shares traded</param>
        /// <param name="monthlyFutureTradeVolume">Monthly Future contracts traded</param>
        /// <param name="monthlyForexTradeAmountInUSDollars">Monthly FX dollar volume traded</param>
        /// <param name="monthlyOptionsTradeAmountInContracts">Monthly options contracts traded</param>
        /// <param name="monthlyCryptoTradeAmountInUSDollars">Monthly Crypto dollar volume traded (in USD)</param>
        public InteractiveBrokersTieredFeeModel(decimal monthlyEquityTradeVolume = 0, decimal monthlyFutureTradeVolume = 0, decimal monthlyForexTradeAmountInUSDollars = 0,
            decimal monthlyOptionsTradeAmountInContracts = 0, decimal monthlyCryptoTradeAmountInUSDollars = 0)
        {
            ReprocessRateSchedule(monthlyEquityTradeVolume, monthlyFutureTradeVolume, monthlyForexTradeAmountInUSDollars, monthlyOptionsTradeAmountInContracts, monthlyCryptoTradeAmountInUSDollars);
            // IB fee + exchange fee
            _futureFee = new()
            {
                { Market.USA, UnitedStatesFutureFees },
                { Market.HKFE, InteractiveBrokersFeeHelper.HongKongFutureFees },
                { Market.EUREX, InteractiveBrokersFeeHelper.EUREXFutureFees }
            };

            _monthlyTradeVolume = new()
            {
                { SecurityType.Equity, monthlyEquityTradeVolume },
                { SecurityType.Future, monthlyFutureTradeVolume },
                { SecurityType.Forex, monthlyForexTradeAmountInUSDollars },
                { SecurityType.Option, monthlyOptionsTradeAmountInContracts },
                { SecurityType.Crypto, monthlyCryptoTradeAmountInUSDollars },
            };
        }

        /// <summary>
        /// Reprocess the rate schedule based on the current traded volume in various assets.
        /// </summary>
        /// <param name="monthlyEquityTradeVolume">Monthly Equity shares traded</param>
        /// <param name="monthlyFutureTradeVolume">Monthly Future contracts traded</param>
        /// <param name="monthlyForexTradeAmountInUSDollars">Monthly FX dollar volume traded</param>
        /// <param name="monthlyOptionsTradeAmountInContracts">Monthly options contracts traded</param>
        /// <param name="monthlyCryptoTradeAmountInUSDollars">Monthly Crypto dollar volume traded (in USD)</param>
        private void ReprocessRateSchedule(decimal monthlyEquityTradeVolume, decimal monthlyFutureTradeVolume, decimal monthlyForexTradeAmountInUSDollars, 
            decimal monthlyOptionsTradeAmountInContracts, decimal monthlyCryptoTradeAmountInUSDollars)
        {
            InteractiveBrokersFeeHelper.ProcessEquityRateSchedule(monthlyEquityTradeVolume, out _equityCommissionRate);
            InteractiveBrokersFeeHelper.ProcessFutureRateSchedule(monthlyFutureTradeVolume, out _futureCommissionTier);
            InteractiveBrokersFeeHelper.ProcessForexRateSchedule(monthlyForexTradeAmountInUSDollars, out _forexCommissionRate, out _forexMinimumOrderFee);
            Func<decimal, decimal, CashAmount> optionsCommissionFunc;
            InteractiveBrokersFeeHelper.ProcessOptionsRateSchedule(monthlyOptionsTradeAmountInContracts, out optionsCommissionFunc);
            _optionFee[Market.USA] = optionsCommissionFunc;
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

            // Reset monthly trade value tracker when month rollover.
            if (_lastOrderTime.Month != order.Time.Month && _lastOrderTime != DateTime.MinValue)
            {
                _monthlyTradeVolume = _monthlyTradeVolume.ToDictionary(kvp => kvp.Key, _ => 0m);
            }
            // Reprocess the rate schedule based on the current traded volume in various assets.
            ReprocessRateSchedule(_monthlyTradeVolume[SecurityType.Equity], _monthlyTradeVolume[SecurityType.Future], _monthlyTradeVolume[SecurityType.Forex],
                _monthlyTradeVolume[SecurityType.Option], _monthlyTradeVolume[SecurityType.Crypto]);

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
                    // Update the monthly value traded
                    _monthlyTradeVolume[SecurityType.Forex] += InteractiveBrokersFeeHelper.CalculateForexFee(security, order, _forexCommissionRate, _forexMinimumOrderFee, out feeResult, out feeCurrency);
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    var orderPrice = InteractiveBrokersFeeHelper.CalculateOptionFee(security, order, quantity, market, _optionFee, out feeResult, out feeCurrency);
                    // Regulatory Fee: Options Regulatory Fee (ORF) + FINRA Consolidated Audit Trail Fees
                    var regulatory = _optionExchangesOrfFee.Contains(market) ?
                        (0.01915m + 0.0048m) * quantity :
                        0.0048m * quantity;
                    // Transaction Fees: SEC Transaction Fee + FINRA Trading Activity Fee (only charge on sell)
                    var transaction = order.Quantity < 0 ? 0.0000278m * Math.Abs(order.GetValue(security)) + 0.00279m * quantity : 0m;
                    // Clearing Fee
                    var clearing = Math.Min(0.02m * quantity, 55m);

                    feeResult += regulatory + transaction + clearing;

                    // Update the monthly value traded
                    _monthlyTradeVolume[SecurityType.Option] += quantity * orderPrice;
                    break;

                case SecurityType.Future:
                case SecurityType.FutureOption:
                    InteractiveBrokersFeeHelper.CalculateFutureFopFee(security, order, quantity, market, _futureFee, out feeResult, out feeCurrency);
                    // Update the monthly contracts traded
                    _monthlyTradeVolume[SecurityType.Future] += quantity;
                    break;

                case SecurityType.Equity:
                    var tradeValue = Math.Abs(order.GetValue(security));
                    var tradeFee = InteractiveBrokersFeeHelper.CalculateEquityFee(security, order, quantity, tradeValue, market, _equityCommissionRate, EquityMinimumOrderFee, out feeResult, out feeCurrency);

                    // Tiered fee model has the below extra cost.
                    // FINRA Trading Activity Fee only applies to sale of security.
                    var finraTradingActivityFee = order.Direction == OrderDirection.Sell ? Math.Min(8.3m, quantity * 0.000166m) : 0m;
                    // Regulatory Fees.
                    var regulatoryFee = tradeValue * 0.0000278m             // SEC Transaction Fee
                        + finraTradingActivityFee                           // FINRA Trading Activity Fee
                        + quantity * 0.000048m;                             // FINRA Consolidated Audit Trail Fees
                    // Clearing Fee: NSCC, DTC Fees.
                    var clearingFee = Math.Min(quantity * 0.0002m, tradeValue * 0.005m);
                    // Exchange related handling fees.
                    var exchangeFee = InteractiveBrokersFeeHelper.GetEquityExchangeFee(order, (security as Equity).PrimaryExchange, tradeValue, tradeFee);
                    // FINRA Pass Through Fees.
                    var passThroughFee = Math.Min(8.3m, tradeFee * 0.00056m);

                    feeResult = feeResult + regulatoryFee + clearingFee + exchangeFee + passThroughFee;

                    // Update the monthly volume shares traded
                    _monthlyTradeVolume[SecurityType.Equity] += quantity;
                    break;

                case SecurityType.Cfd:
                    InteractiveBrokersFeeHelper.CalculateCfdFee(security, order, out feeResult, out feeCurrency);
                    break;
                    
                case SecurityType.Crypto:
                    // Update the monthly value traded
                    _monthlyTradeVolume[SecurityType.Crypto] += InteractiveBrokersFeeHelper.CalculateCryptoFee(security, order, _cryptoCommissionRate, CryptoMinimumOrderFee, out feeResult, out feeCurrency);
                    break;

                default:
                    // unsupported security type
                    throw new ArgumentException(Messages.FeeModel.UnsupportedSecurityType(security));
            }

            _lastOrderTime = order.Time;

            return new OrderFee(new CashAmount(
                feeResult,
                feeCurrency));
        }

        private CashAmount UnitedStatesFutureFees(Security security)
        {
            IDictionary<string, decimal[]> fees;
            IDictionary<string, decimal> exchangeFees;
            decimal[] ibFeePerContract;
            decimal exchangeFeePerContract;
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
                ibFeePerContract = new[] { 0.85m, 0.65m, 0.45m, 0.25m };
            }

            if (!exchangeFees.TryGetValue(symbol, out exchangeFeePerContract))
            {
                exchangeFeePerContract = 1.60m;
            }

            // Add exchange fees + IBKR regulatory fee (0.02)
            return new CashAmount(ibFeePerContract[_futureCommissionTier] + exchangeFeePerContract + 0.02m, Currencies.USD);
        }

        /// <summary>
        /// Reference at https://www.interactivebrokers.com/en/pricing/commissions-futures.php?re=amer
        /// </summary>
        private static readonly Dictionary<string, decimal[]> _usaFuturesFees = new()
        {
            // Micro E-mini Futures
            { "MYM", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "M2K", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "MES", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            { "MNQ", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "2YY", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "5YY", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            { "10Y", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "30Y", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "MCL", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            { "MGC", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "SIL", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            // Cryptocurrency Futures
            { "BTC", new decimal[] { 5m, 5m, 5m, 5m } }, { "MBT", new decimal[] { 2.25m, 2.25m, 2.25m, 2.25m } }, { "ETH", new decimal[] { 3m, 3m, 3m, 3m } }, { "MET", new decimal[] { 0.2m, 0.2m, 0.2m, 0.2m } },
            // E-mini FX (currencies) Futures
            { "E7", new decimal[] { 0.5m, 0.4m, 0.3m, 0.15m } }, { "J7", new decimal[] { 0.5m, 0.4m, 0.3m, 0.15m } },
            // Micro E-mini FX (currencies) Futures
            { "M6E", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "M6A", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "M6B", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } },
            { "MCD", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "MJY", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "MSF", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } },
            { "M6J", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "MIR", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "M6C", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } },
            { "M6S", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } }, { "MNH", new decimal[] { 0.15m, 0.12m, 0.08m, 0.05m } },
        };

        private static readonly Dictionary<string, decimal[]> _usaFutureOptionsFees = new()
        {
            // Micro E-mini Future Options
            { "MYM", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "M2K", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "MES", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            { "MNQ", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "2YY", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "5YY", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            { "10Y", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "30Y", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "MCL", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            { "MGC", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } }, { "SIL", new decimal[] { 0.25m, 0.2m, 0.15m, 0.1m } },
            // Cryptocurrency Future Options
            { "BTC", new decimal[] { 5m, 5m, 5m, 5m } }, { "MBT", new decimal[] { 1.25m, 1.25m, 1.25m, 1.25m } }, { "ETH", new decimal[] { 3m, 3m, 3m, 3m } }, { "MET", new decimal[] { 0.1m, 0.1m, 0.1m, 0.1m } },
        };
    }
}
