/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using System.Diagnostics;
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.ToolBox.CoinApi;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Console application for converting CoinApi raw data into Lean data format for high resolutions (tick, second and minute)
    /// </summary>
    public class CoinApiDataConverter
    {
        /// <summary>
        /// List of supported exchanges
        /// </summary>
        private static readonly HashSet<string> SupportedMarkets = new[]
        {
            Market.Coinbase,
            Market.Bitfinex,
            Market.Binance,
            Market.FTX,
            Market.FTXUS,
            Market.Kraken,
            Market.BinanceUS,
            Market.Bybit
        }.ToHashSet();

        private readonly DirectoryInfo _rawDataFolder;
        private readonly DirectoryInfo _destinationFolder;
        private readonly SecurityType _securityType;
        private readonly DateTime _processingDate;
        private readonly string _market;

        /// <summary>
        /// CoinAPI data converter.
        /// </summary>
        /// <param name="date">the processing date.</param>
        /// <param name="rawDataFolder">path to the raw data folder.</param>
        /// <param name="destinationFolder">destination of the newly generated files.</param>
        /// <param name="securityType">The security type to process</param>
        /// <param name="market">The market to process (optional). Defaults to processing all markets in parallel.</param>
        public CoinApiDataConverter(DateTime date, string rawDataFolder, string destinationFolder, string market = null, SecurityType securityType = SecurityType.Crypto)
        {
            _market = string.IsNullOrWhiteSpace(market) 
                ? null 
                : market.ToLowerInvariant();

            _processingDate = date;
            _securityType = securityType;
            _rawDataFolder = new DirectoryInfo(Path.Combine(rawDataFolder, "crypto", "coinapi"));
            if (!_rawDataFolder.Exists)
            {
                throw new ArgumentException($"CoinApiDataConverter(): Source folder not found: {_rawDataFolder.FullName}");
            }

            _destinationFolder = new DirectoryInfo(destinationFolder);
            _destinationFolder.Create();
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            var stopwatch = Stopwatch.StartNew();

            var symbolMapper = new CoinApiSymbolMapper();
            var success = true;

            // There were cases of files with with an extra suffix, following pattern:
            // <TickType>-<ID>-<Exchange>_SPOT_<BaseCurrency>_<QuoteCurrency>_<ExtraSuffix>.csv.gz
            // Those cases should be ignored for SPOT prices.
            var tradesFolder = new DirectoryInfo(
                Path.Combine(
                    _rawDataFolder.FullName, 
                    "trades", 
                    _processingDate.ToStringInvariant(DateFormat.EightCharacter)));

            var quotesFolder = new DirectoryInfo(
                Path.Combine(
                    _rawDataFolder.FullName,
                    "quotes",
                    _processingDate.ToStringInvariant(DateFormat.EightCharacter)));

            var rawMarket = _market != null &&
                CoinApiSymbolMapper.MapMarketsToExchangeIds.TryGetValue(_market, out var rawMarketValue)
                    ? rawMarketValue
                    : null;

            var securityTypeFilter = (string name) => name.Contains("_SPOT_");
            if(_securityType == SecurityType.CryptoFuture)
            {
                securityTypeFilter = (string name) => name.Contains("_FTS_") || name.Contains("_PERP_");
            }
            
            // Distinct by tick type and first two parts of the raw file name, separated by '-'.
            // This prevents us from double processing the same ticker twice, in case we're given
            // two raw data files for the same symbol. Related: https://github.com/QuantConnect/Lean/pull/3262
            var apiDataReader = new CoinApiDataReader(symbolMapper);
            var filesToProcessCandidates = tradesFolder.EnumerateFiles("*.gz")
                .Concat(quotesFolder.EnumerateFiles("*.gz"))
                .Where(f => securityTypeFilter(f.Name) && (rawMarket == null || f.Name.Contains(rawMarket)))
                .Where(f => f.Name.Split('_').Length == 4)
                .ToList();

            var filesToProcessKeys = new HashSet<string>();
            var filesToProcess = new List<FileInfo>();

            foreach (var candidate in filesToProcessCandidates)
            {
                try
                {
                    var entryData = apiDataReader.GetCoinApiEntryData(candidate, _processingDate, _securityType);
                    CurrencyPairUtil.DecomposeCurrencyPair(entryData.Symbol, out var baseCurrency, out var quoteCurrency);

                    if (!candidate.FullName.Contains(baseCurrency) && !candidate.FullName.Contains(quoteCurrency))
                    {
                        throw new Exception($"Skipping {candidate.FullName} we have the wrong symbol {entryData.Symbol}!");
                    }

                    var key = candidate.Directory.Parent.Name + entryData.Symbol.ID;
                    if (filesToProcessKeys.Add(key))
                    {
                        // Separate list from HashSet to preserve ordering of viable candidates
                        filesToProcess.Add(candidate);
                    }
                }
                catch (Exception err)
                {
                    // Most likely the exchange isn't supported. Log exception message to avoid excessive stack trace spamming in console output 
                    Log.Error(err.Message);
                }
            }

            Parallel.ForEach(filesToProcess, (file, loopState) =>
                {
                    Log.Trace($"CoinApiDataConverter(): Starting data conversion from source file: {file.Name}...");
                    try
                    {
                        ProcessEntry(apiDataReader, file);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, $"CoinApiDataConverter(): Error processing entry: {file.Name}");
                        success = false;
                        loopState.Break();
                    }
                }
            );

            Log.Trace($"CoinApiDataConverter(): Finished in {stopwatch.Elapsed}");
            return success;
        }

        /// <summary>
        /// Processes the entry.
        /// </summary>
        /// <param name="coinapiDataReader">The coinapi data reader.</param>
        /// <param name="file">The file.</param>
        private void ProcessEntry(CoinApiDataReader coinapiDataReader, FileInfo file)
        {
            var entryData = coinapiDataReader.GetCoinApiEntryData(file, _processingDate, _securityType);

            if (!SupportedMarkets.Contains(entryData.Symbol.ID.Market))
            {
                // only convert data for supported exchanges
                return;
            }

            var tickData = coinapiDataReader.ProcessCoinApiEntry(entryData, file);

            // in some cases the first data points from '_processingDate' get's included in the previous date file
            // so we will ready previous date data and drop most of it just to save these midnight ticks
            var yesterdayDate = _processingDate.AddDays(-1);
            var yesterdaysFile = new FileInfo(file.FullName.Replace(
                _processingDate.ToStringInvariant(DateFormat.EightCharacter),
                    yesterdayDate.ToStringInvariant(DateFormat.EightCharacter)));
            if (yesterdaysFile.Exists)
            {
                var yesterdaysEntryData = coinapiDataReader.GetCoinApiEntryData(yesterdaysFile, yesterdayDate, _securityType);
                tickData = tickData.Concat(coinapiDataReader.ProcessCoinApiEntry(yesterdaysEntryData, yesterdaysFile));
            }
            else
            {
                Log.Error($"CoinApiDataConverter(): yesterdays data file not found '{yesterdaysFile.FullName}'");
            }

            // materialize the enumerable into a list, since we need to enumerate over it twice
            var ticks = tickData.Where(tick => tick.Time.Date == _processingDate)
                .OrderBy(t => t.Time)
                .ToList();

            var writer = new LeanDataWriter(Resolution.Tick, entryData.Symbol, _destinationFolder.FullName, entryData.TickType);
            writer.Write(ticks);

            Log.Trace($"CoinApiDataConverter(): Starting consolidation for {entryData.Symbol.Value} {entryData.TickType}");
            var consolidators = new List<TickAggregator>();

            if (entryData.TickType == TickType.Trade)
            {
                consolidators.AddRange(new[]
                {
                    new TradeTickAggregator(Resolution.Second),
                    new TradeTickAggregator(Resolution.Minute)
                });
            }
            else
            {
                consolidators.AddRange(new[]
                {
                    new QuoteTickAggregator(Resolution.Second),
                    new QuoteTickAggregator(Resolution.Minute)
                });
            }

            foreach (var tick in ticks)
            {
                if (tick.Suspicious)
                {
                    // When CoinAPI loses connectivity to the exchange, they indicate
                    // it in the data by providing a value of `-1` for bid/ask price.
                    // We will keep it in tick data, but will remove it from consolidated data.
                    continue;
                }
                
                foreach (var consolidator in consolidators)
                {
                    consolidator.Update(tick);
                }
            }

            foreach (var consolidator in consolidators)
            {
                writer = new LeanDataWriter(consolidator.Resolution, entryData.Symbol, _destinationFolder.FullName, entryData.TickType);
                writer.Write(consolidator.Flush());
            }
        }
    }
}
