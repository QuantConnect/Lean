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

namespace QuantConnect.ToolBox.GDAXDownloader
{
    public class GDAXDownloaderSymbolMapper
    {
        private Dictionary<string, string> _tickerMapping = new Dictionary<string, string>()
        {
            { "ALOEUR", "ALO-EUR"},
            { "ALOGBP", "ALO-GBP"},
            { "ALOUSD", "ALO-USD"},
            { "ATOMBTC", "ATOM-BTC"},
            { "ATOMUSD", "ATOM-USD"},
            { "BATETH", "BAT-ETH"},
            { "BATUSDC", "BAT-USDC"},
            { "BCHBTC", "BCH-BTC"},
            { "BCHEUR", "BCH-EUR"},
            { "BCHGBP", "BCH-GBP"},
            { "BCHUSD", "BCH-USD"},
            { "BTCCAD", "BTC-CAD"},
            { "BTCEUR", "BTC-EUR"},
            { "BTCGBP", "BTC-GBP"},
            { "BTCUSD", "BTC-USD"},
            { "BTCUSDC", "BTC-USDC"},
            { "COMPBTC", "COMP-BTC"},
            { "COMPUSD", "COMP-USD"},
            { "CVCUSDC", "CVC-USDC"},
            { "DAIUSD", "DAI-USD"},
            { "DAIUSDC", "DAI-USDC"},
            { "DNTUSDC", "DNT-USDC"},
            { "DSHBTC", "DSH-BTC"},
            { "DSHUSD", "DSH-USD"},
            { "EOSBTC", "EOS-BTC"},
            { "EOSEUR", "EOS-EUR"},
            { "EOSUSD", "EOS-USD"},
            { "ETCBTC", "ETC-BTC"},
            { "ETCEUR", "ETC-EUR"},
            { "ETCGBP", "ETC-GBP"},
            { "ETCUSD", "ETC-USD"},
            { "ETHBTC", "ETH-BTC"},
            { "ETHDAI", "ETH-DAI"},
            { "ETHEUR", "ETH-EUR"},
            { "ETHGBP", "ETH-GBP"},
            { "ETHUSD", "ETH-USD"},
            { "ETHUSDC", "ETH-USDC"},
            { "GNTUSDC", "GNT-USDC"},
            { "KNCBTC", "KNC-BTC"},
            { "KNCUSD", "KNC-USD"},
            { "LIKETH", "LIK-ETH"},
            { "LIKEUR", "LIK-EUR"},
            { "LIKGBP", "LIK-GBP"},
            { "LIKUSD", "LIK-USD"},
            { "LOMUSDC", "LOM-USDC"},
            { "LTCBTC", "LTC-BTC"},
            { "LTCEUR", "LTC-EUR"},
            { "LTCGBP", "LTC-GBP"},
            { "LTCUSD", "LTC-USD"},
            { "MKRBTC", "MKR-BTC"},
            { "MKRUSD", "MKR-USD"},
            { "MKRUSDC", "MKR-USDC"},
            { "MNAUSDC", "MNA-USDC"},
            { "OMGBTC", "OMG-BTC"},
            { "OMGEUR", "OMG-EUR"},
            { "OMGGBP", "OMG-GBP"},
            { "OMGUSD", "OMG-USD"},
            { "OXTUSD", "OXT-USD"},
            { "REPBTC", "REP-BTC"},
            { "REPEUR", "REP-EUR"},
            { "REPUSD", "REP-USD"},
            { "XLMBTC", "XLM-BTC"},
            { "XLMEUR", "XLM-EUR"},
            { "XLMUSD", "XLM-USD"},
            { "XRPBTC", "XRP-BTC"},
            { "XRPEUR", "XRP-EUR"},
            { "XRPGBP", "XRP-GBP"},
            { "XRPUSD", "XRP-USD"},
            { "XTZBTC", "XTZ-BTC"},
            { "XTZEUR", "XTZ-EUR"},
            { "XTZGBP", "XTZ-GBP"},
            { "XTZUSD", "XTZ-USD"},
            { "ZECBTC", "ZEC-BTC"},
            { "ZECUSDC", "ZEC-USDC"},
            { "ZILUSDC", "ZIL-USDC"},
            { "ZRXBTC", "ZRX-BTC"},
            { "ZRXEUR", "ZRX-EUR"},
            { "ZRXUSD", "ZRX-USD"},
        };

        /// <summary>
        /// Map a ticker used in QC with the ticker to use to download history on GDAX
        /// ex : QC use BTCUSD as ticker but we have to ask the history of BTC-USD
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Symbol GetGDAXDownloadSymbol(Symbol symbol)
        {
            if (!_tickerMapping.ContainsKey(symbol.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(symbol.Value), $"Unsupported ticker {symbol.Value}");
            }

            return Symbol.Create(_tickerMapping[symbol.Value], symbol.ID.SecurityType, symbol.ID.Market);
        }
    }
}
