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
using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a holding of a currency in cash.
    /// </summary>
    public class Cash
    {
        private readonly bool _isBaseCurrency;
        private readonly int _subscriptionIndex;
        private readonly bool _invertRealTimePrice;

        /// <summary>
        /// Gets the symbol used to represent this cash
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Gets or sets the amount of cash held
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets the conversion rate into base currency
        /// </summary>
        public decimal ConversionRate { get; private set; }

        /// <summary>
        /// Gets the value of this cash in the base currency
        /// </summary>
        public decimal ValueInBaseCurrency
        {
            get { return Quantity*ConversionRate; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cash"/> class
        /// </summary>
        /// <param name="symbol">The symbol used to represent this cash</param>
        /// <param name="subscriptions">The subscription manager used to resolve forex pair for value conversion data</param>
        public Cash(string symbol, SubscriptionManager subscriptions)
        {
            Symbol = symbol.ToUpper();
            if (Symbol == CashBook.BaseCurrency)
            {
                _isBaseCurrency = true;
                ConversionRate = 1.0m;
                return;
            }
            
            // we require a subscription that converts this into the base currency
            string normal = Symbol + CashBook.BaseCurrency;
            string invert = CashBook.BaseCurrency + Symbol;
            for (int i = 0; i < subscriptions.Subscriptions.Count; i++)
            {
                var config = subscriptions.Subscriptions[i];
                if (config.Security != SecurityType.Forex)
                {
                    continue;
                }
                if (config.Symbol == normal)
                {
                    _subscriptionIndex = i;
                    return;
                }
                if (config.Symbol == invert)
                {
                    _subscriptionIndex = i;
                    _invertRealTimePrice = true;
                    return;
                }
            }

            // if this still hasn't been set then it's an error condition
            throw new ArgumentException(string.Format("In order to maintain cash in {0} you are required to add a subscription for Forex pair {0}{1} or {1}{0}", Symbol, CashBook.BaseCurrency));
        }

        /// <summary>
        /// Sets the <see cref="ConversionRate"/> for this cash into the base currency
        /// </summary>
        /// <param name="rate">The new conversion rate</param>
        public void SetConversionRate(decimal rate)
        {
            ConversionRate = rate;
        }

        /// <summary>
        /// Update the current conversion rate
        /// </summary>
        /// <param name="data">The list of real time prices directly from the data feed</param>
        public void Update(Dictionary<int, List<BaseData>> data)
        {
            // conversions for base currencies are always identity
            if (_isBaseCurrency) return;

            var realTimePrice = data[_subscriptionIndex];
            if (!data.Any())
            {
                // if we don't have data we can't do anything
                return;
            }

            decimal rate = realTimePrice[0].Value;
            if (_invertRealTimePrice)
            {
                rate = 1/rate;
            }

            ConversionRate = rate;
        }
    }
}