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

namespace QuantConnect.Securities.Equity 
{
    /// <summary>
    /// Equity security type data filter 
    /// </summary>
    /// <seealso cref="SecurityDataFilter"/>
    public class EquityDataFilter : SecurityDataFilter
    {
        /// <summary>
        /// Initialize Data Filter Class:
        /// </summary>
        public EquityDataFilter() : base()
        {

        }

        /// <summary>
        /// Equity filter the data: true - accept, false - fail.
        /// </summary>
        /// <param name="data">Data class</param>
        /// <param name="vehicle">Security asset</param>
        public override bool Filter(Security vehicle, BaseData data)
        {
            // No data filter for bad ticks. All raw data will be piped into algorithm
            return true;
        }

    } //End Filter

} //End Namespace