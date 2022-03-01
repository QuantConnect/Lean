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

using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.ToolBox.BitfinexDownloader
{
    /// <summary>
    /// Bitfinex implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class BitfinexExchangeInfoDownloader : IExchangeInfoDownloader
    {
        /// <summary>
        /// Currency mapping
        /// </summary>
        private readonly Dictionary<string, string> _oldWithNewCurrencies = new()
        {
            { "UST", "USDT" },
            { "ALG", "ALGO" },
            { "AMP", "AMPL" },
            { "ATO", "ATOM" },
            { "B21X", "B21" },
            { "XCH", "XCHF" },
            { "DAT", "DATA" },
            { "DOG", "MDOG" },
            { "DSH", "DASH" },
            { "ETH2X", "ETH2" },
            { "EUS", "EURS" },
            { "EUT", "EURT" },
            { "GNT", "GLM" },
            { "IDX", "ID" },
            { "IOT", "IOTA" },
            { "PAS", "PASS" },
            { "QTM", "QTUM" },
            { "RBT", "RBTC" },
            { "REP", "REP2" },
            { "STJ", "STORJ" },
            { "TSD", "TUSD" },
            { "UDC", "USDC" },
            { "WBT", "WBTC" },
            { "OMN", "OMNI" },
            { "MNA", "MANA" },
        };

        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.Bitfinex;

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            const string tradingPairsUrl = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange";
            const string currenciesUrl = "https://api-pub.bitfinex.com/v2/conf/pub:list:currency";
            const string pairInfosUrl = "https://api-pub.bitfinex.com/v2/conf/pub:info:pair";
            const string pairLabelUrl = "https://api-pub.bitfinex.com/v2/conf/pub:map:currency:label";
            Dictionary<string, string> headers = new() { { "User-Agent", ".NET Client" } };

            // Fetch trading pairs
            var json = tradingPairsUrl.DownloadData(headers);
            var tradingPairs = JToken.Parse(json).First.ToObject<List<string>>();

            // Fetch currencies
            json = currenciesUrl.DownloadData(headers);
            var currencies = JToken.Parse(json).First.ToObject<List<string>>();

            // Fetch pair info
            Dictionary<string, List<string>> pairsInfo = new();
            json = pairInfosUrl.DownloadData(headers);
            var jObject = JToken.Parse(json);
            foreach (var kvp in jObject.First)
            {
                pairsInfo[kvp[0].ToString()] = kvp[1].ToObject<List<string>>();
            }

            // Fetch trading label
            Dictionary<string, string> currencyLabel = new();
            json = pairLabelUrl.DownloadData(headers);
            jObject = JToken.Parse(json);
            foreach (var kvp in jObject.First)
            {
                currencyLabel[kvp[0].ToString()] = kvp[1].ToString();
            }

            List<string> result = new();
            foreach (var tradingPair in tradingPairs)
            {
                // market,symbol,type,description,quote_currency,contract_multiplier,minimum_price_variation,lot_size,market_ticker,minimum_order_size
                var symbol = tradingPair.Replace(":", string.Empty);
                var quoteCurrency = currencies.Where(x => tradingPair.EndsWith(x)).OrderByDescending(s => s.Length).First();
                var baseCurrency = symbol.RemoveFromEnd(quoteCurrency);
                var quoteLabel = quoteCurrency;
                var baseLabel = baseCurrency;

                // Use old currency symbols registered with LEAN
                if (_oldWithNewCurrencies.TryGetValue(quoteCurrency, out string oldQuoteCurrency))
                {
                    symbol = baseCurrency + oldQuoteCurrency;
                }
                if (_oldWithNewCurrencies.TryGetValue(baseCurrency, out string oldBaseCurrency))
                {
                    symbol = oldBaseCurrency + (oldQuoteCurrency ?? quoteCurrency);
                }

                // Get Full Name of the currency
                if (!currencyLabel.TryGetValue(quoteCurrency, out string quoteLabelValue))
                {
                    Log.Trace($"BitfinexExchangeInfoDownloader.Get(): missing label value for currency {quoteCurrency} using {quoteLabel} instead");
                }
                else
                {
                    quoteLabel = quoteLabelValue;
                }
                if (!currencyLabel.TryGetValue(baseCurrency, out string baseLabelValue))
                {
                    Log.Trace($"BitfinexExchangeInfoDownloader.Get(): missing label value for currency {baseCurrency} using {baseLabel} instead");
                }
                else
                {
                    baseLabel = baseLabelValue;
                }

                var description = $"{baseLabel}-{quoteLabel}";
                var contractMultiplier = 1;
                // default value for minimum_price_variation
                var minimum_price_variation = "0.00001";
                // default value for lot_size
                var lot_size = "0.00000001";
                // follow exchange reference format
                var marketTicker = "t" + tradingPair;
                var minimum_order_size = pairsInfo[tradingPair][3];
                var resultLine = $"{Market},{symbol},crypto,{description},{oldQuoteCurrency ?? quoteCurrency},{contractMultiplier},{minimum_price_variation},{lot_size},{marketTicker},{minimum_order_size}";
                result.Add(resultLine);
            }
            return result.OrderBy(x => x.Split(",")[1]);
        }
    }
}
