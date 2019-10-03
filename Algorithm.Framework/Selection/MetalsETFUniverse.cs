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
    /// Universe Selection Model that adds the following Metals ETFs at their inception date
    /// 2004-11-18   GLD    SPDR Gold Trust
    /// 2005-01-28   IAU    iShares Gold Trust
    /// 2006-04-28   SLV    iShares Silver Trust
    /// 2006-05-22   GDX    VanEck Vectors Gold Miners ETF
    /// 2008-12-04   AGQ    ProShares Ultra Silver
    /// 2009-11-11   GDXJ   VanEck Vectors Junior Gold Miners ETF
    /// 2010-01-08   PPLT   Aberdeen Standard Platinum Shares ETF
    /// 2010-12-08   NUGT   Direxion Daily Gold Miners Bull 3X Shares
    /// 2010-12-08   DUST   Direxion Daily Gold Miners Bear 3X Shares
    /// 2011-10-17   USLV   VelocityShares 3x Long Silver ETN
    /// 2011-10-17   UGLD   VelocityShares 3x Long Gold ETN
    /// 2013-10-03   JNUG   Direxion Daily Junior Gold Miners Index Bull 3x Shares
    /// 2013-10-03   JDST   Direxion Daily Junior Gold Miners Index Bear 3X Shares
    /// </summary>
    public class MetalsETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the MetalsETFUniverse class
        /// </summary>
        public MetalsETFUniverse() :
            base(
                "qc-metals-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    {"GLD", new DateTime(2004, 11, 18)},
                    {"IAU", new DateTime(2005, 1, 28)},
                    {"SLV", new DateTime(2006, 4, 28)},
                    {"GDX", new DateTime(2006, 5, 22)},
                    {"AGQ", new DateTime(2008, 12, 4)},
                    {"GDXJ", new DateTime(2009, 11, 11)},
                    {"PPLT", new DateTime(2010, 1, 8)},
                    {"NUGT", new DateTime(2010, 12, 8)},
                    {"DUST", new DateTime(2010, 12, 8)},
                    {"USLV", new DateTime(2011, 10, 17)},
                    {"UGLD", new DateTime(2011, 10, 17)},
                    {"JNUG", new DateTime(2013, 10, 3)},
                    {"JDST", new DateTime(2013, 10, 3)}
                }
            )
        {

        }
    }
}