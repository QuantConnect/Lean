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

using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Contains additional properties and settings for an order submitted to Interactive Brokers
    /// </summary>
    public class InteractiveBrokersOrderProperties : OrderProperties
    {
        /// <summary>
        /// The linked account for which to submit the order (only used by Financial Advisors)
        /// </summary>
        /// <remarks>Mutually exclusive with FaProfile and FaGroup</remarks>
        public string Account { get; set; }

        /// <summary>
        /// The account group for the order (only used by Financial Advisors)
        /// </summary>
        /// <remarks>Mutually exclusive with FaProfile and Account</remarks>
        public string FaGroup { get; set; }

        /// <summary>
        /// The allocation method for the account group order (only used by Financial Advisors)
        /// Supported allocation methods are: Equal, NetLiq, AvailableEquity, PctChange
        /// </summary>
        public string FaMethod { get; set; }

        /// <summary>
        /// The percentage for the percent change method (only used by Financial Advisors)
        /// </summary>
        public int FaPercentage { get; set; }

        /// <summary>
        /// The allocation profile to be used for the order (only used by Financial Advisors)
        /// </summary>
        /// <remarks>Mutually exclusive with FaGroup and Account</remarks>
        public string FaProfile { get; set; }

        /// <summary>
        /// If set to true, allows orders to also trigger or fill outside of regular trading hours.
        /// </summary>
        public bool OutsideRegularTradingHours { get; set; }

        /// <summary>
        /// Returns a new instance clone of this object
        /// </summary>
        public override IOrderProperties Clone()
        {
            return (InteractiveBrokersOrderProperties)MemberwiseClone();
        }
    }
}
