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

namespace QuantConnect.Util
{
    public static class OptionPayoff
    {
        /// <summary>
        /// Intrinsic value function of the option
        /// </summary>
        /// <param name="underlyingPrice">The price of the underlying</param>
        /// <param name="strike">The strike price of the option</param>
        /// <param name="right">The option right of the option, call or put</param>
        /// <returns>The intrinsic value remains for the option at expiry</returns>
        public static decimal GetIntrinsicValue(decimal underlyingPrice, decimal strike, OptionRight right)
        {
            return Math.Max(0.0m, GetPayOff(underlyingPrice, strike, right));
        }

        /// <summary>
        /// Option payoff function at expiration time
        /// </summary>
        /// <param name="underlyingPrice">The price of the underlying</param>
        /// <param name="strike">The strike price of the option</param>
        /// <param name="right">The option right of the option, call or put</param>
        /// <returns></returns>
        public static decimal GetPayOff(decimal underlyingPrice, decimal strike, OptionRight right)
        {
            return right == OptionRight.Call ? underlyingPrice - strike : strike - underlyingPrice;
        }
    }
}
