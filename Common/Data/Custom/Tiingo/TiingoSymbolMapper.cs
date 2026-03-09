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

namespace QuantConnect.Data.Custom.Tiingo
{
    /// <summary>
    /// Helper class to map a Lean format ticker to Tiingo format
    /// </summary>
    /// <remarks>To be used when performing direct queries to Tiingo API</remarks>
    /// <remarks>https://api.tiingo.com/documentation/appendix/symbology</remarks>
    public static class TiingoSymbolMapper
    {
        /// <summary>
        /// Maps a given <see cref="Symbol"/> instance to it's Tiingo equivalent
        /// </summary>
        public static string GetTiingoTicker(Symbol symbol)
        {
            return symbol.Value.Replace(".", "-");
        }

        /// <summary>
        /// Maps a given Tiingo ticker to Lean equivalent
        /// </summary>
        public static string GetLeanTicker(string ticker)
        {
            return ticker.Replace("-", ".");
        }
    }
}
