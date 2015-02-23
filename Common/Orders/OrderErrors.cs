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

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /******************************************************** 
    * ORDER EVENT CLASS DEFINITION
    *********************************************************/
    /// <summary>
    /// Indexed order error codes:
    /// </summary>
    public static class OrderErrors
    {
        /// <summary>
        /// Order validation error codes
        /// </summary>
        public static Dictionary<int, string> ErrorTypes = new Dictionary<int, string>() 
        {
            {-1, "Order quantity must not be zero"},
            {-2, "There is no data yet for this security - please wait for data (market order price not available yet)"},
            {-3, "Attempting market order outside of market hours"},
            {-4, "Insufficient capital to execute order"},
            {-5, "Exceeded maximum allowed orders for one analysis period"},
            {-6, "Order timestamp error. Order appears to be executing in the future"},
            {-7, "General error in order"},
            {-8, "Order has already been filled and cannot be modified"},
        };
    }

} // End QC Namespace:
