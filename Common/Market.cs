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
using System.Linq;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// Markets Collection: Soon to be expanded to a collection of items specifying the market hour, timezones and country codes.
    /// </summary>
    public static class Market
    {
        // the upper bound (non-inclusive) for market identifiers
        private const int MaxMarketIdentifier = 1000;

        private static Dictionary<string, int> Markets = new Dictionary<string, int>();
        private static Dictionary<int, string> ReverseMarkets = new Dictionary<int, string>();
        private static readonly IEnumerable<Tuple<string, int>> HardcodedMarkets = new List<Tuple<string, int>>
        {
            Tuple.Create("empty", 0),
            Tuple.Create(USA, 1),
            Tuple.Create(FXCM, 2),
            Tuple.Create(Oanda, 3),
            Tuple.Create(Dukascopy, 4),
            Tuple.Create(Bitfinex, 5),

            Tuple.Create(Globex, 6),
            Tuple.Create(NYMEX, 7),
            Tuple.Create(CBOT, 8),
            Tuple.Create(ICE, 9),
            Tuple.Create(CBOE, 10),
            Tuple.Create(India, 11),

            Tuple.Create(GDAX, 12),
            Tuple.Create(Kraken, 13),
            Tuple.Create(Bittrex, 14),
            Tuple.Create(Bithumb, 15),
            Tuple.Create(Binance, 16),
            Tuple.Create(Poloniex, 17),
            Tuple.Create(Coinone, 18),
            Tuple.Create(HitBTC, 19),
            Tuple.Create(OkCoin, 20),
            Tuple.Create(Bitstamp, 21),

            Tuple.Create(COMEX, 22),
            Tuple.Create(CME, 23),
            Tuple.Create(SGX, 24),
            Tuple.Create(HKFE, 25),
            Tuple.Create(NYSELIFFE, 26),

            Tuple.Create(CFE, 33),
            Tuple.Create(FTX, 34),
            Tuple.Create(FTXUS, 35),
            Tuple.Create(BinanceUS, 36),
            Tuple.Create(Bybit, 37),
            Tuple.Create(Coinbase, 38),
            Tuple.Create(InteractiveBrokers, 39),
        };

        static Market()
        {
            // initialize our maps
            foreach (var market in HardcodedMarkets)
            {
                Markets[market.Item1] = market.Item2;
                ReverseMarkets[market.Item2] = market.Item1;
            }
        }

        /// <summary>
        /// USA Market
        /// </summary>
        public const string USA = "usa";

        /// <summary>
        /// Oanda Market
        /// </summary>
        public const string Oanda = "oanda";

        /// <summary>
        /// FXCM Market Hours
        /// </summary>
        public const string FXCM = "fxcm";

        /// <summary>
        /// Dukascopy Market
        /// </summary>
        public const string Dukascopy = "dukascopy";

        /// <summary>
        /// Bitfinex market
        /// </summary>
        public const string Bitfinex = "bitfinex";

        // Futures exchanges

        /// <summary>
        /// CME Globex
        /// </summary>
        public const string Globex = "cmeglobex";

        /// <summary>
        /// NYMEX
        /// </summary>
        public const string NYMEX = "nymex";

        /// <summary>
        /// CBOT
        /// </summary>
        public const string CBOT = "cbot";

        /// <summary>
        /// ICE
        /// </summary>
        public const string ICE = "ice";

        /// <summary>
        /// CBOE
        /// </summary>
        public const string CBOE = "cboe";

        /// <summary>
        /// CFE
        /// </summary>
        public const string CFE = "cfe";

        /// <summary>
        /// NSE - National Stock Exchange
        /// </summary>
        public const string India = "india";

        /// <summary>
        /// Comex
        /// </summary>
        public const string COMEX = "comex";

        /// <summary>
        /// CME
        /// </summary>
        public const string CME = "cme";

        /// <summary>
        /// Singapore Exchange
        /// </summary>
        public const string SGX = "sgx";

        /// <summary>
        /// Hong Kong Exchange
        /// </summary>
        public const string HKFE = "hkfe";

        /// <summary>
        /// London International Financial Futures and Options Exchange
        /// </summary>
        public const string NYSELIFFE = "nyseliffe";

        /// <summary>
        /// GDAX
        /// </summary>
        [Obsolete("The GDAX constant is deprecated. Please use Coinbase instead.")]
        public const string GDAX = Coinbase;

        /// <summary>
        /// Kraken
        /// </summary>
        public const string Kraken = "kraken";

        /// <summary>
        /// Bitstamp
        /// </summary>
        public const string Bitstamp = "bitstamp";

        /// <summary>
        /// OkCoin
        /// </summary>
        public const string OkCoin = "okcoin";

        /// <summary>
        /// Bithumb
        /// </summary>
        public const string Bithumb = "bithumb";

        /// <summary>
        /// Binance
        /// </summary>
        public const string Binance = "binance";

        /// <summary>
        /// Poloniex
        /// </summary>
        public const string Poloniex = "poloniex";

        /// <summary>
        /// Coinone
        /// </summary>
        public const string Coinone = "coinone";

        /// <summary>
        /// HitBTC
        /// </summary>
        public const string HitBTC = "hitbtc";

        /// <summary>
        /// Bittrex
        /// </summary>
        public const string Bittrex = "bittrex";

        /// <summary>
        /// FTX
        /// </summary>
        public const string FTX = "ftx";

        /// <summary>
        /// FTX.US
        /// </summary>
        public const string FTXUS = "ftxus";

        /// <summary>
        /// Binance.US
        /// </summary>
        public const string BinanceUS = "binanceus";

        /// <summary>
        /// Bybit
        /// </summary>
        public const string Bybit = "bybit";

        /// <summary>
        /// Coinbase
        /// </summary>
        public const string Coinbase = "coinbase";

        /// <summary>
        /// InteractiveBrokers market
        /// </summary>
        public const string InteractiveBrokers = "interactivebrokers";

        /// <summary>
        /// Adds the specified market to the map of available markets with the specified identifier.
        /// </summary>
        /// <param name="market">The market string to add</param>
        /// <param name="identifier">The identifier for the market, this value must be positive and less than 1000</param>
        public static void Add(string market, int identifier)
        {
            if (identifier >= MaxMarketIdentifier)
            {
                throw new ArgumentOutOfRangeException(nameof(identifier), Messages.Market.InvalidMarketIdentifier(MaxMarketIdentifier));
            }

            market = market.ToLowerInvariant();

            int marketIdentifier;
            if (Markets.TryGetValue(market, out marketIdentifier) && identifier != marketIdentifier)
            {
                throw new ArgumentException(Messages.Market.TriedToAddExistingMarketWithDifferentIdentifier(market));
            }

            string existingMarket;
            if (ReverseMarkets.TryGetValue(identifier, out existingMarket))
            {
                throw new ArgumentException(Messages.Market.TriedToAddExistingMarketIdentifier(market, existingMarket));
            }

            // update our maps.
            // We make a copy and update the copy, later swap the references so it's thread safe with no lock
            var newMarketDictionary = Markets.ToDictionary(entry => entry.Key,
                entry => entry.Value);
            newMarketDictionary[market] = identifier;

            var newReverseMarketDictionary = ReverseMarkets.ToDictionary(entry => entry.Key,
                entry => entry.Value);
            newReverseMarketDictionary[identifier] = market;

            Markets = newMarketDictionary;
            ReverseMarkets = newReverseMarketDictionary;
        }

        /// <summary>
        /// Gets the market code for the specified market. Returns <c>null</c> if the market is not found
        /// </summary>
        /// <param name="market">The market to check for (case sensitive)</param>
        /// <returns>The internal code used for the market. Corresponds to the value used when calling <see cref="Add"/></returns>
        public static int? Encode(string market)
        {
            return !Markets.TryGetValue(market, out var code) ? null : code;
        }

        /// <summary>
        /// Gets the market string for the specified market code.
        /// </summary>
        /// <param name="code">The market code to be decoded</param>
        /// <returns>The string representation of the market, or null if not found</returns>
        public static string Decode(int code)
        {
            return !ReverseMarkets.TryGetValue(code, out var market) ? null : market;
        }

        /// <summary>
        /// Returns a list of the supported markets
        /// </summary>
        public static List<string> SupportedMarkets()
        {
            return Markets.Keys.ToList();
        }
    }
}
