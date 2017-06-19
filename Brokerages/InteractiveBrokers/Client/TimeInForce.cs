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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Order Time in Force Values
    /// </summary>
    public static class TimeInForce
    {
        /// <summary>
        /// Day
        /// </summary>
        public const string Day = "DAY";

        /// <summary>
        /// Good Till Cancel
        /// </summary>
        public const string GoodTillCancel = "GTC";

        /// <summary>
        /// You can set the time in force for MARKET or LIMIT orders as IOC. This dictates that any portion of the order not executed immediately after it becomes available on the market will be cancelled.
        /// </summary>
        public const string ImmediateOrCancel = "IOC";

        /// <summary>
        /// Setting FOK as the time in force dictates that the entire order must execute immediately or be canceled.
        /// </summary>
        public const string FillOrKill = "FOK";

        /// <summary>
        /// Good Till Date
        /// </summary>
        public const string GoodTillDate = "GTD";

        /// <summary>
        /// Market On Open
        /// </summary>
        public const string MarketOnOpen = "OPG";

        /// <summary>
        /// Undefined
        /// </summary>
        public const string Undefined = "";
    }
}
