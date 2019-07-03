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
using System.Net;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IOptionChainProvider"/> that fetches the list of contracts
    /// from the Options Clearing Corporation (OCC) website
    /// </summary>
    public class LiveOptionChainProvider : IOptionChainProvider
    {
        private const int MaxDownloadAttempts = 5;

        /// <summary>
        /// Static constructor for the <see cref="LiveOptionChainProvider"/> class
        /// </summary>
        static LiveOptionChainProvider()
        {
            // The OCC website now requires at least TLS 1.1 for API requests.
            // NET 4.5.2 and below does not enable these more secure protocols by default, so we add them in here
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Gets the list of option contracts for a given underlying symbol
        /// </summary>
        /// <param name="symbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the option chain (only used in backtesting)</param>
        /// <returns>The list of option contracts</returns>
        public IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            if (symbol.SecurityType != SecurityType.Equity)
            {
                throw new NotSupportedException($"LiveOptionChainProvider.GetOptionContractList(): SecurityType.Equity is expected but was {symbol.SecurityType}");
            }

            var attempt = 1;
            IEnumerable<Symbol> contracts;

            while (true)
            {
                try
                {
                    Log.Trace($"LiveOptionChainProvider.GetOptionContractList(): Fetching option chain for {symbol.Value} [Attempt {attempt}]");

                    contracts = FindOptionContracts(symbol.Value);
                    break;
                }
                catch (WebException exception)
                {
                    Log.Error(exception);

                    if (++attempt > MaxDownloadAttempts)
                    {
                        throw;
                    }

                    Thread.Sleep(1000);
                }
            }

            return contracts;
        }

        /// <summary>
        /// Retrieve the list of option contracts for an underlying symbol from the OCC website
        /// </summary>
        private static IEnumerable<Symbol> FindOptionContracts(string underlyingSymbol)
        {
            var symbols = new List<Symbol>();

            using (var client = new WebClient())
            {
                // use QC url to bypass TLS issues with Mono pre-4.8 version
                var url = "https://www.quantconnect.com/api/v2/theocc/series-search?symbolType=U&symbol=" + underlyingSymbol;

                // download the text file
                var fileContent = client.DownloadString(url);

                // read the lines, skipping the headers
                var lines = fileContent.Split(new[] { "\r\n" }, StringSplitOptions.None).Skip(7);

                // parse the lines, creating the Lean option symbols
                foreach (var line in lines)
                {
                    var fields = line.Split('\t');

                    var ticker = fields[0].Trim();
                    if (ticker != underlyingSymbol)
                        continue;

                    var expiryDate = new DateTime(fields[2].ToInt32(), fields[3].ToInt32(), fields[4].ToInt32());
                    var strike = (fields[5] + "." + fields[6]).ToDecimal();

                    if (fields[7].Contains("C"))
                    {
                        symbols.Add(Symbol.CreateOption(underlyingSymbol, Market.USA, OptionStyle.American, OptionRight.Call, strike, expiryDate));
                    }

                    if (fields[7].Contains("P"))
                    {
                        symbols.Add(Symbol.CreateOption(underlyingSymbol, Market.USA, OptionStyle.American, OptionRight.Put, strike, expiryDate));
                    }
                }
            }

            return symbols;
        }
    }
}
