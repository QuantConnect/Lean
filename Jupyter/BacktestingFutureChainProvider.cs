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

using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="BacktestingFutureChainProvider"/> that reads the list of contracts from open interest zip data files
    /// </summary>
    public class BacktestingFutureChainProvider
    {
        /// <summary>
        /// Gets the list of future contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the future chain (only used in backtesting)</param>
        /// <returns>The list of future contracts</returns>
        public IEnumerable<Symbol> GetFutureContractList(Symbol symbol, DateTime date)
        {
            if (symbol.SecurityType != SecurityType.Future)
            {
                throw new NotSupportedException($"BacktestingFutureChainProvider.GetFutureContractList(): SecurityType.Future is expected but was {symbol.SecurityType}");
            }

            // build the future contract list from the open interest zip file entry names

            // build the zip file name for open interest data
            var zipFileName = LeanData.GenerateZipFilePath(Globals.DataFolder, symbol, date, Resolution.Minute, TickType.OpenInterest);

            if (!File.Exists(zipFileName))
            {
                Log.Trace($"BacktestingFutureChainProvider.GetFutureContractList(): File not found: {zipFileName}");
                yield break;
            }

            // generate and return the contract symbol for each zip entry
            var zipEntryNames = Compression.GetZipEntryFileNames(zipFileName);
            foreach (var zipEntryName in zipEntryNames)
            {
                yield return LeanData.ReadSymbolFromZipEntry(symbol, Resolution.Minute, zipEntryName);
            }
        }
    }
}
