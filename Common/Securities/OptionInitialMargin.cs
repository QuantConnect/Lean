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

using System;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Result type for <see cref="Option.OptionStrategyPositionGroupBuyingPowerModel.GetInitialMarginRequirement"/>
    /// </summary>
    public class OptionInitialMargin : InitialMargin
    {
        /// <summary>
        /// Gets an instance of <see cref="OptionInitialMargin"/> with zero values
        /// </summary>
        public static OptionInitialMargin Zero { get; } = new OptionInitialMargin(0m, 0m);

        /// <summary>
        /// The option/strategy premium value in account currency
        /// </summary>
        public decimal Premium { get; }

        /// <summary>
        /// The initial margin value in account currency, not including the premium in cases that apply (premium debited)
        /// </summary>
        public decimal ValueWithoutPremium { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionInitialMargin"/> class
        /// </summary>
        /// <param name="value">The initial margin</param>
        /// <param name="premium">The premium of the option/option strategy</param>
        public OptionInitialMargin(decimal value, decimal premium)
            : base(value + Math.Max(premium, 0))
        {
            Premium = premium;
            ValueWithoutPremium = value;
        }
    }
}
