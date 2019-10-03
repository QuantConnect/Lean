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
    /// Universe Selection Model that adds the following US Treasuries ETFs at their inception date
    /// 2002-07-26   IEF    iShares 7-10 Year Treasury Bond ETF
    /// 2002-07-26   SHY    iShares 1-3 Year Treasury Bond ETF
    /// 2002-07-26   TLT    iShares 20+ Year Treasury Bond ETF
    /// 2007-01-11   SHV    iShares Short Treasury Bond ETF
    /// 2007-01-11   IEI    iShares 3-7 Year Treasury Bond ETF
    /// 2007-01-11   TLH    iShares 10-20 Year Treasury Bond ETF
    /// 2007-12-10   EDV    Vanguard Ext Duration Treasury ETF
    /// 2007-05-30   BIL    SPDR Barclays 1-3 Month T-Bill ETF
    /// 2007-05-30   SPTL   SPDR Portfolio Long Term Treasury ETF
    /// 2008-05-01   TBT    UltraShort Barclays 20+ Year Treasury
    /// 2009-04-16   TMF    Direxion Daily 20-Year Treasury Bull 3X
    /// 2009-04-16   TMV    Direxion Daily 20-Year Treasury Bear 3X
    /// 2009-08-20   TBF    ProShares Short 20+ Year Treasury
    /// 2009-11-23   VGSH   Vanguard Short-Term Treasury ETF
    /// 2009-11-23   VGIT   Vanguard Intermediate-Term Treasury ETF
    /// 2009-11-24   VGLT   Vanguard Long-Term Treasury ETF
    /// 2010-08-06   SCHO   Schwab Short-Term U.S. Treasury ETF
    /// 2010-08-06   SCHR   Schwab Intermediate-Term U.S. Treasury ETF
    /// 2011-12-01   SPTS   SPDR Portfolio Short Term Treasury ETF
    /// 2012-02-24   GOVT   iShares U.S. Treasury Bond ETF
    /// </summary>
    public class USTreasuriesETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the USTreasuriesETFUniverse class
        /// </summary>
        public USTreasuriesETFUniverse() :
            base(
                "qc-us-treasuries-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    {"IEF", new DateTime(2002, 7, 26)},
                    {"SHY", new DateTime(2002, 7, 26)},
                    {"TLT", new DateTime(2002, 7, 26)},
                    {"IEI", new DateTime(2007, 1, 11)},
                    {"SHV", new DateTime(2007, 1, 11)},
                    {"TLH", new DateTime(2007, 1, 11)},
                    {"EDV", new DateTime(2007, 12, 10)},
                    {"BIL", new DateTime(2007, 5, 30)},
                    {"SPTL", new DateTime(2007, 5, 30)},
                    {"TBT", new DateTime(2008, 5, 1)},
                    {"TMF", new DateTime(2009, 4, 16)},
                    {"TMV", new DateTime(2009, 4, 16)},
                    {"TBF", new DateTime(2009, 8, 20)},
                    {"VGSH", new DateTime(2009, 11, 23)},
                    {"VGIT", new DateTime(2009, 11, 23)},
                    {"VGLT", new DateTime(2009, 11, 24)},
                    {"SCHO", new DateTime(2010, 8, 6)},
                    {"SCHR", new DateTime(2010, 8, 6)},
                    {"SPTS", new DateTime(2011, 12, 1)},
                    {"GOVT", new DateTime(2012, 2, 24)}
                }
            )
        {

        }
    }
}