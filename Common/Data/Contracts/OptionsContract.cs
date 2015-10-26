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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    /// <summary>
    /// Options Contract Class: 
    /// Implements specific characteritcs of an option contract 
    /// </summary>
    public class OptionsContract : BaseContract
    {
        // In Base Contract classe:
        // public SecurityType Underlying;
        // public DateTime Expiration;

        /// <summary>
        /// Specifies the right (buy or sell) of an option buyer/holder  
        /// </summary>
        public OptionRight Right { get; set; }

        /// <summary>
        /// Specifies class into which the option falls, usually defined by the dates on which the option may be exercised 
        /// </summary>
        public OptionStyle Style { get; set; }

        /// <summary>
        /// Strike: price that the option owner has right, but not the obligation, to buy/sell  
        /// </summary>
        public decimal Strike { get; set; }

        /// <summary>
        /// Constructor for initialising the options contract class
        /// </summary>
        public OptionsContract()
            : base()
        {
            // Default will be a call option contract
            Right = OptionRight.Call;

            // Default will be a call option contract
            Style = OptionStyle.American;

            Strike = 0m;
        }

        /// <summary>
        /// Constructor for initialising the options contract class
        /// </summary>
        public OptionsContract(SecurityType underlyingtype, string underlyingsymbol, DateTime expiration, OptionRight right, OptionStyle style, decimal strike)
            : base(underlyingtype, underlyingsymbol, expiration)
        {
            Right = right;
            Style = style;
            Strike = strike;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}-Option: {2} on {3:yyyyMMdd} at {4}", UnderlyingSymbol, Style, Right, Expiration, Strike);
        }
    }
}