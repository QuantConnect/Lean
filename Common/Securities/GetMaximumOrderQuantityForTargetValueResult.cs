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
    /// Contains the information returned by <see cref="IBuyingPowerModel.GetMaximumOrderQuantityForTargetValue"/>
    /// </summary>
    public class GetMaximumOrderQuantityForTargetValueResult
    {
        /// <summary>
        /// Returns the maximum quantity for the order
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Returns the reason for which the maximum order quantity is zero
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Returns true if the zero order quantity is an error condition and will be shown to the user.
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityForTargetValueResult"/> class
        /// </summary>
        /// <param name="quantity">Returns the maximum quantity for the order</param>
        /// <param name="reason">The reason for which the maximum order quantity is zero</param>
        public GetMaximumOrderQuantityForTargetValueResult(decimal quantity, string reason = null)
        {
            Quantity = quantity;
            Reason = reason ?? string.Empty;
            IsError = Reason != string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityForTargetValueResult"/> class
        /// </summary>
        /// <param name="quantity">Returns the maximum quantity for the order</param>
        /// <param name="reason">The reason for which the maximum order quantity is zero</param>
        /// <param name="isError">True if the zero order quantity is an error condition</param>
        public GetMaximumOrderQuantityForTargetValueResult(decimal quantity, string reason, bool isError = true)
        {
            Quantity = quantity;
            Reason = reason ?? string.Empty;
            IsError = isError;
        }
    }
}
