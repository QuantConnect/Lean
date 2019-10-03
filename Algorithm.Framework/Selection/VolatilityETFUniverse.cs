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

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Universe Selection Model that adds the following Volatility ETFs at their inception date
    /// 2010-02-11   SQQQ   ProShares UltraPro ShortQQQ
    /// 2010-02-11   TQQQ   ProShares UltraProQQQ
    /// 2010-11-30   TVIX   VelocityShares Daily 2x VIX Short Term ETN
    /// 2011-01-04   VIXY   ProShares VIX Short-Term Futures ETF
    /// 2011-05-05   SPLV   Invesco S&P 500® Low Volatility ETF
    /// 2011-10-04   SVXY   ProShares Short VIX Short-Term Futures
    /// 2011-10-04   UVXY   ProShares Ultra VIX Short-Term Futures
    /// 2011-10-20   EEMV   iShares Edge MSCI Min Vol Emerging Markets ETF
    /// 2011-10-20   EFAV   iShares Edge MSCI Min Vol EAFE ETF
    /// 2011-10-20   USMV   iShares Edge MSCI Min Vol USA ETF
    /// </summary>
    public class VolatilityETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the VolatilityETFUniverse class
        /// </summary>
        public VolatilityETFUniverse() :
            base(
                "qc-volatility-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    {"SQQQ", new DateTime(2010, 2, 11)},
                    {"TQQQ", new DateTime(2010, 2, 11)},
                    {"TVIX", new DateTime(2010, 11, 30)},
                    {"VIXY", new DateTime(2011, 1, 4)},
                    {"SPLV", new DateTime(2011, 5, 5)},
                    {"SVXY", new DateTime(2011, 10, 4)},
                    {"UVXY", new DateTime(2011, 10, 4)},
                    {"EEMV", new DateTime(2011, 10, 20)},
                    {"EFAV", new DateTime(2011, 10, 20)},
                    {"USMV", new DateTime(2011, 10, 20)}
                }
            )
        {

        }
    }
}