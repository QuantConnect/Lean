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

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Result type for <see cref="IPositionGroupBuyingPowerModel.GetMaximumLotsForDeltaBuyingPower"/>
    /// and <see cref="IPositionGroupBuyingPowerModel.GetMaximumLotsForTargetBuyingPower"/>
    /// </summary>
    public class GetMaximumLotsResult
    {
        /// <summary>
        /// Returns the maximum number of lots of the position group that can be
        /// ordered. This is a whole number and is the <see cref="IPositionGroup.Quantity"/>
        /// </summary>
        public decimal NumberOfLots { get; }

        /// <summary>
        /// Returns the reason for which the maximum order quantity is zero
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Returns true if the zero order quantity is an error condition and will be shown to the user.
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityResult"/> class
        /// </summary>
        /// <param name="numberOfLots">Returns the maximum number of lots of the position group that can be ordered</param>
        /// <param name="reason">The reason for which the maximum order quantity is zero</param>
        public GetMaximumLotsResult(decimal numberOfLots, string reason = null)
        {
            NumberOfLots = numberOfLots;
            Reason = reason ?? string.Empty;
            IsError = !string.IsNullOrEmpty(Reason);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMaximumOrderQuantityResult"/> class
        /// </summary>
        /// <param name="numberOfLots">Returns the maximum number of lots of the position group that can be ordered</param>
        /// <param name="reason">The reason for which the maximum order quantity is zero</param>
        /// <param name="isError">True if the zero order quantity is an error condition</param>
        public GetMaximumLotsResult(decimal numberOfLots, string reason, bool isError = true)
        {
            IsError = isError;
            NumberOfLots = numberOfLots;
            Reason = reason ?? string.Empty;
        }
    }
}
