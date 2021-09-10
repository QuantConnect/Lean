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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Resolves standardized security definitions such as FIGI, CUSIP, ISIN, SEDOL into
    /// a properly mapped Lean <see cref="Symbol"/>.
    /// </summary>
    public class SecurityDefinitionSymbolResolver
    {
        private readonly List<SecurityDefinition> _securityDefinitions;
        private readonly MapFileResolver _mapFileResolver;
        
        /// <summary>
        /// Creates an instance of the symbol resolver
        /// </summary>
        /// <param name="dataFolder">Data folder to read the security-database.csv file from</param>
        /// <param name="mapFileProvider">Map file provider used to obtain symbol mappings</param>
        /// <param name="market">The market of the assets referred to by the security definitions</param>
        public SecurityDefinitionSymbolResolver(
            DirectoryInfo dataFolder = null,
            IMapFileProvider mapFileProvider = null,
            string market = Market.USA)
        {
            var dataFolderPath = dataFolder?.FullName ?? Globals.DataFolder;
            
            var securityDefinitionFile = new FileInfo(
                Path.Combine(
                    dataFolderPath,
                    "symbol-properties",
                    "security-database.csv"));            
            
            if (!SecurityDefinition.TryFromCsvFile(securityDefinitionFile, out _securityDefinitions))
            {
                Log.Error($"SecurityDefinitionSymbolResolver(): No security definitions data loaded from file: {securityDefinitionFile.FullName}");
            }

            if (mapFileProvider == null)
            {
                var dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider", "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider"));
                mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
                mapFileProvider.Initialize(dataProvider);
            }
            
            _mapFileResolver = mapFileProvider.Get(market);
        }
        
        /// <summary>
        /// Converts CUSIP into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="cusip">CUSIP</param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the CUSIP provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The CUSIP's corresponding Symbol</returns>
        public Symbol CUSIP(string cusip, DateTime tradingDate)
        {
            return SecurityDefinitionToSymbol(
                _securityDefinitions?.FirstOrDefault(x => x.CUSIP == cusip && !string.IsNullOrWhiteSpace(x.CUSIP)),
                tradingDate);
        }
        
        /// <summary>
        /// Converts FIGI into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="compositeFigi">Composite FIGI</param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the composite FIGI provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The composite FIGI's corresponding Symbol</returns>
        public Symbol CompositeFIGI(string compositeFigi, DateTime tradingDate)
        {
            return SecurityDefinitionToSymbol(
                _securityDefinitions?.FirstOrDefault(x => x.FIGI == compositeFigi && !string.IsNullOrWhiteSpace(x.FIGI)),
                tradingDate);
        }
        
        /// <summary>
        /// Converts SEDOL into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="sedol">SEDOL</param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the SEDOL provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The SEDOL's corresponding Symbol</returns>
        public Symbol SEDOL(string sedol, DateTime tradingDate)
        {
            return SecurityDefinitionToSymbol(
                _securityDefinitions?.FirstOrDefault(x => x.SEDOL == sedol && !string.IsNullOrWhiteSpace(x.SEDOL)),
                tradingDate);
        }

        /// <summary>
        /// Converts ISIN into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="isin">ISIN</param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the ISIN provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The ISIN's corresponding Lean Symbol</returns>
        public Symbol ISIN(string isin, DateTime tradingDate)
        {
            return SecurityDefinitionToSymbol(
                _securityDefinitions?.FirstOrDefault(x => x.ISIN == isin),
                tradingDate);
        }

        /// <summary>
        /// Converts a SecurityDefinition to a <see cref="Symbol" />
        /// </summary>
        /// <param name="securityDefinition">Security definition</param>
        /// <param name="tradingDate">
        /// The date that the stock was being traded. This is used to resolve
        /// the ticker that the stock was trading under on this date.
        /// </param>
        /// <returns>Symbol if matching Lean Symbol was found on the trading date, null otherwise</returns>
        private Symbol SecurityDefinitionToSymbol(SecurityDefinition securityDefinition, DateTime tradingDate)
        {
            if (securityDefinition == null)
            {
                return null;
            }
            
            // Get the first ticker the symbol traded under, and then lookup the
            // trading date to get the ticker on the trading date.
            var mappedTicker = _mapFileResolver
                .ResolveMapFile(securityDefinition.SecurityIdentifier.Symbol, securityDefinition.SecurityIdentifier.Date)
                .GetMappedSymbol(tradingDate);

            return string.IsNullOrWhiteSpace(mappedTicker) 
                ? null 
                : new Symbol(securityDefinition.SecurityIdentifier, mappedTicker);
        }
    }
}
