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
using System.Reflection;
using QuantConnect.Brokerages;
using QuantConnect.Securities;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Provides symbol instancs for unit tests
    /// </summary>
    public static class Symbols
    {

        public static readonly Symbol SPY = CreateEquitySymbol("SPY");
        public static readonly Symbol AAPL = CreateEquitySymbol("AAPL");
        public static readonly Symbol MSFT = CreateEquitySymbol("MSFT");
        public static readonly Symbol ZNGA = CreateEquitySymbol("ZNGA");
        public static readonly Symbol FXE = CreateEquitySymbol("FXE");
        public static readonly Symbol LODE = CreateEquitySymbol("LODE");
        public static readonly Symbol IBM = CreateEquitySymbol("IBM");
        public static readonly Symbol GOOG = CreateEquitySymbol("GOOG");

        public static readonly Symbol USDJPY = CreateForexSymbol("USDJPY");
        public static readonly Symbol EURUSD = CreateForexSymbol("EURUSD");
        public static readonly Symbol EURGBP = CreateForexSymbol("EURGBP");
        public static readonly Symbol GBPUSD = CreateForexSymbol("GBPUSD");
        public static readonly Symbol GBPJPY = CreateForexSymbol("GBPJPY");

        public static readonly Symbol BTCUSD = CreateCryptoSymbol("BTCUSD");
        public static readonly Symbol LTCUSD = CreateCryptoSymbol("LTCUSD");
        public static readonly Symbol ETHUSD = CreateCryptoSymbol("ETHUSD");
        public static readonly Symbol BTCEUR = CreateCryptoSymbol("BTCEUR");
        public static readonly Symbol ETHBTC = CreateCryptoSymbol("ETHBTC");

        public static readonly Symbol DE10YBEUR = CreateCfdSymbol("DE10YBEUR", Market.FXCM);
        public static readonly Symbol DE30EUR = CreateCfdSymbol("DE30EUR", Market.FXCM);
        public static readonly Symbol XAGUSD = CreateCfdSymbol("XAGUSD", Market.FXCM);
        public static readonly Symbol XAUUSD = CreateCfdSymbol("XAUUSD", Market.FXCM);

        public static readonly Symbol SPY_Option_Chain = CreateOptionsCanonicalSymbol("SPY");
        public static readonly Symbol SPY_C_192_Feb19_2016 = CreateOptionSymbol("SPY", OptionRight.Call, 192m, new DateTime(2016, 02, 19));
        public static readonly Symbol SPY_P_192_Feb19_2016 = CreateOptionSymbol("SPY", OptionRight.Put, 192m, new DateTime(2016, 02, 19));

        public static readonly Symbol Fut_SPY_Feb19_2016 = CreateFutureSymbol(Futures.Indices.SP500EMini, new DateTime(2016, 02, 19));
        public static readonly Symbol Fut_SPY_Mar19_2016 = CreateFutureSymbol(Futures.Indices.SP500EMini, new DateTime(2016, 03, 19));

        public static readonly Symbol ES_Future_Chain = CreateFuturesCanonicalSymbol(Futures.Indices.SP500EMini);
        public static readonly Symbol Future_ESZ18_Dec2018 = CreateFutureSymbol(Futures.Indices.SP500EMini, new DateTime(2018, 12, 21));
        public static readonly Symbol Future_CLF19_Jan2019 = CreateFutureSymbol("CL", new DateTime(2018, 12, 19));

        /// <summary>
        /// Can be supplied in TestCase attribute
        /// </summary>
        public enum SymbolsKey
        {
            SPY,
            AAPL,
            MSFT,
            ZNGA,
            FXE,
            USDJPY,
            EURUSD,
            BTCUSD,
            EURGBP,
            GBPUSD,
            DE10YBEUR,
            SPY_C_192_Feb19_2016,
            SPY_P_192_Feb19_2016,
            Fut_SPY_Feb19_2016,
            Fut_SPY_Mar19_2016
        }

        /// <summary>
        /// Convert key into symbol instance
        /// </summary>
        /// <param name="key">the symbol key</param>
        /// <returns>The matching symbol instance</returns>
        /// <remarks>Using reflection minimizes maintenance but is slower at runtime.</remarks>
        public static Symbol Lookup(SymbolsKey key)
        {
            return (Symbol)typeof(Symbols).GetField(key.ToString(), BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        private static Symbol CreateForexSymbol(string symbol)
        {
            return Symbol.Create(symbol, SecurityType.Forex, Market.Oanda);
        }

        private static Symbol CreateEquitySymbol(string symbol)
        {
            return Symbol.Create(symbol, SecurityType.Equity, Market.USA);
        }
        private static Symbol CreateFutureSymbol(string symbol, DateTime expiry)
        {
            string market;
            if (!SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(symbol, SecurityType.Future, out market))
            {
                market = DefaultBrokerageModel.DefaultMarketMap[SecurityType.Future];
            }
            return Symbol.CreateFuture(symbol, market, expiry);
        }

        private static Symbol CreateCfdSymbol(string symbol, string market)
        {
            return Symbol.Create(symbol, SecurityType.Cfd, market);
        }

        private static Symbol CreateOptionSymbol(string symbol, OptionRight right, decimal strike, DateTime expiry)
        {
            return Symbol.CreateOption(symbol, Market.USA, OptionStyle.American, right, strike, expiry);
        }

        private static Symbol CreateCryptoSymbol(string symbol)
        {
            return Symbol.Create(symbol, SecurityType.Crypto, Market.GDAX);
        }

        private static Symbol CreateOptionsCanonicalSymbol(string underlying)
        {
            return Symbol.Create(underlying, SecurityType.Option, Market.USA, "?" + underlying);
        }

        private static Symbol CreateFuturesCanonicalSymbol(string ticker)
        {
            string market;
            if (!SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(ticker, SecurityType.Future, out market))
            {
                market = DefaultBrokerageModel.DefaultMarketMap[SecurityType.Future];
            }
            return Symbol.Create(ticker, SecurityType.Future, market, "/" + ticker);
        }
    }
}
