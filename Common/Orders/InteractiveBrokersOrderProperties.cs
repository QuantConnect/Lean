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
        /// <remarks>
        /// Mutually exclusive with FaProfile and Account. When unified Financial Advisor groups are enabled and the
        /// group uses its saved ContractsOrShares allocation method, saved child values may be fractional, but their
        /// total must be lot-aligned; for a lot size of one, 12.5 + 7.5 = 20 is valid.
        /// </remarks>
        public string FaGroup { get; set; }

        /// <summary>
        /// The allocation-method override for the account group order (only used by Financial Advisors).
        /// </summary>
        /// <remarks>
        /// When <see cref="FaGroup"/> is explicitly set, supported values remain order overrides and are validated
        /// against the saved group configuration whenever a Ready snapshot is available. Leave this field empty to
        /// use the group's saved allocation method; this requires a Ready brokerage account snapshot containing the
        /// group. PctChange is not supported by unified Financial Advisor groups. When unified groups are disabled,
        /// the legacy integer <see cref="FaPercentage"/> route remains available for PctChange.
        /// </remarks>
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
