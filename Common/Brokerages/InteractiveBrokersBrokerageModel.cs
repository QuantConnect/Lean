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
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Option;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides properties specific to interactive brokers
    /// </summary>
    public class InteractiveBrokersBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// The default markets for the IB brokerage
        /// </summary>
        public new static readonly IReadOnlyDictionary<SecurityType, string> DefaultMarketMap = new Dictionary<SecurityType, string>
        {
            {SecurityType.Base, Market.USA},
            {SecurityType.Equity, Market.USA},
            {SecurityType.Index, Market.USA},
            {SecurityType.Option, Market.USA},
            {SecurityType.IndexOption, Market.USA},
            {SecurityType.Future, Market.CME},
            {SecurityType.FutureOption, Market.CME},
            {SecurityType.Forex, Market.Oanda},
            {SecurityType.Cfd, Market.InteractiveBrokers}
        }.ToReadOnlyDictionary();

        /// <summary>
        /// Supported time in force
        /// </summary>
        protected virtual Type[] SupportedTimeInForces { get; } =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce),
            typeof(GoodTilDateTimeInForce)
        };

        /// <summary>
        /// Supported order types
        /// </summary>
        protected virtual HashSet<OrderType> SupportedOrderTypes { get; } = new HashSet<OrderType>
        {
            OrderType.Market,
            OrderType.MarketOnOpen,
            OrderType.MarketOnClose,
            OrderType.Limit,
            OrderType.StopMarket,
            OrderType.StopLimit,
            OrderType.TrailingStop,
            OrderType.LimitIfTouched,
            OrderType.ComboMarket,
            OrderType.ComboLimit,
            OrderType.ComboLegLimit,
            OrderType.OptionExercise
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveBrokersBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Margin"/></param>
        public InteractiveBrokersBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => DefaultMarketMap;

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            // Equivalent to no benchmark
            return new FuncBenchmark(x => 0);
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new InteractiveBrokersFeeModel();
        }

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash)
            {
                return 1m;
            }

            return security.Type == SecurityType.Cfd ? 10m : base.GetLeverage(security);
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            // validate order type
            if (!SupportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, SupportedOrderTypes));

                return false;
            }
            else if (order.Type == OrderType.MarketOnClose && security.Type != SecurityType.Future && security.Type != SecurityType.Equity)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, $"Unsupported order type for {security.Type} security type",
                    "InteractiveBrokers does not support Market-on-Close orders for other security types different than Future and Equity.");
                return false;
            }
            else if (order.Type == OrderType.MarketOnOpen && security.Type != SecurityType.Equity && !security.Type.IsOption())
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, $"Unsupported order type for {security.Type} security type",
                    "InteractiveBrokers does not support Market-on-Open orders for other security types different than Option and Equity.");
                return false;
            }

            // validate security type
            if (security.Type != SecurityType.Equity &&
                security.Type != SecurityType.Forex &&
                security.Type != SecurityType.Option &&
                security.Type != SecurityType.Future &&
                security.Type != SecurityType.FutureOption &&
                security.Type != SecurityType.Index &&
                security.Type != SecurityType.IndexOption &&
                security.Type != SecurityType.Cfd)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

                return false;
            }

            // validate order quantity
            //https://www.interactivebrokers.com/en/?f=%2Fen%2Ftrading%2FforexOrderSize.php
            if (security.Type == SecurityType.Forex &&
                !IsForexWithinOrderSizeLimits(order.Symbol.Value, order.Quantity, out message))
            {
                return false;
            }

            // validate time in force
            if (!SupportedTimeInForces.Contains(order.TimeInForce.GetType()))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedTimeInForce(this, order));

                return false;
            }

            // IB doesn't support index options and cash-settled options exercise
            if (order.Type == OrderType.OptionExercise &&
                (security.Type == SecurityType.IndexOption ||
                (security.Type == SecurityType.Option && (security as Option).ExerciseSettlement == SettlementType.Cash)))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.InteractiveBrokersBrokerageModel.UnsupportedExerciseForIndexAndCashSettledOptions(this, order));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = null;

            if (order.SecurityType == SecurityType.Forex && request.Quantity != null)
            {
                return IsForexWithinOrderSizeLimits(order.Symbol.Value, request.Quantity.Value, out message);
            }

            return true;
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public override bool CanExecuteOrder(Security security, Order order)
        {
            return order.SecurityType != SecurityType.Base;
        }

        /// <summary>
        /// Returns true if the specified order is within IB's order size limits
        /// </summary>
        private bool IsForexWithinOrderSizeLimits(string currencyPair, decimal quantity, out BrokerageMessageEvent message)
        {
            /* https://www.interactivebrokers.com/en/trading/forexOrderSize.php
            Currency    Currency Description	    Minimum Order Size	Maximum Order Size
            USD	        US Dollar	                25,000	            7,000,000
            AUD	        Australian Dollar	        25,000	            6,000,000
            CAD	        Canadian Dollar	            25,000	            6,000,000
            CHF	        Swiss Franc	                25,000	            6,000,000
            CNH	        China Renminbi (offshore)	150,000	            40,000,000
            CZK	        Czech Koruna	            USD 25,000(1)       USD 7,000,000(1)
            DKK	        Danish Krone	            150,000	            35,000,000
            EUR	        Euro	                    20,000	            6,000,000
            GBP	        British Pound Sterling	    20,000	            5,000,000
            HKD	        Hong Kong Dollar	        200,000	            50,000,000
            HUF	        Hungarian Forint	        USD 25,000(1)   	USD 7,000,000(1)
            ILS	        Israeli Shekel	            USD 25,000(1)   	USD 7,000,000(1)
            KRW	        Korean Won	                0	                200,000,000
            JPY	        Japanese Yen	            2,500,000	        550,000,000
            MXN	        Mexican Peso	            300,000	            70,000,000
            NOK	        Norwegian Krone	            150,000	            35,000,000
            NZD	        New Zealand Dollar	        35,000	            8,000,000
            PLN	        Polish Zloty	            USD 25,000(1)       USD 7,000,000(1)
            RUB	        Russian Ruble	            750,000	            30,000,000
            SEK	        Swedish Krona	            175,000	            40,000,000
            SGD	        Singapore Dollar	        35,000	            8,000,000
            ZAR	        South African Rand	        350,000	            100,000,000
             */

            message = null;

            // switch on the currency being bought
            Forex.DecomposeCurrencyPair(currencyPair, out var baseCurrency, out _);

            ForexCurrencyLimits.TryGetValue(baseCurrency, out var limits);
            var min = limits?.Item1 ?? 0m;
            var max = limits?.Item2 ?? 0m;

            var absoluteQuantity = Math.Abs(quantity);
            var orderIsWithinForexSizeLimits = ((min == 0 && absoluteQuantity > min) || (min > 0 && absoluteQuantity >= min)) && absoluteQuantity <= max;
            if (!orderIsWithinForexSizeLimits)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderSizeLimit",
                    Messages.InteractiveBrokersBrokerageModel.InvalidForexOrderSize(min, max, baseCurrency));
            }
            return orderIsWithinForexSizeLimits;
        }

        // currency -> (min, max)
        private static readonly IReadOnlyDictionary<string, Tuple<decimal, decimal>> ForexCurrencyLimits =
            new Dictionary<string, Tuple<decimal, decimal>>()
            {
                {"USD", Tuple.Create(25000m, 7000000m)},
                {"AUD", Tuple.Create(25000m, 6000000m)},
                {"CAD", Tuple.Create(25000m, 6000000m)},
                {"CHF", Tuple.Create(25000m, 6000000m)},
                {"CNH", Tuple.Create(150000m, 40000000m)},
                {"CZK", Tuple.Create(0m, 0m)}, // need market price in USD or EUR -- do later when we support
                {"DKK", Tuple.Create(150000m, 35000000m)},
                {"EUR", Tuple.Create(20000m, 6000000m)},
                {"GBP", Tuple.Create(20000m, 5000000m)},
                {"HKD", Tuple.Create(200000m, 50000000m)},
                {"HUF", Tuple.Create(0m, 0m)}, // need market price in USD or EUR -- do later when we support
                {"ILS", Tuple.Create(0m, 0m)}, // need market price in USD or EUR -- do later when we support
                {"KRW", Tuple.Create(0m, 200000000m)},
                {"JPY", Tuple.Create(2500000m, 550000000m)},
                {"MXN", Tuple.Create(300000m, 70000000m)},
                {"NOK", Tuple.Create(150000m, 35000000m)},
                {"NZD", Tuple.Create(35000m, 8000000m)},
                {"PLN", Tuple.Create(0m, 0m)}, // need market price in USD or EUR -- do later when we support
                {"RUB", Tuple.Create(750000m, 30000000m)},
                {"SEK", Tuple.Create(175000m, 40000000m)},
                {"SGD", Tuple.Create(35000m, 8000000m)},
                {"ZAR", Tuple.Create(350000m, 100000000m)}
            };
    }
}
