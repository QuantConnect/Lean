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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a holding of a currency in cash.
    /// </summary>
    public class Cash
    {
        private bool _isBaseCurrency;
        private bool _invertRealTimePrice;

        /// <summary>
        /// Gets the symbol of the security required to provide conversion rates.
        /// </summary>
        public Symbol SecuritySymbol { get; private set; }

        /// <summary>
        /// Gets the symbol used to represent this cash
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Gets or sets the amount of cash held
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets the conversion rate into account currency
        /// </summary>
        public decimal ConversionRate { get; internal set; }

        /// <summary>
        /// Gets the value of this cash in the accout currency
        /// </summary>
        public decimal ValueInAccountCurrency
        {
            get { return Quantity*ConversionRate; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cash"/> class
        /// </summary>
        /// <param name="symbol">The symbol used to represent this cash</param>
        /// <param name="quantity">The quantity of this currency held</param>
        /// <param name="conversionRate">The initial conversion rate of this currency into the <see cref="CashBook.AccountCurrency"/></param>
        public Cash(string symbol, decimal quantity, decimal conversionRate)
        {
            if (symbol == null || symbol.Length != 3)
            {
                throw new ArgumentException("Cash symbols must be exactly 3 characters.");
            }
            Quantity = quantity;
            ConversionRate = conversionRate;
            Symbol = symbol.ToUpper();
        }

        /// <summary>
        /// Updates this cash object with the specified data
        /// </summary>
        /// <param name="data">The new data for this cash object</param>
        public void Update(BaseData data)
        {
            if (_isBaseCurrency) return;
            
            var rate = data.Value;
            if (_invertRealTimePrice)
            {
                rate = 1/rate;
            }
            ConversionRate = rate;
        }

        /// <summary>
        /// Ensures that we have a data feed to conver this currency into the base currency.
        /// This will add a subscription at the lowest resolution if one is not found.
        /// </summary>
        /// <param name="securities">The security manager</param>
        /// <param name="subscriptions">The subscription manager used for searching and adding subscriptions</param>
        /// <param name="exchangeHoursProvider">A security exchange hours provider instance used to resolve exchange hours for new subscriptions</param>
        public void EnsureCurrencyDataFeed(SecurityManager securities, SubscriptionManager subscriptions, SecurityExchangeHoursProvider exchangeHoursProvider)
        {
            if (Symbol == CashBook.AccountCurrency)
            {
                SecuritySymbol = QuantConnect.Symbol.Empty;
                _isBaseCurrency = true;
                ConversionRate = 1.0m;
                return;
            }

            if (subscriptions.Count == 0)
            {
                throw new InvalidOperationException("Unable to add cash when no subscriptions are present. Please add subscriptions in the Initialize() method.");
            }

            // we require a subscription that converts this into the base currency
            string normal = Symbol + CashBook.AccountCurrency;
            string invert = CashBook.AccountCurrency + Symbol;
            foreach (var config in subscriptions.Subscriptions.Where(config => config.SecurityType == SecurityType.Forex))
            {
                if (config.Symbol.Value == normal)
                {
                    SecuritySymbol = config.Symbol;
                    return;
                }
                if (config.Symbol.Value == invert)
                {
                    SecuritySymbol = config.Symbol;
                    _invertRealTimePrice = true;
                    return;
                }
            }

            // get the market from the first Forex subscription
            string market = (from config in subscriptions.Subscriptions
                             where config.SecurityType == SecurityType.Forex
                             select config.Market).FirstOrDefault() ?? "fxcm";

            // if we've made it here we didn't find a subscription, so we'll need to add one
            var currencyPairs = Forex.Forex.CurrencyPairs.Select(x => new Symbol(x));
            var minimumResolution = subscriptions.Subscriptions.Min(x => x.Resolution);
            var objectType = minimumResolution == Resolution.Tick ? typeof (Tick) : typeof (TradeBar);
            foreach (var symbol in currencyPairs)
            {
                if (symbol.Value == normal || symbol.Value == invert)
                {
                    _invertRealTimePrice = symbol.Value == invert;
                    var exchangeHours = exchangeHoursProvider.GetExchangeHours(market, symbol, SecurityType.Forex);
                    // set this as an internal feed so that the data doesn't get sent into the algorithm's OnData events
                    var config = subscriptions.Add(objectType, SecurityType.Forex, symbol, minimumResolution, market, exchangeHours.TimeZone, false, true, false, true);
                    var security = new Forex.Forex(this, config, 1m);
                    SecuritySymbol = config.Symbol;
                    securities.Add(config.Symbol, security);
                    Log.Trace("Cash.EnsureCurrencyDataFeed(): Adding " + symbol + " for cash " + Symbol + " currency feed");
                    return;
                }
            }

            // if this still hasn't been set then it's an error condition
            throw new ArgumentException(string.Format("In order to maintain cash in {0} you are required to add a subscription for Forex pair {0}{1} or {1}{0}", Symbol, CashBook.AccountCurrency));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="QuantConnect.Securities.Cash"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="QuantConnect.Securities.Cash"/>.</returns>
        public override string ToString()
        {
            // round the conversion rate for output
            decimal rate = ConversionRate;
            rate = rate < 1000 ? rate.RoundToSignificantDigits(5) : Math.Round(rate, 2);
            return string.Format("{0}: {1,10} @ ${2,10} = {3}", Symbol, Quantity.ToString("0.00"), rate.ToString("0.00####"), ValueInAccountCurrency.ToString("C"));
        }
    }
}