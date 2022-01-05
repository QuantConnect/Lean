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

using QuantConnect.Securities;
using System;
using QuantConnect.Data.Market;


namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Pricing model used to determine the fair price or theoretical value for a call or a put option price
    /// based on Black-Scholes-Merton model
    /// </summary>
    public class BlackScholesPriceGenerator : IPriceGenerator
    {
        private readonly Securities.Option.Option _option;

        /// <summary>
        /// Creates instance of <see cref="BlackScholesPriceGenerator"/>
        /// </summary>
        ///<param name="security"><see cref="Security"/> object for which to generate price data</param>
        public BlackScholesPriceGenerator(Security security)
        {
            if (security == null)
            {
                throw new ArgumentNullException(nameof(security), "security cannot be null");
            }

            if (!security.Symbol.SecurityType.IsOption())
            {
                throw new ArgumentException("Black-Scholes pricing model cannot be applied to non-option security.");
            }

            _option = security as Securities.Option.Option;
        }

        /// <summary>
        /// For Black-Scholes-Merton model price calculation relies <see cref="IOptionPriceModel"/> of the security
        /// </summary>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// /// <param name="referenceDate">current reference date</param>
        /// <returns>A new decimal suitable for usage as new security price</returns>
        public decimal NextValue(decimal maximumPercentDeviation, DateTime referenceDate)
        {
            return _option.PriceModel
                .Evaluate(
                    _option,
                    null,
                    OptionContract.Create(
                        _option.Symbol,
                        _option.Symbol.Underlying,
                        referenceDate,
                        _option,
                        _option.Underlying.Price
                        ))
                .TheoreticalPrice;
        }
    }
}
