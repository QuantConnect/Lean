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

using System.ComponentModel;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Specifies the possible error states during presubmission checks
    /// </summary>
    public enum OrderError
    {
        /// <summary>
        /// Order has already been filled and cannot be modified
        /// </summary>
        [Description("Order has already been filled and cannot be modified")]
        CanNotUpdateFilledOrder = -8,

        /// <summary>
        /// General error in order
        /// </summary>
        [Description("General error in order")]
        GeneralError = -7,

        /// <summary>
        /// Order timestamp error. Order appears to be executing in the future
        /// </summary>
        [Description("Order timestamp error. Order appears to be executing in the future")]
        TimestampError = -6,

        /// <summary>
        /// Exceeded maximum allowed orders for one analysis period
        /// </summary>
        [Description("Exceeded maximum allowed orders for one analysis period")]
        MaxOrdersExceeded = -5,

        /// <summary>
        /// Insufficient capital to execute order
        /// </summary>
        [Description("Insufficient capital to execute order")]
        InsufficientCapital = -4,

        /// <summary>
        /// Attempting market order outside of market hours
        /// </summary>
        [Description("Attempting market order outside of market hours")]
        MarketClosed = -3,

        /// <summary>
        /// There is no data yet for this security - please wait for data (market order price not available yet)
        /// </summary>
        [Description("There is no data yet for this security - please wait for data (market order price not available yet)")]
        NoData = -2,

        /// <summary>
        /// Order quantity must not be zero
        /// </summary>
        [Description("Order quantity must not be zero")]
        ZeroQuantity = -1,

        /// <summary>
        /// The order is OK
        /// </summary>
        [Description("The order is OK")]
        None = 0
    }
}
