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
    /// Option Right Type (Put or Call)
    /// </summary>
    public static class RightType
    {
        /// <summary>
        /// Option type is a Put (Right to sell)
        /// </summary>
        public const string Put = "P";

        /// <summary>
        /// Option type is a Call (Right to buy)
        /// </summary>
        public const string Call = "C";

        /// <summary>
        /// Option type is not defined (contract is not an option).
        /// </summary>
        public const string Undefined = "";
    }
}
