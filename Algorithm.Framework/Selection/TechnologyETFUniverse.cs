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
    /// Universe Selection Model that adds the following Technology ETFs at their inception date
    /// 1998-12-22   XLK    Technology Select Sector SPDR Fund
    /// 1999-03-10   QQQ    Invesco QQQ
    /// 2001-07-13   SOXX   iShares PHLX Semiconductor ETF
    /// 2001-07-13   IGV    iShares Expanded Tech-Software Sector ETF
    /// 2004-01-30   VGT    Vanguard Information Technology ETF
    /// 2006-04-25   QTEC   First Trust NASDAQ 100 Technology
    /// 2006-06-23   FDN    First Trust Dow Jones Internet Index
    /// 2007-05-10   FXL    First Trust Technology AlphaDEX Fund
    /// 2008-12-17   TECL   Direxion Daily Technology Bull 3X Shares
    /// 2008-12-17   TECS   Direxion Daily Technology Bear 3X Shares
    /// 2010-03-11   SOXL   Direxion Daily Semiconductor Bull 3x Shares
    /// 2010-03-11   SOXS   Direxion Daily Semiconductor Bear 3x Shares
    /// 2011-07-06   SKYY   First Trust ISE Cloud Computing Index Fund
    /// 2011-12-21   SMH    VanEck Vectors Semiconductor ETF
    /// 2013-08-01   KWEB   KraneShares CSI China Internet ETF
    /// 2013-10-24   FTEC   Fidelity MSCI Information Technology Index ETF
    /// </summary>
    public class TechnologyETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the TechnologyETFUniverse class
        /// </summary>
        public TechnologyETFUniverse() :
            base(
                "qc-technology-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    {"XLK", new DateTime(1998, 12, 22)},
                    {"QQQ", new DateTime(1999, 3, 10)},
                    {"SOXX", new DateTime(2001, 7, 13)},
                    {"IGV", new DateTime(2001, 7, 13)},
                    {"VGT", new DateTime(2004, 1, 30)},
                    {"QTEC", new DateTime(2006, 4, 25)},
                    {"FDN", new DateTime(2006, 6, 23)},
                    {"FXL", new DateTime(2007, 5, 10)},
                    {"TECL", new DateTime(2008, 12, 17)},
                    {"TECS", new DateTime(2008, 12, 17)},
                    {"SOXL", new DateTime(2010, 3, 11)},
                    {"SOXS", new DateTime(2010, 3, 11)},
                    {"SKYY", new DateTime(2011, 7, 6)},
                    {"SMH", new DateTime(2011, 12, 21)},
                    {"KWEB", new DateTime(2013, 8, 1)},
                    {"FTEC", new DateTime(2013, 10, 24)}
                }
            )
        {

        }
    }
}