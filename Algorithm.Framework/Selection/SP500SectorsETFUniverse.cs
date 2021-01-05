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
    /// Universe Selection Model that adds the following SP500 Sectors ETFs at their inception date
    /// 1998-12-22   XLB   Materials Select Sector SPDR ETF
    /// 1998-12-22   XLE   Energy Select Sector SPDR Fund
    /// 1998-12-22   XLF   Financial Select Sector SPDR Fund
    /// 1998-12-22   XLI   Industrial Select Sector SPDR Fund
    /// 1998-12-22   XLK   Technology Select Sector SPDR Fund
    /// 1998-12-22   XLP   Consumer Staples Select Sector SPDR Fund
    /// 1998-12-22   XLU   Utilities Select Sector SPDR Fund
    /// 1998-12-22   XLV   Health Care Select Sector SPDR Fund
    /// 1998-12-22   XLY   Consumer Discretionary Select Sector SPDR Fund
    /// </summary>
    public class SP500SectorsETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the SP500SectorsETFUniverse class
        /// </summary>
        public SP500SectorsETFUniverse() :
            base(
                "qc-sp500-sectors-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    {"XLB", new DateTime(1998, 12, 22)},
                    {"XLE", new DateTime(1998, 12, 22)},
                    {"XLF", new DateTime(1998, 12, 22)},
                    {"XLI", new DateTime(1998, 12, 22)},
                    {"XLK", new DateTime(1998, 12, 22)},
                    {"XLP", new DateTime(1998, 12, 22)},
                    {"XLU", new DateTime(1998, 12, 22)},
                    {"XLV", new DateTime(1998, 12, 22)},
                    {"XLY", new DateTime(1998, 12, 22)}
                }
            )
        {

        }
    }
}