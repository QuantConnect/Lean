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
using System.Globalization;
using System.Linq;
using QuantConnect.Brokerages.Zerodha.Messages;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Zerodha symbols.
    /// </summary>
    public class ZerodhaSymbolMapper : ISymbolMapper
    {

        /// <summary>
        /// Symbols that are Tradable
        /// </summary>
        public List<Symbol> KnownSymbols
        {
            get
            {
                return KnownSymbolsList;
            }
        }


        /// <summary>
        /// The list of known Zerodha symbols.
        /// </summary>
        public static List<Symbol> KnownSymbolsList = new List<Symbol>();

        /// <summary>
        /// The list of known Zerodha symbols.
        /// </summary>
        public static Dictionary<string, uint> ZerodhaInstrumentsList = new Dictionary<string, uint>();

        /// <summary>
        /// Constructs default instance of the Zerodha Sybol Mapper
        /// </summary>
        public ZerodhaSymbolMapper(Kite kite, string exchange = "")
        {
            KnownSymbolsList = GetTradableInstrumentsList(kite, exchange);
        }
        /// <summary>
        /// Get list of tradable symbol
        /// </summary>
        /// <param name="kite">Kite</param>
        /// <param name="exchange">Exchange</param>
        /// <returns></returns>
        public List<Symbol> GetTradableInstrumentsList(Kite kite, string exchange = "")
        {

            var tradableInstruments = kite.GetInstruments(exchange);
            var symbols = new List<Symbol>();
            var zerodhaInstrumentsMapping = new Dictionary<string, uint>();


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
                        market = Market.NFO;
                        break;
                    case "CDS":
                        market = Market.CDS;
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
                    //TODO: Handle parsing of BCDOPT strike price
                    if(tp.Segment!= "BCD-OPT")
                    {
                        var symbol = GetLeanSymbol(tp.Name.Trim().Replace(" ", ""), securityType, market, (DateTime)expiryDate, GetStrikePrice(tp), optionRight);
                        symbols.Add(symbol);
                        zerodhaInstrumentsMapping.Add(tp.TradingSymbol.Trim().Replace(" ", "") + "^" + market, tp.InstrumentToken);
                    }                    
                }
                if (securityType == SecurityType.Future)
                {
                    var expiryDate = tp.Expiry;
                    var symbol = GetLeanSymbol(tp.TradingSymbol.Trim().Replace(" ", ""), securityType, market, (DateTime)expiryDate);
                    symbols.Add(symbol);
                    zerodhaInstrumentsMapping.Add(tp.TradingSymbol.Trim().Replace(" ", "") + "^" + market, tp.InstrumentToken);
                }
                if (securityType == SecurityType.Equity)
                {
                    var symbol = GetLeanSymbol(tp.TradingSymbol.Trim().Replace(" ", ""), securityType, market);
                    symbols.Add(symbol);
                    zerodhaInstrumentsMapping.Add(tp.TradingSymbol.Trim().Replace(" ", "") + "^" + market, tp.InstrumentToken);
                }
            }
            ZerodhaInstrumentsList = zerodhaInstrumentsMapping;
            return symbols;
        }

        private decimal GetStrikePrice(CsvInstrument scrip)
        {
            var strikePrice = scrip.TradingSymbol.Trim().Replace(" ", "").Replace(scrip.Name, "");
            var strikePriceTemp = strikePrice.Substring(5, strikePrice.Length - 5);
            var strikePriceResult = strikePriceTemp.Substring(0, strikePriceTemp.Length - 2);

            return Convert.ToDecimal(strikePriceResult, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a Lean symbol instance to an Zerodha symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Zerodha symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
            {
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));
            }

            if (symbol.ID.SecurityType != SecurityType.Equity && symbol.ID.SecurityType != SecurityType.Future && symbol.ID.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);
            }

            var brokerageSymbol = ConvertLeanSymbolToZerodhaSymbol(symbol.Value);

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
            {
                throw new ArgumentException($"Invalid Zerodha symbol: {brokerageSymbol}");
            }

            //if (!IsKnownBrokerageSymbol(brokerageSymbol))
            //   throw new ArgumentException($"Unknown Zerodha symbol: {brokerageSymbol}");

            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd || securityType == SecurityType.Commodity || securityType == SecurityType.Crypto)
            {
                throw new ArgumentException($"Invalid security type: {securityType}");
            }

            if (!Market.Encode(market).HasValue)
            {
                throw new ArgumentException($"Invalid market: {market}");
            }


            switch (securityType)
            {
                case SecurityType.Option:
                    OptionStyle optionStyle = OptionStyle.European;
                    return Symbol.CreateOption(brokerageSymbol.Replace(" ", "").Trim(), market, optionStyle, optionRight, strike, expirationDate);
                case SecurityType.Future:
                    return Symbol.CreateFuture(brokerageSymbol.Replace(" ", "").Trim(), market, expirationDate);
                default:
                    return Symbol.Create(brokerageSymbol.Replace(" ", "").Trim(), securityType, market);
            }

        }


        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            brokerageSymbol = brokerageSymbol.Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException($"Invalid Zerodha symbol: {brokerageSymbol}");
            }

            if (IsKnownBrokerageSymbol(brokerageSymbol))
            {
                throw new ArgumentException($"Symbol not present : {brokerageSymbol}");
            }


            var symbol = KnownSymbols.FirstOrDefault(s => s.Value == brokerageSymbol);
            return GetLeanSymbol(brokerageSymbol, symbol.SecurityType, symbol.ID.Market);
        }

        /// <summary>
        /// Converts an Zerodha symbol to a Zerodha Instrument Token instance
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <param name="market">The trading market</param>
        /// <returns>A new Lean Symbol instance</returns>
        public uint GetZerodhaInstrumentToken(string brokerageSymbol, string market)
        {
            var symbol = KnownSymbols.Where(s => s.Value == brokerageSymbol.Replace(" ", "").Trim() && s.ID.Market.ToUpperInvariant()==market.ToUpperInvariant()).FirstOrDefault();
            uint token = 0;
            if (symbol != null && ZerodhaInstrumentsList.TryGetValue(symbol.ID.Symbol + "^" + symbol.ID.Market, out token))
            {
                return token;
            }
            return 0;
        }

        /// <summary>
        /// Checks if the symbol is supported by Zerodha
        /// </summary>
        /// <param name="brokerageSymbol">The Zerodha symbol</param>
        /// <returns>True if Zerodha supports the symbol</returns>
        public static bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                return false;
            }

            return KnownSymbolsList.Where(x => x.Value.Contains(brokerageSymbol)).IsNullOrEmpty();
        }

        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Zerodha supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value))
            {
                return false;
            }

            var ZerodhaSymbol = ConvertLeanSymbolToZerodhaSymbol(symbol.Value);

            return GetLeanSymbol(ZerodhaSymbol).SecurityType == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Zerodha symbol to a Lean symbol string
        /// </summary>
        public Symbol ConvertZerodhaSymbolToLeanSymbol(uint ZerodhaSymbol)
        {
            var _symbol = ZerodhaInstrumentsList.FirstOrDefault(x => x.Value == ZerodhaSymbol).Key.Replace(" ","").Split("^".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            // return as it is due to Zerodha has similar Symbol format
            return KnownSymbolsList.Where(s => s.Value == _symbol[0] && s.ID.Market == _symbol[1]).FirstOrDefault();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Zerodha symbol
        /// </summary>
        private static string ConvertLeanSymbolToZerodhaSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
            {
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");
            }

            // return as it is due to Zerodha has similar Symbol format
            return leanSymbol.ToUpperInvariant();
        }
    }
}
