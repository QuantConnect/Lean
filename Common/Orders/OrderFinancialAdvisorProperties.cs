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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Represents optional order properties used by financial advisors.
    /// These properties are currently used only by the Interactive Brokers brokerage.
    /// </summary>
    public class OrderFinancialAdvisorProperties
    {
        /// <summary>
        /// The linked account for which to submit the order
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// The account group for the order
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// The method for the account group order
        /// Supported methods are: EqualQuantity, NetLiq, AvailableEquity, PctChange
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// The percentage for the percent change method
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// The allocation profile to be used for the order
        /// </summary>
        public string Profile { get; set; }
    }
}
