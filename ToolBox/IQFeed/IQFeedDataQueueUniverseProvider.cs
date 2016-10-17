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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace QuantConnect.ToolBox.IQFeed
{
    /// <summary>
    /// Class implements several interfaces to support IQFeed symbol mapping to LEAN and symbol lookup
    /// </summary>
    public class IQFeedDataQueueUniverseProvider : IDataQueueUniverseProvider, ISymbolMapper
    {
        // we define in-memory database for all symbols, but futures
        private List<SymbolData> _symbolUniverse;

        // futures definitions are not complete. We miss expiration dates. 
        // we store those incomplete symbols in a separate collection
        private List<SymbolData> _incompleteFutures;

        // tickets and symbols are isomorphic
        private Dictionary<Symbol, string> _symbols;
        private Dictionary<string, Symbol> _tickers;

        // map of IQFeed exchange names to QC markets
        private readonly Dictionary<string, string> _futuresExchanges = new Dictionary<string, string>
        {
            { "CME", Market.Globex },
            { "NYMEX", Market.NYMEX },
            { "ICEFU", Market.ICE },
            { "CBOT", Market.CBOT },
            { "CFE", Market.CBOE  }
        };

        // futures fundamental data resolver
        private readonly SymbolFundamentalData _symbolFundamentalData;

        public IQFeedDataQueueUniverseProvider()
        {
            _symbolFundamentalData = new SymbolFundamentalData();
            _symbolFundamentalData.Connect();
            _symbolFundamentalData.SetClientName("SymbolFundamentalData");

            LoadSymbols();

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

            var result = _symbolUniverse.Where(x => lookupFunc(x.Symbol) == lookupName &&
                                            x.Symbol.ID.SecurityType == securityType && 
                                            (securityCurrency == null || x.SecurityCurrency == securityCurrency) && 
                                            (securityExchange == null || x.SecurityExchange == securityExchange));

            if (result.Count() == 0 && securityType == SecurityType.Future)
            {
                result = _incompleteFutures.Where(x => lookupFunc(x.Symbol) == lookupName &&
                                            x.Symbol.ID.SecurityType == securityType &&
                                            (securityCurrency == null || x.SecurityCurrency == securityCurrency) &&
                                            (securityExchange == null || x.SecurityExchange == securityExchange))
                                            .ToList();

                // now we update expiration dates for the futures, if any
                foreach (var item in result)
                {
                    var expirationDate = _symbolFundamentalData.Request(item.Ticker).Item1;

                    // if correct expiration date is found, we update our collections with new symbol object and from now on
                    // we use it in the system (we move the symbol from _incompleteFutures to _symbolUniverse)
                    if (expirationDate != DateTime.MinValue)
                    {
                        _symbols.Remove(item.Symbol);
                        _incompleteFutures.Remove(item);

                        item.Symbol = Symbol.CreateFuture(item.Symbol.Underlying.Value, item.Symbol.ID.Market, expirationDate);

                        _tickers[item.Ticker] = item.Symbol;
                        _symbols[item.Symbol] = item.Ticker;
                        _symbolUniverse.Add(item);
                    }
                }
            }

            return result.Select(x => x.Symbol);
        }

        private void LoadSymbols()
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

            if (!Directory.Exists(Globals.Cache)) Directory.CreateDirectory(Globals.Cache);

            // we try to check if we already downloaded the file and it is in cache. If yes, we use it. Otherwise, download new file. 
            IStreamReader reader;

            // we update the files every week
            var dayOfWeek = DateTimeFormatInfo.CurrentInfo.Calendar.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            var thisYearWeek = DateTime.Today.ToString("yyyy") + "-" + dayOfWeek.ToString();

            var todayFileName = "IQFeed-symbol-universe-" + thisYearWeek + ".zip";
            var todayFullName = Path.Combine(Globals.Cache, todayFileName);

            var iqfeedNameMapFileName = "IQFeed-symbol-map.json";
            var iqfeedNameMapFullName = Path.Combine("IQFeed", iqfeedNameMapFileName);

            // we have a special treatment of futures, because IQFeed renamed exchange tickers and doesn't include 
            // futures expiration dates in the symbol universe file. We fix this.

            // We map those tickers back to their original names using the map below
            var iqfeedNameMap = new Dictionary<string, string>();

            var mapExists = File.Exists(iqfeedNameMapFullName);

            if (mapExists)
            {
                Log.Trace("Loading IQFeed futures symbol map file...");
                iqfeedNameMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(iqfeedNameMapFullName));
            }

            if (!File.Exists(todayFullName))
            {
                Log.Trace("Loading IQFeed symbol universe file ({0})...", uri);
                reader = new RemoteFileSubscriptionStreamReader(uri, Globals.Cache, todayFileName);
            }
            else
            {
                Log.Trace("Found up-to-date IQFeed symbol universe file in local cache. Loading it...");
                reader = new LocalFileSubscriptionStreamReader(todayFullName);
            }

            _symbolUniverse = new List<SymbolData>();
            _incompleteFutures = new List<SymbolData>();

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

                        _symbolUniverse.Add(new SymbolData
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

                        _symbolUniverse.Add(new SymbolData
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

                            _symbolUniverse.Add(new SymbolData
                            {
                                Symbol = Symbol.Create(symbol, SecurityType.Forex, Market.FXCM),
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

                        var futuresTicker = columns[columnSymbol].TrimStart(new [] { '@' });
                        var expirationYearString = futuresTicker.Substring(futuresTicker.Length - 2, 2);
                        var expirationMonthString = futuresTicker.Substring(futuresTicker.Length - 3, 1);
                        var underlyingString = futuresTicker.Substring(0, futuresTicker.Length - 3);

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

                        if (expirationYearMonth < DateTime.Now.Date.AddDays(-1.0))
                        {
                            continue;
                        }

                        if (iqfeedNameMap.ContainsKey(underlyingString))
                            underlyingString = iqfeedNameMap[underlyingString];
                        else
                        {
                            if (!mapExists)
                            {
                                if (!iqfeedNameMap.ContainsKey(underlyingString))
                                {
                                    // if map is not created yet, we request this information from IQFeed
                                    var exchangeSymbol = _symbolFundamentalData.Request(columns[columnSymbol]).Item2;
                                    if (!string.IsNullOrEmpty(exchangeSymbol))
                                    {
                                        iqfeedNameMap[underlyingString] = exchangeSymbol;
                                        underlyingString = exchangeSymbol;
                                    }
                                    else
                                    {
                                        Log.Trace("IQFeed futures ticker {0} had no exchange root symbol assigned in IQFeed system.", underlyingString);
                                    }
                                }
                            }
                        }

                        var market = Market.USA;

                        _incompleteFutures.Add(new SymbolData
                        {
                            Symbol = Symbol.CreateFuture(underlyingString,
                                                        market,
                                                        expirationYearMonth),
                            SecurityCurrency = "USD",
                            SecurityExchange = market,
                            Ticker = columns[columnSymbol]
                        });
                        break;

                    default:

                        continue;
                }
            }

            if (!mapExists)
            {
                Log.Trace("Saving IQFeed futures symbol map file...");
                File.WriteAllText(iqfeedNameMapFullName, JsonConvert.SerializeObject(iqfeedNameMap));
            }

            Log.Trace("Finished loading IQFeed symbol universe file.");
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

        /// <summary>
        /// Private class that helps requesting IQFeed fundamental data 
        /// </summary>
        public class SymbolFundamentalData : IQLevel1Client
        {
            public SymbolFundamentalData(): base(80)
            {
            }

            /// <summary>
            /// Method returns two fields of the fundamental data that we need: expiration date (tuple field 1),
            /// and exchange root symbol (tuple field 2)
            /// </summary>
            public Tuple<DateTime, string> Request(string ticker)
            {
                const int timeout = 180; // sec
                var manualResetEvent = new ManualResetEvent(false);

                var expiry = DateTime.MinValue;
                var rootSymbol = string.Empty;

                EventHandler<Level1FundamentalEventArgs> dataEventHandler = (sender, e) =>
                {
                    expiry = e.ExpirationDate;
                    rootSymbol = e.ExchangeRoot;
                    manualResetEvent.Set();
                };
                EventHandler<Level1SummaryUpdateEventArgs> noDataEventHandler = (sender, e) =>
                {
                    if (e.NotFound)
                    {
                        manualResetEvent.Set();
                    }
                };

                Level1FundamentalEvent += dataEventHandler;
                Level1SummaryUpdateEvent += noDataEventHandler;

                Subscribe(ticker);

                if (!manualResetEvent.WaitOne(timeout * 1000))
                {
                    Log.Error("SymbolFundamentalData.Request() failed to receive response from IQFeed within {0} seconds", timeout);
                }

                Unsubscribe(ticker);

                Level1SummaryUpdateEvent -= noDataEventHandler;

                Level1FundamentalEvent -= dataEventHandler;

                return Tuple.Create(expiry, rootSymbol);
            }
        }
        
    }
}
