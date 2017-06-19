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

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Historical Data Request Return Types
    /// </summary>
    public static class HistoricalDataType
    {
        /// <summary>
        /// Return Trade data only
        /// </summary>
        public const string Trades = "TRADES";

        /// <summary>
        /// Return the mid point between the bid and ask
        /// </summary>
        public const string Midpoint = "MIDPOINT";

        /// <summary>
        /// Return Bid Prices only
        /// </summary>
        public const string Bid = "BID";

        /// <summary>
        /// Return ask prices only
        /// </summary>
        public const string Ask = "ASK";

        /// <summary>
        /// Return Bid / Ask price only
        /// </summary>
        public const string BidAsk = "BID_ASK";
    }
}
