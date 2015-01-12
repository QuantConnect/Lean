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
 *	TRADIER BROKERAGE MODEL
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Brokerage interface - store common objects and properties which are common across all brokerages.
    /// </summary>
    public interface IBrokerage
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Brokerage Name:
        /// </summary>
        string Name
        {
            get;
        }

        /******************************************************** 
        * INTERFACE METHODS
        *********************************************************/
        /// <summary>
        /// Add an error handler for the specific brokerage error.
        /// </summary>
        /// <param name="key">String error name</param>
        /// <param name="callback">Action call back</param>
        void AddErrorHander(string key, Action callback);

        /// <summary>
        /// Refresh brokerage login session where applicable.
        /// </summary>
        bool RefreshSession();
    }
}
