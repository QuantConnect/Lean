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
    /// Universe Selection Model that adds the following Energy ETFs at their inception date
    /// 1998-12-22   XLE    Energy Select Sector SPDR Fund
    /// 2000-06-16   IYE    iShares U.S. Energy ETF
    /// 2004-09-29   VDE    Vanguard Energy ETF
    /// 2006-04-10   USO    United States Oil Fund
    /// 2006-06-22   XES    SPDR S&P Oil & Gas Equipment & Services ETF
    /// 2006-06-22   XOP    SPDR S&P Oil & Gas Exploration & Production ETF
    /// 2007-04-18   UNG    United States Natural Gas Fund
    /// 2008-06-25   ICLN   iShares Global Clean Energy ETF
    /// 2008-11-06   ERX    Direxion Daily Energy Bull 3X Shares
    /// 2008-11-06   ERY    Direxion Daily Energy Bear 3x Shares
    /// 2008-11-25   SCO    ProShares UltraShort Bloomberg Crude Oil
    /// 2008-11-25   UCO    ProShares Ultra Bloomberg Crude Oil
    /// 2009-06-02   AMJ    JPMorgan Alerian MLP Index ETN
    /// 2010-06-02   BNO    United States Brent Oil Fund
    /// 2010-08-25   AMLP   Alerian MLP ETF
    /// 2011-12-21   OIH    VanEck Vectors Oil Services ETF
    /// 2012-02-08   DGAZ   VelocityShares 3x Inverse Natural Gas
    /// 2012-02-08   UGAZ   VelocityShares 3x Long Natural Gas
    /// 2012-02-15   TAN    Invesco Solar ETF
    /// </summary>
	public class EnergyETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the EnergyETFUniverse class
        /// </summary>
        public EnergyETFUniverse() :
            base(
                "qc-energy-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    {"XLE", new DateTime(1998, 12, 22)},
                    {"IYE", new DateTime(2000, 6, 16)},
                    {"VDE", new DateTime(2004, 9, 29)},
                    {"USO", new DateTime(2006, 4, 10)},
                    {"XES", new DateTime(2006, 6, 22)},
                    {"XOP", new DateTime(2006, 6, 22)},
                    {"UNG", new DateTime(2007, 4, 18)},
                    {"ICLN", new DateTime(2008, 6, 25)},
                    {"ERX", new DateTime(2008, 11, 6)},
                    {"ERY", new DateTime(2008, 11, 6)},
                    {"SCO", new DateTime(2008, 11, 25)},
                    {"UCO", new DateTime(2008, 11, 25)},
                    {"AMJ", new DateTime(2009, 6, 2)},
                    {"BNO", new DateTime(2010, 6, 2)},
                    {"AMLP", new DateTime(2010, 8, 25)},
                    {"OIH", new DateTime(2011, 12, 21)},
                    {"DGAZ", new DateTime(2012, 2, 8)},
                    {"UGAZ", new DateTime(2012, 2, 8)},
                    {"TAN", new DateTime(2012, 2, 15)}
                }
            )
        {

        }
    }
}