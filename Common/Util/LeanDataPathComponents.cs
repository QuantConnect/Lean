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
using System.IO;

namespace QuantConnect.Util
{
    /// <summary>
    /// Type representing the various pieces of information emebedded into a lean data file path
    /// </summary>
    public class LeanDataPathComponents
    {
        /// <summary>
        /// Gets the date component from the file name
        /// </summary>
        public DateTime Date
        {
            get; private set;
        }

        /// <summary>
        /// Gets the security type from the path
        /// </summary>
        public SecurityType SecurityType
        {
            get; private set;
        }

        /// <summary>
        /// Gets the market from the path
        /// </summary>
        public string Market
        {
            get; private set;
        }

        /// <summary>
        /// Gets the resolution from the path
        /// </summary>
        public Resolution Resolution
        {
            get; private set;
        }

        /// <summary>
        /// Gets the file name, not inluding directory information
        /// </summary>
        public string Filename
        {
            get; private set;
        }

        /// <summary>
        /// Gets the symbol object implied by the path. For options, or any
        /// multi-entry zip file, this should be the canonical symbol
        /// </summary>
        public Symbol Symbol
        {
            get; private set;
        }

        /// <summary>
        /// Gets the tick type from the file name
        /// </summary>
        public TickType TickType
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanDataPathComponents"/> class
        /// </summary>
        public LeanDataPathComponents(SecurityType securityType, string market, Resolution resolution, Symbol symbol, string filename, DateTime date, TickType tickType)
        {
            Date = date;
            SecurityType = securityType;
            Market = market;
            Resolution = resolution;
            Filename = filename;
            Symbol = symbol;
            TickType = tickType;
        }

        /// <summary>
        /// Parses the specified path into a new instance of the <see cref="LeanDataPathComponents"/> class
        /// </summary>
        /// <param name="path">The path to be parsed</param>
        /// <returns>A new instance of the <see cref="LeanDataPathComponents"/> class representing the specified path</returns>
        public static LeanDataPathComponents Parse(string path)
        {
            //"../Data/equity/usa/hour/spy.zip"
            //"../Data/equity/usa/hour/spy/20160218_trade.zip"
            var fileinfo = new FileInfo(path);
            var filename = fileinfo.Name;
            var parts = path.Split('/', '\\');

            // defines the offsets of the security relative to the end of the path
            const int LowResSecurityTypeOffset = 4;
            const int HighResSecurityTypeOffset = 5;

            // defines other offsets relative to the beginning of the substring produce by the above offsets
            const int MarketOffset = 1;
            const int ResolutionOffset = 2;
            const int TickerOffset = 3;


            if (parts.Length < LowResSecurityTypeOffset)
            {
                throw new FormatException($"Unexpected path format: {path}");
            }

            var securityTypeOffset = LowResSecurityTypeOffset;
            SecurityType securityType;
            var rawValue = parts[parts.Length - securityTypeOffset];
            if (!Enum.TryParse(rawValue, true, out securityType))
            {
                securityTypeOffset = HighResSecurityTypeOffset;
                rawValue = parts[parts.Length - securityTypeOffset];
                if (!Enum.TryParse(rawValue, true, out securityType))
                {
                    throw new FormatException($"Unexpected path format: {path}");
                }
            }

            var market = parts[parts.Length - securityTypeOffset + MarketOffset];
            var resolution = (Resolution) Enum.Parse(typeof (Resolution), parts[parts.Length - securityTypeOffset + ResolutionOffset], true);
            string ticker;
            if (securityTypeOffset == LowResSecurityTypeOffset)
            {
                ticker = Path.GetFileNameWithoutExtension(path);
                if (securityType == SecurityType.Option)
                {
                    // ticker_trade_american
                    var tickerWithoutStyle = ticker.Substring(0, ticker.LastIndexOfInvariant("_"));
                    ticker = tickerWithoutStyle.Substring(0, tickerWithoutStyle.LastIndexOfInvariant("_"));
                }
                if (securityType == SecurityType.Future)
                {
                    // ticker_trade
                    ticker = ticker.Substring(0, ticker.LastIndexOfInvariant("_"));
                }
                if (securityType == SecurityType.Crypto &&
                    (resolution == Resolution.Daily || resolution == Resolution.Hour))
                {
                    // ticker_trade or ticker_quote
                    ticker = ticker.Substring(0, ticker.LastIndexOfInvariant("_"));
                }
            }
            else
            {
                ticker = parts[parts.Length - securityTypeOffset + TickerOffset];
            }

            var date = securityTypeOffset == LowResSecurityTypeOffset ? DateTime.MinValue : DateTime.ParseExact(filename.Substring(0, filename.IndexOf("_", StringComparison.Ordinal)), DateFormat.EightCharacter, null);

            Symbol symbol;
            if (securityType == SecurityType.Option)
            {
                var withoutExtension = Path.GetFileNameWithoutExtension(filename);
                rawValue = withoutExtension.Substring(withoutExtension.LastIndexOf("_", StringComparison.Ordinal) + 1);
                var style = (OptionStyle) Enum.Parse(typeof (OptionStyle), rawValue, true);
                symbol = Symbol.CreateOption(ticker, market, style, OptionRight.Call | OptionRight.Put, 0, SecurityIdentifier.DefaultDate);
            }
            else if (securityType == SecurityType.Future)
            {
                symbol = Symbol.CreateFuture(ticker, market, SecurityIdentifier.DefaultDate);
            }
            else
            {
                symbol = Symbol.Create(ticker, securityType, market);
            }

            var tickType = filename.Contains("_quote") ? TickType.Quote : (filename.Contains("_openinterest") ? TickType.OpenInterest : TickType.Trade);

            return new LeanDataPathComponents(securityType, market, resolution, symbol, filename, date, tickType);
        }
    }
}