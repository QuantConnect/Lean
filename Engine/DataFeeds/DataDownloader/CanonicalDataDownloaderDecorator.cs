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
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.DataFeeds.DataDownloader.Exceptions;

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
        /// 
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="mapFileProvider"></param>
        /// <param name="factorFileProvider"></param>
        /// <param name="dataDownloader"></param>
        public CanonicalDataDownloaderDecorator(IDataProvider dataProvider, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, IDataDownloader dataDownloader)
        {
            _dataDownloader = dataDownloader;

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
            ArgumentNullException.ThrowIfNull(dataDownloaderGetParameters);

            var downloadedData = default(IEnumerable<BaseData>);
            try
            {
                downloadedData = _dataDownloader.Get(dataDownloaderGetParameters);

            }
            catch (CanonicalNotSupportedException ex)
            {
                if (!_firedCanonicalNotSupportedWarning)
                {
                    _firedCanonicalNotSupportedWarning = true;
                    Log.Error($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(Get)}.Exception: {ex.Message} Using chain provider fallback.");
                }
                downloadedData = GetContractsData(dataDownloaderGetParameters);
            }

            if (downloadedData == null)
            {
                return null;
            }

            return downloadedData;
        }

        /// <summary>
        /// Downloads data for all contracts of a canonical symbol in parallel, streaming results as they arrive.
        /// </summary>
        private IEnumerable<BaseData>? GetContractsData(DataDownloaderGetParameters parameters)
        {
            var contracts = GetContracts(parameters.Symbol, parameters.StartUtc, parameters.EndUtc);

            var blockingCollection = new BlockingCollection<BaseData>();

            var producerTask = Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(
                        contracts,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, TaskScheduler = TaskScheduler.Default },
                        contract =>
                        {
                            var contractParameters = new DataDownloaderGetParameters(
                                contract,
                                parameters.Resolution,
                                parameters.StartUtc,
                                parameters.EndUtc,
                                parameters.TickType);


                            var contractData = default(IEnumerable<BaseData>);
                            try
                            {
                                // TODO: add try/catch unavailable contract, log and continue with other contracts instead of failing the entire download
                                contractData = _dataDownloader.Get(contractParameters);
                                if (contractData == null)
                                {
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(GetContractsData)}: " +
                                    $"Error downloading data for {contractParameters}. Exception: {ex.Message}. Continuing...");
                                return;
                            }

                            foreach (var data in contractData)
                            {
                                blockingCollection.Add(data);
                            }
                        });
                }
                finally
                {
                    Log.Debug($"{nameof(CanonicalDataDownloaderDecorator)}.{nameof(GetContractsData)}:Finished downloading data for canonical symbol, marking the collection as complete for consuming");
                    blockingCollection.CompleteAdding();
                }
            });

            var consumingEnumerable = blockingCollection.GetConsumingEnumerable();

            if (!consumingEnumerable.Any())
            {
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
            switch (symbol.SecurityType)
            {
                case SecurityType.Future:
                    chainProvider = _futureChainProvider.Value.GetFutureContractList;
                    break;
                case SecurityType.Option:
                case SecurityType.FutureOption:
                case SecurityType.IndexOption:
                    chainProvider = _optionChainProvider.Value.GetOptionContractList;
                    break;
                default:
                    throw new ArgumentException($"Unsupported security type {symbol.SecurityType} for canonical data downloader", nameof(symbol));
            }

            var uniqueContracts = new HashSet<Symbol>();
            foreach (var date in Time.EachDay(startUtc.Date, endUtc.Date))
            {
                foreach (var contract in chainProvider(symbol, date))
                {
                    if (uniqueContracts.Add(contract))
                    {
                        yield return contract;
                    }
                }
            }
        }
    }
}
