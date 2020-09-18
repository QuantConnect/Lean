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

using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using System.Threading.Tasks;
using QuantConnect.Lean.Engine.Setup;

namespace QuantConnect.Research
{
    /// <summary>
    /// Provides access to data for quantitative analysis
    /// </summary>
    public class QuantBook : QCAlgorithm
    {
        private dynamic _pandas;
        private IDataCacheProvider _dataCacheProvider;
        private IDataProvider _dataProvider;
        private LeanEngineSystemHandlers _systemHandlers;
        private LeanEngineAlgorithmHandlers _algorithmHandlers;
        private AlgorithmManager _algorithmManager;
        private DataManager _dataManager;
        private static bool _isPythonNotebook;

        static QuantBook()
        {
            Logging.Log.LogHandler =
                Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            //Determine if we are in a Python Notebook
            try
            {
                using (Py.GIL())
                {
                    var isPython = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                        "try:\n" +
                        "   import IPython\n" +
                        "   def IsPythonNotebook():\n" +
                        "       return (IPython.get_ipython() != None)\n" +
                        "except:\n" +
                        "   print('No IPython installed')\n" +
                        "   def IsPythonNotebook():\n" +
                        "       return false\n").GetAttr("IsPythonNotebook").Invoke();
                    isPython.TryConvert(out _isPythonNotebook);
                }
            }
            catch
            {
                //Default to false
                _isPythonNotebook = false;
                Logging.Log.Error("QuantBook failed to determine Notebook kernel language");
            }

            Logging.Log.Trace($"QuantBook started; Is Python: {_isPythonNotebook}");
        }

        /// <summary>
        /// <see cref = "QuantBook" /> constructor.
        /// Provides access to data for quantitative analysis
        /// </summary>
        public QuantBook() : base()
        {
            try
            {
                using (Py.GIL())
                {
                    _pandas = Py.Import("pandas");
                }

                // By default, set start date to end data which is yesterday
                SetStartDate(EndDate);

                // Sets PandasConverter
                SetPandasConverter();

                // Get our handlers
                var composer = new Composer();
                _algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(composer);
                _systemHandlers = LeanEngineSystemHandlers.FromConfiguration(composer);

                // Initialize the system handlers
                _systemHandlers.Initialize();
                _systemHandlers.LeanManager.Initialize(_systemHandlers, _algorithmHandlers, new BacktestNodePacket(), new AlgorithmManager(false));
                _systemHandlers.LeanManager.SetAlgorithm(this);

                // Store our data providers
                _dataCacheProvider = new ZipDataCacheProvider(_algorithmHandlers.DataProvider);
                _dataProvider = _algorithmHandlers.DataProvider;

                // Start up the handlers we need
                // Object store
                _algorithmHandlers.ObjectStore.Initialize("QuantBook", Config.GetInt("job-user-id"), Config.GetInt("project-id"), 
                    Config.Get("api-access-token"),
                    new Controls
                    {
                        // if <= 0 we disable periodic persistence and make it synchronous
                        PersistenceIntervalSeconds = -1,
                        StorageLimitMB = Config.GetInt("storage-limit-mb", 5),
                        StorageFileCount = Config.GetInt("storage-file-count", 100),
                        StoragePermissions = (FileAccess)Config.GetInt("storage-permissions", (int)FileAccess.ReadWrite)
                    });

                // Security service
                var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
                var registeredTypes = new RegisteredSecurityDataTypesProvider();
                var securityService = new SecurityService(Portfolio.CashBook,
                    MarketHoursDatabase,
                    symbolPropertiesDataBase,
                    this,
                    registeredTypes,
                    new SecurityCacheProvider(Portfolio));
                

                // Data Manager
                _dataManager = new DataManager(_algorithmHandlers.DataFeed,
                    new UniverseSelection(
                        this,
                        securityService,
                        _algorithmHandlers.DataPermissionsManager,
                        _algorithmHandlers.DataProvider),
                    this,
                    TimeKeeper,
                    MarketHoursDatabase,
                    false,
                    registeredTypes,
                    _algorithmHandlers.DataPermissionsManager);

                // Setup history provider
                HistoryProvider = composer.GetExportedValueByTypeName<IHistoryProvider>(Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider"));
                HistoryProvider.Initialize(
                    new HistoryProviderInitializeParameters(
                        null,
                        _systemHandlers.Api,
                        _algorithmHandlers.DataProvider,
                        _dataCacheProvider,
                        _algorithmHandlers.MapFileProvider,
                        _algorithmHandlers.FactorFileProvider,
                        null,
                        true,
                        _algorithmHandlers.DataPermissionsManager
                    )
                );

                // Set our algorithm internals
                SetObjectStore(_algorithmHandlers.ObjectStore);
                Securities.SetSecurityService(securityService);
                SubscriptionManager.SetDataManager(_dataManager);
                Transactions.SetOrderProcessor(_algorithmHandlers.Transactions);

                // Set our options and future chain providers
                SetOptionChainProvider(new CachingOptionChainProvider(new BacktestingOptionChainProvider()));
                SetFutureChainProvider(new CachingFutureChainProvider(new BacktestingFutureChainProvider()));
            }
            catch (Exception exception)
            {
                throw new Exception("QuantBook.Main(): " + exception);
            }
        }

        /// <summary>
        /// Run this as an algorithm for the given time
        /// This function seeks to emulate a mini engine to run an Algorithm through
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void Run(DateTime start, DateTime end)
        {
            // Backtest start time
            var startTime = DateTime.UtcNow;

            // Change our dates according to requested range
            SetStartDate(start);
            SetEndDate(end);

            // Maybe not important??? Fetch job information from config
            string assemblyPath;
            var job = _systemHandlers.JobQueue.NextJob(out assemblyPath);

            //Setup synchronizer
            var synchronizer = new Synchronizer();
            synchronizer.Initialize(this, _dataManager);

            // Initialize the brokerage
            IBrokerageFactory factory;
            IBrokerage brokerage = _algorithmHandlers.Setup.CreateBrokerage(job, this, out factory);

            // initialize the default brokerage message handler
            BrokerageMessageHandler = factory.CreateBrokerageMessageHandler(this, job, _systemHandlers.Api);

            //Initialize the internal state of algorithm and job: executes the algorithm.Initialize() method.
            _algorithmHandlers.Setup.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, this, brokerage, job, _algorithmHandlers.Results, _algorithmHandlers.Transactions, _algorithmHandlers.RealTime, _algorithmHandlers.ObjectStore));
            _algorithmHandlers.Results.SetAlgorithm(this, _algorithmHandlers.Setup.StartingPortfolioValue);

            //-> Initialize messaging system
            _systemHandlers.Notify.SetAuthentication(job);

            //Manager for the Algorithm 
            var algorithmManager = new AlgorithmManager(false, job);

            // Initialize all our handlers
            _algorithmHandlers.DataFeed.Initialize(this, job, _algorithmHandlers.Results, _algorithmHandlers.MapFileProvider, _algorithmHandlers.FactorFileProvider,
                _algorithmHandlers.DataProvider, _dataManager, (IDataFeedTimeProvider)synchronizer, _algorithmHandlers.DataPermissionsManager.DataChannelProvider);
            _algorithmHandlers.Results.Initialize(job, _systemHandlers.Notify, _systemHandlers.Api, _algorithmHandlers.Transactions);
            _algorithmHandlers.Transactions.Initialize(this, brokerage, _algorithmHandlers.Results);
            _algorithmHandlers.RealTime.Setup(this, job, _algorithmHandlers.Results, _systemHandlers.Api, algorithmManager.TimeLimit);
            _algorithmHandlers.Alphas.Initialize(job, this, _systemHandlers.Notify, _systemHandlers.Api, _algorithmHandlers.Transactions);

            _algorithmHandlers.Alphas.OnAfterAlgorithmInitialized(this);

            //Run the Algorithm
            algorithmManager.Run(
                job,
                this,
                synchronizer,
                _algorithmHandlers.Transactions,
                _algorithmHandlers.Results, 
                _algorithmHandlers.RealTime,
                _systemHandlers.LeanManager,
                _algorithmHandlers.Alphas,
                CancellationToken.None
            );

            // Diagnostics Completed, Send Result Packet:
            var totalSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            var dataPoints = algorithmManager.DataPoints + HistoryProvider.DataPointCount;
            var kps = dataPoints / (double)1000 / totalSeconds;
            _algorithmHandlers.Results.DebugMessage($"Algorithm Id:({job.AlgorithmId}) completed in {totalSeconds:F2} seconds at {kps:F0}k data points per second. Processing total of {dataPoints:N0} data points.");

            // Shut everything we don't need down
            synchronizer.DisposeSafely();

            _algorithmHandlers.Results.Exit();
            _algorithmHandlers.DataFeed.Exit();
            _algorithmHandlers.Alphas.Exit();
            _algorithmHandlers.Transactions.Exit();
            _algorithmHandlers.RealTime.Exit();

            brokerage.Disconnect();
            brokerage.Dispose();

            // Tell the Lean Manager we have finished
            _systemHandlers.LeanManager.OnAlgorithmEnd();
        }

        /// <summary>
        /// Python implementation of GetFundamental, get fundamental data for input symbols or tickers
        /// </summary>
        /// <param name="input">The symbols or tickers to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>pandas DataFrame</returns>
        public PyObject GetFundamental(PyObject input, string selector, DateTime? start = null, DateTime? end = null)
        {
            //Null selector is not allowed for Python DataFrame
            if (string.IsNullOrWhiteSpace(selector))
            {
                throw new ArgumentException("Invalid selector. Cannot be None, empty or consist only of white-space characters");
            }

            //Covert to symbols
            var symbols = PythonUtil.ConvertToSymbols(input);

            //Fetch the data
            var fundamentalData = GetAllFundamental(symbols, selector, start, end);

            using (Py.GIL())
            {
                var data = new PyDict();
                foreach (var day in fundamentalData.OrderBy(x => x.Key))
                {
                    var orderedValues = day.Value.OrderBy(x => x.Key.ID.ToString()).ToList();
                    var columns = orderedValues.Select(x => x.Key.ID.ToString());
                    var values = orderedValues.Select(x => x.Value);
                    var row = _pandas.Series(values, columns);
                    data.SetItem(day.Key.ToPython(), row);
                }

                return _pandas.DataFrame.from_dict(data, orient: "index");
            }
        }

        /// <summary>
        /// Get fundamental data from given symbols
        /// </summary>
        /// <param name="symbols">The symbols to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>Enumerable collection of DataDictionaries, one dictionary for each day there is data</returns>
        public IEnumerable<DataDictionary<dynamic>> GetFundamental(IEnumerable<Symbol> symbols, string selector, DateTime? start = null, DateTime? end = null)
        {
            var data = GetAllFundamental(symbols, selector, start, end);

            foreach (var kvp in data.OrderBy(kvp => kvp.Key))
            {
                yield return kvp.Value;
            }
        }

        /// <summary>
        /// Get fundamental data for a given symbol
        /// </summary>
        /// <param name="symbol">The symbol to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>Enumerable collection of DataDictionaries, one Dictionary for each day there is data.</returns>
        public IEnumerable<DataDictionary<dynamic>> GetFundamental(Symbol symbol, string selector, DateTime? start = null, DateTime? end = null)
        {
            var list = new List<Symbol>
            {
                symbol
            };

            return GetFundamental(list, selector, start, end);
        }

        /// <summary>
        /// Get fundamental data for a given set of tickers
        /// </summary>
        /// <param name="tickers">The tickers to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>Enumerable collection of DataDictionaries, one dictionary for each day there is data.</returns>
        public IEnumerable<DataDictionary<dynamic>> GetFundamental(IEnumerable<string> tickers, string selector, DateTime? start = null, DateTime? end = null)
        {
            var list = new List<Symbol>();
            foreach (var ticker in tickers)
            {
                list.Add(QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA));
            }

            return GetFundamental(list, selector, start, end);
        }

        /// <summary>
        /// Get fundamental data for a given ticker
        /// </summary>
        /// <param name="symbol">The symbol to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>Enumerable collection of DataDictionaries, one Dictionary for each day there is data.</returns>
        public dynamic GetFundamental(string ticker, string selector, DateTime? start = null, DateTime? end = null)
        {
            //Check if its Python; PythonNet likes to convert the strings, but for python we want the DataFrame as the return object
            //So we must route the function call to the Python version.
            if (_isPythonNotebook)
            {
                return GetFundamental(ticker.ToPython(), selector, start, end);
            }

            var symbol = QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA);
            var list = new List<Symbol>
            {
                symbol
            };

            return GetFundamental(list, selector, start, end);
        }

        /// <summary>
        /// Gets <see cref="OptionHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical option data for</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>A <see cref="OptionHistory"/> object that contains historical option data.</returns>
        public OptionHistory GetOptionHistory(Symbol symbol, DateTime start, DateTime? end = null, Resolution? resolution = null)
        {
            if (!end.HasValue || end.Value == start)
            {
                end = start.AddDays(1);
            }

            // Load a canonical option Symbol if the user provides us with an underlying Symbol
            if (symbol.SecurityType != SecurityType.Option)
            {
                symbol = AddOption(symbol.Value, resolution, symbol.ID.Market).Symbol;
            }

            IEnumerable<Symbol> symbols;
            if (symbol.IsCanonical())
            {
                // canonical symbol, lets find the contracts
                var option = Securities[symbol] as Option;
                var resolutionToUseForUnderlying = resolution ?? SubscriptionManager.SubscriptionDataConfigService
                                                       .GetSubscriptionDataConfigs(symbol)
                                                       .GetHighestResolution();
                if (!Securities.ContainsKey(symbol.Underlying))
                {
                    // only add underlying if not present
                    AddEquity(symbol.Underlying.Value, resolutionToUseForUnderlying);
                }
                var allSymbols = new List<Symbol>();
                for (var date = start; date < end; date = date.AddDays(1))
                {
                    if (option.Exchange.DateIsOpen(date))
                    {
                        allSymbols.AddRange(OptionChainProvider.GetOptionContractList(symbol.Underlying, date));
                    }
                }

                var optionFilterUniverse = new OptionFilterUniverse();
                var distinctSymbols = allSymbols.Distinct();
                symbols = base.History(symbol.Underlying, start, end.Value, resolution)
                    .SelectMany(x =>
                    {
                        // the option chain symbols wont change so we can set 'exchangeDateChange' to false always
                        optionFilterUniverse.Refresh(distinctSymbols, x, exchangeDateChange: false);
                        return option.ContractFilter.Filter(optionFilterUniverse);
                    })
                    .Distinct().Concat(new[] { symbol.Underlying });
            }
            else
            {
                // the symbol is a contract
                symbols = new List<Symbol> { symbol };
            }

            return new OptionHistory(History(symbols, start, end.Value, resolution));
        }

        /// <summary>
        /// Gets <see cref="FutureHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical future data for</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>A <see cref="FutureHistory"/> object that contains historical future data.</returns>
        public FutureHistory GetFutureHistory(Symbol symbol, DateTime start, DateTime? end = null, Resolution? resolution = null)
        {
            if (!end.HasValue || end.Value == start)
            {
                end = start.AddDays(1);
            }

            var allSymbols = new HashSet<Symbol>();
            if (symbol.IsCanonical())
            {
                // canonical symbol, lets find the contracts
                var future = Securities[symbol] as Future;

                for (var date = start; date < end; date = date.AddDays(1))
                {
                    if (future.Exchange.DateIsOpen(date))
                    {
                        var underlying = new Tick { Time = date };
                        var allList = FutureChainProvider.GetFutureContractList(future.Symbol, date);

                        allSymbols.UnionWith(future.ContractFilter.Filter(new FutureFilterUniverse(allList, underlying)));
                    }
                }
            }
            else
            {
                // the symbol is a contract
                allSymbols.Add(symbol);
            }

            return new FutureHistory(History(allSymbols, start, end.Value, resolution));
        }

        /// <summary>
        /// Gets the historical data of an indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of an indicator</returns>
        public PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var history = History(new[] { symbol }, period, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of a bar indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of a bar indicator</returns>
        public PyObject Indicator(IndicatorBase<IBaseDataBar> indicator, Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var history = History(new[] { symbol }, period, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of a bar indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of a bar indicator</returns>
        public PyObject Indicator(IndicatorBase<TradeBar> indicator, Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var history = History(new[] { symbol }, period, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of an indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of an indicator</returns>
        public PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, Symbol symbol, TimeSpan span, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var history = History(new[] { symbol }, span, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of a bar indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of a bar indicator</returns>
        public PyObject Indicator(IndicatorBase<IBaseDataBar> indicator, Symbol symbol, TimeSpan span, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var history = History(new[] { symbol }, span, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of a bar indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of a bar indicator</returns>
        public PyObject Indicator(IndicatorBase<TradeBar> indicator, Symbol symbol, TimeSpan span, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var history = History(new[] { symbol }, span, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of an indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of an indicator</returns>
        public PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var history = History(new[] { symbol }, start, end, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of a bar indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of a bar indicator</returns>
        public PyObject Indicator(IndicatorBase<IBaseDataBar> indicator, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var history = History(new[] { symbol }, start, end, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets the historical data of a bar indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of a bar indicator</returns>
        public PyObject Indicator(IndicatorBase<TradeBar> indicator, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var history = History(new[] { symbol }, start, end, resolution);
            return Indicator(indicator, history, selector);
        }

        /// <summary>
        /// Gets Portfolio Statistics from a pandas.DataFrame with equity and benchmark values
        /// </summary>
        /// <param name="dataFrame">pandas.DataFrame with the information required to compute the Portfolio statistics</param>
        /// <returns><see cref="PortfolioStatistics"/> object wrapped in a <see cref="PyDict"/> with the portfolio statistics.</returns>
        public PyDict GetPortfolioStatistics(PyObject dataFrame)
        {
            var dictBenchmark = new SortedDictionary<DateTime, double>();
            var dictEquity = new SortedDictionary<DateTime, double>();
            var dictPL = new SortedDictionary<DateTime, double>();

            using (Py.GIL())
            {
                var result = new PyDict();

                try
                {
                    // Converts the data from pandas.DataFrame into dictionaries keyed by time
                    var df = ((dynamic)dataFrame).dropna();
                    dictBenchmark = GetDictionaryFromSeries((PyObject)df["benchmark"]);
                    dictEquity = GetDictionaryFromSeries((PyObject)df["equity"]);
                    dictPL = GetDictionaryFromSeries((PyObject)df["equity"].pct_change());
                }
                catch (PythonException e)
                {
                    result.SetItem("Runtime Error", e.Message.ToPython());
                    return result;
                }

                // Convert the double into decimal
                var equity = new SortedDictionary<DateTime, decimal>(dictEquity.ToDictionary(kvp => kvp.Key, kvp => (decimal)kvp.Value));
                var profitLoss = new SortedDictionary<DateTime, decimal>(dictPL.ToDictionary(kvp => kvp.Key, kvp => double.IsNaN(kvp.Value) ? 0 : (decimal)kvp.Value));

                // Gets the last value of the day of the benchmark and equity
                var listBenchmark = CalculateDailyRateOfChange(dictBenchmark);
                var listPerformance = CalculateDailyRateOfChange(dictEquity);

                // Gets the startting capital
                var startingCapital = Convert.ToDecimal(dictEquity.FirstOrDefault().Value);

                // Compute portfolio statistics
                var stats = new PortfolioStatistics(profitLoss, equity, listPerformance, listBenchmark, startingCapital);

                result.SetItem("Average Win (%)", Convert.ToDouble(stats.AverageWinRate * 100).ToPython());
                result.SetItem("Average Loss (%)", Convert.ToDouble(stats.AverageLossRate * 100).ToPython());
                result.SetItem("Compounding Annual Return (%)", Convert.ToDouble(stats.CompoundingAnnualReturn * 100m).ToPython());
                result.SetItem("Drawdown (%)", Convert.ToDouble(stats.Drawdown * 100).ToPython());
                result.SetItem("Expectancy", Convert.ToDouble(stats.Expectancy).ToPython());
                result.SetItem("Net Profit (%)", Convert.ToDouble(stats.TotalNetProfit * 100).ToPython());
                result.SetItem("Sharpe Ratio", Convert.ToDouble(stats.SharpeRatio).ToPython());
                result.SetItem("Win Rate (%)", Convert.ToDouble(stats.WinRate * 100).ToPython());
                result.SetItem("Loss Rate (%)", Convert.ToDouble(stats.LossRate * 100).ToPython());
                result.SetItem("Profit-Loss Ratio", Convert.ToDouble(stats.ProfitLossRatio).ToPython());
                result.SetItem("Alpha", Convert.ToDouble(stats.Alpha).ToPython());
                result.SetItem("Beta", Convert.ToDouble(stats.Beta).ToPython());
                result.SetItem("Annual Standard Deviation", Convert.ToDouble(stats.AnnualStandardDeviation).ToPython());
                result.SetItem("Annual Variance", Convert.ToDouble(stats.AnnualVariance).ToPython());
                result.SetItem("Information Ratio", Convert.ToDouble(stats.InformationRatio).ToPython());
                result.SetItem("Tracking Error", Convert.ToDouble(stats.TrackingError).ToPython());
                result.SetItem("Treynor Ratio", Convert.ToDouble(stats.TreynorRatio).ToPython());

                return result;
            }
        }

        /// <summary>
        /// Converts a pandas.Series into a <see cref="SortedDictionary{DateTime, Double}"/>
        /// </summary>
        /// <param name="series">pandas.Series to be converted</param>
        /// <returns><see cref="SortedDictionary{DateTime, Double}"/> with pandas.Series information</returns>
        private SortedDictionary<DateTime, double> GetDictionaryFromSeries(PyObject series)
        {
            var dictionary = new SortedDictionary<DateTime, double>();

            var pyDict = new PyDict(((dynamic)series).to_dict());
            foreach (PyObject item in pyDict.Items())
            {
                var key = (DateTime)item[0].AsManagedObject(typeof(DateTime));
                var value = (double)item[1].AsManagedObject(typeof(double));
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        /// <summary>
        /// Calculates the daily rate of change
        /// </summary>
        /// <param name="dictionary"><see cref="IDictionary{DateTime, Double}"/> with prices keyed by time</param>
        /// <returns><see cref="List{Double}"/> with daily rate of change</returns>
        private List<double> CalculateDailyRateOfChange(IDictionary<DateTime, double> dictionary)
        {
            var daily = dictionary.GroupBy(kvp => kvp.Key.Date)
                .ToDictionary(x => x.Key, v => v.LastOrDefault().Value)
                .Values.ToArray();

            var rocp = new double[daily.Length];
            for (var i = 1; i < daily.Length; i++)
            {
                rocp[i] = (daily[i] - daily[i - 1]) / daily[i - 1];
            }
            rocp[0] = 0;

            return rocp.ToList();
        }

        /// <summary>
        /// Gets the historical data of an indicator and convert it into pandas.DataFrame
        /// </summary>
        /// <param name="indicator">Indicator</param>
        /// <param name="history">Historical data used to calculate the indicator</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame containing the historical data of <param name="indicator"></returns>
        private PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, IEnumerable<Slice> history, Func<IBaseData, decimal> selector = null)
        {
            // Reset the indicator
            indicator.Reset();

            // Create a dictionary of the properties
            var name = indicator.GetType().Name;

            var properties = indicator.GetType().GetProperties()
                .Where(x => x.PropertyType.IsGenericType)
                .ToDictionary(x => x.Name, y => new List<IndicatorDataPoint>());
            properties.Add(name, new List<IndicatorDataPoint>());

            indicator.Updated += (s, e) =>
            {
                if (!indicator.IsReady)
                {
                    return;
                }

                foreach (var kvp in properties)
                {
                    var dataPoint = kvp.Key == name ? e : GetPropertyValue(s, kvp.Key + ".Current");
                    kvp.Value.Add((IndicatorDataPoint)dataPoint);
                }
            };

            selector = selector ?? (x => x.Value);

            history.PushThrough(bar =>
            {
                var value = selector(bar);
                indicator.Update(bar.EndTime, value);
            });

            return PandasConverter.GetIndicatorDataFrame(properties);
        }

        /// <summary>
        /// Gets the historical data of an bar indicator and convert it into pandas.DataFrame
        /// </summary>
        /// <param name="indicator">Bar indicator</param>
        /// <param name="history">Historical data used to calculate the indicator</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame containing the historical data of <param name="indicator"></returns>
        private PyObject Indicator<T>(IndicatorBase<T> indicator, IEnumerable<Slice> history, Func<IBaseData, T> selector = null)
            where T : IBaseData
        {
            // Reset the indicator
            indicator.Reset();

            // Create a dictionary of the properties
            var name = indicator.GetType().Name;

            var properties = indicator.GetType().GetProperties()
                .Where(x => x.PropertyType.IsGenericType)
                .ToDictionary(x => x.Name, y => new List<IndicatorDataPoint>());
            properties.Add(name, new List<IndicatorDataPoint>());

            indicator.Updated += (s, e) =>
            {
                if (!indicator.IsReady)
                {
                    return;
                }

                foreach (var kvp in properties)
                {
                    var dataPoint = kvp.Key == name ? e : GetPropertyValue(s, kvp.Key + ".Current");
                    kvp.Value.Add((IndicatorDataPoint)dataPoint);
                }
            };

            selector = selector ?? (x => (T)x);

            history.PushThrough(bar => indicator.Update(selector(bar)));

            return PandasConverter.GetIndicatorDataFrame(properties);
        }

        /// <summary>
        /// Gets a value of a property
        /// </summary>
        /// <param name="baseData">Object with the desired property</param>
        /// <param name="fullName">Property name</param>
        /// <returns>Property value</returns>
        private object GetPropertyValue(object baseData, string fullName)
        {
            foreach (var name in fullName.Split('.'))
            {
                if (baseData == null) return null;

                var info = baseData.GetType().GetProperty(name);

                baseData = info?.GetValue(baseData, null);
            }

            return baseData;
        }

        /// <summary>
        /// Get all fundamental data for given symbols
        /// </summary>
        /// <param name="symbols">The symbols to retrieve fundamental data for</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>DataDictionary of Enumerable IBaseData</returns>
        private Dictionary<DateTime, DataDictionary<dynamic>> GetAllFundamental(IEnumerable<Symbol> symbols, string selector, DateTime? start = null, DateTime? end = null)
        {
            //SubscriptionRequest does not except nullable DateTimes, so set a startTime and endTime
            var startTime = start.HasValue ? (DateTime)start : QuantConnect.Time.BeginningOfTime;
            var endTime = end.HasValue ? (DateTime)end : QuantConnect.Time.EndOfTime;

            //Collection to store our results
            var data = new Dictionary<DateTime, DataDictionary<dynamic>>();

            //Build factory
            var factory = new FineFundamentalSubscriptionEnumeratorFactory(false);

            //Get all data for each symbol and fill our dictionary
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(symbols, options, symbol =>
            {
                var config = new SubscriptionDataConfig(
                        typeof(FineFundamental),
                        symbol,
                        Resolution.Daily,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        false,
                        false,
                        false
                    );
                var security = Securities.CreateSecurity(symbol, config);
                var request = new SubscriptionRequest(false, null, security, config, startTime.ConvertToUtc(TimeZones.NewYork), endTime.ConvertToUtc(TimeZones.NewYork));
                using (var enumerator = factory.CreateEnumerator(request, _dataProvider))
                {
                    while (enumerator.MoveNext())
                    {
                        var dataPoint = string.IsNullOrWhiteSpace(selector)
                            ? enumerator.Current
                            : GetPropertyValue(enumerator.Current, selector);

                        lock (data)
                        {
                            if (!data.ContainsKey(enumerator.Current.Time))
                            {
                                data[enumerator.Current.Time] = new DataDictionary<dynamic>(enumerator.Current.Time);
                            }
                            data[enumerator.Current.Time].Add(enumerator.Current.Symbol, dataPoint);
                        }
                    }
                }
            });
            return data;
        }
    }
}
