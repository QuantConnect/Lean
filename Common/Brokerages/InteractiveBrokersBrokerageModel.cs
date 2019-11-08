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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

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
            {SecurityType.Option, Market.USA},
            {SecurityType.Future, Market.USA},
            {SecurityType.Forex, Market.Oanda},
            {SecurityType.Cfd, Market.Oanda}
        }.ToReadOnlyDictionary();

        private readonly Type[] _supportedTimeInForces =
        {
            typeof(GoodTilCanceledTimeInForce),
            typeof(DayTimeInForce),
            typeof(GoodTilDateTimeInForce)
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
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new InteractiveBrokersFeeModel();
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

            // validate security type
            if (security.Type != SecurityType.Equity &&
                security.Type != SecurityType.Forex &&
                security.Type != SecurityType.Option &&
                security.Type != SecurityType.Future)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(InteractiveBrokersBrokerageModel)} does not support {security.Type} security type.")
                );

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
            if (!_supportedTimeInForces.Contains(order.TimeInForce.GetType()))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Invariant($"The {nameof(InteractiveBrokersBrokerageModel)} does not support {order.TimeInForce.GetType().Name} time in force.")
                );

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
            /* https://www.interactivebrokers.com/en/?f=%2Fen%2Ftrading%2FforexOrderSize.php
            Currency    Currency Description	    Minimum Order Size	Maximum Order Size
            USD	        US Dollar	        	    25,000              7,000,000
            AUD	        Australian Dollar	        25,000              6,000,000
            CAD	        Canadian Dollar	            25,000              6,000,000
            CHF	        Swiss Franc	                25,000	            6,000,000
            CNH	        China Renminbi (offshore)	160,000	            40,000,000
            CZK	        Czech Koruna	            USD 25,000(1)	    USD 7,000,000(1)
            DKK	        Danish Krone	            150,000	            35,000,000
            EUR	        Euro	                    20,000	            5,000,000
            GBP	        British Pound Sterling	    17,000	            4,000,000
            HKD	        Hong Kong Dollar	        200,000	            50,000,000
            HUF	        Hungarian Forint	        USD 25,000(1)	    USD 7,000,000(1)
            ILS	        Israeli Shekel	            USD 25,000(1)	    USD 7,000,000(1)
            KRW	        Korean Won	                50,000,000	        750,000,000
            JPY	        Japanese Yen	            2,500,000	        550,000,000
            MXN	        Mexican Peso	            300,000	            70,000,000
            NOK	        Norwegian Krone	            150,000	            35,000,000
            NZD	        New Zealand Dollar	        35,000	            8,000,000
            RUB	        Russian Ruble	            750,000	            30,000,000
            SEK	        Swedish Krona	            175,000	            40,000,000
            SGD	        Singapore Dollar	        35,000	            8,000,000
             */

            message = null;

            // switch on the currency being bought
            string baseCurrency, quoteCurrency;
            Forex.DecomposeCurrencyPair(currencyPair, out baseCurrency, out quoteCurrency);

            decimal max;
            ForexCurrencyLimits.TryGetValue(baseCurrency, out max);

            var orderIsWithinForexSizeLimits = quantity < max;
            if (!orderIsWithinForexSizeLimits)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderSizeLimit",
                    Invariant($"The maximum allowable order size is {max}{baseCurrency}.")
                );
            }
            return orderIsWithinForexSizeLimits;
        }


        private static readonly IReadOnlyDictionary<string, decimal> ForexCurrencyLimits = new Dictionary<string, decimal>()
        {
            {"USD", 7000000m},
            {"AUD", 6000000m},
            {"CAD", 6000000m},
            {"CHF", 6000000m},
            {"CNH", 40000000m},
            {"CZK", 0m}, // need market price in USD or EUR -- do later when we support
            {"DKK", 35000000m},
            {"EUR", 5000000m},
            {"GBP", 4000000m},
            {"HKD", 50000000m},
            {"HUF", 0m}, // need market price in USD or EUR -- do later when we support
            {"ILS", 0m}, // need market price in USD or EUR -- do later when we support
            {"KRW", 750000000m},
            {"JPY", 550000000m},
            {"MXN", 70000000m},
            {"NOK", 35000000m},
            {"NZD", 8000000m},
            {"RUB", 30000000m},
            {"SEK", 40000000m},
            {"SGD", 8000000m}
        };
    }
}
