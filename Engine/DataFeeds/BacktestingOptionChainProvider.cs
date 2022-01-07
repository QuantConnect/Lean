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
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IOptionChainProvider"/> that reads the list of contracts from open interest zip data files
    /// </summary>
    public class BacktestingOptionChainProvider : IOptionChainProvider
    {
        private IDataProvider _dataProvider;
        private IMapFileProvider _mapFileProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="dataProvider">The data provider instance to use</param>
        public BacktestingOptionChainProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
            _mapFileProvider =
                Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider",
                    "LocalDiskMapFileProvider"));
        }

        /// <summary>
        /// Gets the list of option contracts for a given underlying symbol
        /// </summary>
        /// <param name="underlyingSymbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the option chain (only used in backtesting)</param>
        /// <returns>The list of option contracts</returns>
        public IEnumerable<Symbol> GetOptionContractList(Symbol underlyingSymbol, DateTime date)
        {
            if (!underlyingSymbol.SecurityType.HasOptions())
            {
                throw new NotSupportedException($"BacktestingOptionChainProvider.GetOptionContractList(): SecurityType.Equity, SecurityType.Future, or SecurityType.Index is expected but was {underlyingSymbol.SecurityType}");
            }

            // Resolve any mapping before requesting option contract list for equities
            // Needs to be done in order for the data file key to be accurate
            Symbol mappedSymbol;
            if (underlyingSymbol.RequiresMapping())
            {
                var mapFileResolver = _mapFileProvider.Get(AuxiliaryDataKey.Create(underlyingSymbol));
                var mapFile = mapFileResolver.ResolveMapFile(underlyingSymbol);
                var ticker = mapFile.GetMappedSymbol(date, underlyingSymbol.Value);
                mappedSymbol = underlyingSymbol.UpdateMappedSymbol(ticker);
            }
            else
            {
                mappedSymbol = underlyingSymbol;
            }


            // build the option contract list from the open interest zip file entry names

            // create a canonical option symbol for the given underlying
            var canonicalSymbol = Symbol.CreateOption(
                mappedSymbol,
                mappedSymbol.ID.Market,
                mappedSymbol.SecurityType.DefaultOptionStyle(),
                default(OptionRight),
                0,
                SecurityIdentifier.DefaultDate);

            var zipFileName = string.Empty;
            Stream stream = null;

            // In order of trust-worthiness of containing the complete option chain, OpenInterest is guaranteed
            // to have the complete option chain. Quotes come after open-interest
            // because it's also likely to contain the option chain. Trades may be
            // missing portions of the option chain, so we resort to it last.
            foreach (var tickType in new[] { TickType.OpenInterest, TickType.Quote, TickType.Trade })
            {
                // build the zip file name and fetch it with our provider
                zipFileName = LeanData.GenerateZipFilePath(Globals.DataFolder, canonicalSymbol, date, Resolution.Minute, tickType);
                stream = _dataProvider.Fetch(zipFileName);

                if (stream != null)
                {
                    break;
                }
            }

            if (stream == null)
            {
                Log.Trace($"BacktestingOptionChainProvider.GetOptionContractList(): File not found: {zipFileName}");
                yield break;
            }

            // generate and return the contract symbol for each zip entry
            var zipEntryNames = Compression.GetZipEntryFileNames(stream);
            foreach (var zipEntryName in zipEntryNames)
            {
                yield return LeanData.ReadSymbolFromZipEntry(canonicalSymbol, Resolution.Minute, zipEntryName);
            }

            stream.DisposeSafely();
        }
    }
}
