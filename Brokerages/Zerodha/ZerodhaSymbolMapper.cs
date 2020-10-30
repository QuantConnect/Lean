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
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Zerodha symbols.
    /// </summary>
    public class ZerodhaSymbolMapper : ISymbolMapper
    {

        private void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            if (!info.Exists)
            {
                info.Create();
            }

            string path = Path.Combine(filePath, fileName);
            using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
            {
                inputStream.CopyTo(outputFileStream);
            }
        }

        /// <summary>
        /// Symbols that are Tradable
        /// </summary>
        public List<Symbol> KnownSymbols
        {
            get
            {
                var symbols = new List<Symbol>();
                var mapper = new ZerodhaSymbolMapper();
                return KnownSymbolsList;
            }
        }

        /// <summary>
        /// The list of known Zerodha symbols.
        /// </summary>
        public List<Symbol> KnownSymbolsList = new List<Symbol>();


        public ZerodhaSymbolMapper()
        {
            
            var kite = new Kite("", "");
            var tradableInstruments = kite.GetInstruments();
            var symbols = new List<Symbol>();
            var mapper = new ZerodhaSymbolMapper();

            foreach (var tp in tradableInstruments)
            {
                var securityType = SecurityType.Equity;
                var market = Market.NSE;
                OptionRight optionRight = 0;

                switch (tp.InstrumentType)
                {
                    //Equities
                    case "EQ":
                        securityType = SecurityType.Equity;
                        break;
                    //Call Options
                    case "CE":
                        securityType = SecurityType.Option;
                        optionRight = OptionRight.Call;
                        break;
                    //Put Options
                    case "PE":
                        securityType = SecurityType.Option;
                        optionRight = OptionRight.Put;
                        break;
                    //Stock Futures
                    case "FUT":
                        securityType = SecurityType.Future;
                        break;
                    default:
                        securityType = SecurityType.Base;
                        break;
                }


                switch (tp.Exchange)
                {
                    case "NSE":
                        market = Market.NSE;
                        break;
                    case "NFO":
                        market = Market.NSEFO;
                        break;
                    case "CDS":
                        market = Market.NSECDS;
                        break;
                    case "BSE":
                        market = Market.BSE;
                        break;
                    case "BCD":
                        market = Market.BSE;
                        break;
                    case "MCX":
                        market = Market.MCX;
                        break;
                    default:
                        market = Market.NSE;
                        break;
                }
               
                
                if (securityType == SecurityType.Option)
                {
                    var strikePrice = tp.Strike;
                    var expiryDate = tp.Expiry;
                    symbols.Add(mapper.GetLeanSymbol(tp.TradingSymbol, securityType, market, (DateTime)expiryDate, strikePrice, optionRight));
                }
                if (securityType == SecurityType.Future) {
                    var expiryDate = tp.Expiry;
                    symbols.Add(mapper.GetLeanSymbol(tp.TradingSymbol, securityType, market, (DateTime)expiryDate));
                }
                if (securityType == SecurityType.Equity)
                {
                    symbols.Add(mapper.GetLeanSymbol(tp.TradingSymbol, securityType, market));
                }

            }

            KnownSymbolsList = symbols;
            
    }

        /// <summary>
        /// Converts a Lean symbol instance to an Zerodha symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Zerodha symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            //if (symbol.ID.SecurityType != SecurityType.Equity)
            //    throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToZerodhaSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        } 

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Zerodha symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Zerodha symbol: {brokerageSymbol}");

            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd || securityType == SecurityType.Commodity || securityType == SecurityType.Crypto)
                throw new ArgumentException($"Invalid security type: {securityType}");

            if (!Market.Encode(market).HasValue)
                throw new ArgumentException($"Invalid market: {market}");

            switch (securityType)
            {
                case SecurityType.Option:
                    OptionStyle optionStyle = OptionStyle.European;
                    return Symbol.CreateOption(ConvertZerodhaSymbolToLeanSymbol(brokerageSymbol) , market, optionStyle, optionRight,strike,expirationDate);
                case SecurityType.Future:
                    return Symbol.CreateFuture(ConvertZerodhaSymbolToLeanSymbol(brokerageSymbol), market,expirationDate);
                default:
                    return Symbol.Create(ConvertZerodhaSymbolToLeanSymbol(brokerageSymbol), securityType, market);
            }

        }

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            var symbol = KnownSymbols.Where(s => s.ID.Symbol == brokerageSymbol).FirstOrDefault();
            return GetLeanSymbol(brokerageSymbol, securityType, symbol.ID.Market);
        }
        
        /// <summary>
        /// Returns the security type for an Zerodha symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Zerodha symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Zerodha symbol: {brokerageSymbol}");
            //var symbol = KnownSymbols.Where(s => s.ID.Symbol == brokerageSymbol).FirstOrDefault();
            //TODO:Handle in better way
            return SecurityType.Equity;
            //return symbol.SecurityType;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToZerodhaSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Zerodha
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>True if Zerodha supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            return KnownSymbolsList.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Zerodha
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Zerodha supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value) || symbol.Value.Length <= 3)
                return false;

            var ZerodhaSymbol = ConvertLeanSymbolToZerodhaSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(ZerodhaSymbol) && GetBrokerageSecurityType(ZerodhaSymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol string
        /// </summary>
        private static string ConvertZerodhaSymbolToLeanSymbol(string ZerodhaSymbol)
        {
            if (string.IsNullOrWhiteSpace(ZerodhaSymbol))
                throw new ArgumentException($"Invalid Zerodha symbol: {ZerodhaSymbol}");

            // return as it is due to Zerodha has similar Symbol format
            return ZerodhaSymbol.ToUpperInvariant();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Zerodha symbol
        /// </summary>
        private static string ConvertLeanSymbolToZerodhaSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");

            // return as it is due to Zerodha has similar Symbol format
            return leanSymbol.ToUpperInvariant();
        }
    }
}
