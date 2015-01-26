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

using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities.Equity 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Equity Security Type : Extension of the underlying Security class for equity specific behaviours.
    /// </summary>
    /// <seealso cref="Security"/>
    public class Equity : Security
    {
        /******************************************************** 
        * CLASS VARIABLES
        *********************************************************/
        
        /******************************************************** 
        * CONSTRUCTOR/DELEGATE DEFINITIONS
        *********************************************************/
        /// <summary>
        /// Construct the Equity Object
        /// </summary>
        public Equity(string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours, bool isDynamicallyLoadedData = false) :
            base(symbol, SecurityType.Equity, resolution, fillDataForward, leverage, extendedMarketHours, isDynamicallyLoadedData) 
        {
            //Holdings for new Vehicle:
            Cache = new EquityCache();
            Holdings = new EquityHolding(symbol, this.Model);
            Exchange = new EquityExchange();

            //Set the Equity Transaction Model
            Model = new EquityTransactionModel();
        }

        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Equity cache class for caching data, charting and orders.
        /// </summary>
        public new EquityCache Cache 
        {
            get { return (EquityCache)base.Cache; }
            set { base.Cache = value; }
        }

        /// <summary>
        /// Equity holdings class for managing cash, quantity held, portfolio
        /// </summary>
        public new EquityHolding Holdings 
        {
            get { return (EquityHolding)base.Holdings; }
            set { base.Holdings = value; }
        }

        /// <summary>
        /// Equity exchange class for manaing time open and close.
        /// </summary>
        public new EquityExchange Exchange 
        {
            get { return (EquityExchange)base.Exchange; }
            set { base.Exchange = value; }
        }

        /// <summary>
        /// Equity security transaction and fill models
        /// </summary>
        public new ISecurityTransactionModel Model 
        {
            get { return (EquityTransactionModel)base.Model; }
            set { base.Model = value; }
        }

        /******************************************************** 
        * CLASS METHODS
        *********************************************************/


    } // End Market

} // End QC Namespace
