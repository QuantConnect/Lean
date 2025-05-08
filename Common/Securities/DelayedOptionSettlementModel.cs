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

using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the model responsible for applying cash settlement rules in options
    /// </summary>
    /// <remarks>This model applies cash settlement after T+N days</remarks>
    public class DelayedOptionSettlementModel : DelayedSettlementModel
    {
        /// <summary>
        /// Dictionary of changes in settlement days in USA. An entry in a market dictionary
        /// (d, k) means that from the date d until the next date in the dictionary, the settlement
        /// days were k
        /// </summary>
        public static Dictionary<DateTime, int> DefaultOptionSettlementPerDate = new()
        {
            { DateTime.MinValue, Option.Option.DefaultSettlementDays},
        };

        /// <summary>
        /// Get dictionary of changes in settlement days in USA equity markets
        /// </summary>
        public override Dictionary<DateTime, int> GetDefaultSettlementPerDate => DefaultOptionSettlementPerDate;

        /// <summary>
        /// Dictionary of changes in settlement days in the markets across the world. An entry
        /// in a market dictionary (d, k) means that from the date d until the next date in the
        /// dictionary, the settlement days were k
        /// </summary>
        public static Dictionary<DateTime, int> InternationalOptionSettlementPerDate = new()
        {
            { DateTime.MinValue, Option.Option.DefaultSettlementDays}
        };

        /// <summary>
        /// Get dictionary of changes in settlement days in option markets across the world
        /// </summary>
        public override Dictionary<DateTime, int> GetInternationalSettlementPerDate => InternationalOptionSettlementPerDate;

        /// <summary>
        /// Creates an instance of the <see cref="DelayedSettlementModel"/> class
        /// </summary>
        /// <param name="numberOfDays">The number of days required for settlement</param>
        /// <param name="timeOfDay">The time of day used for settlement</param>
        public DelayedOptionSettlementModel(int numberOfDays, TimeSpan timeOfDay) : base(numberOfDays, timeOfDay)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="DelayedSettlementModel"/> class
        /// </summary>
        /// <param name="isUSAMarket">True to use default USA settlement days. False to use default international settlement days</param>
        /// <param name="timeOfDay">The time of day used for settlement</param>
        public DelayedOptionSettlementModel(bool isUSAMarket, TimeSpan timeOfDay) : base(isUSAMarket, timeOfDay)
        {
        }
    }
}
