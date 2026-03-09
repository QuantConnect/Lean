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

using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides useful infrastructure methods to the <see cref="Security"/> class.
    /// These are added in this way to avoid mudding the class's public API
    /// </summary>
    public static class SecurityExtensions
    {
        /// <summary>
        /// Determines if all subscriptions for the security are internal feeds
        /// </summary>
        public static bool IsInternalFeed(this Security security)
        {
            return security.Subscriptions.All(x => x.IsInternalFeed);
        }
    }
}
