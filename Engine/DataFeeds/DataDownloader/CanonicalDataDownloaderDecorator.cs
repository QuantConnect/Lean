/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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
using System.Linq;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.Lean.Engine.DataFeeds.DataDownloader
{
    /// <summary>
    /// Decorates an <see cref="IDataDownloader"/> to support canonical symbols by automatically
    /// resolving their option or future contract chains and downloading data for each constituent contract.
    /// </summary>
    public class CanonicalDataDownloaderDecorator : IDataDownloader
    {
        /// <summary>
        /// Prevents multiple warnings being fired when the underlying data downloader doesn't support canonical symbols.
        /// </summary>
        private bool _firedCanonicalNotSupportedWarning;

        /// <summary>
        /// Lazily initialized option chain provider for resolving option contract lists.
        /// </summary>
        private readonly Lazy<IOptionChainProvider> _optionChainProvider;

        /// <summary>
        /// Lazily initialized future chain provider for resolving future contract lists.
        /// </summary>
        private readonly Lazy<IFutureChainProvider> _futureChainProvider;

        /// <summary>
        /// The underlying data downloader that performs the actual data retrieval.
        /// </summary>
        private readonly IDataDownloader _dataDownloader;

        /// <summary>
        /// Controls parallelism for concurrent operations, 
        /// limiting execution to a configurable number of threads (default: 4) on the default task scheduler.
        /// </summary>
        private readonly ParallelOptions _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Config.GetInt("downloader-thread-count", 4),
            TaskScheduler = TaskScheduler.Default
        };

        /// <summary>
        /// Configurable look-back period for canonical option symbols, used to limit the date range of underlying contract data downloads.
        /// </summary>
        private static readonly int _optionLookbackYeard = Config.GetInt("options-lookback-years", 1);

        /// <summary>
        /// Configurable look-back period for canonical future symbols, used to limit the date range of underlying contract data downloads.
        /// </summary>
        private static readonly int _futureLookbackYeard = Config.GetInt("futures-lookback-years", 2);

        /// <summary>
        /// Initializes a new instance of the <see cref="CanonicalDataDownloaderDecorator"/> class.
        /// </summary>
        /// <param name="dataDownloader">The underlying data downloader to decorate with canonical symbol support.</param>
        public CanonicalDataDownloaderDecorator(IDataDownloader dataDownloader)
        {
            _dataDownloader = dataDownloader;

            var dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>("DefaultDataProvider");
            var mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
            var factorFileProvider = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>(Config.Get("factor-file-provider", "LocalDiskFactorFileProvider"));

            mapFileProvider.Initialize(dataProvider);
            factorFileProvider.Initialize(mapFileProvider, dataProvider);

            var historyManager = new HistoryProviderManager();
            historyManager.Initialize(
                new HistoryProviderInitializeParameters(
                    job: null,
                    api: null,
                    dataProvider,
                    new SingleEntryDataCacheProvider(dataProvider, isDataEphemeral: true),
                    mapFileProvider,
                    factorFileProvider: factorFileProvider, // Probably not needed since canonical data doesn't require factor files
                    statusUpdateAction: null,
                    parallelHistoryRequestsEnabled: false,
                    new DataPermissionManager(),
                    objectStore: null,
                    new AlgorithmSettings()));

            _optionChainProvider = new(() =>
            {
                var optionChainProvider = Composer.Instance.GetPart<IOptionChainProvider>();
                if (optionChainProvider == null)
                {
                    var baseOptionChainProvider = new LiveOptionChainProvider();
                    baseOptionChainProvider.Initialize(new(mapFileProvider, historyManager));
                    optionChainProvider = new CachingOptionChainProvider(baseOptionChainProvider);
                    Composer.Instance.AddPart(optionChainProvider);
                }
                return optionChainProvider;
            });

            _futureChainProvider = new(() =>
            {
                var futureChainProvider = Composer.Instance.GetPart<IFutureChainProvider>();
                if (futureChainProvider == null)
                {
                    var baseFutureChainProvider = new BacktestingFutureChainProvider();
                    baseFutureChainProvider.Initialize(new(mapFileProvider, historyManager));
                    futureChainProvider = new CachingFutureChainProvider(baseFutureChainProvider);
                    Composer.Instance.AddPart(futureChainProvider);
                }
                return futureChainProvider;
            });
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// For canonical symbols, automatically resolves and downloads data for all underlying contracts.
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData>? Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var downloadedData = default(IEnumerable<BaseData>?);
            try
            {
                downloadedData = _dataDownloader.Get(dataDownloaderGetParameters);
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(Get)}.Exceptoin: {ex.Message}");
            }

            if (downloadedData == null && dataDownloaderGetParameters.Symbol.IsCanonical())
            {
                if (!_firedCanonicalNotSupportedWarning)
                {
                    _firedCanonicalNotSupportedWarning = true;
                    Log.Trace($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(Get)}: {_dataDownloader.GetType().Name} does not support canonical symbols. Falling back to chain provider.");
                }
                downloadedData = GetContractsData(dataDownloaderGetParameters);
            }

            return downloadedData;
        }

        /// <summary>
        /// Tries to adjust the date range for a given contract based on its security type and expiry date.
        /// The start date is clamped to a minimum look-back period and the end date is clamped to the contract expiry date.
        /// Returns false if the minimum look-back period exceeds the requested end date, meaning no valid range exists.
        /// </summary>
        /// <param name="contract">The contract symbol containing the security type and expiry date.</param>
        /// <param name="originalStartDateUtc">The requested start date in UTC.</param>
        /// <param name="originalEndDateUtc">The requested end date in UTC.</param>
        /// <param name="adjustedStartDateUtc">The adjusted start date in UTC, or default if no valid range exists.</param>
        /// <param name="adjustedEndDateUtc">The adjusted end date in UTC, or default if no valid range exists.</param>
        /// <returns>True if a valid adjusted date range was found, false otherwise.</returns>
        public static bool TryAdjustDateRangeForContract(Symbol contract, DateTime originalStartDateUtc, DateTime originalEndDateUtc,
            out DateTime adjustedStartDateUtc, out DateTime adjustedEndDateUtc)
        {
            var expiryDate = contract.ID.Date;
            var minLookBack = expiryDate;
            if (contract.ID.SecurityType.IsOption())
            {
                minLookBack = expiryDate.AddYears(-_optionLookbackYeard);
            }
            else if (contract.ID.SecurityType == SecurityType.Future)
            {
                minLookBack = expiryDate.AddYears(-_futureLookbackYeard);
            }

            if (minLookBack > originalEndDateUtc || expiryDate < originalStartDateUtc)
            {
                adjustedStartDateUtc = default;
                adjustedEndDateUtc = default;
                return false;
            }

            adjustedStartDateUtc = minLookBack >= originalStartDateUtc ? minLookBack : originalStartDateUtc;
            adjustedEndDateUtc = expiryDate <= originalEndDateUtc ? expiryDate.AddDays(1) : originalEndDateUtc;
            return true;
        }

        /// <summary>
        /// Downloads data for all contracts of a canonical symbol in parallel, streaming results as they arrive.
        /// </summary>
        private IEnumerable<BaseData>? GetContractsData(DataDownloaderGetParameters parameters)
        {
            var contracts = GetContracts(parameters.Symbol, parameters.StartUtc, parameters.EndUtc);

            var blockingCollection = new BlockingCollection<BaseData>();

            var processedContracts = 0L;
            var producerTask = Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(
                        contracts,
                        _parallelOptions,
                        contract =>
                        {
                            Interlocked.Increment(ref processedContracts);
                            if (!TryAdjustDateRangeForContract(contract, parameters.StartUtc, parameters.EndUtc, out var adjustedStartDateUtc, out var adjustedEndDateUtc))
                            {
                                return;
                            }

                            var contractParameters = new DataDownloaderGetParameters(
                                contract,
                                parameters.Resolution,
                                adjustedStartDateUtc,
                                adjustedEndDateUtc,
                                parameters.TickType);

                            try
                            {
                                var contractData = _dataDownloader.Get(contractParameters);

                                foreach (var data in contractData)
                                {
                                    blockingCollection.Add(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Debug($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(GetContractsData)}: " +
                                    $"Error downloading data for {contractParameters}. Exception: {ex.Message}. Continuing...");
                                return;
                            }
                        });
                }
                finally
                {
                    Log.Debug($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(GetContractsData)}: Finished downloading {processedContracts} for canonical symbol.");
                    blockingCollection.CompleteAdding();
                }
            });

            var consumingEnumerable = blockingCollection.GetConsumingEnumerable();

            if (!consumingEnumerable.Any())
            {
                if (Interlocked.Read(ref processedContracts) == 0)
                {
                    Log.Error($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(GetContractsData)}: No contracts were found. Do you have universe data?");
                }
                return null;
            }

            return consumingEnumerable;
        }

        /// <summary>
        /// Retrieves unique contracts for the given canonical symbol across the specified date range.
        /// </summary>
        private IEnumerable<Symbol> GetContracts(Symbol symbol, DateTime startUtc, DateTime endUtc)
        {
            var chainProvider = default(Func<Symbol, DateTime, IEnumerable<Symbol>>);
            if (symbol.SecurityType == SecurityType.Future)
            {
                chainProvider = _futureChainProvider.Value.GetFutureContractList;
            }
            else if (symbol.SecurityType.IsOption())
            {
                chainProvider = _optionChainProvider.Value.GetOptionContractList;
            }
            else
            {
                throw new ArgumentException($"Unsupported security type {symbol.SecurityType} for canonical data downloader", nameof(symbol));
            }

            var contractsCache = new HashSet<Symbol>();
            foreach (var date in Time.EachDay(startUtc.Date, endUtc.Date))
            {
                foreach (var contract in chainProvider(symbol, date))
                {
                    if (contractsCache.Add(contract))
                    {
                        yield return contract;
                    }
                }
            }
        }
    }
}
