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
using System.IO;
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Resolves standardized security definitions such as FIGI, CUSIP, ISIN, SEDOL into
    /// a properly mapped Lean <see cref="Symbol"/>.
    /// </summary>
    public class SecurityDefinitionSymbolResolver
    {
        private List<SecurityDefinition> _securityDefinitions;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly string _securitiesDefinitionKey;
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Creates an instance of the symbol resolver
        /// </summary>
        /// <param name="dataProvider">Data provider used to obtain symbol mappings data</param>
        /// <param name="securitiesDefinitionKey">Location to read the securities definition data from</param>
        public SecurityDefinitionSymbolResolver(IDataProvider dataProvider = null, string securitiesDefinitionKey = null)
        {
            _securitiesDefinitionKey = securitiesDefinitionKey ?? Path.Combine(Globals.DataFolder, "symbol-properties", "security-database.csv");

            _dataProvider = dataProvider ??
                Composer.Instance.GetExportedValueByTypeName<IDataProvider>(
                    Config.Get("data-provider", "QuantConnect.Lean.Engine.DataFeeds.DefaultDataProvider"));

            _mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
            _mapFileProvider.Initialize(_dataProvider);
        }

        /// <summary>
        /// Converts CUSIP into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="cusip">
        /// The Committee on Uniform Securities Identification Procedures (CUSIP) number of a security
        /// </param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the CUSIP provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The Lean Symbol corresponding to the CUSIP number on the trading date provided</returns>
        public Symbol CUSIP(string cusip, DateTime tradingDate)
        {
            if (string.IsNullOrWhiteSpace(cusip))
            {
                return null;
            }

            return SecurityDefinitionToSymbol(
                GetSecurityDefinitions().FirstOrDefault(x => x.CUSIP != null && x.CUSIP.Equals(cusip, StringComparison.InvariantCultureIgnoreCase)),
                tradingDate);
        }

        /// <summary>
        /// Converts a Lean <see cref="Symbol"/> to its CUSIP number
        /// </summary>
        /// <param name="symbol">The Lean <see cref="Symbol"/></param>
        /// <returns>The Committee on Uniform Securities Identification Procedures (CUSIP) number corresponding to the given Lean <see cref="Symbol"/></returns>
        public string CUSIP(Symbol symbol)
        {
            return SymbolToSecurityDefinition(symbol)?.CUSIP;
        }

        /// <summary>
        /// Converts an asset's composite FIGI into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="compositeFigi">
        /// The composite Financial Instrument Global Identifier (FIGI) of a security
        /// </param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the composite FIGI provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The Lean Symbol corresponding to the composite FIGI on the trading date provided</returns>
        public Symbol CompositeFIGI(string compositeFigi, DateTime tradingDate)
        {
            if (string.IsNullOrWhiteSpace(compositeFigi))
            {
                return null;
            }

            return SecurityDefinitionToSymbol(
                GetSecurityDefinitions().FirstOrDefault(x => x.CompositeFIGI != null && x.CompositeFIGI.Equals(compositeFigi, StringComparison.InvariantCultureIgnoreCase)),
                tradingDate);
        }

        /// <summary>
        /// Converts a Lean <see cref="Symbol"/> to its composite FIGI representation
        /// </summary>
        /// <param name="symbol">The Lean <see cref="Symbol"/></param>
        /// <returns>The composite Financial Instrument Global Identifier (FIGI) corresponding to the given Lean <see cref="Symbol"/></returns>
        public string CompositeFIGI(Symbol symbol)
        {
            return SymbolToSecurityDefinition(symbol)?.CompositeFIGI;
        }

        /// <summary>
        /// Converts SEDOL into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="sedol">
        /// The Stock Exchange Daily Official List (SEDOL) security identifier of a security
        /// </param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the SEDOL provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The Lean Symbol corresponding to the SEDOL on the trading date provided</returns>
        public Symbol SEDOL(string sedol, DateTime tradingDate)
        {
            if (string.IsNullOrWhiteSpace(sedol))
            {
                return null;
            }

            return SecurityDefinitionToSymbol(
                GetSecurityDefinitions().FirstOrDefault(x => x.SEDOL != null && x.SEDOL.Equals(sedol, StringComparison.InvariantCultureIgnoreCase)),
                tradingDate);
        }

        /// <summary>
        /// Converts a Lean <see cref="Symbol"/> to its SEDOL representation
        /// </summary>
        /// <param name="symbol">The Lean <see cref="Symbol"/></param>
        /// <returns>The Stock Exchange Daily Official List (SEDOL) security identifier corresponding to the given Lean <see cref="Symbol"/></returns>
        public string SEDOL(Symbol symbol)
        {
            return SymbolToSecurityDefinition(symbol)?.SEDOL;
        }

        /// <summary>
        /// Converts ISIN into a Lean <see cref="Symbol"/>
        /// </summary>
        /// <param name="isin">
        /// The International Securities Identification Number (ISIN) of a security
        /// </param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the ISIN provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The Lean Symbol corresponding to the ISIN on the trading date provided</returns>
        public Symbol ISIN(string isin, DateTime tradingDate)
        {
            if (string.IsNullOrWhiteSpace(isin))
            {
                return null;
            }

            return SecurityDefinitionToSymbol(
                GetSecurityDefinitions().FirstOrDefault(x => x.ISIN != null && x.ISIN.Equals(isin, StringComparison.InvariantCultureIgnoreCase)),
                tradingDate);
        }

        /// <summary>
        /// Converts a Lean <see cref="Symbol"/> to its ISIN representation
        /// </summary>
        /// <param name="symbol">The Lean <see cref="Symbol"/></param>
        /// <returns>The International Securities Identification Number (ISIN) corresponding to the given Lean <see cref="Symbol"/></returns>
        public string ISIN(Symbol symbol)
        {
            return SymbolToSecurityDefinition(symbol)?.ISIN;
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

            var mapFileResolver = _mapFileProvider.Get(AuxiliaryDataKey.Create(securityDefinition.SecurityIdentifier));

            // Get the first ticker the symbol traded under, and then lookup the
            // trading date to get the ticker on the trading date.
            var mapFile = mapFileResolver
                .ResolveMapFile(securityDefinition.SecurityIdentifier.Symbol, securityDefinition.SecurityIdentifier.Date);

            // The mapped ticker will be null if the map file is null or there's
            // no entry found for the given trading date.
            var mappedTicker = mapFile?.GetMappedSymbol(tradingDate, null);

            // If we're null, then try again; get the last entry of the map file and use
            // it as the Symbol we return to the caller.
            mappedTicker ??= mapFile?
                .LastOrDefault()?
                .MappedSymbol;

            return string.IsNullOrWhiteSpace(mappedTicker)
                ? null
                : new Symbol(securityDefinition.SecurityIdentifier, mappedTicker);
        }

        /// <summary>
        /// Gets the SecurityDefinition corresponding to the given Lean <see cref="Symbol"/>
        /// </summary>
        private SecurityDefinition SymbolToSecurityDefinition(Symbol symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            return GetSecurityDefinitions()
                .FirstOrDefault(x => x.SecurityIdentifier.ToString().Equals(symbol.ID.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get's the security definitions using a lazy initialization
        /// </summary>
        private IEnumerable<SecurityDefinition> GetSecurityDefinitions()
        {
            if (_securityDefinitions != null)
            {
                return _securityDefinitions;
            }

            if (!SecurityDefinition.TryRead(_dataProvider, _securitiesDefinitionKey, out _securityDefinitions))
            {
                _securityDefinitions = new List<SecurityDefinition>();
                Log.Error("SecurityDefinitionSymbolResolver(): " +
                    Messages.SecurityDefinitionSymbolResolver.NoSecurityDefinitionsLoaded(_securitiesDefinitionKey));
            }
            return _securityDefinitions;
        }
    }
}
