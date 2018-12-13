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

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Oanda symbols.
    /// </summary>
    public class OandaSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Symbols that are both active and delisted
        /// </summary>
        public static List<Symbol> KnownSymbols
        {
            get
            {
                var symbols = new List<Symbol>();
                var mapper = new OandaSymbolMapper();
                foreach (var tp in KnownSymbolStrings)
                {
                    symbols.Add(mapper.GetLeanSymbol(tp, mapper.GetBrokerageSecurityType(tp), QuantConnect.Market.Oanda));
                }
                return symbols;
            }
        }

        /// <summary>
        /// Symbols that have been delisted from Oanda
        /// </summary>
        public static List<Symbol> DelistedSymbols
        {
            get
            {
                var symbols = new List<Symbol>();
                var mapper = new OandaSymbolMapper();
                foreach (var tp in DelistedSymbolStrings)
                {
                    symbols.Add(mapper.GetLeanSymbol(tp, mapper.GetBrokerageSecurityType(tp), QuantConnect.Market.Oanda));
                }
                return symbols;
            }
        }

        /// <summary>
        /// Symbols that are active on Oanda
        /// </summary>
        public static List<Symbol> ActiveSymbols
        {
            get
            {
                var symbols = new List<Symbol>();
                var mapper = new OandaSymbolMapper();
                foreach (var tp in KnownSymbolStrings.Where(x => !DelistedSymbolStrings.Contains(x)))
                {
                    symbols.Add(mapper.GetLeanSymbol(tp, mapper.GetBrokerageSecurityType(tp), QuantConnect.Market.Oanda));
                }
                return symbols;
            }
        }


        /// <summary>
        /// The list of known Oanda symbols.
        /// </summary>
        public static readonly HashSet<string> KnownSymbolStrings = new HashSet<string>
        {
            "AU200_AUD",
            "AUD_CAD",
            "AUD_CHF",
            "AUD_CNY",
            "AUD_CZK",
            "AUD_DKK",
            "AUD_HKD",
            "AUD_HUF",
            "AUD_INR",
            "AUD_JPY",
            "AUD_MXN",
            "AUD_NOK",
            "AUD_NZD",
            "AUD_PLN",
            "AUD_SAR",
            "AUD_SEK",
            "AUD_SGD",
            "AUD_THB",
            "AUD_TRY",
            "AUD_TWD",
            "AUD_USD",
            "AUD_ZAR",
            "BCO_USD",
            "CAD_AUD",
            "CAD_CHF",
            "CAD_CNY",
            "CAD_CZK",
            "CAD_DKK",
            "CAD_HKD",
            "CAD_HUF",
            "CAD_INR",
            "CAD_JPY",
            "CAD_MXN",
            "CAD_NOK",
            "CAD_NZD",
            "CAD_PLN",
            "CAD_SAR",
            "CAD_SEK",
            "CAD_SGD",
            "CAD_THB",
            "CAD_TRY",
            "CAD_TWD",
            "CAD_ZAR",
            "CH20_AUD",
            "CH20_CAD",
            "CH20_CHF",
            "CH20_EUR",
            "CH20_GBP",
            "CH20_HKD",
            "CH20_JPY",
            "CH20_SGD",
            "CH20_USD",
            "CHF_AUD",
            "CHF_CAD",
            "CHF_CNY",
            "CHF_CZK",
            "CHF_DKK",
            "CHF_HKD",
            "CHF_HUF",
            "CHF_INR",
            "CHF_JPY",
            "CHF_MXN",
            "CHF_NOK",
            "CHF_NZD",
            "CHF_PLN",
            "CHF_SAR",
            "CHF_SEK",
            "CHF_SGD",
            "CHF_THB",
            "CHF_TRY",
            "CHF_TWD",
            "CHF_USD",
            "CHF_ZAR",
            "CNH_USD",
            "CNY_JPY",
            "CORN_AUD",
            "CORN_CAD",
            "CORN_CHF",
            "CORN_EUR",
            "CORN_GBP",
            "CORN_HKD",
            "CORN_JPY",
            "CORN_SGD",
            "CORN_USD",
            "CZK_JPY",
            "DAILY100_USD",
            "DE10YB_AUD",
            "DE10YB_CAD",
            "DE10YB_CHF",
            "DE10YB_EUR",
            "DE10YB_GBP",
            "DE10YB_HKD",
            "DE10YB_JPY",
            "DE10YB_SGD",
            "DE10YB_USD",
            "DE30_AUD",
            "DE30_CAD",
            "DE30_CHF",
            "DE30_EUR",
            "DE30_GBP",
            "DE30_HKD",
            "DE30_JPY",
            "DE30_SGD",
            "DE30_USD",
            "DKK_JPY",
            "EU50_AUD",
            "EU50_CAD",
            "EU50_CHF",
            "EU50_EUR",
            "EU50_GBP",
            "EU50_HKD",
            "EU50_JPY",
            "EU50_SGD",
            "EU50_USD",
            "EUR_AUD",
            "EUR_CAD",
            "EUR_CHF",
            "EUR_CNY",
            "EUR_CZK",
            "EUR_DKK",
            "EUR_GBP",
            "EUR_HKD",
            "EUR_HUF",
            "EUR_INR",
            "EUR_JPY",
            "EUR_MXN",
            "EUR_NOK",
            "EUR_NZD",
            "EUR_PLN",
            "EUR_SAR",
            "EUR_SEK",
            "EUR_SGD",
            "EUR_THB",
            "EUR_TRY",
            "EUR_TWD",
            "EUR_USD",
            "EUR_ZAR",
            "FR40_AUD",
            "FR40_CAD",
            "FR40_CHF",
            "FR40_EUR",
            "FR40_GBP",
            "FR40_HKD",
            "FR40_JPY",
            "FR40_SGD",
            "FR40_USD",
            "GBP_AUD",
            "GBP_CAD",
            "GBP_CHF",
            "GBP_CNY",
            "GBP_CZK",
            "GBP_DKK",
            "GBP_HKD",
            "GBP_HUF",
            "GBP_INR",
            "GBP_JPY",
            "GBP_MXN",
            "GBP_NOK",
            "GBP_NZD",
            "GBP_PLN",
            "GBP_SAR",
            "GBP_SEK",
            "GBP_SGD",
            "GBP_THB",
            "GBP_TRY",
            "GBP_TWD",
            "GBP_USD",
            "GBP_ZAR",
            "HK33_AUD",
            "HK33_CAD",
            "HK33_CHF",
            "HK33_EUR",
            "HK33_GBP",
            "HK33_HKD",
            "HK33_JPY",
            "HK33_SGD",
            "HK33_USD",
            "HKD_CNY",
            "HKD_CZK",
            "HKD_DKK",
            "HKD_HUF",
            "HKD_INR",
            "HKD_JPY",
            "HKD_MXN",
            "HKD_NOK",
            "HKD_PLN",
            "HKD_SAR",
            "HKD_SEK",
            "HKD_SGD",
            "HKD_THB",
            "HKD_TRY",
            "HKD_TWD",
            "HKD_ZAR",
            "INR_JPY",
            "JP225_AUD",
            "JP225_CAD",
            "JP225_CHF",
            "JP225_EUR",
            "JP225_GBP",
            "JP225_HKD",
            "JP225_JPY",
            "JP225_SGD",
            "JP225_USD",
            "JPY_HUF",
            "JPY_USD",
            "MXN_JPY",
            "NAS100_AUD",
            "NAS100_CAD",
            "NAS100_CHF",
            "NAS100_EUR",
            "NAS100_GBP",
            "NAS100_HKD",
            "NAS100_JPY",
            "NAS100_SGD",
            "NAS100_USD",
            "NATGAS_AUD",
            "NATGAS_CAD",
            "NATGAS_CHF",
            "NATGAS_EUR",
            "NATGAS_GBP",
            "NATGAS_HKD",
            "NATGAS_JPY",
            "NATGAS_SGD",
            "NATGAS_USD",
            "NL25_AUD",
            "NL25_CAD",
            "NL25_CHF",
            "NL25_EUR",
            "NL25_GBP",
            "NL25_HKD",
            "NL25_JPY",
            "NL25_SGD",
            "NL25_USD",
            "NOK_JPY",
            "NZD_CAD",
            "NZD_CHF",
            "NZD_HKD",
            "NZD_JPY",
            "NZD_SGD",
            "NZD_USD",
            "PLN_JPY",
            "SAR_JPY",
            "SEK_JPY",
            "SG30_SGD",
            "SGD_CHF",
            "SGD_CNY",
            "SGD_CZK",
            "SGD_DKK",
            "SGD_HKD",
            "SGD_HUF",
            "SGD_INR",
            "SGD_JPY",
            "SGD_MXN",
            "SGD_NOK",
            "SGD_PLN",
            "SGD_SAR",
            "SGD_SEK",
            "SGD_THB",
            "SGD_TRY",
            "SGD_TWD",
            "SGD_ZAR",
            "SOYBN_AUD",
            "SOYBN_CAD",
            "SOYBN_CHF",
            "SOYBN_EUR",
            "SOYBN_GBP",
            "SOYBN_HKD",
            "SOYBN_JPY",
            "SOYBN_SGD",
            "SOYBN_USD",
            "SPX500_AUD",
            "SPX500_CAD",
            "SPX500_CHF",
            "SPX500_EUR",
            "SPX500_GBP",
            "SPX500_HKD",
            "SPX500_JPY",
            "SPX500_SGD",
            "SPX500_USD",
            "SUGAR_AUD",
            "SUGAR_CAD",
            "SUGAR_CHF",
            "SUGAR_EUR",
            "SUGAR_GBP",
            "SUGAR_HKD",
            "SUGAR_JPY",
            "SUGAR_SGD",
            "SUGAR_USD",
            "THB_JPY",
            "TRY_JPY",
            "TWD_JPY",
            "UK100_AUD",
            "UK100_CAD",
            "UK100_CHF",
            "UK100_EUR",
            "UK100_GBP",
            "UK100_HKD",
            "UK100_JPY",
            "UK100_SGD",
            "UK100_USD",
            "UK10YB_AUD",
            "UK10YB_CAD",
            "UK10YB_CHF",
            "UK10YB_EUR",
            "UK10YB_GBP",
            "UK10YB_HKD",
            "UK10YB_JPY",
            "UK10YB_SGD",
            "UK10YB_USD",
            "US2000_AUD",
            "US2000_CAD",
            "US2000_CHF",
            "US2000_EUR",
            "US2000_GBP",
            "US2000_HKD",
            "US2000_JPY",
            "US2000_SGD",
            "US2000_USD",
            "US30_AUD",
            "US30_CAD",
            "US30_CHF",
            "US30_EUR",
            "US30_GBP",
            "US30_HKD",
            "US30_JPY",
            "US30_SGD",
            "US30_USD",
            "USB02Y_AUD",
            "USB02Y_CAD",
            "USB02Y_CHF",
            "USB02Y_EUR",
            "USB02Y_GBP",
            "USB02Y_HKD",
            "USB02Y_JPY",
            "USB02Y_SGD",
            "USB02Y_USD",
            "USB05Y_AUD",
            "USB05Y_CAD",
            "USB05Y_CHF",
            "USB05Y_EUR",
            "USB05Y_GBP",
            "USB05Y_HKD",
            "USB05Y_JPY",
            "USB05Y_SGD",
            "USB05Y_USD",
            "USB10Y_AUD",
            "USB10Y_CAD",
            "USB10Y_CHF",
            "USB10Y_EUR",
            "USB10Y_GBP",
            "USB10Y_HKD",
            "USB10Y_JPY",
            "USB10Y_SGD",
            "USB10Y_USD",
            "USB30Y_AUD",
            "USB30Y_CAD",
            "USB30Y_CHF",
            "USB30Y_EUR",
            "USB30Y_GBP",
            "USB30Y_HKD",
            "USB30Y_JPY",
            "USB30Y_SGD",
            "USB30Y_USD",
            "USD_AUD",
            "USD_CAD",
            "USD_CHF",
            "USD_CNH",
            "USD_CNY",
            "USD_CZK",
            "USD_DKK",
            "USD_EUR",
            "USD_GBP",
            "USD_HKD",
            "USD_HUF",
            "USD_INR",
            "USD_JPY",
            "USD_MXN",
            "USD_NOK",
            "USD_PLN",
            "USD_SAR",
            "USD_SEK",
            "USD_SGD",
            "USD_THB",
            "USD_TRY",
            "USD_TWD",
            "USD_ZAR",
            "WHEAT_AUD",
            "WHEAT_CAD",
            "WHEAT_CHF",
            "WHEAT_EUR",
            "WHEAT_GBP",
            "WHEAT_HKD",
            "WHEAT_JPY",
            "WHEAT_SGD",
            "WHEAT_USD",
            "WTICO_AUD",
            "WTICO_CAD",
            "WTICO_CHF",
            "WTICO_EUR",
            "WTICO_GBP",
            "WTICO_HKD",
            "WTICO_JPY",
            "WTICO_SGD",
            "WTICO_USD",
            "XAG_AUD",
            "XAG_CAD",
            "XAG_CHF",
            "XAG_EUR",
            "XAG_GBP",
            "XAG_HKD",
            "XAG_JPY",
            "XAG_NZD",
            "XAG_SGD",
            "XAG_USD",
            "XAU_AUD",
            "XAU_CAD",
            "XAU_CHF",
            "XAU_EUR",
            "XAU_GBP",
            "XAU_HKD",
            "XAU_JPY",
            "XAU_NZD",
            "XAU_SGD",
            "XAU_USD",
            "XAU_XAG",
            "XCU_AUD",
            "XCU_CAD",
            "XCU_CHF",
            "XCU_EUR",
            "XCU_GBP",
            "XCU_HKD",
            "XCU_JPY",
            "XCU_SGD",
            "XCU_USD",
            "XPD_AUD",
            "XPD_CAD",
            "XPD_CHF",
            "XPD_EUR",
            "XPD_GBP",
            "XPD_HKD",
            "XPD_JPY",
            "XPD_SGD",
            "XPD_USD",
            "XPT_AUD",
            "XPT_CAD",
            "XPT_CHF",
            "XPT_EUR",
            "XPT_GBP",
            "XPT_HKD",
            "XPT_JPY",
            "XPT_SGD",
            "XPT_USD",
            "ZAR_JPY"
        };

        /// <summary>
        /// The list of delisted/invalid Oanda symbols.
        /// </summary>
        public static HashSet<string> DelistedSymbolStrings = new HashSet<string>
        {
            "AUD_CNY",
            "AUD_CZK",
            "AUD_DKK",
            "AUD_HUF",
            "AUD_INR",
            "AUD_MXN",
            "AUD_NOK",
            "AUD_PLN",
            "AUD_SAR",
            "AUD_SEK",
            "AUD_THB",
            "AUD_TRY",
            "AUD_TWD",
            "AUD_ZAR",
            "CAD_AUD",
            "CAD_CNY",
            "CAD_CZK",
            "CAD_DKK",
            "CAD_HUF",
            "CAD_INR",
            "CAD_MXN",
            "CAD_NOK",
            "CAD_NZD",
            "CAD_PLN",
            "CAD_SAR",
            "CAD_SEK",
            "CAD_THB",
            "CAD_TRY",
            "CAD_TWD",
            "CAD_ZAR",
            "CH20_AUD",
            "CH20_CAD",
            "CH20_EUR",
            "CH20_GBP",
            "CH20_HKD",
            "CH20_JPY",
            "CH20_SGD",
            "CH20_USD",
            "CHF_AUD",
            "CHF_CAD",
            "CHF_CNY",
            "CHF_CZK",
            "CHF_DKK",
            "CHF_HUF",
            "CHF_INR",
            "CHF_MXN",
            "CHF_NOK",
            "CHF_NZD",
            "CHF_PLN",
            "CHF_SAR",
            "CHF_SEK",
            "CHF_SGD",
            "CHF_THB",
            "CHF_TRY",
            "CHF_TWD",
            "CHF_USD",
            "CNH_USD",
            "CNY_JPY",
            "CORN_AUD",
            "CORN_CAD",
            "CORN_CHF",
            "CORN_EUR",
            "CORN_GBP",
            "CORN_HKD",
            "CORN_JPY",
            "CORN_SGD",
            "CZK_JPY",
            "DAILY100_USD",
            "DE10YB_AUD",
            "DE10YB_CAD",
            "DE10YB_CHF",
            "DE10YB_GBP",
            "DE10YB_HKD",
            "DE10YB_JPY",
            "DE10YB_SGD",
            "DE10YB_USD",
            "DE30_AUD",
            "DE30_CAD",
            "DE30_CHF",
            "DE30_GBP",
            "DE30_HKD",
            "DE30_JPY",
            "DE30_SGD",
            "DE30_USD",
            "DKK_JPY",
            "EU50_AUD",
            "EU50_CAD",
            "EU50_CHF",
            "EU50_GBP",
            "EU50_HKD",
            "EU50_JPY",
            "EU50_SGD",
            "EU50_USD",
            "EUR_CNY",
            "EUR_INR",
            "EUR_MXN",
            "EUR_SAR",
            "EUR_THB",
            "EUR_TWD",
            "FR40_AUD",
            "FR40_CAD",
            "FR40_CHF",
            "FR40_GBP",
            "FR40_HKD",
            "FR40_JPY",
            "FR40_SGD",
            "FR40_USD",
            "GBP_CNY",
            "GBP_CZK",
            "GBP_DKK",
            "GBP_HUF",
            "GBP_INR",
            "GBP_MXN",
            "GBP_NOK",
            "GBP_SAR",
            "GBP_SEK",
            "GBP_THB",
            "GBP_TRY",
            "GBP_TWD",
            "HK33_AUD",
            "HK33_CAD",
            "HK33_CHF",
            "HK33_EUR",
            "HK33_GBP",
            "HK33_JPY",
            "HK33_SGD",
            "HK33_USD",
            "HKD_CNY",
            "HKD_CZK",
            "HKD_DKK",
            "HKD_HUF",
            "HKD_INR",
            "HKD_MXN",
            "HKD_NOK",
            "HKD_PLN",
            "HKD_SAR",
            "HKD_SEK",
            "HKD_SGD",
            "HKD_THB",
            "HKD_TRY",
            "HKD_TWD",
            "HKD_ZAR",
            "INR_JPY",
            "JP225_AUD",
            "JP225_CAD",
            "JP225_CHF",
            "JP225_EUR",
            "JP225_GBP",
            "JP225_HKD",
            "JP225_JPY",
            "JP225_SGD",
            "JPY_HUF",
            "JPY_USD",
            "MXN_JPY",
            "NAS100_AUD",
            "NAS100_CAD",
            "NAS100_CHF",
            "NAS100_EUR",
            "NAS100_GBP",
            "NAS100_HKD",
            "NAS100_JPY",
            "NAS100_SGD",
            "NATGAS_AUD",
            "NATGAS_CAD",
            "NATGAS_CHF",
            "NATGAS_EUR",
            "NATGAS_GBP",
            "NATGAS_HKD",
            "NATGAS_JPY",
            "NATGAS_SGD",
            "NL25_AUD",
            "NL25_CAD",
            "NL25_CHF",
            "NL25_GBP",
            "NL25_HKD",
            "NL25_JPY",
            "NL25_SGD",
            "NL25_USD",
            "NOK_JPY",
            "PLN_JPY",
            "SAR_JPY",
            "SEK_JPY",
            "SGD_CNY",
            "SGD_CZK",
            "SGD_DKK",
            "SGD_HUF",
            "SGD_INR",
            "SGD_MXN",
            "SGD_NOK",
            "SGD_PLN",
            "SGD_SAR",
            "SGD_SEK",
            "SGD_THB",
            "SGD_TRY",
            "SGD_TWD",
            "SGD_ZAR",
            "SOYBN_AUD",
            "SOYBN_CAD",
            "SOYBN_CHF",
            "SOYBN_EUR",
            "SOYBN_GBP",
            "SOYBN_HKD",
            "SOYBN_JPY",
            "SOYBN_SGD",
            "SPX500_AUD",
            "SPX500_CAD",
            "SPX500_CHF",
            "SPX500_EUR",
            "SPX500_GBP",
            "SPX500_HKD",
            "SPX500_JPY",
            "SPX500_SGD",
            "SUGAR_AUD",
            "SUGAR_CAD",
            "SUGAR_CHF",
            "SUGAR_EUR",
            "SUGAR_GBP",
            "SUGAR_HKD",
            "SUGAR_JPY",
            "SUGAR_SGD",
            "THB_JPY",
            "TWD_JPY",
            "UK100_AUD",
            "UK100_CAD",
            "UK100_CHF",
            "UK100_EUR",
            "UK100_HKD",
            "UK100_JPY",
            "UK100_SGD",
            "UK100_USD",
            "UK10YB_AUD",
            "UK10YB_CAD",
            "UK10YB_CHF",
            "UK10YB_EUR",
            "UK10YB_HKD",
            "UK10YB_JPY",
            "UK10YB_SGD",
            "UK10YB_USD",
            "US2000_AUD",
            "US2000_CAD",
            "US2000_CHF",
            "US2000_EUR",
            "US2000_GBP",
            "US2000_HKD",
            "US2000_JPY",
            "US2000_SGD",
            "US30_AUD",
            "US30_CAD",
            "US30_CHF",
            "US30_EUR",
            "US30_GBP",
            "US30_HKD",
            "US30_JPY",
            "US30_SGD",
            "USB02Y_AUD",
            "USB02Y_CAD",
            "USB02Y_CHF",
            "USB02Y_EUR",
            "USB02Y_GBP",
            "USB02Y_HKD",
            "USB02Y_JPY",
            "USB02Y_SGD",
            "USB05Y_AUD",
            "USB05Y_CAD",
            "USB05Y_CHF",
            "USB05Y_EUR",
            "USB05Y_GBP",
            "USB05Y_HKD",
            "USB05Y_JPY",
            "USB05Y_SGD",
            "USB10Y_AUD",
            "USB10Y_CAD",
            "USB10Y_CHF",
            "USB10Y_EUR",
            "USB10Y_GBP",
            "USB10Y_HKD",
            "USB10Y_JPY",
            "USB10Y_SGD",
            "USB30Y_AUD",
            "USB30Y_CAD",
            "USB30Y_CHF",
            "USB30Y_EUR",
            "USB30Y_GBP",
            "USB30Y_HKD",
            "USB30Y_JPY",
            "USB30Y_SGD",
            "USD_AUD",
            "USD_EUR",
            "USD_GBP",
            "WHEAT_AUD",
            "WHEAT_CAD",
            "WHEAT_CHF",
            "WHEAT_EUR",
            "WHEAT_GBP",
            "WHEAT_HKD",
            "WHEAT_JPY",
            "WHEAT_SGD",
            "WTICO_AUD",
            "WTICO_CAD",
            "WTICO_CHF",
            "WTICO_EUR",
            "WTICO_GBP",
            "WTICO_HKD",
            "WTICO_JPY",
            "WTICO_SGD",
            "XCU_AUD",
            "XCU_CAD",
            "XCU_CHF",
            "XCU_EUR",
            "XCU_GBP",
            "XCU_HKD",
            "XCU_JPY",
            "XCU_SGD",
            "XPD_AUD",
            "XPD_CAD",
            "XPD_CHF",
            "XPD_EUR",
            "XPD_GBP",
            "XPD_HKD",
            "XPD_JPY",
            "XPD_SGD",
            "XPT_AUD",
            "XPT_CAD",
            "XPT_CHF",
            "XPT_EUR",
            "XPT_GBP",
            "XPT_HKD",
            "XPT_JPY",
            "XPT_SGD",
        };

        /// <summary>
        /// The list of known Oanda currencies.
        /// </summary>
        private static readonly HashSet<string> KnownCurrencies = new HashSet<string>
        {
            "AUD", "CAD", "CHF", "CNH", "CNY", "CZK", "DKK", "EUR", "GBP", "HKD", "HUF", "INR", "JPY",
            "MXN", "NOK", "NZD", "PLN", "SAR", "SEK", "SGD", "THB", "TRY", "TWD", "USD", "ZAR"
        };

        /// <summary>
        /// Converts a Lean symbol instance to an Oanda symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Oanda symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToOandaSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Oanda symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Oanda symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid Oanda symbol: " + brokerageSymbol);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown Oanda symbol: " + brokerageSymbol);

            if (securityType != SecurityType.Forex && securityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + securityType);

            if (market != Market.Oanda)
                throw new ArgumentException("Invalid market: " + market);

            return Symbol.Create(ConvertOandaSymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.Oanda);
        }

        /// <summary>
        /// Returns the security type for an Oanda symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Oanda symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            var tokens = brokerageSymbol.Split('_');
            if (tokens.Length != 2)
                throw new ArgumentException("Unable to determine SecurityType for Oanda symbol: " + brokerageSymbol);

            return KnownCurrencies.Contains(tokens[0]) && KnownCurrencies.Contains(tokens[1])
                ? SecurityType.Forex
                : SecurityType.Cfd;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToOandaSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Oanda
        /// </summary>
        /// <param name="brokerageSymbol">The Oanda symbol</param>
        /// <returns>True if Oanda supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            return KnownSymbolStrings.Contains(brokerageSymbol) && !DelistedSymbolStrings.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Oanda
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Oanda supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value) || symbol.Value.Length <= 3)
                return false;

            var oandaSymbol = ConvertLeanSymbolToOandaSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(oandaSymbol) && GetBrokerageSecurityType(oandaSymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Oanda symbol to a Lean symbol string
        /// </summary>
        private static string ConvertOandaSymbolToLeanSymbol(string oandaSymbol)
        {
            // Lean symbols are equal to Oanda symbols with underscores removed
            return oandaSymbol.Replace("_", "");
        }

        /// <summary>
        /// Converts a Lean symbol string to an Oanda symbol
        /// </summary>
        private static string ConvertLeanSymbolToOandaSymbol(string leanSymbol)
        {
            // All Oanda symbols end with '_XYZ', where XYZ is the quote currency
            return leanSymbol.Insert(leanSymbol.Length - 3, "_");
        }
    }
}
