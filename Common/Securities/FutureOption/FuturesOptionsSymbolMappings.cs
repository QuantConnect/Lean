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

using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Provides conversions from a GLOBEX Futures ticker to a GLOBEX Futures Options ticker
    /// </summary>
    public static class FuturesOptionsSymbolMappings
    {
        /// <summary>
        /// Defines Futures GLOBEX Ticker -> Futures Options GLOBEX Ticker
        /// </summary>
        private static Dictionary<string, string> _futureToFutureOptionsGLOBEX = new Dictionary<string, string>
        {
            { "EH", "OEH" },
            { "KE", "OKE" },
            { "TN", "OTN" },
            { "UB", "OUB" },
            { "YM", "OYM" },
            { "ZB", "OZB" },
            { "ZC", "OZC" },
            { "ZF", "OZF" },
            { "ZL", "OZL" },
            { "ZM", "OZM" },
            { "ZN", "OZN" },
            { "ZO", "OZO" },
            { "ZS", "OZS" },
            { "ZT", "OZT" },
            { "ZW", "OZW" },
            { "RTY", "RTO" },
            { "GC", "OG" },
            { "HG", "HXE" },
            { "SI", "SO" },
            { "CL", "LO" },
            { "HCL", "HCO" },
            { "HO", "OH" },
            { "NG", "ON" },
            { "PA", "PAO" },
            { "PL", "PO" },
            { "RB", "OB" },
        };

        private static Dictionary<string, string> _futureOptionsToFutureGLOBEX = _futureToFutureOptionsGLOBEX
            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key); 
        
        /// <summary>
        /// Returns the futures options ticker for the given futures ticker.
        /// </summary>
        /// <param name="futureTicker">Future GLOBEX ticker to get Future Option GLOBEX ticker for</param>
        /// <returns>Future option ticker. Defaults to future ticker provided if no entry is found</returns>
        public static string Map(string futureTicker)
        {
            futureTicker = futureTicker.ToUpperInvariant();

            string result;
            if (!_futureToFutureOptionsGLOBEX.TryGetValue(futureTicker, out result))
            {
                return futureTicker;
            }

            return result;
        }

        /// <summary>
        /// Maps a futures options ticker to its underlying future's ticker
        /// </summary>
        /// <param name="futureOptionTicker">Future option ticker to map to the underlying</param>
        /// <returns>Future ticker</returns>
        public static string MapFromOption(string futureOptionTicker)
        {
            futureOptionTicker = futureOptionTicker.ToUpperInvariant();

            string result;
            if (!_futureOptionsToFutureGLOBEX.TryGetValue(futureOptionTicker, out result))
            {
                return futureOptionTicker;
            }

            return result;
        }
    }
}
