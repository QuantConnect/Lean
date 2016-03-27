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

namespace QuantConnect.Securities 
{
    /// <summary>
    /// Base class caching caching spot for security data and any other temporary properties.
    /// </summary>
    /// <remarks>
    /// This class is virtually unused and will soon be made obsolete. 
    /// This comment made in a remark to prevent obsolete errors in all users algorithms
    /// </remarks>
    public class SecurityCache 
    { 
        // Last data for this security
        private BaseData _lastData;

        /// <summary>
        /// Create a new cache for this security
        /// </summary>
        public SecurityCache() 
        { }

        /// <summary>
        /// Add a new market data point to the local security cache for the current market price.
        /// </summary>
        public virtual void AddData(BaseData data)
        {
            //Record as Last Added Packet:
            if (data != null) _lastData = data;
        }

        /// <summary>
        /// Get last data packet recieved for this security
        /// </summary>
        /// <returns>BaseData type of the security</returns>
        public virtual BaseData GetData()
        {
            return _lastData;
        }

        /// <summary>
        /// Reset cache storage and free memory
        /// </summary>
        public virtual void Reset()
        {
            _lastData = null;
        }
    } //End Cache

} //End Namespace