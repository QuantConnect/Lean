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
    /// Order Action Side. Specifies whether securities should be bought or sold.
    /// </summary>
    public static class ActionSide
    {
        /// <summary>
        /// Security is to be bought.
        /// </summary>
        public const string Buy = "BUY";

        /// <summary>
        /// Security is to be sold.
        /// </summary>
        public const string Sell = "SELL";

        /// <summary>
        /// Undefined
        /// </summary>
        public const string Undefined = "";

        /// <summary>
        /// Sell Short as part of a combo leg
        /// </summary>
        public const string SShort = "SSHORT";

        /// <summary>
        /// Short Sale Exempt action.
        /// SSHORTX allows some orders to be marked as exempt from the new SEC Rule 201
        /// </summary>
        public const string SShortX = "SSHORTX";
    }
}
