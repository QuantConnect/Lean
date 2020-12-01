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
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.FutureOption.Api;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IOptionChainProvider"/> that fetches the list of contracts
    /// from the Options Clearing Corporation (OCC) website
    /// </summary>
    public class LiveOptionChainProvider : IOptionChainProvider
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1);

        private readonly RateGate _rateGate = new RateGate(1, TimeSpan.FromSeconds(0.5));

        private const string CMESymbolReplace = "{{SYMBOL}}";
        private const string CMEProductCodeReplace = "{{PRODUCT_CODE}}";
        private const string CMEContractCodeReplace = "{{CONTRACT_CODE}}";
        private const string CMEProductExpirationReplace = "{{PRODUCT_EXPIRATION}}";
        private const string CMEDateTimeReplace = "{{DT_REPLACE}}";

        private const string CMEProductSlateURL = "https://www.cmegroup.com/CmeWS/mvc/ProductSlate/V2/List?pageNumber=1&sortAsc=false&sortField=rank&searchString=" + CMESymbolReplace + "&pageSize=5";
        private const string CMEOptionsTradeDateAndExpirations = "https://www.cmegroup.com/CmeWS/mvc/Settlements/Options/TradeDateAndExpirations/" + CMEProductCodeReplace;
        private const string CMEOptionChainQuotesURL = "https://www.cmegroup.com/CmeWS/mvc/Quotes/Option/" + CMEProductCodeReplace + "/G/" + CMEProductExpirationReplace + "/ALL?_=";

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
        /// Gets the option chain associated with the underlying Symbol
        /// </summary>
        /// <param name="underlyingSymbol">Underlying symbol to get the option chain for</param>
        /// <param name="date">Unused</param>
        /// <returns>Option chain</returns>
        /// <exception cref="ArgumentException">Option underlying Symbol is not Future or Equity</exception>
        public IEnumerable<Symbol> GetOptionContractList(Symbol underlyingSymbol, DateTime date)
        {
            if (underlyingSymbol.SecurityType == SecurityType.Equity)
            {
                // Source data from TheOCC if we're trading equity options
                return GetEquityOptionContractList(underlyingSymbol, date);
            }
            if (underlyingSymbol.SecurityType == SecurityType.Future)
            {
                // We get our data from CME if we're trading future options
                return GetFutureOptionContractList(underlyingSymbol, date);
            }

            throw new ArgumentException("Option Underlying SecurityType is not supported. Supported types are: Equity, Future");
        }

        private IEnumerable<Symbol> GetFutureOptionContractList(Symbol futureContractSymbol, DateTime date)
        {
            var symbols = new List<Symbol>();
            var retries = 0;
            var maxRetries = 5;

            while (++retries <= maxRetries)
            {
                try
                {
                    _rateGate.WaitToProceed();

                    var productResponse = _client.GetAsync(CMEProductSlateURL.Replace(CMESymbolReplace, futureContractSymbol.ID.Symbol))
                        .SynchronouslyAwaitTaskResult();

                    productResponse.EnsureSuccessStatusCode();

                    var productResults = JsonConvert.DeserializeObject<CMEProductSlateV2ListResponse>(productResponse.Content
                        .ReadAsStringAsync()
                        .SynchronouslyAwaitTaskResult());

                    productResponse.Dispose();

                    // We want to gather the future product to get the future options ID
                    var futureProductId = productResults.Products.Where(p => p.Globex == futureContractSymbol.ID.Symbol && p.GlobexTraded && p.Cleared == "Futures")
                        .Select(p => p.Id)
                        .Single();


                    var optionsTradesAndExpiries = CMEOptionsTradeDateAndExpirations.Replace(CMEProductCodeReplace, futureProductId.ToStringInvariant());

                    _rateGate.WaitToProceed();

                    var optionsTradesAndExpiriesResponse = _client.GetAsync(optionsTradesAndExpiries).SynchronouslyAwaitTaskResult();
                    optionsTradesAndExpiriesResponse.EnsureSuccessStatusCode();

                    var tradesAndExpiriesResponse = JsonConvert.DeserializeObject<List<CMEOptionsTradeDatesAndExpiration>>(optionsTradesAndExpiriesResponse.Content
                        .ReadAsStringAsync()
                        .SynchronouslyAwaitTaskResult());

                    optionsTradesAndExpiriesResponse.Dispose();

                    // For now, only support American options on CME
                    var selectedOption = tradesAndExpiriesResponse
                        .FirstOrDefault(x => !x.Daily && !x.Weekly && !x.Sto && x.OptionType == "AME");

                    if (selectedOption == null)
                    {
                        Log.Error($"LiveOptionChainProvider.GetFutureOptionContractList(): Found no matching future options for contract {futureContractSymbol}");
                        yield break;
                    }

                    // Gather the month code and the year's last number to query the next API, which expects an expiration as `<MONTH_CODE><YEAR_LAST_NUMBER>`
                    var canonicalFuture = Symbol.Create(futureContractSymbol.ID.Symbol, SecurityType.Future, futureContractSymbol.ID.Market);
                    var expiryFunction = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture);

                    var futureContractExpiration = selectedOption.Expirations
                        .Select(x => new KeyValuePair<CMEOptionsExpiration, DateTime>(x, expiryFunction(new DateTime(x.Expiration.Year, x.Expiration.Month, 1))))
                        .FirstOrDefault(x => x.Value.Year == futureContractSymbol.ID.Date.Year && x.Value.Month == futureContractSymbol.ID.Date.Month)
                        .Key;

                    if (futureContractExpiration == null)
                    {
                        Log.Error($"LiveOptionChainProvider.GetFutureOptionContractList(): Found no future options with matching expiry year and month for contract {futureContractSymbol}");
                        yield break;
                    }

                    var futureContractMonthCode = futureContractExpiration.Expiration.Code;

                    _rateGate.WaitToProceed();

                    // Subtract one day from now for settlement API since settlement may not be available for today yet
                    var optionChainQuotesResponseResult = _client.GetAsync(CMEOptionChainQuotesURL
                        .Replace(CMEProductCodeReplace, selectedOption.ProductId.ToStringInvariant())
                        .Replace(CMEProductExpirationReplace, futureContractMonthCode)
                        + Math.Floor((DateTime.UtcNow - _epoch).TotalMilliseconds).ToStringInvariant());

                    optionChainQuotesResponseResult.Result.EnsureSuccessStatusCode();

                    var futureOptionChain = JsonConvert.DeserializeObject<CMEOptionChainQuotes>(optionChainQuotesResponseResult.Result.Content
                        .ReadAsStringAsync()
                        .SynchronouslyAwaitTaskResult())
                        .Quotes
                        .DistinctBy(s => s.StrikePrice)
                        .ToList();

                    optionChainQuotesResponseResult.Dispose();

                    // Each CME contract can have arbitrary scaling applied to the strike price, so we normalize it to the
                    // underlying's price via static entries.
                    var optionStrikePriceScaleFactor = CMEStrikePriceScalingFactors.GetScaleFactor(futureContractSymbol);
                    var canonicalOption = Symbol.CreateOption(
                        futureContractSymbol,
                        futureContractSymbol.ID.Market,
                        default(OptionStyle),
                        default(OptionRight),
                        default(decimal),
                        SecurityIdentifier.DefaultDate);

                    foreach (var optionChainEntry in futureOptionChain)
                    {
                        var futureOptionExpiry = FuturesOptionsExpiryFunctions.GetFutureOptionExpiryFromFutureExpiry(futureContractSymbol, canonicalOption);
                        var scaledStrikePrice = optionChainEntry.StrikePrice / optionStrikePriceScaleFactor;

                        // Calls and puts share the same strike, create two symbols per each to avoid iterating twice.
                        symbols.Add(Symbol.CreateOption(
                            futureContractSymbol,
                            futureContractSymbol.ID.Market,
                            OptionStyle.American,
                            OptionRight.Call,
                            scaledStrikePrice,
                            futureOptionExpiry));

                       symbols.Add(Symbol.CreateOption(
                           futureContractSymbol,
                           futureContractSymbol.ID.Market,
                           OptionStyle.American,
                           OptionRight.Put,
                           scaledStrikePrice,
                           futureOptionExpiry));
                    }

                    break;
                }
                catch (HttpRequestException err)
                {
                    if (retries != maxRetries)
                    {
                        Log.Error(err, $"Failed to retrieve futures options chain from CME, retrying ({retries} / {maxRetries})");
                        continue;
                    }

                    Log.Error(err, $"Failed to retrieve futures options chain from CME, returning empty result ({retries} / {retries})");
                }
            }

            foreach (var symbol in symbols)
            {
                yield return symbol;
            }
        }

        /// <summary>
        /// Gets the list of option contracts for a given underlying equity symbol
        /// </summary>
        /// <param name="symbol">The underlying symbol</param>
        /// <param name="date">The date for which to request the option chain (only used in backtesting)</param>
        /// <returns>The list of option contracts</returns>
        private IEnumerable<Symbol> GetEquityOptionContractList(Symbol symbol, DateTime date)
        {
            var attempt = 1;
            IEnumerable<Symbol> contracts;

            while (true)
            {
                try
                {
                    Log.Trace($"LiveOptionChainProvider.GetOptionContractList(): Fetching option chain for {symbol.Value} [Attempt {attempt}]");

                    contracts = FindEquityOptionContracts(symbol.Value);
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
        private static IEnumerable<Symbol> FindEquityOptionContracts(string underlyingSymbol)
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
