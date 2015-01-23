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
 *	TRADIER BROKERAGE CONTROLLER
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages.Paper
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Paper Trading Brokerage
    /// </summary>
    public class PaperBrokerage : IBrokerage
    {
        /// <summary>
        /// Name of our brokerage
        /// </summary>
        public string Name
        {
            get { return "Paper-Trading Brokerage"; }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Add an error handler for the specific brokerage error.
        /// </summary>
        /// <param name="key">Key for the error name.</param>
        /// <param name="callback">Callback for the error actions.</param>
        public void AddErrorHander(string key, Action callback)
        {
            // No error handlers//no errors from paper trading brokerage inc.
        }

        /// <summary>
        /// Refresh the login session with the brokerage.
        /// </summary>
        public bool RefreshSession()
        {
            //No need to refresh session.
            return true;
        }

    } // End of Paper Brokerage:

}
