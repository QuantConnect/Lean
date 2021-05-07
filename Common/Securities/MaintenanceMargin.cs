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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Result type for <see cref="IBuyingPowerModel.GetMaintenanceMargin"/>
    /// </summary>
    public class MaintenanceMargin
    {
        /// <summary>
        /// Gets an instance of <see cref="MaintenanceMargin"/> with zero values.
        /// </summary>
        public static MaintenanceMargin Zero { get; } = new MaintenanceMargin(0m);

        /// <summary>
        /// The maintenance margin value in account currency
        /// </summary>
        public decimal Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaintenanceMargin"/> class
        /// </summary>
        /// <param name="value">The maintenance margin</param>
        public MaintenanceMargin(decimal value)
        {
            Value = value;
        }

        /// <summary>
        /// Implicit operator <see cref="MaintenanceMargin"/> -> <see cref="decimal"/>
        /// </summary>
        public static implicit operator decimal(MaintenanceMargin margin)
        {
            return margin.Value;
        }

        /// <summary>
        /// Implicit operator <see cref="decimal"/> -> <see cref="MaintenanceMargin"/>
        /// </summary>
        public static implicit operator MaintenanceMargin(decimal margin)
        {
            return new MaintenanceMargin(margin);
        }
    }
}
