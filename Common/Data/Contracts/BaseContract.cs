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

namespace QuantConnect.Data
{
    /// <summary>
    /// Base Contract Class: Underlying and expiration 
    /// </summary>
    public class BaseContract : IBaseContract
    {
        /// <summary>
        /// Security Type of the underlying  
        /// </summary>
        public SecurityType UnderlyingType { get; set; }

        /// <summary>
        /// Symbol of the underlying  
        /// </summary>
        public string UnderlyingSymbol { get; set; }

        /// <summary>
        /// DateTime of contract expiration  
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Constructor for initialising the base contract class
        /// </summary>
        public BaseContract()
        {
            UnderlyingType = SecurityType.Base;
            UnderlyingSymbol = Symbol.Empty;
            Expiration = new DateTime();
        }

        /// <summary>
        /// Constructor for initialising the base contract class
        /// </summary>
        /// <param name="underlying">Security Type of the underlying</param>
        /// <param name="expiration">DateTime of contract expiration</param>
        public BaseContract(SecurityType underlyingtype, string underlyingsymbol, DateTime expiration)
        {
            UnderlyingType = underlyingtype;
            UnderlyingSymbol = underlyingsymbol;
            Expiration = expiration;
        }
    }
}