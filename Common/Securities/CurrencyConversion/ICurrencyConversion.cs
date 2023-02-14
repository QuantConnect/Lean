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

using System;
using System.Collections.Generic;

namespace QuantConnect.Securities.CurrencyConversion
{
    /// <summary>
    /// Represents a type capable of calculating the conversion rate between two currencies
    /// </summary>
    public interface ICurrencyConversion
    {
        /// <summary>
        /// Event fired when the conversion rate is updated
        /// </summary>
        event EventHandler<decimal> ConversionRateUpdated;

        /// <summary>
        /// The currency this conversion converts from
        /// </summary>
        string SourceCurrency { get; }

        /// <summary>
        /// The currency this conversion converts to
        /// </summary>
        string DestinationCurrency { get; }

        /// <summary>
        /// The current conversion rate between <see cref="SourceCurrency"/> and <see cref="DestinationCurrency"/>
        /// </summary>
        decimal ConversionRate { get; set; }

        /// <summary>
        /// The securities which the conversion rate is based on
        /// </summary>
        IEnumerable<Security> ConversionRateSecurities { get; }

        /// <summary>
        /// Updates the internal conversion rate based on the latest data, and returns the new conversion rate
        /// </summary>
        /// <returns>The new conversion rate</returns>
        void Update();
    }
}
