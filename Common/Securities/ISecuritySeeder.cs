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

using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Used to seed the security with the correct price
    /// </summary>
    public interface ISecuritySeeder
    {
        /// <summary>
        /// Get the last data point using the seed function
        /// </summary>
        /// <param name="security"><see cref="Security"/> being seeded</param>
        /// <returns><see cref="BaseData"/> representing the last known data of the security</returns>
        BaseData GetSeedData(Security security);
    }
}
