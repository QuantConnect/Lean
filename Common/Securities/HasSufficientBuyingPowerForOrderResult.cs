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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Contains the information returned by <see cref="IBuyingPowerModel.HasSufficientBuyingPowerForOrder"/>
    /// </summary>
    public class HasSufficientBuyingPowerForOrderResult
    {
        /// <summary>
        /// Returns true if there is sufficient buying power to execute an order
        /// </summary>
        public bool IsSufficient { get; }

        /// <summary>
        /// Returns the reason for insufficient buying power to execute an order
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HasSufficientBuyingPowerForOrderResult"/> class
        /// </summary>
        /// <param name="isSufficient">True if the order can be executed</param>
        /// <param name="reason">The reason for insufficient buying power</param>
        public HasSufficientBuyingPowerForOrderResult(bool isSufficient, string reason = null)
        {
            IsSufficient = isSufficient;
            Reason = reason ?? string.Empty;
        }
    }
}
