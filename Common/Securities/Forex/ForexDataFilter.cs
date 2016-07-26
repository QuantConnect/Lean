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

namespace QuantConnect.Securities.Forex 
{
    /// <summary>
    /// Forex packet by packet data filtering mechanism for dynamically detecting bad ticks.
    /// </summary>
    /// <seealso cref="SecurityDataFilter"/>
    public class ForexDataFilter : SecurityDataFilter
    {
        /// <summary>
        /// Initialize forex data filter class:
        /// </summary>
        public ForexDataFilter()
            : base() 
        {
            
        }

        /// <summary>
        /// Forex data filter: a true value means accept the packet, a false means fail.
        /// </summary>
        /// <param name="data">Data object we're scanning to filter</param>
        /// <param name="vehicle">Security asset</param>
        public override bool Filter(Security vehicle, BaseData data)
        {
            //FX data is from FXCM and fairly clean already. Accept all packets.
            return true;
        }

    } //End Filter

} //End Namespace