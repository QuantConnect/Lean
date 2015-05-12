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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm
{
	/// <summary>
	/// Provides static properties to be used as selectors with the indicator system
	/// </summary>
    public static class Field
    {
        /// <summary>
        /// Gets a selector that selects the Open value
        /// </summary>
        public static Func<BaseData, decimal> Open
        {
            get { return TradeBarPropertyOrValue(x => x.Open); }
        }

        /// <summary>
        /// Gets a selector that selects the High value
        /// </summary>
        public static Func<BaseData, decimal> High
        {
            get { return TradeBarPropertyOrValue(x => x.High); }
        }

        /// <summary>
        /// Gets a selector that selects the Low value
        /// </summary>
        public static Func<BaseData, decimal> Low
        {
            get { return TradeBarPropertyOrValue(x => x.Low); }
        }

        /// <summary>
        /// Gets a selector that selects the Close value
        /// </summary>
        public static Func<BaseData, decimal> Close
        {
            get { return x => x.Value; }
        }

        private static Func<BaseData, decimal> TradeBarPropertyOrValue(Func<TradeBar, decimal> selector)
        {
            return x =>
            {
                var bar = x as TradeBar;
                return bar != null ? selector(bar) : x.Value;
            };
        }
    }
}
