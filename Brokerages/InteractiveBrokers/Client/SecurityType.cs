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
    /// Contract Security Types
    /// </summary>
    public static class SecurityType
    {
        /// <summary>
        /// Stock
        /// </summary>
        public const string Stock = "STK";

        /// <summary>
        /// Option
        /// </summary>
        public const string Option = "OPT";

        /// <summary>
        /// Future
        /// </summary>
        public const string Future = "FUT";

        /// <summary>
        /// Index
        /// </summary>
        public const string Index = "IND";

        /// <summary>
        /// FOP = options on futures
        /// </summary>
        public const string FutureOption = "FOP";

        /// <summary>
        /// Cash
        /// </summary>
        public const string Cash = "CASH";

        /// <summary>
        /// For Combination Orders - must use combo leg details
        /// </summary>
        public const string Bag = "BAG";

        /// <summary>
        /// Bond
        /// </summary>
        public const string Bond = "BOND";

        /// <summary>
        /// Warrant
        /// </summary>
        public const string Warrant = "WAR";

        /// <summary>
        /// Commodity
        /// </summary>
        public const string Commodity = "CMDTY";

        /// <summary>
        /// Bill
        /// </summary>
        public const string Bill = "BILL";

        /// <summary>
        /// Contract For Difference
        /// </summary>
        public const string ContractForDifference = "CFD";

        /// <summary>
        /// Undefined Security Type
        /// </summary>
        public const string Undefined = "";
    }
}
