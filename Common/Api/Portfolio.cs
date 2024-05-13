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

using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Api
{
    /// <summary>
    /// Class containing the basic portfolio information of a live algorithm
    /// </summary>
    public class Portfolio
    {
        /// <summary>
        /// Dictionary of algorithm holdings information
        /// </summary>
        public Dictionary<string, Holding> Holdings { get; set; }

        /// <summary>
        /// Dictionary of algorithm cash currencies information
        /// </summary>
        public Dictionary<string, Cash> Cash { get; set; }
    }

    /// <summary>
    /// Response class for reading the portfolio of a live algorithm
    /// </summary>
    public class PortfolioResponse : RestResponse
    {
        /// <summary>
        /// Object containing the basic portfolio information of a live algorithm
        /// </summary>
        public Portfolio Portfolio { get; set; }
    }
}
