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
namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// Contains data used for authentication
    /// </summary>
    public class AuthenticationToken
    {
        /// <summary>
        /// The key
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The hashed signature
        /// </summary>
        public string Signature { get; set; }
        /// <summary>
        /// The timestamp
        /// </summary>
        public string Timestamp { get; set; }
        /// <summary>
        /// The pass phrase
        /// </summary>
        public string Passphrase { get; set; }
    }
}