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
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Packets;
using System.Threading.Tasks;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Indicators;
using QuantConnect.Scheduling;

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
        private static bool _isPythonNotebook;

        static QuantBook()
        {
            //Determine if we are in a Python Notebook
            try
            {
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    var isPython = PyModule.FromString(Guid.NewGuid().ToString(),
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

            RecycleMemory();

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

                // Issue #4892 : Set start time relative to NY time
                // when the data is available from the previous day
                var newYorkTime = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
                var hourThreshold = Config.GetInt("qb-data-hour", 9);

                // If it is after our hour threshold; then we can use today
                if (newYorkTime.Hour >= hourThreshold)
                {
                    SetStartDate(newYorkTime);
                }
                else
                {
                    SetStartDate(newYorkTime - TimeSpan.FromDays(1));
                }

                // Sets PandasConverter
                SetPandasConverter();

                // Reset our composer; needed for re-creation of QuantBook
                Composer.Instance.Reset();
                var composer = Composer.Instance;
                Config.Reset();

                // Create our handlers with our composer instance
                var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(composer);
                // init the API
                systemHandlers.Initialize();
                var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(composer, researchMode: true);
;

                var algorithmPacket = new BacktestNodePacket
                {
                    UserToken = Globals.UserToken,
                    UserId = Globals.UserId,
                    ProjectId = Globals.ProjectId,
                    OrganizationId = Globals.OrganizationID,
                    Version = Globals.Version
                };

                ProjectId = algorithmPacket.ProjectId;
                systemHandlers.LeanManager.Initialize(systemHandlers,
                    algorithmHandlers,
                    algorithmPacket,
                    new AlgorithmManager(false));
                systemHandlers.LeanManager.SetAlgorithm(this);


                algorithmHandlers.DataPermissionsManager.Initialize(algorithmPacket);

                algorithmHandlers.ObjectStore.Initialize(algorithmPacket.UserId,
                    algorithmPacket.ProjectId,
                    algorithmPacket.UserToken,
                    new Controls
                    {
                        // if <= 0 we disable periodic persistence and make it synchronous
                        PersistenceIntervalSeconds = -1,
                        StorageLimit = Config.GetValue("storage-limit", 10737418240L),
                        StorageFileCount = Config.GetInt("storage-file-count", 10000),
                        StorageAccess = Config.GetValue("storage-permissions", new Packets.StoragePermissions())
                    });
                SetObjectStore(algorithmHandlers.ObjectStore);

                _dataCacheProvider = new ZipDataCacheProvider(algorithmHandlers.DataProvider);
                _dataProvider = algorithmHandlers.DataProvider;

                var symbolPropertiesDataBase = SymbolPropertiesDatabase.FromDataFolder();
                var registeredTypes = new RegisteredSecurityDataTypesProvider();
                var securityService = new SecurityService(Portfolio.CashBook,
                    MarketHoursDatabase,
                    symbolPropertiesDataBase,
                    this,
                    registeredTypes,
                    new SecurityCacheProvider(Portfolio),
                    algorithm: this);
                Securities.SetSecurityService(securityService);
                SubscriptionManager.SetDataManager(
                    new DataManager(new NullDataFeed(),
                        new UniverseSelection(this, securityService, algorithmHandlers.DataPermissionsManager, algorithmHandlers.DataProvider),
                        this,
                        TimeKeeper,
                        MarketHoursDatabase,
                        false,
                        registeredTypes,
                        algorithmHandlers.DataPermissionsManager));

                var mapFileProvider = algorithmHandlers.MapFileProvider;
                HistoryProvider = new HistoryProviderManager();
                HistoryProvider.Initialize(
                    new HistoryProviderInitializeParameters(
                        null,
                        null,
                        algorithmHandlers.DataProvider,
                        _dataCacheProvider,
                        mapFileProvider,
                        algorithmHandlers.FactorFileProvider,
                        null,
                        true,
                        algorithmHandlers.DataPermissionsManager,
                        ObjectStore,
                        Settings
                    )
                );

                var initParameters = new ChainProviderInitializeParameters(mapFileProvider, HistoryProvider);
                var optionChainProvider = new BacktestingOptionChainProvider();
                optionChainProvider.Initialize(initParameters);
                var futureChainProvider = new BacktestingFutureChainProvider();
                futureChainProvider.Initialize(initParameters);
                SetOptionChainProvider(new CachingOptionChainProvider(optionChainProvider));
                SetFutureChainProvider(new CachingFutureChainProvider(futureChainProvider));

                SetAlgorithmMode(AlgorithmMode.Research);
                SetDeploymentTarget(Config.GetValue("deployment-target", DeploymentTarget.LocalPlatform));
            }
            catch (Exception exception)
            {
                throw new Exception("QuantBook.Main(): " + exception);
            }
        }

        /// <summary>
        /// Python implementation of GetFundamental, get fundamental data for input symbols or tickers
        /// </summary>
        /// <param name="input">The symbols or tickers to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns>pandas DataFrame</returns>
        [Obsolete("Please use the 'UniverseHistory()' API")]
        public PyObject GetFundamental(PyObject input, string selector = null, DateTime? start = null, DateTime? end = null)
        {
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

                return _pandas.DataFrame.from_dict(data, orient:"index");
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
        [Obsolete("Please use the 'UniverseHistory()' API")]
        public IEnumerable<DataDictionary<dynamic>> GetFundamental(IEnumerable<Symbol> symbols, string selector = null, DateTime? start = null, DateTime? end = null)
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
        [Obsolete("Please use the 'UniverseHistory()' API")]
        public IEnumerable<DataDictionary<dynamic>> GetFundamental(Symbol symbol, string selector = null, DateTime? start = null, DateTime? end = null)
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
        [Obsolete("Please use the 'UniverseHistory()' API")]
        public IEnumerable<DataDictionary<dynamic>> GetFundamental(IEnumerable<string> tickers, string selector = null, DateTime? start = null, DateTime? end = null)
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
        [Obsolete("Please use the 'UniverseHistory()' API")]
        public dynamic GetFundamental(string ticker, string selector = null, DateTime? start = null, DateTime? end = null)
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
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <returns>A <see cref="OptionHistory"/> object that contains historical option data.</returns>
        public OptionHistory OptionHistory(Symbol symbol, string targetOption, DateTime start, DateTime? end = null, Resolution? resolution = null,
            bool fillForward = true, bool extendedMarketHours = false)
        {
            symbol = GetOptionSymbolForHistoryRequest(symbol, targetOption, resolution, fillForward);

            return OptionHistory(symbol, start, end, resolution, fillForward, extendedMarketHours);
        }

        /// <summary>
        /// Gets <see cref="OptionHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical option data for</param>
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <returns>A <see cref="OptionHistory"/> object that contains historical option data.</returns>
        [Obsolete("Please use the 'OptionHistory()' API")]
        public OptionHistory GetOptionHistory(Symbol symbol, string targetOption, DateTime start, DateTime? end = null, Resolution? resolution = null,
            bool fillForward = true, bool extendedMarketHours = false)
        {
            return OptionHistory(symbol, targetOption, start, end, resolution, fillForward, extendedMarketHours);
        }

        /// <summary>
        /// Gets <see cref="OptionHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical option data for</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <returns>A <see cref="OptionHistory"/> object that contains historical option data.</returns>
        public OptionHistory OptionHistory(Symbol symbol, DateTime start, DateTime? end = null, Resolution? resolution = null,
            bool fillForward = true, bool extendedMarketHours = false)
        {
            if (!end.HasValue || end.Value == start)
            {
                end = start.AddDays(1);
            }

            // Load a canonical option Symbol if the user provides us with an underlying Symbol
            symbol = GetOptionSymbolForHistoryRequest(symbol, null, resolution, fillForward);

            IEnumerable<Symbol> symbols;
            if (symbol.IsCanonical())
            {
                // canonical symbol, lets find the contracts
                var option = Securities[symbol] as Option;
                if (!Securities.ContainsKey(symbol.Underlying))
                {
                    var resolutionToUseForUnderlying = resolution ?? SubscriptionManager.SubscriptionDataConfigService
                                                           .GetSubscriptionDataConfigs(symbol.Underlying)
                                                           .Select(x => (Resolution?)x.Resolution)
                                                           .DefaultIfEmpty(null)
                                                           .Min();
                    if (!resolutionToUseForUnderlying.HasValue && UniverseManager.TryGetValue(symbol, out var universe))
                    {
                        resolutionToUseForUnderlying = universe.UniverseSettings.Resolution;
                    }

                    if (symbol.Underlying.SecurityType == SecurityType.Equity)
                    {
                        // only add underlying if not present
                        AddEquity(symbol.Underlying.Value, resolutionToUseForUnderlying, fillForward: fillForward,
                            extendedMarketHours: extendedMarketHours);
                    }
                    else if (symbol.Underlying.SecurityType == SecurityType.Index)
                    {
                        // only add underlying if not present
                        AddIndex(symbol.Underlying.Value, resolutionToUseForUnderlying, fillForward: fillForward);
                    }
                    else if (symbol.Underlying.SecurityType == SecurityType.Future && symbol.Underlying.IsCanonical())
                    {
                        AddFuture(symbol.Underlying.ID.Symbol, resolutionToUseForUnderlying, fillForward: fillForward,
                            extendedMarketHours: extendedMarketHours);
                    }
                    else if (symbol.Underlying.SecurityType == SecurityType.Future)
                    {
                        AddFutureContract(symbol.Underlying, resolutionToUseForUnderlying, fillForward: fillForward,
                            extendedMarketHours: extendedMarketHours);
                    }
                }

                var allSymbols = new HashSet<Symbol>();
                var optionFilterUniverse = new OptionFilterUniverse(option);

                foreach (var (date, chainData, underlyingData) in GetChainHistory<OptionUniverse>(option, start, end.Value, extendedMarketHours))
                {
                    if (underlyingData is not null)
                    {
                        optionFilterUniverse.Refresh(chainData, underlyingData, underlyingData.EndTime);
                        allSymbols.UnionWith(option.ContractFilter.Filter(optionFilterUniverse).Select(x => x.Symbol));
                    }
                }

                var distinctSymbols = allSymbols.Distinct().Select(x => new OptionUniverse() { Symbol = x, Time = start });
                symbols = allSymbols.Concat(new[] { symbol.Underlying });
            }
            else
            {
                // the symbol is a contract
                symbols = new List<Symbol>{ symbol };
            }

            return new OptionHistory(History(symbols, start, end.Value, resolution, fillForward, extendedMarketHours));
        }

        /// <summary>
        /// Gets <see cref="OptionHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical option data for</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <returns>A <see cref="OptionHistory"/> object that contains historical option data.</returns>
        [Obsolete("Please use the 'OptionHistory()' API")]
        public OptionHistory GetOptionHistory(Symbol symbol, DateTime start, DateTime? end = null, Resolution? resolution = null,
            bool fillForward = true, bool extendedMarketHours = false)
        {
            return OptionHistory(symbol, start, end, resolution, fillForward, extendedMarketHours);
        }

        /// <summary>
        /// Gets <see cref="FutureHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical future data for</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <returns>A <see cref="FutureHistory"/> object that contains historical future data.</returns>
        public FutureHistory FutureHistory(Symbol symbol, DateTime start, DateTime? end = null, Resolution? resolution = null,
            bool fillForward = true, bool extendedMarketHours = false)
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

                foreach (var (date, chainData, underlyingData) in GetChainHistory<FutureUniverse>(future, start, end.Value, extendedMarketHours))
                {
                    allSymbols.UnionWith(future.ContractFilter.Filter(new FutureFilterUniverse(chainData, date)).Select(x => x.Symbol));
                }
            }
            else
            {
                // the symbol is a contract
                allSymbols.Add(symbol);
            }

            return new FutureHistory(History(allSymbols, start, end.Value, resolution, fillForward, extendedMarketHours));
        }

        /// <summary>
        /// Gets <see cref="FutureHistory"/> object for a given symbol, date and resolution
        /// </summary>
        /// <param name="symbol">The symbol to retrieve historical future data for</param>
        /// <param name="start">The history request start time</param>
        /// <param name="end">The history request end time. Defaults to 1 day if null</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <returns>A <see cref="FutureHistory"/> object that contains historical future data.</returns>
        [Obsolete("Please use the 'FutureHistory()' API")]
        public FutureHistory GetFutureHistory(Symbol symbol, DateTime start, DateTime? end = null, Resolution? resolution = null,
            bool fillForward = true, bool extendedMarketHours = false)
        {
            return FutureHistory(symbol, start, end, resolution, fillForward, extendedMarketHours);
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var history = History(new[] { symbol }, period, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<IBaseDataBar> indicator, Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var history = History(new[] { symbol }, period, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<TradeBar> indicator, Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var history = History(new[] { symbol }, period, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, Symbol symbol, TimeSpan span, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var history = History(new[] { symbol }, span, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<IBaseDataBar> indicator, Symbol symbol, TimeSpan span, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var history = History(new[] { symbol }, span, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<TradeBar> indicator, Symbol symbol, TimeSpan span, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var history = History(new[] { symbol }, span, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<IndicatorDataPoint> indicator, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var history = History(new[] { symbol }, start, end, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<IBaseDataBar> indicator, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var history = History(new[] { symbol }, start, end, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
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
        [Obsolete("Please use the 'IndicatorHistory()', pandas dataframe available through '.DataFrame'")]
        public PyObject Indicator(IndicatorBase<TradeBar> indicator, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var history = History(new[] { symbol }, start, end, resolution);
            return IndicatorHistory(indicator, history, selector).DataFrame;
        }

        /// <summary>
        /// Will return the universe selection data and will optionally perform selection
        /// </summary>
        /// <typeparam name="T1">The universe selection universe data type, for example Fundamentals</typeparam>
        /// <typeparam name="T2">The selection data type, for example Fundamental</typeparam>
        /// <param name="start">The start date</param>
        /// <param name="end">Optionally the end date, will default to today</param>
        /// <param name="func">Optionally the universe selection function</param>
        /// <param name="dateRule">Date rule to apply for the history data</param>
        /// <returns>Enumerable of universe selection data for each date, filtered if the func was provided</returns>
        public IEnumerable<IEnumerable<T2>> UniverseHistory<T1, T2>(DateTime start, DateTime? end = null, Func<IEnumerable<T2>, IEnumerable<Symbol>> func = null, IDateRule dateRule = null)
            where T1 : BaseDataCollection
            where T2 : IBaseData
        {
            var universeSymbol = ((BaseDataCollection)typeof(T1).GetBaseDataInstance()).UniverseSymbol();

            var symbols = new[] { universeSymbol };
            var endDate = end ?? DateTime.UtcNow.Date;
            var requests = CreateDateRangeHistoryRequests(new[] { universeSymbol }, typeof(T1), start, endDate);
            var history = GetDataTypedHistory<BaseDataCollection>(requests).Select(x => x.Values.Single());

            HashSet<Symbol> filteredSymbols = null;
            Func<BaseDataCollection, IEnumerable<T2>> castDataPoint = dataPoint =>
            {
                var castedType = dataPoint.Data.OfType<T2>();
                if (func != null)
                {
                    var selection = func(castedType);
                    if (!ReferenceEquals(selection, Universe.Unchanged))
                    {
                        filteredSymbols = selection.ToHashSet();
                    }
                    return castedType.Where(x => filteredSymbols == null || filteredSymbols.Contains(x.Symbol));
                }
                else
                {
                    return castedType;
                }
            };

            Func<BaseDataCollection, DateTime> getTime = datapoint => datapoint.EndTime.Date;


            return PerformSelection<IEnumerable<T2>, BaseDataCollection>(history, castDataPoint, getTime, start, endDate, dateRule);
        }

        /// <summary>
        /// Will return the universe selection data and will optionally perform selection
        /// </summary>
        /// <param name="universe">The universe to fetch the data for</param>
        /// <param name="start">The start date</param>
        /// <param name="end">Optionally the end date, will default to today</param>
        /// <param name="dateRule">Date rule to apply for the history data</param>
        /// <returns>Enumerable of universe selection data for each date, filtered if the func was provided</returns>
        public IEnumerable<IEnumerable<BaseData>> UniverseHistory(Universe universe, DateTime start, DateTime? end = null, IDateRule dateRule = null)
        {
            return RunUniverseSelection(universe, start, end, dateRule);
        }

        /// <summary>
        /// Will return the universe selection data and will optionally perform selection
        /// </summary>
        /// <param name="universe">The universe to fetch the data for</param>
        /// <param name="start">The start date</param>
        /// <param name="end">Optionally the end date, will default to today</param>
        /// <param name="func">Optionally the universe selection function</param>
        /// <param name="dateRule">Date rule to apply for the history data</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// For universe data, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>Enumerable of universe selection data for each date, filtered if the func was provided</returns>
        public PyObject UniverseHistory(PyObject universe, DateTime start, DateTime? end = null, PyObject func = null, IDateRule dateRule = null,
            bool flatten = false)
        {
            if (universe.TryConvert<Universe>(out var convertedUniverse))
            {
                if (func != null)
                {
                    throw new ArgumentException($"When providing a universe, the selection func argument isn't supported. Please provider a universe or a type and a func");
                }
                var filteredUniverseSelectionData = RunUniverseSelection(convertedUniverse, start, end, dateRule);

                return GetDataFrame(filteredUniverseSelectionData, flatten);
            }
            // for backwards compatibility
            if (universe.TryConvert<Type>(out var convertedType) && convertedType.IsAssignableTo(typeof(BaseDataCollection)))
            {
                var endDate = end ?? DateTime.UtcNow.Date;
                var universeSymbol = ((BaseDataCollection)convertedType.GetBaseDataInstance()).UniverseSymbol();
                if (func == null)
                {
                    return History(universe, universeSymbol, start, endDate, flatten: flatten);
                }

                var requests = CreateDateRangeHistoryRequests(new[] { universeSymbol }, convertedType, start, endDate);
                var history = History(requests);

                return GetDataFrame(GetFilteredSlice(history, func, start, endDate, dateRule), flatten, convertedType);
            }

            throw new ArgumentException($"Failed to convert given universe {universe}. Please provider a valid {nameof(Universe)}");
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

                // call method to set tradingDayPerYear for Algorithm (use: backwards compatibility)
                BaseSetupHandler.SetBrokerageTradingDayPerYear(algorithm: this);

                // Compute portfolio statistics
                var stats = new PortfolioStatistics(profitLoss, equity, new(), listPerformance, listBenchmark, startingCapital, RiskFreeInterestRateModel,
                    Settings.TradingDaysPerYear.Value);

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
        /// Get's the universe data for the specified date
        /// </summary>
        private IEnumerable<T> GetChainHistory<T>(Symbol canonicalSymbol, DateTime date, out BaseData underlyingData)
            where T : BaseChainUniverseData
        {
            // Use this GetEntry extension method since it's data type dependent, so we get the correct entry for the option universe
            var marketHoursEntry = MarketHoursDatabase.GetEntry(canonicalSymbol, new[] { typeof(T) });
            var startInExchangeTz = QuantConnect.Time.GetStartTimeForTradeBars(marketHoursEntry.ExchangeHours, date, QuantConnect.Time.OneDay, 1,
                extendedMarketHours: false, marketHoursEntry.DataTimeZone);
            var start = startInExchangeTz.ConvertTo(marketHoursEntry.ExchangeHours.TimeZone, TimeZone);
            var end = date.ConvertTo(marketHoursEntry.ExchangeHours.TimeZone, TimeZone);
            var universeData = History<T>(canonicalSymbol, start, end).SingleOrDefault();

            if (universeData is not null)
            {
                underlyingData = universeData.Underlying;
                return universeData.Data.Cast<T>();
            }

            underlyingData = null;
            return Enumerable.Empty<T>();
        }

        /// <summary>
        /// Helper method to get option/future chain historical data for a given date range
        /// </summary>
        private IEnumerable<(DateTime Date, IEnumerable<T> ChainData, BaseData UnderlyingData)> GetChainHistory<T>(
            Security security, DateTime start, DateTime end, bool extendedMarketHours)
            where T : BaseChainUniverseData
        {
            foreach (var date in QuantConnect.Time.EachTradeableDay(security, start.Date, end.Date, extendedMarketHours))
            {
                var universeData = GetChainHistory<T>(security.Symbol, date, out var underlyingData);
                yield return (date, universeData, underlyingData);
            }
        }

        /// <summary>
        /// Helper method to perform selection on the given data and filter it
        /// </summary>
        private IEnumerable<Slice> GetFilteredSlice(IEnumerable<Slice> history, dynamic func, DateTime start, DateTime end, IDateRule dateRule = null)
        {
            HashSet<Symbol> filteredSymbols = null;
            Func<Slice, Slice> processSlice = slice =>
            {
                var filteredData = slice.AllData.OfType<BaseDataCollection>();
                using (Py.GIL())
                {
                    using PyObject selection = func(filteredData.SelectMany(baseData => baseData.Data));
                    if (!selection.TryConvert<object>(out var result) || !ReferenceEquals(result, Universe.Unchanged))
                    {
                        filteredSymbols = ((Symbol[])selection.AsManagedObject(typeof(Symbol[]))).ToHashSet();
                    }
                }
                return new Slice(slice.Time, filteredData.Where(x => {
                    if (filteredSymbols == null)
                    {
                        return true;
                    }
                    x.Data = new List<BaseData>(x.Data.Where(dataPoint => filteredSymbols.Contains(dataPoint.Symbol)));
                    return true;
                }), slice.UtcTime);
            };

            Func<Slice, DateTime> getTime = slice => slice.Time.Date;
            return PerformSelection<Slice, Slice>(history, processSlice, getTime, start, end, dateRule);
        }

        /// <summary>
        /// Helper method to perform selection on the given data and filter it using the given universe
        /// </summary>
        private IEnumerable<BaseDataCollection> RunUniverseSelection(Universe universe, DateTime start, DateTime? end = null, IDateRule dateRule = null)
        {
            var endDate = end ?? DateTime.UtcNow.Date;
            var history = History(universe, start, endDate);

            HashSet<Symbol> filteredSymbols = null;
            Func<BaseDataCollection, BaseDataCollection> processDataPoint = dataPoint =>
            {
                var utcTime = dataPoint.EndTime.ConvertToUtc(universe.Configuration.ExchangeTimeZone);
                var selection = universe.SelectSymbols(utcTime, dataPoint);
                if (!ReferenceEquals(selection, Universe.Unchanged))
                {
                    filteredSymbols = selection.ToHashSet();
                }
                dataPoint.Data = dataPoint.Data.Where(x => filteredSymbols == null || filteredSymbols.Contains(x.Symbol)).ToList();
                return dataPoint;
            };

            Func<BaseDataCollection, DateTime> getTime = dataPoint => dataPoint.EndTime.Date;

            return PerformSelection<BaseDataCollection, BaseDataCollection>(history, processDataPoint, getTime, start, endDate, dateRule);
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

                // TODO this is expensive and can be cached
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
            var startTime = start.HasValue ? (DateTime)start : QuantConnect.Time.Start;
            var endTime = end.HasValue ? (DateTime) end : DateTime.UtcNow.Date;

            //Collection to store our results
            var data = new Dictionary<DateTime, DataDictionary<dynamic>>();

            //Get all data for each symbol and fill our dictionary
            foreach (var symbol in symbols)
            {
                var exchangeHours = MarketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
                foreach (var date in QuantConnect.Time.EachTradeableDayInTimeZone(exchangeHours, startTime, endTime, TimeZones.NewYork))
                {
                    var currentData = new Fundamental(date, symbol);
                    var time = currentData.EndTime;
                    object dataPoint = currentData;
                    if (!string.IsNullOrWhiteSpace(selector))
                    {
                        dataPoint = GetPropertyValue(currentData, selector);
                        if (BaseFundamentalDataProvider.IsNone(dataPoint))
                        {
                            dataPoint = null;
                        }
                    }

                    if (!data.TryGetValue(time, out var dataAtTime))
                    {
                        dataAtTime = data[time] = new DataDictionary<dynamic>(time);
                    }
                    dataAtTime.Add(currentData.Symbol, dataPoint);
                }
            }
            return data;
        }

        private Symbol GetOptionSymbolForHistoryRequest(Symbol symbol, string targetOption, Resolution? resolution, bool fillForward)
        {
            // Load a canonical option Symbol if the user provides us with an underlying Symbol
            if (!symbol.SecurityType.IsOption())
            {
                var option = AddOption(symbol, targetOption,  resolution, symbol.ID.Market, fillForward);

                // Allow 20 strikes from the money for futures. No expiry filter is applied
                // so that any future contract provided will have data returned.
                if (symbol.SecurityType == SecurityType.Future && symbol.IsCanonical())
                {
                    throw new ArgumentException("The Future Symbol provided is a canonical Symbol (i.e. a Symbol representing all Futures), which is not supported at this time. " +
                        "If you are using the Symbol accessible from `AddFuture(...)`, use the Symbol from `AddFutureContract(...)` instead. " +
                        "You can use `qb.FutureOptionChainProvider(canonicalFuture, datetime)` to get a list of futures contracts for a given date, and add them to your algorithm with `AddFutureContract(symbol, Resolution)`.");
                }
                if (symbol.SecurityType == SecurityType.Future && !symbol.IsCanonical())
                {
                    option.SetFilter(universe => universe.Strikes(-10, +10));
                }

                symbol = option.Symbol;
            }

            return symbol;
        }

        private static void RecycleMemory()
        {
            Task.Delay(TimeSpan.FromSeconds(20)).ContinueWith(_ =>
            {
                if (Logging.Log.DebuggingEnabled)
                {
                    Logging.Log.Debug($"QuantBook.RecycleMemory(): running...");
                }

                GC.Collect();

                RecycleMemory();
            }, TaskScheduler.Current);
        }

        protected static IEnumerable<T1> PerformSelection<T1, T2>(
            IEnumerable<T2> history,
            Func<T2, T1> processDataPointFunction,
            Func<T2, DateTime> getTime,
            DateTime start,
            DateTime endDate,
            IDateRule dateRule = null)
        {
            if (dateRule == null)
            {
                foreach(var dataPoint in history)
                {
                    yield return processDataPointFunction(dataPoint);
                }

                yield break;
            }

            var targetDatesQueue = new Queue<DateTime>(dateRule.GetDates(start, endDate));
            T2 previousDataPoint = default;
            foreach (var dataPoint in history)
            {
                var dataPointWasProcessed = false;

                // If the datapoint date is greater than the target date on the top, process the last
                // datapoint and remove target dates from the queue until the target date on the top is
                // greater than the current datapoint date
                while (targetDatesQueue.TryPeek(out var targetDate) && getTime(dataPoint) >= targetDate)
                {
                    if (getTime(dataPoint) == targetDate)
                    {
                        yield return processDataPointFunction(dataPoint);

                        // We use each data point just once, this is, we cannot return the same datapoint
                        // twice
                        dataPointWasProcessed = true;
                    }
                    else
                    {
                        if (!Equals(previousDataPoint, default(T2)))
                        {
                            yield return processDataPointFunction(previousDataPoint);
                        }
                    }

                    previousDataPoint = default;
                    // Search the next target date
                    targetDatesQueue.Dequeue();
                }

                if (!dataPointWasProcessed)
                {
                    previousDataPoint = dataPoint;
                }
            }
        }
    }
}
