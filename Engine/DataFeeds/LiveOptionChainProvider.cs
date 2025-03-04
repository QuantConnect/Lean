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
using System.Net;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.FutureOption.Api;
using System.Net.Http.Headers;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An implementation of <see cref="IOptionChainProvider"/> that fetches the list of contracts
    /// from the Options Clearing Corporation (OCC) website
    /// </summary>
    public class LiveOptionChainProvider : BacktestingOptionChainProvider
    {
        private static readonly HttpClient _client;
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1);

        private static RateGate _cmeRateGate;

        private const string CMESymbolReplace = "{{SYMBOL}}";
        private const string CMEProductCodeReplace = "{{PRODUCT_CODE}}";
        private const string CMEProductExpirationReplace = "{{PRODUCT_EXPIRATION}}";

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

            _client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
            _client.DefaultRequestHeaders.Connection.Add("keep-alive");
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0");
            _client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.5));
        }

        /// <summary>
        /// Gets the option chain associated with the underlying Symbol
        /// </summary>
        /// <param name="symbol">The option or the underlying symbol to get the option chain for.
        /// Providing the option allows targetting an option ticker different than the default e.g. SPXW</param>
        /// <param name="date">The date to ask for the option contract list for</param>
        /// <returns>Option chain</returns>
        /// <exception cref="ArgumentException">Option underlying Symbol is not Future or Equity</exception>
        public override IEnumerable<Symbol> GetOptionContractList(Symbol symbol, DateTime date)
        {
            HashSet<Symbol> result = null;
            try
            {
                result = base.GetOptionContractList(symbol, date).ToHashSet();
            }
            catch (Exception ex)
            {
                result = new();
                // this shouldn't happen but just in case let's log it
                Log.Error(ex);
            }

            // during warmup we rely on the backtesting provider, but as we get closer to current time let's join the data with our live chain sources
            if (date.Date >= DateTime.UtcNow.Date.AddDays(-5) || result.Count == 0)
            {
                var underlyingSymbol = symbol;
                if (symbol.SecurityType.IsOption())
                {
                    // we were given the option
                    underlyingSymbol = symbol.Underlying;
                }

                if (underlyingSymbol.SecurityType == SecurityType.Equity || underlyingSymbol.SecurityType == SecurityType.Index)
                {
                    var expectedOptionTicker = underlyingSymbol.Value;
                    if (underlyingSymbol.SecurityType == SecurityType.Index)
                    {
                        expectedOptionTicker = symbol.ID.Symbol;
                    }

                    // Source data from TheOCC if we're trading equity or index options
                    foreach (var optionSymbol in GetEquityIndexOptionContractList(underlyingSymbol, expectedOptionTicker).Where(symbol => !IsContractExpired(symbol, date)))
                    {
                        result.Add(optionSymbol);
                    }
                }
                else if (underlyingSymbol.SecurityType == SecurityType.Future)
                {
                    // We get our data from CME if we're trading future options
                    foreach (var optionSymbol in GetFutureOptionContractList(underlyingSymbol, date).Where(symbol => !IsContractExpired(symbol, date)))
                    {
                        result.Add(optionSymbol);
                    }
                }
                else
                {
                    throw new ArgumentException("Option Underlying SecurityType is not supported. Supported types are: Equity, Index, Future");
                }
            }

            foreach (var optionSymbol in result)
            {
                yield return optionSymbol;
            }
        }

        private IEnumerable<Symbol> GetFutureOptionContractList(Symbol futureContractSymbol, DateTime date)
        {
            var symbols = new List<Symbol>();
            var retries = 0;
            var maxRetries = 5;

            // rate gate will start a timer in the background, so let's avoid it we if don't need it
            _cmeRateGate ??= new RateGate(1, TimeSpan.FromSeconds(0.5));

            while (++retries <= maxRetries)
            {
                try
                {
                    _cmeRateGate.WaitToProceed();

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

                    _cmeRateGate.WaitToProceed();

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

                    _cmeRateGate.WaitToProceed();

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
                        futureContractSymbol.SecurityType.DefaultOptionStyle(),
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
        /// <param name="expectedOptionTicker">The expected option ticker</param>
        /// <returns>The list of option contracts</returns>
        private static IEnumerable<Symbol> GetEquityIndexOptionContractList(Symbol symbol, string expectedOptionTicker)
        {
            var attempt = 1;
            IEnumerable<Symbol> contracts;

            while (true)
            {
                try
                {
                    Log.Trace($"LiveOptionChainProvider.GetOptionContractList(): Fetching option chain for option {expectedOptionTicker} underlying {symbol.Value} [Attempt {attempt}]");

                    contracts = FindOptionContracts(symbol, expectedOptionTicker);
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
        private static IEnumerable<Symbol> FindOptionContracts(Symbol underlyingSymbol, string expectedOptionTicker)
        {
            var symbols = new List<Symbol>();

            // use QC url to bypass TLS issues with Mono pre-4.8 version
            var url = "https://www.quantconnect.com/api/v2/theocc/series-search?symbolType=U&symbol=" + underlyingSymbol.Value;

            // download the text file
            var fileContent = _client.DownloadData(url);

            // read the lines, skipping the headers
            var lines = fileContent.Split(new[] { "\r\n" }, StringSplitOptions.None).Skip(7);

            // Example of a line:
            // SPY		2021	03	26	190	000	C P 	0	612	360000000

            // avoid being sensitive to case
            expectedOptionTicker = expectedOptionTicker.LazyToUpper();

            var optionStyle = underlyingSymbol.SecurityType.DefaultOptionStyle();

            // parse the lines, creating the Lean option symbols
            foreach (var line in lines)
            {
                var fields = line.Split('\t');

                var ticker = fields[0].Trim();
                if (ticker != expectedOptionTicker)
                {
                    // skip undesired options. For example SPX underlying has SPX & SPXW option tickers
                    continue;
                }

                var expiryDate = new DateTime(fields[2].ToInt32(), fields[3].ToInt32(), fields[4].ToInt32());
                var strike = (fields[5] + "." + fields[6]).ToDecimal();

                foreach (var right in fields[7].Trim().Split(' '))
                {
                    OptionRight? targetRight = null;

                    if (right.Equals("C", StringComparison.OrdinalIgnoreCase))
                    {
                        targetRight = OptionRight.Call;
                    }
                    else if (right.Equals("P", StringComparison.OrdinalIgnoreCase))
                    {
                        targetRight = OptionRight.Put;
                    }

                    if (targetRight.HasValue)
                    {
                        symbols.Add(Symbol.CreateOption(
                            underlyingSymbol,
                            expectedOptionTicker,
                            underlyingSymbol.ID.Market,
                            optionStyle,
                            targetRight.Value,
                            strike,
                            expiryDate));
                    }
                }
            }

            return symbols;
        }
    }
}
