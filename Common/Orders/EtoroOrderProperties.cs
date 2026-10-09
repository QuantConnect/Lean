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
 *
*/

using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Represents the properties of an order in eToro.
    /// </summary>
    public class EtoroOrderProperties : OrderProperties
    {
        /// <summary>
        /// The absolute price at which eToro closes the position when the market moves against it.
        /// eToro requires it for a short sale and for any position with leverage above 1;
        /// Lean sends every order at leverage 1, so it is needed for short sales only.
        /// </summary>
        public decimal? StopLossRate { get; set; }

        /// <summary>
        /// Returns a new instance clone of this object
        /// </summary>
        public override IOrderProperties Clone()
        {
            return (EtoroOrderProperties)MemberwiseClone();
        }
    }
}
