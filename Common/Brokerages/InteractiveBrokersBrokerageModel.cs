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
using QuantConnect.Orders;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides properties specific to interactive brokers
    /// </summary>
    public class InteractiveBrokersBrokerageModel : DefaultBrokerageModel
    {
        public override bool CanSubmitOrder(DateTime time, Order order, out BrokerageMessageEvent message)
        {
            message = null;

            //https://www.interactivebrokers.com/en/?f=%2Fen%2Ftrading%2FforexOrderSize.php
            switch (order.SecurityType)
            {
                case SecurityType.Base:
                    return false;
                case SecurityType.Equity:
                    return true; // could not find order limits on equities
                case SecurityType.Option:
                    return true;
                case SecurityType.Commodity:
                    return true;
                case SecurityType.Forex:
                    return IsForexWithinOrderSizeLimits(order, out message);
                case SecurityType.Future:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool CanExecuteOrder(DateTime time, Order order)
        {
            return order.SecurityType != SecurityType.Base;
        }

        private bool IsForexWithinOrderSizeLimits(Order order, out BrokerageMessageEvent message)
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
            Forex.DecomposeCurrencyPair(order.Symbol, out baseCurrency, out quoteCurrency);


            decimal max;
            ForexCurrencyLimits.TryGetValue(baseCurrency, out max);

            var orderIsWithinForexSizeLimits = order.Quantity < max;
            if (!orderIsWithinForexSizeLimits)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "OrderSizeLimit",
                    string.Format("The maximum allowable order size is {0}{1}.", max, baseCurrency)
                    );
            }
            return orderIsWithinForexSizeLimits;
        }


        private static readonly IReadOnlyDictionary<string, decimal> ForexCurrencyLimits = new Dictionary<string, decimal>()
        {
            {"USD", 7000000},
            {"USD", 7000000},
            {"AUD", 6000000},
            {"CAD", 6000000},
            {"CHF", 6000000},
            {"CNH", 40000000},
            {"CZK", 0}, // need market price in USD or EUR -- do later when we support
            {"DKK", 35000000},
            {"EUR", 5000000},
            {"GBP", 4000000},
            {"HKD", 50000000},
            {"HUF", 0}, // need market price in USD or EUR -- do later when we support
            {"ILS", 0}, // need market price in USD or EUR -- do later when we support
            {"KRW", 750000000},
            {"JPY", 550000000},
            {"MXN", 70000000},
            {"NOK", 35000000},
            {"NZD", 8000000},
            {"RUB", 30000000},
            {"SEK", 40000000},
            {"SGD", 8000000}
        };
    }
}
