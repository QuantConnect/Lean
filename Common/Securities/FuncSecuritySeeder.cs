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
using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Seed a security price from a history function
    /// </summary>
    public class FuncSecuritySeeder : ISecuritySeeder
    {
        private readonly Func<Security, BaseData> _seedFunction;

        /// <summary>
        /// Constructor that takes as a parameter the security used to seed the price
        /// </summary>
        /// <param name="seedFunction"></param>
        public FuncSecuritySeeder(Func<Security, BaseData> seedFunction)
        {
            _seedFunction = seedFunction;
        }

        /// <summary>
        /// Seed the security
        /// </summary>
        /// <param name="security"><see cref="Security"/> being seeded</param>
        /// <returns>true if the security was seeded, false otherwise</returns>
        public bool SeedSecurity(Security security)
        {
            try
            {
                // Do not seed canonical symbols
                if (!security.Symbol.IsCanonical())
                {
                    var seedData = _seedFunction(security);
                    if (seedData != null)
                    {
                        security.SetMarketPrice(seedData);
                        Log.Debug($"FuncSecuritySeeder.SeedSecurity(): Seeded security: {seedData.Symbol.Value}: {seedData.Value}");
                    }
                    else
                    {
                        Log.Trace($"FuncSecuritySeeder.SeedSecurity(): Unable to seed security: {security.Symbol.Value}");
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Trace($"FuncSecuritySeeder.SeedSecurity(): Could not seed price for security {security.Symbol}: {exception}");
                return false;
            }

            return true;
        }
    }
}
