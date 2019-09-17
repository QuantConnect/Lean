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
using System.Linq;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Bitfinex symbols.
    /// </summary>
    public class BitfinexSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Symbols that are both active and delisted
        /// </summary>
        public static List<Symbol> KnownSymbols
        {
            get
            {
                var symbols = new List<Symbol>();
                var mapper = new BitfinexSymbolMapper();
                foreach (var tp in KnownSymbolStrings)
                {
                    symbols.Add(mapper.GetLeanSymbol(tp, mapper.GetBrokerageSecurityType(tp), Market.Bitfinex));
                }
                return symbols;
            }
        }

        /// <summary>
        /// The list of known Bitfinex symbols.
        /// https://api.bitfinex.com/v1/symbols
        /// </summary>
        public static readonly HashSet<string> KnownSymbolStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BTCUSD","LTCUSD","LTCBTC","ETHUSD","ETHBTC","ETCBTC","ETCUSD","RRTUSD","RRTBTC","ZECUSD",
            "ZECBTC","XMRUSD","XMRBTC","DSHUSD","DSHBTC","BTCEUR","BTCJPY","XRPUSD","XRPBTC","IOTUSD",
            "IOTBTC","IOTETH","EOSUSD","EOSBTC","EOSETH","SANUSD","SANBTC","SANETH","OMGUSD","OMGBTC",
            "OMGETH","BCHUSD","BCHBTC","BCHETH","NEOUSD","NEOBTC","NEOETH","ETPUSD","ETPBTC","ETPETH",
            "QTMUSD","QTMBTC","QTMETH","AVTUSD","AVTBTC","AVTETH","EDOUSD","EDOBTC","EDOETH","BTGUSD",
            "BTGBTC","DATUSD","DATBTC","DATETH","QSHUSD","QSHBTC","QSHETH","YYWUSD","YYWBTC","YYWETH",
            "GNTUSD","GNTBTC","GNTETH","SNTUSD","SNTBTC","SNTETH","IOTEUR","BATUSD","BATBTC","BATETH",
            "MNAUSD","MNABTC","MNAETH","FUNUSD","FUNBTC","FUNETH","ZRXUSD","ZRXBTC","ZRXETH","TNBUSD",
            "TNBBTC","TNBETH","SPKUSD","SPKBTC","SPKETH","TRXUSD","TRXBTC","TRXETH","RCNUSD","RCNBTC",
            "RCNETH","RLCUSD","RLCBTC","RLCETH","AIDUSD","AIDBTC","AIDETH","SNGUSD","SNGBTC","SNGETH",
            "REPUSD","REPBTC","REPETH","ELFUSD","ELFBTC","ELFETH","BTCGBP","ETHEUR","ETHJPY","ETHGBP",
            "NEOEUR","NEOJPY","NEOGBP","EOSEUR","EOSJPY","EOSGBP","IOTJPY","IOTGBP","IOSUSD","IOSBTC",
            "IOSETH","AIOUSD","AIOBTC","AIOETH","REQUSD","REQBTC","REQETH","RDNUSD","RDNBTC","RDNETH",
            "LRCUSD","LRCBTC","LRCETH","WAXUSD","WAXBTC","WAXETH","DAIUSD","DAIBTC","DAIETH","CFIUSD",
            "CFIBTC","CFIETH","AGIUSD","AGIBTC","AGIETH","BFTUSD","BFTBTC","BFTETH","MTNUSD","MTNBTC",
            "MTNETH","ODEUSD","ODEBTC","ODEETH","ANTUSD","ANTBTC","ANTETH","DTHUSD","DTHBTC","DTHETH",
            "MITUSD","MITBTC","MITETH","STJUSD","STJBTC","STJETH","XLMUSD","XLMEUR","XLMJPY","XLMGBP",
            "XLMBTC","XLMETH","XVGUSD","XVGEUR","XVGJPY","XVGGBP","XVGBTC","XVGETH","BCIUSD","BCIBTC",
            "MKRUSD","MKRBTC","MKRETH","VENUSD","VENBTC","VENETH","KNCUSD","KNCBTC","KNCETH","POAUSD",
            "POABTC","POAETH","LYMUSD","LYMBTC","LYMETH","UTKUSD","UTKBTC","UTKETH","VEEUSD","VEEBTC",
            "VEEETH","DADUSD","DADBTC","DADETH","ORSUSD","ORSBTC","ORSETH","AUCUSD","AUCBTC","AUCETH",
            "POYUSD","POYBTC","POYETH","FSNUSD","FSNBTC","FSNETH","CBTUSD","CBTBTC","CBTETH","ZCNUSD",
            "ZCNBTC","ZCNETH","SENUSD","SENBTC","SENETH","NCAUSD","NCABTC","NCAETH","CNDUSD","CNDBTC",
            "CNDETH","CTXUSD","CTXBTC","CTXETH","PAIUSD","PAIBTC","SEEUSD","SEEBTC","SEEETH","ESSUSD",
            "ESSBTC","ESSETH","ATMUSD","ATMBTC","ATMETH","HOTUSD","HOTBTC","HOTETH","DTAUSD","DTABTC",
            "DTAETH","IQXUSD","IQXBTC","IQXEOS","WPRUSD","WPRBTC","WPRETH","ZILUSD","ZILBTC","ZILETH",
            "BNTUSD","BNTBTC","BNTETH","ABSUSD","ABSETH","XRAUSD","XRAETH","MANUSD","MANETH","BBNUSD",
            "BBNETH","NIOUSD","NIOETH","DGXUSD","DGXETH","VETUSD","VETBTC","VETETH","UTNUSD","UTNETH",
            "TKNUSD","TKNETH","GOTUSD","GOTEUR","GOTETH","XTZUSD","XTZBTC","CNNUSD","CNNETH","BOXUSD",
            "BOXETH","TRXEUR","TRXGBP","TRXJPY","MGOUSD","MGOETH","RTEUSD","RTEETH","YGGUSD","YGGETH",
            "MLNUSD","MLNETH","WTCUSD","WTCETH","CSXUSD","CSXETH","OMNUSD","OMNBTC","INTUSD","INTETH",
            "DRNUSD","DRNETH","PNKUSD","PNKETH","DGBUSD","DGBBTC","BSVUSD","BSVBTC","BABUSD","BABBTC",
            "WLOUSD","WLOXLM","VLDUSD","VLDETH","ENJUSD","ENJETH","ONLUSD","ONLETH","RBTUSD","RBTBTC",
            "USTUSD","EUTEUR","EUTUSD","GSDUSD","UDCUSD","TSDUSD","PAXUSD","RIFUSD","RIFBTC","PASUSD",
            "PASETH","VSYUSD","VSYBTC","ZRXDAI","MKRDAI","OMGDAI"
        };

        /// <summary>
        /// The list of delisted/invalid Bitfinex symbols.
        /// </summary>
        public static HashSet<string> DelistedSymbolStrings = new HashSet<string>
        {
            "BCHUSD","BCHBTC","BCHETH",
            "CFIUSD","CFIBTC","CFIETH",
            "VENUSD","VENBTC","VENETH"
        };

        /// <summary>
        /// The list of active Bitfinex symbols.
        /// </summary>
        public static List<string> ActiveSymbolStrings =
            KnownSymbolStrings
                .Where(x => !DelistedSymbolStrings.Contains(x))
                .ToList();

        /// <summary>
        /// The list of known Bitfinex currencies.
        /// </summary>
        private static readonly HashSet<string> KnownCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EUR", "GBP", "JPY", "USD"
        };

        /// <summary>
        /// Converts a Lean symbol instance to an Bitfinex symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Bitfinex symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Crypto)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToBitfinexSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Bitfinex symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Bitfinex symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Bitfinex symbol: {brokerageSymbol}");

            if (securityType != SecurityType.Crypto)
                throw new ArgumentException($"Invalid security type: {securityType}");

            if (market != Market.Bitfinex)
                throw new ArgumentException($"Invalid market: {market}");

            return Symbol.Create(ConvertBitfinexSymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.Bitfinex);
        }

        /// <summary>
        /// Converts an Bitfinex symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            return GetLeanSymbol(brokerageSymbol, securityType, Market.Bitfinex);
        }

        /// <summary>
        /// Returns the security type for an Bitfinex symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Bitfinex symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Bitfinex symbol: {brokerageSymbol}");

            return SecurityType.Crypto;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToBitfinexSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Bitfinex
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <returns>True if Bitfinex supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            return KnownSymbolStrings.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the currency is supported by Bitfinex
        /// </summary>
        /// <returns>True if Bitfinex supports the currency</returns>
        public bool IsKnownFiatCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return false;

            return KnownCurrencies.Contains(currency);
        }

        /// <summary>
        /// Checks if the symbol is supported by Bitfinex
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Bitfinex supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value) || symbol.Value.Length <= 3)
                return false;

            var bitfinexSymbol = ConvertLeanSymbolToBitfinexSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(bitfinexSymbol) && GetBrokerageSecurityType(bitfinexSymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Bitfinex symbol to a Lean symbol string
        /// </summary>
        private static string ConvertBitfinexSymbolToLeanSymbol(string bitfinexSymbol)
        {
            if (string.IsNullOrWhiteSpace(bitfinexSymbol))
                throw new ArgumentException($"Invalid Bitfinex symbol: {bitfinexSymbol}");

            // return as it is due to Bitfinex has similar Symbol format
            return bitfinexSymbol.ToUpperInvariant();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Bitfinex symbol
        /// </summary>
        private static string ConvertLeanSymbolToBitfinexSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");

            // return as it is due to Bitfinex has similar Symbol format
            return leanSymbol.ToUpperInvariant();
        }
    }
}
