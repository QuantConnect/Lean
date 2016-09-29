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

using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.IQFeed
{
    /// <summary>
    /// Class implements several interfaces to support IQFeed symbol mapping to LEAN and symbol lookup
    /// </summary>
    public class IQFeedDataQueueUniverseProvider : IDataQueueUniverseProvider, ISymbolMapper
    {
        private List<SymbolData> _symbolUniverse;

        // tickets and symbols are isomorphic
        private Dictionary<Symbol, string> _symbols;
        private Dictionary<string, Symbol> _tickers;

        // IQFeed are using their own (feed) symbology 
        // We map those tickers back to their original names using the map below

        private readonly Dictionary<string, string> _iqfeedNameMap =
            new Dictionary<string, string>
            {
                // IQFeed -> Original
                { "C", "ZC" },
                { "W", "ZW" },
                { "S", "ZS" },
                { "SM", "ZM" },
                { "BO", "ZL" },
                { "O", "ZO" },
                { "KW", "KE" },
                { "DX", "DX" },
                { "BP", "GBP" },
                { "CD", "CAD" },
                { "JY", "JPY" },
                { "SF", "CHF" },
                { "EU", "EUR" },
                { "AD", "AUD" },
                { "NE", "NZD" },
                { "QRB", "RB" },
                { "QNG", "NG" },
                { "AC", "AC" },
                { "US", "ZB" },
                { "TY", "ZN" },
                { "FV", "ZF" },
                { "TU", "ZT" },
                { "ED", "GE" },
                { "ES", "ES" },
                { "NQ", "NQ" },
                { "YM", "YM" },
                { "LE", "LE" },
                { "GF", "GF" },
                { "HE", "HE" },
                { "QGC", "GC" },
                { "QSI", "SI" },
                { "QPL", "PL" },
                { "QPA", "PA" },
                { "CT", "CT" },
                { "OJ", "OJ" },
                { "SB", "SB" },
                { "CC", "CC" }
            };



        public IQFeedDataQueueUniverseProvider()
        {
            _symbolUniverse = LoadSymbols();

            _symbols = _symbolUniverse.ToDictionary(kv => kv.Symbol, kv => kv.Ticker);
            _tickers = _symbolUniverse.ToDictionary(kv => kv.Ticker, kv => kv.Symbol);
        }

        /// <summary>
        /// Converts a Lean symbol instance to IQFeed ticker
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>IQFeed ticker</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            return _symbols.ContainsKey(symbol) ? _symbols[symbol] : string.Empty;
        }

        /// <summary>
        /// Converts IQFeed ticker to a Lean symbol instance
        /// </summary>
        /// <param name="ticker">IQFeed ticker</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string ticker, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            return _tickers.ContainsKey(ticker) ? _tickers[ticker] : Symbol.Empty;
        }

        /// <summary>
        /// Method returns a collection of Symbols that are available at IQFeed. 
        /// </summary>
        /// <param name="lookupName">String representing the name to lookup</param>
        /// <param name="securityType">Expected security type of the returned symbols (if any)</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <param name="securityExchange">Expected security exchange name(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, string securityCurrency = null, string securityExchange = null)
        {
            Func<Symbol, string> lookupFunc;

            // for option, futures contract we search the underlying
            if (securityType == SecurityType.Option ||
                securityType == SecurityType.Future)
            {
                lookupFunc = symbol => symbol.HasUnderlying ? symbol.Underlying.Value : string.Empty;
            }
            else
            {
                lookupFunc = symbol => symbol.Value;
            }

            return _symbolUniverse.Where(x => lookupFunc(x.Symbol) == lookupName &&
                                            x.Symbol.ID.SecurityType == securityType && 
                                            (securityCurrency == null || x.SecurityCurrency == securityCurrency) && 
                                            (securityExchange == null || x.SecurityExchange == securityExchange))
                                  .Select(x => x.Symbol);
        }

        private List<SymbolData> LoadSymbols()
        {
            // default URI
            const string uri = "http://www.dtniq.com/product/mktsymbols_v2.zip";

            // IQFeed CSV file column nomenclature
            const int columnSymbol = 0;
            const int columnDescription = 1;
            const int columnExchange = 2;
            const int columnListedMarket = 3;
            const int columnSecurityType = 4;
            const int columnSIC = 5;
            const int columnFrontMonth = 6;
            const int columnNAICS = 7;
            const int totalColumns = 8;

            var symbols = new List<SymbolData>();

            Log.Trace("Loading IQFeed symbol universe file ({0})...", uri);

            if (!Directory.Exists(Globals.Cache)) Directory.CreateDirectory(Globals.Cache);

            // we try to check if we already downloaded the file and it is in cache. If yes, we use it. Otherwise, download new file. 
            IStreamReader reader;
            var todayFileName = "IQFeed-symbol-universe-" + DateTime.Today.ToString("yyyy-MM-dd") + ".zip";
            var todayFullName = Path.Combine(Globals.Cache, todayFileName);

            if (!File.Exists(todayFullName))
            {
                reader = new RemoteFileSubscriptionStreamReader(uri, Globals.Cache, todayFileName);
            }
            else
            {
                Log.Trace("Found up-to-date file in local cache. Loading it...");
                reader = new LocalFileSubscriptionStreamReader(todayFullName);
            }
            
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var columns = line.Split('\t');

                if (columns.Length != totalColumns)
                {
                    Log.Trace("Discrepancy found while parsing IQFeed symbol universe file. Expected 8 columns, but arrived {0}. Line: {1}", columns.Length, line);
                    continue;
                }

                switch (columns[columnSecurityType])
                {
                    case "EQUITY":

                        symbols.Add(new SymbolData
                        {
                            Symbol = Symbol.Create(columns[columnSymbol], SecurityType.Equity, Market.USA),
                            SecurityCurrency = "USD",
                            SecurityExchange = Market.USA,
                            Ticker = columns[columnSymbol]
                        });
                        break;

                    case "IEOPTION":

                        // This table describes IQFeed option symbology  
                        var symbology = new Dictionary<string, Tuple<int, OptionRight>>
                        {
                            { "A", Tuple.Create(1, OptionRight.Call) }, { "M", Tuple.Create(1, OptionRight.Put) },
                            { "B", Tuple.Create(2, OptionRight.Call) }, { "N", Tuple.Create(2, OptionRight.Put) },
                            { "C", Tuple.Create(3, OptionRight.Call) }, { "O", Tuple.Create(3, OptionRight.Put) },
                            { "D", Tuple.Create(4, OptionRight.Call) }, { "P", Tuple.Create(4, OptionRight.Put) },
                            { "E", Tuple.Create(5, OptionRight.Call) }, { "Q", Tuple.Create(5, OptionRight.Put) },
                            { "F", Tuple.Create(6, OptionRight.Call) }, { "R", Tuple.Create(6, OptionRight.Put) },
                            { "G", Tuple.Create(7, OptionRight.Call) }, { "S", Tuple.Create(7, OptionRight.Put) },
                            { "H", Tuple.Create(8, OptionRight.Call) }, { "T", Tuple.Create(8, OptionRight.Put) },
                            { "I", Tuple.Create(9, OptionRight.Call) }, { "U", Tuple.Create(9, OptionRight.Put) },
                            { "J", Tuple.Create(10, OptionRight.Call) }, { "V", Tuple.Create(10, OptionRight.Put) },
                            { "K", Tuple.Create(11, OptionRight.Call) }, { "W", Tuple.Create(11, OptionRight.Put) },
                            { "L", Tuple.Create(12, OptionRight.Call) }, { "X", Tuple.Create(12, OptionRight.Put) },

                        };

                        // Extracting option information from IQFeed symbol
                        // Symbology details: http://www.iqfeed.net/symbolguide/index.cfm?symbolguide=guide&displayaction=support%C2%A7ion=guide&web=iqfeed&guide=options&web=IQFeed&type=stock

                        var ticker = columns[columnSymbol];
                        var letterRange = symbology.Keys
                                        .Select(x => x[0])
                                        .ToArray();
                        var optionTypeDelimiter = ticker.LastIndexOfAny(letterRange);
                        var strikePriceString = ticker.Substring(optionTypeDelimiter+1, ticker.Length - optionTypeDelimiter - 1);

                        var lookupResult = symbology[ticker[optionTypeDelimiter].ToString()];
                        var month = lookupResult.Item1;
                        var optionType = lookupResult.Item2;

                        var dayString = ticker.Substring(optionTypeDelimiter - 2, 2);
                        var yearString = ticker.Substring(optionTypeDelimiter - 4, 2);
                        var underlying = ticker.Substring(0, optionTypeDelimiter - 4);

                        // if we cannot parse strike price, we ignore this contract, but log the information. 
                        decimal strikePrice;
                        if (!Decimal.TryParse(strikePriceString, out strikePrice))
                        {
                            Log.Trace("Discrepancy found while parsing IQFeed option strike price in symbol universe file. Strike price {0}. Line: {1}", strikePriceString, line);
                            continue;
                        }

                        int day;

                        if(!int.TryParse(dayString, out day))
                        {
                            Log.Trace("Discrepancy found while parsing IQFeed option expiration day in symbol universe file. Day {0}. Line: {1}", dayString, line);
                            continue;
                        }

                        int year;

                        if (!int.TryParse(yearString, out year))
                        {
                            Log.Trace("Discrepancy found while parsing IQFeed option expiration year in symbol universe file. Year {0}. Line: {1}", yearString, line);
                            continue;
                        }

                        var expirationDate = new DateTime(2000 + year, month, day);

                        symbols.Add(new SymbolData
                        {
                            Symbol = Symbol.CreateOption(underlying,
                                                        Market.USA,
                                                        OptionStyle.American,
                                                        optionType,
                                                        strikePrice,
                                                        expirationDate),
                            SecurityCurrency = "USD",
                            SecurityExchange = Market.USA,
                            Ticker = columns[columnSymbol]
                        });

                        break;

                    case "FOREX":

                        // we use FXCM symbols only
                        if (columns[columnSymbol].EndsWith(".FXCM"))
                        {
                            var symbol = columns[columnSymbol].Replace(".FXCM", string.Empty);

                            symbols.Add(new SymbolData
                            {
                                Symbol = Symbol.Create(columns[columnSymbol], SecurityType.Forex, Market.FXCM),
                                SecurityCurrency = "USD",
                                SecurityExchange = Market.FXCM,
                                Ticker = columns[columnSymbol]
                            });
                        }
                        break;

                    case "FUTURE":

                        var futuresExpirationSymbology = new Dictionary<string, int>
                        {
                            { "F", 1 },
                            { "G", 2 },
                            { "H", 3 },
                            { "J", 4 },
                            { "K", 5 },
                            { "M", 6 },
                            { "N", 7 },
                            { "Q", 8 },
                            { "U", 9 },
                            { "V", 10 },
                            { "X", 11 },
                            { "Z", 12 }
                        };

                        // we are not interested in designated front month contracts as they come twice in the file (marked with #, and using standard symbology)
                        if (columns[columnSymbol].EndsWith("#"))
                            continue;

                        // we are interested in electronically traded symbols only
                        if (!columns[columnSymbol].StartsWith("@"))
                            continue;

                        var futuresTicker = columns[columnSymbol].TrimStart(new [] { '@' });
                        var expirationYearString = futuresTicker.Substring(futuresTicker.Length - 2, 2);
                        var expirationMonthString = futuresTicker.Substring(futuresTicker.Length - 3, 1);
                        var underlyingString = futuresTicker.Substring(0, futuresTicker.Length - 3);

                        if (_iqfeedNameMap.ContainsKey(underlyingString))
                            underlyingString = _iqfeedNameMap[underlyingString];

                        // parsing expiration date

                        int expirationYearShort;

                        if (!int.TryParse(expirationYearString, out expirationYearShort))
                        {
                            Log.Trace("Discrepancy found while parsing IQFeed future contract expiration year in symbol universe file. Year {0}. Line: {1}", expirationYearString, line);
                            continue;
                        }

                        if (!futuresExpirationSymbology.ContainsKey(expirationMonthString))
                        {
                            Log.Trace("Discrepancy found while parsing IQFeed future contract expiration month in symbol universe file. Month {0}. Line: {1}", expirationMonthString, line);
                            continue;
                        }

                        var expirationMonth = futuresExpirationSymbology[expirationMonthString];
                        var exprirationYear = 2000 + expirationYearShort;

                        // Futures contracts have different idiosyncratic expiration dates
                        // We specify year and month of expiration here, and put last day of the month as an expiration date
                        // Later this information will be amended with the expiration data from futures expiration calendar

                        var expirationYearMonth = new DateTime(exprirationYear, expirationMonth, DateTime.DaysInMonth(exprirationYear, expirationMonth));

                        symbols.Add(new SymbolData
                        {
                            Symbol = Symbol.CreateFuture(underlyingString,
                                                        Market.USA,
                                                        expirationYearMonth),
                            SecurityCurrency = "USD",
                            SecurityExchange = Market.USA,
                            Ticker = columns[columnSymbol]
                        });
                        break;

                    default:

                        continue;
                }
            }

            Log.Trace("Finished loading IQFeed symbol universe file.");

            return symbols;
        }


        // this is a private POCO type for storing symbol universe
        class SymbolData
        {
            public string Ticker { get; set; }

            public string SecurityCurrency { get; set; }

            public string SecurityExchange { get; set; }

            public Symbol Symbol { get; set; }

            protected bool Equals(SymbolData other)
            {
                return string.Equals(Ticker, other.Ticker) &&
                    string.Equals(SecurityCurrency, other.SecurityCurrency) &&
                    string.Equals(SecurityExchange, other.SecurityExchange) &&
                    Equals(Symbol, other.Symbol);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((SymbolData)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Ticker != null ? Ticker.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (SecurityCurrency != null ? SecurityCurrency.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (SecurityExchange != null ? SecurityExchange.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

    }
}
