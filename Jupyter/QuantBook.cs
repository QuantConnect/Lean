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
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.Jupyter
{
    /// <summary>
    /// Provides access to data for quantitative analysis
    /// </summary>
    public class QuantBook
    {
        private dynamic _pandas;
        private IDataCacheProvider _dataCacheProvider;
        private IHistoryProvider _historyProvider;

        /// <summary>
        /// <see cref = "QuantBook" /> constructor.
        /// Provides access to data for quantitative analysis
        /// </summary>
        public QuantBook()
        {
            try
            {
                using (Py.GIL())
                {
                    _pandas = Py.Import("pandas");
                }
                
                var composer = new Composer();
                var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(composer);
                _dataCacheProvider = new SingleEntryDataCacheProvider(algorithmHandlers.DataProvider);

                var mapFileProvider = algorithmHandlers.MapFileProvider;
                _historyProvider = composer.GetExportedValueByTypeName<IHistoryProvider>(Config.Get("history-provider", "SubscriptionDataReaderHistoryProvider"));
                _historyProvider.Initialize(null, algorithmHandlers.DataProvider, _dataCacheProvider, mapFileProvider, algorithmHandlers.FactorFileProvider, null);
            }
            catch (Exception exception)
            {
                throw new Exception("QuantBook.Main(): " + exception);
            }
        }

        /// <summary>
        /// Get the Lean <see cref = "Symbol"/> object given its ticker, security type and market.
        /// </summary>
        /// <param name="ticker">The asset symbol name</param>
        /// <param name="type">The asset security type</param>
        /// <param name="market">The asset market</param>
        /// <returns><see cref = "Symbol"/> object wrapped in a <see cref = "PyObject"/></returns>
        public PyObject GetSymbol(string ticker, string type = "Equity", string market = null)
        {
            var securityType = (SecurityType)Enum.Parse(typeof(SecurityType), type, true);

            SecurityIdentifier sid;

            if (securityType == SecurityType.Cfd)
            {
                sid = SecurityIdentifier.GenerateCfd(ticker, market ?? Market.Oanda);
            }
            else if (securityType == SecurityType.Equity)
            {
                sid = SecurityIdentifier.GenerateEquity(ticker, market ?? Market.USA);
            }
            else if (securityType == SecurityType.Forex)
            {
                sid = SecurityIdentifier.GenerateForex(ticker, market ?? Market.FXCM);
            }
            else
            {
                return "Invalid security type. Use Equity, Forex or Cfd.".ToPython();
            }

            // Add symbol to cache
            SymbolCache.Set(ticker, new Symbol(sid, ticker));

            return SymbolCache.GetSymbol(ticker).ToPython();
        }

        /// <summary>
        /// Gets symbol information from a list of tickers
        /// </summary>
        /// <param name="pyObject">The tickers to retrieve information for</param>
        /// <returns>A pandas.DataFrame containing the symbol information</returns>
        public PyObject PrintSymbols(PyObject pyObject)
        {
            var symbols = GetSymbolsFromPyObject(pyObject);
            if (symbols == null)
            {
                return "Invalid ticker(s). Please use get_symbol to add symbols.".ToPython();
            }

            using (Py.GIL())
            {
                var data = new PyDict();
                var index = symbols.Select(x => x.ID);
                data.SetItem("Value", _pandas.Series(symbols.Select(x => x.Value).ToList(), index));
                data.SetItem("Asset Type", _pandas.Series(symbols.Select(x => x.SecurityType.ToString()).ToList(), index));
                data.SetItem("Market", _pandas.Series(symbols.Select(x => x.ID.Market.ToString()).ToList(), index));
                data.SetItem("Start Date", _pandas.Series(symbols.Select(x => x.SecurityType == SecurityType.Equity ? x.ID.Date.ToShortDateString() : null).ToList(), index));
                return _pandas.DataFrame(data, columns: "Value,Asset Type,Market,Start Date".Split(',').ToList());
            }
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to filter the request output, if null retuns all OHLCV</param>
        /// <param name="dataNormalizationMode">The data normalization mode. Default is Adjusted</param>
        /// <param name="extendedMarket">True to include extended market hours data, false otherwise</param>
        /// <returns>A pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject symbols, int periods, Resolution resolution = Resolution.Daily, Func<IBaseData, decimal> selector = null, DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted, bool extendedMarket = false)
        {
            var span = TimeSpan.FromDays(periods);

            switch (resolution)
            {
                case Resolution.Second:
                    span = TimeSpan.FromSeconds(periods);
                    break;
                case Resolution.Minute:
                    span = TimeSpan.FromMinutes(periods);
                    break;
                case Resolution.Hour:
                    span = TimeSpan.FromHours(periods);
                    break;
                default:
                    break;
            }

            return History(symbols, span, resolution, selector, dataNormalizationMode, extendedMarket);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to filter the request output, if null retuns all OHLCV</param>
        /// <param name="dataNormalizationMode">The data normalization mode. Default is Adjusted</param>
        /// <param name="extendedMarket">True to include extended market hours data, false otherwise</param>
        /// <returns>A pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject symbols, TimeSpan span, Resolution resolution = Resolution.Daily, Func<IBaseData, decimal> selector = null, DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted, bool extendedMarket = false)
        {
            var endTimeUtc = DateTime.Now;
            var startTimeUtc = endTimeUtc - span;
            return History(symbols, startTimeUtc, endTimeUtc, resolution, selector, dataNormalizationMode, extendedMarket);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="pyObject">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to filter the request output, if null retuns all OHLCV</param>
        /// <param name="dataNormalizationMode">The data normalization mode. Default is Adjusted</param>
        /// <param name="extendedMarket">True to include extended market hours data, false otherwise</param>
        /// <returns>A pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject pyObject, DateTime? start = null, DateTime? end = null, Resolution resolution = Resolution.Daily, Func<IBaseData, decimal> selector = null, DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted, bool extendedMarket = false)
        {
            var symbols = GetSymbolsFromPyObject(pyObject);
            if (symbols == null)
            {
                return "Invalid ticker(s). Please use get_symbol to add symbols.".ToPython();
            }

            var requests = symbols.Select(symbol =>
            {
                if (symbol.SecurityType == SecurityType.Forex || symbol.SecurityType == SecurityType.Cfd)
                {
                    start = start ?? new DateTime(2005, 1, 1);
                }

                return new HistoryRequest()
                {
                    Symbol = symbol,
                    StartTimeUtc = start ?? symbol.ID.Date,
                    EndTimeUtc = end ?? DateTime.Now,
                    Resolution = resolution,
                    FillForwardResolution = null,
                    DataNormalizationMode = dataNormalizationMode,
                    IncludeExtendedMarketHours = extendedMarket,
                    DataType = symbol.ID.SecurityType == SecurityType.Equity ? typeof(TradeBar) : typeof(QuoteBar)
                };
            });

            return CreateDataFrame(requests, _historyProvider.GetHistory(requests, TimeZones.NewYork), selector);
        }

        /// <summary>
        /// Get fundamental data from given symbols
        /// </summary>
        /// <param name="pyObject">The symbols to retrieve fundamental data for</param>
        /// <param name="selector">Selects a value from the Fundamental data to filter the request output</param>
        /// <param name="start">The start date of selected data</param>
        /// <param name="end">The end date of selected data</param>
        /// <returns></returns>
        public PyObject GetFundamental(PyObject pyObject, string selector, DateTime? start = null, DateTime? end = null)
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                return "Invalid selector. Cannot be None, empty or consist only of white-space characters".ToPython();
            }

            var symbols = GetSymbolsFromPyObject(pyObject, true);
            if (symbols == null)
            {
                return "Invalid ticker(s). Please use get_symbol to add symbols.".ToPython();
            }

            var list = new List<Tuple<Symbol, DateTime, object>>();

            foreach (var symbol in symbols)
            {
                var dir = new DirectoryInfo(Path.Combine(Globals.DataFolder, "equity", symbol.ID.Market, "fundamental", "fine", symbol.Value.ToLower()));
                if (!dir.Exists) continue;

                var config = new SubscriptionDataConfig(typeof(FineFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false);

                foreach (var fileName in dir.EnumerateFiles())
                {
                    var date = DateTime.ParseExact(fileName.Name.Substring(0, 8), DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    if (date < start || date > end) continue;

                    var factory = new TextSubscriptionDataSourceReader(_dataCacheProvider, config, date, false);
                    var source = new SubscriptionDataSource(fileName.FullName, SubscriptionTransportMedium.LocalFile);
                    var value = factory.Read(source).Select(x => GetPropertyValue(x, selector)).First();

                    list.Add(Tuple.Create(symbol, date, value));
                }
            }

            using (Py.GIL())
            {
                var data = new PyDict();
                foreach (var item in list.GroupBy(x => x.Item1))
                {
                    var index = item.Select(x => x.Item2);
                    data.SetItem(item.Key, _pandas.Series(item.Select(x => x.Item3).ToList(), index));
                }

                return _pandas.DataFrame(data);
            }
        }

        /// <summary>
        /// Get list of symbols from a PyObject
        /// </summary>
        /// <param name="pyObject">PyObject containing a list of tickers</param>
        /// <returns>List of Symbol</returns>
        private List<Symbol> GetSymbolsFromPyObject(PyObject pyObject, bool isEquity = false)
        {
            using (Py.GIL())
            {
                // If not a PyList, convert it into one
                if (!PyList.IsListType(pyObject))
                {
                    var tmp = new PyList();
                    tmp.Append(pyObject);
                    pyObject = tmp;
                }

                var symbols = new List<Symbol>();
                foreach (PyObject item in pyObject)
                {
                    var symbol = (Symbol)item.AsManagedObject(typeof(Symbol));

                    if (isEquity && string.IsNullOrWhiteSpace(symbol.Value))
                    {
                        var ticker = (string)item.AsManagedObject(typeof(string));
                        symbol = new Symbol(SecurityIdentifier.GenerateEquity(ticker, Market.USA), ticker);
                    }

                    symbols.Add(symbol);
                }
                return symbols.Count == 0 || string.IsNullOrEmpty(symbols.First().Value) ? null : symbols;
            }
        }

        /// <summary>
        /// Creates a pandas.DataFrame from an enumerable of slice
        /// </summary>
        /// <param name="requests">The history requests to execute</param>
        /// <param name="slices">An enumerable of slice containing the requested historical data</param>
        /// <param name="selector">Selects a value from the BaseData to filter the request output, if null retuns all OHLCV</param>
        /// <returns>A pandas.DataFrame containing the requested historical data</returns>
        private PyObject CreateDataFrame(IEnumerable<HistoryRequest> requests, IEnumerable<Slice> slices, Func<IBaseData, decimal> selector)
        {
            if (selector == null)
            {
                return CreateMultiIndex(requests, slices);
            }

            using (Py.GIL())
            {
                var history = slices.ToList();
                var data = new PyDict();

                foreach (var request in requests)
                {
                    var symbol = request.Symbol;

                    var list = history.Get(symbol, selector).Select(x => (double)x).ToList();
                    if (list.Count == 0) continue;

                    var index = request.DataType.Equals(typeof(QuoteBar))
                        ? history.Get<QuoteBar>(symbol).Select(x => x.Time)
                        : history.Get<TradeBar>(symbol).Select(x => x.Time);

                    data.SetItem(symbol, _pandas.Series(list, index));
                }

                return _pandas.DataFrame(data);
            }
        }

        /// <summary>
        /// Creates a pandas.DataFrame from an enumerable of slice
        /// </summary>
        /// <param name="requests">The history requests to execute</param>
        /// <param name="slices">An enumerable of slice containing the requested historical data</param>
        /// <returns>A pandas.DataFrame containing the requested historical data</returns>
        private PyObject CreateMultiIndex(IEnumerable<HistoryRequest> requests, IEnumerable<Slice> slices)
        {
            using (Py.GIL())
            {
                var history = slices.ToList();
                var data = new PyDict();
                var symbols = new Dictionary<PyObject, string>();

                foreach (var request in requests)
                {
                    var symbol = request.Symbol;
                    var dict = new Dictionary<string, List<double>>();

                    if (request.DataType.Equals(typeof(QuoteBar)))
                    {
                        var bars = history.Get<QuoteBar>(symbol).ToList();
                        if (bars.Count == 0) continue;

                        var index = bars.Select(x => x.Time);
                        dict.Add("Open", bars.Select(x => (double)x.Open).ToList());
                        dict.Add("High", bars.Select(x => (double)x.High).ToList());
                        dict.Add("Low", bars.Select(x => (double)x.Low).ToList());
                        dict.Add("Close", bars.Select(x => (double)x.Close).ToList());

                        foreach (var kvp in dict)
                        {
                            var idx = new PyTuple(new[] { symbol.ToPython(), kvp.Key.ToPython() });
                            data.SetItem(idx, _pandas.Series(kvp.Value, index));
                        }
                    }
                    else
                    {
                        var bars = history.Get<TradeBar>(symbol).ToList();
                        if (bars.Count == 0) continue;

                        var index = bars.Select(x => x.Time);
                        dict.Add("Open", bars.Select(x => (double)x.Open).ToList());
                        dict.Add("High", bars.Select(x => (double)x.High).ToList());
                        dict.Add("Low", bars.Select(x => (double)x.Low).ToList());
                        dict.Add("Close", bars.Select(x => (double)x.Close).ToList());
                        dict.Add("Volume", bars.Select(x => (double)x.Volume).ToList());

                        foreach (var kvp in dict)
                        {
                            var idx = new PyTuple(new[] { symbol.ToPython(), kvp.Key.ToPython() });
                            data.SetItem(idx, _pandas.Series(kvp.Value, index));
                        }
                    }

                    symbols.Add(symbol.ToPython(), string.Join(",", dict.Keys));
                }

                // Return null if no data was found
                if (symbols.Count == 0)
                {
                    return "No data found for requested symbols".ToPython();
                }

                var values = symbols.Values.OrderBy(x => x.Length).Last().Split(',').Select(x => x.ToPython()).ToArray();
                var keys = symbols.Keys.SelectMany(x => Enumerable.Repeat(x, values.Length)).ToArray();

                var columns = new PyList(new[]
                {
                    new PyList(keys),
                    new PyList(values).Repeat(keys.Length / values.Length)
                });
                
                var df = _pandas.DataFrame(data, columns: columns);

                // If there is one one symbol, drop top level
                if (symbols.Count == 1)
                {
                    df.columns = df.columns.droplevel(0);
                }

                return df;
            }
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

                baseData = info == null ? null : info.GetValue(baseData, null);
            }

            return baseData;
        }
    }
}