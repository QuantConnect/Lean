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
    /// Universe Selection Model that adds the following ETFs at their inception date
    /// </summary>
	public class LiquidETFUniverse : InceptionDateUniverseSelectionModel
    {
        /// <summary>
        /// Initializes a new instance of the EnergyETFUniverse class
        /// </summary>
        public LiquidETFUniverse() :
            base(
                "qc-liquid-etf-basket",
                new Dictionary<string, DateTime>()
                {
                    // Energy
                    {"XLE", new DateTime(1998, 12, 22)},
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
                    {"DGAZ", new DateTime(2012, 2, 8)},
                    {"UGAZ", new DateTime(2012, 2, 8)},
                    {"TAN", new DateTime(2012, 2, 15)},

                    // Metals
                    {"GLD", new DateTime(2004, 11, 18)},
                    {"IAU", new DateTime(2005, 1, 28)},
                    {"SLV", new DateTime(2006, 4, 28)},
                    {"GDX", new DateTime(2006, 5, 22)},
                    {"AGQ", new DateTime(2008, 12, 4)},
                    {"PPLT", new DateTime(2010, 1, 8)},
                    {"NUGT", new DateTime(2010, 12, 8)},
                    {"DUST", new DateTime(2010, 12, 8)},
                    {"USLV", new DateTime(2011, 10, 17)},
                    {"UGLD", new DateTime(2011, 10, 17)},
                    {"JNUG", new DateTime(2013, 10, 3)},
                    {"JDST", new DateTime(2013, 10, 3)},

                    // Technology
                    {"XLK", new DateTime(1998, 12, 22)},
                    {"QQQ", new DateTime(1999, 3, 10)},
                    {"IGV", new DateTime(2001, 7, 13)},
                    {"QTEC", new DateTime(2006, 4, 25)},
                    {"FDN", new DateTime(2006, 6, 23)},
                    {"FXL", new DateTime(2007, 5, 10)},
                    {"TECL", new DateTime(2008, 12, 17)},
                    {"TECS", new DateTime(2008, 12, 17)},
                    {"SOXL", new DateTime(2010, 3, 11)},
                    {"SOXS", new DateTime(2010, 3, 11)},
                    {"SKYY", new DateTime(2011, 7, 6)},
                    {"KWEB", new DateTime(2013, 8, 1)},

                    // US Treasuries
                    {"IEF", new DateTime(2002, 7, 26)},
                    {"SHY", new DateTime(2002, 7, 26)},
                    {"TLT", new DateTime(2002, 7, 26)},
                    {"IEI", new DateTime(2007, 1, 11)},
                    {"SHV", new DateTime(2007, 1, 11)},
                    {"TLH", new DateTime(2007, 1, 11)},
                    {"BIL", new DateTime(2007, 5, 30)},
                    {"SPTL", new DateTime(2007, 5, 30)},
                    {"TBT", new DateTime(2008, 5, 1)},
                    {"TMF", new DateTime(2009, 4, 16)},
                    {"TMV", new DateTime(2009, 4, 16)},
                    {"TBF", new DateTime(2009, 8, 20)},
                    {"SCHO", new DateTime(2010, 8, 6)},
                    {"SCHR", new DateTime(2010, 8, 6)},
                    {"SPTS", new DateTime(2011, 12, 1)},
                    {"GOVT", new DateTime(2012, 2, 24)},

                    // Volatility
                    {"TVIX", new DateTime(2010, 11, 30)},
                    {"VIXY", new DateTime(2011, 1, 4)},
                    {"SPLV", new DateTime(2011, 5, 5)},
                    {"SVXY", new DateTime(2011, 10, 4)},
                    {"UVXY", new DateTime(2011, 10, 4)},
                    {"EEMV", new DateTime(2011, 10, 20)},
                    {"EFAV", new DateTime(2011, 10, 20)},
                    {"USMV", new DateTime(2011, 10, 20)},
		    
		    //Sp500 Sectors:
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
