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

using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Securities.Forex
{
    /********************************************************
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// FOREX Security Object Implementation for FOREX Assets
    /// </summary>
    /// <seealso cref="Security"/>
    public class Forex : Security
    {
        /********************************************************
        * CLASS VARIABLES
        *********************************************************/


        /********************************************************
        * CONSTRUCTOR/DELEGATE DEFINITIONS
        *********************************************************/
        /// <summary>
        /// Constructor for the forex security
        /// </summary>
        public Forex(string symbol, Resolution resolution, bool fillDataForward, decimal leverage, bool extendedMarketHours, bool isDynamicallyLoadedData = false) :
            base(symbol, SecurityType.Forex, resolution, fillDataForward, leverage, extendedMarketHours, isDynamicallyLoadedData)
        {
            //Holdings for new Vehicle:
            Cache = new ForexCache();
            Holdings = new ForexHolding(symbol, leverage, this.Model);
            Exchange = new ForexExchange();
            Model = new ForexTransactionModel();
        }

        /********************************************************
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Forex cache class for caching pricing data and charts
        /// </summary>
        public new ForexCache Cache
        {
            get { return (ForexCache)base.Cache; }
            set { base.Cache = value; }
        }

        /// <summary>
        /// Forex holdings class models the cash quantity held and portfolio
        /// </summary>
        public new ForexHolding Holdings
        {
            get { return (ForexHolding)base.Holdings; }
            set { base.Holdings = value; }
        }

        /// <summary>
        /// Forex exchange class monitors the open and close market times.
        /// </summary>
        public new ForexExchange Exchange
        {
            get { return (ForexExchange)base.Exchange; }
            set { base.Exchange = value; }
        }

        /// <summary>
        /// Forex security transaction and fill models
        /// </summary>
        public new ISecurityTransactionModel Model
        {
            get { return (ForexTransactionModel)base.Model; }
            set { base.Model = value; }
        }

        /********************************************************
        * CLASS METHODS
        *********************************************************/

    } // End Market

} // End QC Namespace
