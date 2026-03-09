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

namespace QuantConnect.Securities.Index
{
    /// <summary>
    /// Helper methods for Index Symbols
    /// </summary>
    public static class IndexSymbol
    {
        private static readonly Dictionary<string, string> _indexExchange = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "SPX", Market.CBOE },
            { "NDX", "NASDAQ" },
            { "VIX", Market.CBOE },
            { "SPXW", Market.CBOE },
            { "NQX", "NASDAQ" },
            { "VIXW", Market.CBOE },
            { "RUT", "RUSSELL" },
            { "BKX", "PHLX" },
            { "BXD", Market.CBOE },
            { "BXM", Market.CBOE },
            { "BXN", Market.CBOE },
            { "BXR", Market.CBOE },
            { "CLL", Market.CBOE },
            { "COR1M", Market.CBOE },
            { "COR1Y", Market.CBOE },
            { "COR30D", Market.CBOE },
            { "COR3M", Market.CBOE },
            { "COR6M", Market.CBOE },
            { "COR9M", Market.CBOE },
            { "DJX", Market.CBOE },
            { "DUX", Market.CBOE },
            { "DVS", Market.CBOE },
            { "DXL", Market.CBOE },
            { "EVZ", Market.CBOE },
            { "FVX", Market.CBOE },
            { "GVZ", Market.CBOE },
            { "HGX", "PHLX" },
            { "MID", "PSE" },
            { "MIDG", Market.CBOE },
            { "MIDV", Market.CBOE },
            { "MRUT", "RUSSELL" },
            { "NYA", "PSE" },
            { "NYFANG", "NYSE" },
            { "NYXBT", "NYSE" },
            { "OEX", Market.CBOE },
            { "OSX", "PHLX" },
            { "OVX", Market.CBOE },
            { "XDA", "PHLX" },
            { "XDB", "PHLX" },
            { "XEO", Market.CBOE },
            { "XMI", "PSE" },
            { "XNDX", "NASDAQ" },
            { "XSP", Market.CBOE },
            { "BRR", Market.CME },
            { "BRTI", Market.CME },
            { "CEX", Market.CBOE },
            { "COMP", "NASDAQ" },
            { "DJCIAGC", Market.CME },
            { "DJCICC", Market.CME },
            { "DJCIGC", Market.CME },
            { "DJCIGR", Market.CME },
            { "DJCIIK", Market.CME },
            { "DJCIKC", Market.CME },
            { "DJCISB", Market.CME },
            { "DJCISI", Market.CME },
            { "DJR", Market.CBOE },
            { "DRG", "PSE" },
            { "PUT", Market.CBOE },
            { "RUA", "RUSSELL" },
            { "RUI", "RUSSELL" },
            { "RVX", Market.CBOE },
            { "SET", Market.CBOE },
            { "SGX", Market.CBOE },
            { "SKEW", Market.CBOE },
            { "SPSIBI", "PSE" },
            { "SVX", Market.CBOE },
            { "TNX", Market.CBOE },
            { "TYX", Market.CBOE },
            { "UKX", "ISE" },
            { "UTY", "PHLX" },
            { "VIF", Market.CBOE },
            { "VIN", Market.CBOE },
            { "VIX1D", Market.CBOE },
            { "VIX1Y", Market.CBOE },
            { "VIX3M", Market.CBOE },
            { "VIX6M", Market.CBOE },
            { "VIX9D", Market.CBOE },
            { "VOLI", "NASDAQ" },
            { "VPD", Market.CBOE },
            { "VPN", Market.CBOE },
            { "VVIX", Market.CBOE },
            { "VWA", Market.CBOE },
            { "VWB", Market.CBOE },
            { "VXD", Market.CBOE },
            { "VXN", Market.CBOE },
            { "VXO", Market.CBOE },
            { "VXSLV", Market.CBOE },
            { "VXTH", Market.CBOE },
            { "VXTLT", Market.CBOE },
            { "XAU", "PHLX" },
            { "DJI", Market.CME },
            { "DWCPF", Market.CME },
            { "UTIL", Market.CME },
            { "DAX", Market.EUREX },
            { "DXY", "NYBOT" },
            { "RLS", Market.CBOE },
            { "SMLG", "PSE" },
            { "SPGSCI", Market.CME },
            { "VAF", Market.CBOE },
            { "VRO", Market.CBOE },
            { "AEX", "FTA" },
            { "DJINET", Market.CBOE },
            { "DTX", Market.CBOE },
            { "SP600", Market.CBOE },
            { "SPSV", "PSE" },
            { "FTW5000", "AMEX" },
            { "DWCF", "PSE" },
        };

        private static readonly Dictionary<string, string> _indexMarket = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "HSI", Market.HKFE },
            { "N225", Market.OSE },
            { "SX5E", Market.EUREX },
            { "DAX", Market.EUREX }
        };

        /// <summary>
        /// Gets the actual exchange the index lives on
        /// </summary>
        /// <remarks>Useful for live trading</remarks>
        /// <returns>The exchange of the index</returns>
        public static string GetIndexExchange(Symbol symbol)
        {
            return _indexExchange.TryGetValue(symbol.Value, out var market)
                ? market
                : symbol.ID.Market;
        }

        /// <summary>
        /// Gets the lean market for this index ticker
        /// </summary>
        /// <returns>The market of the index</returns>
        public static bool TryGetIndexMarket(string ticker, out string market)
        {
            return _indexMarket.TryGetValue(ticker, out market);
        }
    }
}
