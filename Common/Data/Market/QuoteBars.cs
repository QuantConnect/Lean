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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Collection of <see cref="QuoteBar"/> keyed by symbol
    /// </summary>
    public class QuoteBars : DataDictionary<QuoteBar>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="QuoteBars"/> dictionary
        /// </summary>
        public QuoteBars()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="QuoteBars"/> dictionary
        /// </summary>
        public QuoteBars(DateTime time)
            : base(time)
        {
        }

        /// <summary>
        /// Collapses QuoteBars into TradeBars object when
        ///  algorithm requires FX data, but calls OnData(<see cref="TradeBars"/>)
        /// TODO: (2017) Remove this method in favor of using OnData(<see cref="Slice"/>)
        /// </summary>
        /// <returns><see cref="TradeBars"/></returns>
        [Obsolete("For backwards compatibility only.  When FX data is traded, all algorithms should use OnData(Slice)")]
        public TradeBars Collapse()
        {
            var tradeBars = new TradeBars();

            foreach (var kvp in this)
            {
                tradeBars.Add(kvp.Key, new TradeBar(kvp.Value.Time,
                                                    kvp.Key,
                                                    kvp.Value.Open,
                                                    kvp.Value.High,
                                                    kvp.Value.Low,
                                                    kvp.Value.Close,
                                                    0));
            }

            return tradeBars;
        }
    }
}