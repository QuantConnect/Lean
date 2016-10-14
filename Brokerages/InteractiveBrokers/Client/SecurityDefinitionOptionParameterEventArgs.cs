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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.SecurityDefinitionOptionParameter"/> event
    /// </summary>
    public class SecurityDefinitionOptionParameterEventArgs : EventArgs
    {
        /// <summary>
        /// ID of the request initiating the callback
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Exchange { get; private set; }

        /// <summary>
        /// The conID of the underlying security
        /// </summary>
        public int UnderlyingConId { get; private set; }

        /// <summary>
        /// The option trading class
        /// </summary>
        public string TradingClass { get; private set; }

        /// <summary>
        /// The option multiplier
        /// </summary>
        public string Multiplier { get; private set; }

        /// <summary>
        /// A list of the expiries for the options of this underlying on this exchange
        /// </summary>
        public HashSet<string> Expirations { get; private set; }

        /// <summary>
        /// A list of the possible strikes for options of this underlying on this exchange
        /// </summary>
        public HashSet<double> Strikes { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityDefinitionOptionParameterEventArgs"/> class
        /// </summary>
        public SecurityDefinitionOptionParameterEventArgs(int reqId, string exchange, int underlyingConId, string tradingClass,
            string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            RequestId = reqId;
            Exchange = exchange;
            UnderlyingConId = underlyingConId;
            TradingClass = tradingClass;
            Multiplier = multiplier;
            Expirations = expirations;
            Strikes = strikes;
        }
    }
}