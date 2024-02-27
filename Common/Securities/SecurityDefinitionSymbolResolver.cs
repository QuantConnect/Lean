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
    /// a properly mapped Lean <see cref="Symbol"/>, and vice-versa.
    /// </summary>
    public class SecurityDefinitionSymbolResolver
    {
        private static SecurityDefinitionSymbolResolver _securityDefinitionSymbolResolver;
        private static readonly object _lock = new object();

        private List<SecurityDefinition> _securityDefinitions;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly string _securitiesDefinitionKey;
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Creates an instance of the symbol resolver
        /// </summary>
        /// <param name="dataProvider">Data provider used to obtain symbol mappings data</param>
        /// <param name="securitiesDefinitionKey">Location to read the securities definition data from</param>
        private SecurityDefinitionSymbolResolver(IDataProvider dataProvider = null, string securitiesDefinitionKey = null)
        {
            _securitiesDefinitionKey = securitiesDefinitionKey ?? Path.Combine(Globals.GetDataFolderPath("symbol-properties"), "security-database.csv");

            _dataProvider = dataProvider ?? Composer.Instance.GetPart<IDataProvider>();

            _mapFileProvider = Composer.Instance.GetPart<IMapFileProvider>();
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
        /// Get's the CIK value associated with the given <see cref="Symbol"/>
        /// </summary>
        /// <param name="symbol">The Lean <see cref="Symbol"/></param>
        /// <returns>The Central Index Key number (CIK) corresponding to the given Lean <see cref="Symbol"/> if any, else null</returns>
        public int? CIK(Symbol symbol)
        {
            return SymbolToSecurityDefinition(symbol)?.CIK;
        }

        /// <summary>
        /// Converts CIK into a Lean <see cref="Symbol"/> array
        /// </summary>
        /// <param name="cik">
        /// The Central Index Key (CIK) of a company
        /// </param>
        /// <param name="tradingDate">
        /// The date that the stock was trading at with the CIK provided. This is used
        /// to get the ticker of the symbol on this date.
        /// </param>
        /// <returns>The Lean Symbols corresponding to the CIK on the trading date provided</returns>
        public Symbol[] CIK(int cik, DateTime tradingDate)
        {
            if (cik == 0)
            {
                return Array.Empty<Symbol>();
            }

            return GetSecurityDefinitions()
                .Where(x => x.CIK != null && x.CIK == cik)
                .Select(securityDefinition => SecurityDefinitionToSymbol(securityDefinition, tradingDate))
                .Where(x => x != null)
                .ToArray();
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

            return GetSecurityDefinitions().FirstOrDefault(x => x.SecurityIdentifier.Equals(symbol.ID));
        }

        /// <summary>
        /// Get's the security definitions using a lazy initialization
        /// </summary>
        private IEnumerable<SecurityDefinition> GetSecurityDefinitions()
        {
            lock (_lock)
            {
                if (_securityDefinitions == null && !SecurityDefinition.TryRead(_dataProvider, _securitiesDefinitionKey, out _securityDefinitions))
                {
                    _securityDefinitions = new List<SecurityDefinition>();
                    Log.Error($"SecurityDefinitionSymbolResolver(): No security definitions data loaded from file: {_securitiesDefinitionKey}");
                }
            }

            return _securityDefinitions;
        }

        /// <summary>
        /// Gets the single instance of the symbol resolver
        /// </summary>
        /// <param name="dataProvider">Data provider used to obtain symbol mappings data</param>
        /// <param name="securitiesDefinitionKey">Location to read the securities definition data from</param>
        /// <returns>The single instance of the symbol resolver</returns>
        public static SecurityDefinitionSymbolResolver GetInstance(IDataProvider dataProvider = null, string securitiesDefinitionKey = null)
        {
            lock (_lock)
            {
                if (_securityDefinitionSymbolResolver == null)
                {
                    _securityDefinitionSymbolResolver = new SecurityDefinitionSymbolResolver(dataProvider, securitiesDefinitionKey);
                }
            }

            return _securityDefinitionSymbolResolver;
        }

        /// <summary>
        /// Resets the security definition symbol resolver, forcing a reload when reused.
        /// Called in tests where multiple algorithms are run sequentially,
        /// and we need to guarantee that every test starts with the same environment.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _securityDefinitionSymbolResolver = null;
            }
        }
    }
}
