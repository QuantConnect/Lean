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

namespace QuantConnect.Securities.Forex
{
    /// <summary>
    /// Forex specific caching support
    /// </summary>
    /// <remarks>Class is vitually empty and scheduled to be made obsolete. Potentially could be used for user data storage.</remarks>
    /// <seealso cref="SecurityCache"/>
    public class ForexCache : SecurityCache
    {
        /// <summary>
        /// Initialize forex cache
        /// </summary>
        public ForexCache()
            : base()
        {
            //Nothing to do:
        }
    } //End ForexCache Class
} //End Namespace