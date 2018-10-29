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

using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities.Volatility;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a model that computes the volatility of a security
    /// </summary>
    /// <remarks>Please use<see cref="BaseVolatilityModel"/> as the base class for
    /// any implementations of<see cref="IVolatilityModel"/></remarks>
    public interface IVolatilityModel
    {
        /// <summary>
        /// Gets the volatility of the security as a percentage
        /// </summary>
        decimal Volatility { get; }

        /// <summary>
        /// Updates this model using the new price information in
        /// the specified security instance
        /// </summary>
        /// <param name="security">The security to calculate volatility for</param>
        /// <param name="data">The new data used to update the model</param>
        void Update(Security security, BaseData data);

        /// <summary>
        /// Returns history requirements for the volatility model expressed in the form of history request
        /// </summary>
        /// <param name="security">The security of the request</param>
        /// <param name="utcTime">The date/time of the request</param>
        /// <returns>History request object list, or empty if no requirements</returns>
        IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime);
    }

    /// <summary>
    /// Provides access to a null implementation for <see cref="IVolatilityModel"/>
    /// </summary>
    public static class VolatilityModel
    {
        /// <summary>
        /// Gets an instance of <see cref="IVolatilityModel"/> that will always
        /// return 0 for its volatility and does nothing during Update.
        /// </summary>
        public static readonly IVolatilityModel Null = new NullVolatilityModel();

        private sealed class NullVolatilityModel : IVolatilityModel
        {
            public decimal Volatility { get; private set; }

            public void Update(Security security, BaseData data) { }

            public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime) { return Enumerable.Empty<HistoryRequest>(); }
        }
    }
}