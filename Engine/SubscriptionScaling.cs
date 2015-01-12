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
 *
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Subscription price adjustment class adjusts the prices of equities backwards to factor in dividends and splits.
    /// </summary>
    public class SubscriptionAdjustment
    {
        /******************************************************** 
        * CLASS PROPERTIES
        *********************************************************/
        /// <summary>
        /// Base Local Data Folder
        /// </summary>
        public static string DataFolder
        {
            get
            {
                return "../../../Data/";
            }
        }

        /******************************************************** 
        * CLASS METHODS:
        *********************************************************/
        /// <summary>
        /// Overload for base method: using a memory table, find the priceFactor.
        /// </summary>
        /// <param name="factorTable">Memeory Price Factor Table</param>
        /// <param name="searchDate">Date we're looking for price scaling factor.</param>
        /// <returns>decimal scaling factor.</returns>
        public static decimal GetTimePriceFactor(SortedDictionary<DateTime, decimal> factorTable, DateTime searchDate)
        {
            decimal factor = 1;
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in factorTable.Keys.Reverse())
            {
                if (splitDate.Date < searchDate.Date) break;
                factor = factorTable[splitDate];
            }
            return factor;
        }


        /// <summary>
        /// Get the factor-table in memory.
        /// </summary>
        /// <param name="symbol">Factor symbol requested</param>
        /// <returns>SortedDictionary with the factors over time</returns>
        public static SortedDictionary<DateTime, decimal> GetFactorTable(string symbol)
        {
            //Read each line, convert to meaningful values:
            var factorTable = new SortedDictionary<DateTime, decimal>();
            var mapFile = File.ReadAllBytes(DataFolder + "equity/factor_files/" + symbol.ToLower() + ".csv");

            using (var stream = new StreamReader(new MemoryStream(mapFile)))
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    if (line == null) continue;

                    var csv = line.Split(',');
                    factorTable.Add(Time.ParseDate(csv[0]), Convert.ToDecimal(csv[1]) * Convert.ToDecimal(csv[2]));
                }
            }
            return factorTable;
        }



        /// <summary>
        /// Get a map table for the symbol requested into memory.
        /// </summary>
        /// <param name="symbol">Symbol requested</param>
        /// <returns>Sorted dictionary of the symbol mappings over time.</returns>
        public static SortedDictionary<DateTime, string> GetMapTable(string symbol)
        {
            var symbolMapTable = new SortedDictionary<DateTime, string>();
            var mapFile = File.ReadAllBytes(DataFolder + "equity/map_files/" + symbol.ToLower() + ".csv");

            //Preserve Memory
            using (var stream = new StreamReader(new MemoryStream(mapFile)))
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    if (line == null) continue;

                    var csv = line.Split(',');
                    symbolMapTable.Add(Time.ParseDate(csv[0]), csv[1]);
                }
            }

            return symbolMapTable;
        }


        /// <summary>
        /// Get a historical mapped symbol for this requested symbol at this date in time.
        /// </summary>
        /// <param name="baseFolder">Location of the map files</param>
        /// <param name="symbol">Symbol used today</param>
        /// <param name="searchDate">Date we want in the past</param>
        /// <returns>Mapped symbol, potentially different.</returns>
        public static string GetMappedSymbol(string baseFolder, string symbol, DateTime searchDate)
        {
            var mappedSymbol = symbol;
            try
            {
                //Read each line, convert to meaningful values:
                var symbolMapTable = GetMapTable(symbol);
                //Iterate backwards to find the most recent factor:
                foreach (var splitDate in symbolMapTable.Keys)
                {
                    if (splitDate < searchDate) continue;
                    mappedSymbol = symbolMapTable[splitDate];
                }
            }
            catch (Exception err)
            {
                Log.Error("GetMappedTick(): " + err.Message);
            }
            return mappedSymbol;
        }


        /// <summary>
        /// Memory overload search method for finding the mapped symbol for this date.
        /// </summary>
        /// <param name="symbolMapTable">Memory table of symbol-dates.</param>
        /// <param name="searchDate">date for symbol we need to find.</param>
        /// <returns>Symbol on this date.</returns>
        public static string GetMappedSymbol(SortedDictionary<DateTime, string> symbolMapTable, DateTime searchDate)
        {
            var mappedSymbol = "";
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in symbolMapTable.Keys)
            {
                if (splitDate < searchDate) continue;
                mappedSymbol = symbolMapTable[splitDate];
                break;
            }
            return mappedSymbol;
        }

    } // End Subscription Class

} // End QC Namespace
