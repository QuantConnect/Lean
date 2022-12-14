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

namespace QuantConnect.Securities.CryptoFuture
{
    /// <summary>
    /// Crypto Future holdings implementation of the base securities class
    /// </summary>
    /// <seealso cref="SecurityHolding"/>
    public class CryptoFutureHolding : SecurityHolding
    {
        /// <summary>
        /// Crypto Future Holding Class constructor
        /// </summary>
        /// <param name="security">The crypto future security being held</param>
        /// <param name="currencyConverter">A currency converter instance</param>
        public CryptoFutureHolding(Security security, ICurrencyConverter currencyConverter)
            : base(security, currencyConverter)
        {
        }
    }
}
