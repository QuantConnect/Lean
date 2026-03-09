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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Used to seed the security with the correct price
    /// </summary>
    public interface ISecuritySeeder
    {
        /// <summary>
        /// Seed the security
        /// </summary>
        /// <param name="security"><see cref="Security"/> being seeded</param>
        /// <returns>true if the security was seeded, false otherwise</returns>
        bool SeedSecurity(Security security);
    }

    /// <summary>
    /// Provides access to a null implementation for <see cref="ISecuritySeeder"/>
    /// </summary>
    public static class SecuritySeeder
    {
        /// <summary>
        /// Gets an instance of <see cref="ISecuritySeeder"/> that is a no-op
        /// </summary>
        public static readonly ISecuritySeeder Null = new NullSecuritySeeder();

        private sealed class NullSecuritySeeder : ISecuritySeeder
        {
            public bool SeedSecurity(Security security)
            {
                return true;
            }
        }
    }
}
