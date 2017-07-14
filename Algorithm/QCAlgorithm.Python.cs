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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using QuantConnect.Securities;
using NodaTime;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using QuantConnect.Python;
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Fundamental;
using System.Linq;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private dynamic _pandas;

        /// <summary>
        /// Sets pandas library
        /// </summary>
        public void SetPandas()
        {
            try
            {
                using (Py.GIL())
                {
                    _pandas = Py.Import("pandas");
                }
            }
            catch (PythonException pythonException)
            {
                Error("QCAlgorithm.SetPandas(): Failed to import pandas module: " + pythonException);
            }
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <remarks>Generic type T must implement base data</remarks>
        public void AddData(PyObject type, string symbol, Resolution resolution = Resolution.Minute)
        {
            AddData(type, symbol, Resolution.Minute, TimeZones.NewYork, false, 1m);
        }
        

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        public void AddData(PyObject type, string symbol, Resolution resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
        {
            var objectType = CreateType(type.Repr().Split('.')[1].Replace("\'>", ""));
            AddData(objectType, symbol, resolution, timeZone, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="T">Data source type</param>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        public void AddData(Type T, string symbol, Resolution resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
        {
            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(Market.USA, symbol, SecurityType.Base, timeZone);

            //Add this to the data-feed subscriptions
            var symbolObject = new Symbol(SecurityIdentifier.GenerateBase(symbol, Market.USA), symbol);
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(Market.USA, symbol, SecurityType.Base, CashBook.AccountCurrency);

            //Add this new generic data as a tradeable security: 
            var security = SecurityManager.CreateSecurity(new List<Type>() { T }, Portfolio, SubscriptionManager, marketHoursDbEntry.ExchangeHours, marketHoursDbEntry.DataTimeZone,
                symbolProperties, SecurityInitializer, symbolObject, resolution, fillDataForward, leverage, true, false, true, LiveMode);

            AddToUserDefinedUniverse(security);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="pycoarse">Defines an initial coarse selection</param>
        public void AddUniverse(PyObject pycoarse)
        {
            var coarse = ToFunc<CoarseFundamental>(pycoarse);
            AddUniverse(coarse);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse and fine fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="pycoarse">Defines an initial coarse selection</param>
        /// <param name="pyfine">Defines a more detailed selection with access to more data</param>
        public void AddUniverse(PyObject pycoarse, PyObject pyfine)
        {
            var coarse = ToFunc<CoarseFundamental>(pycoarse);
            var fine = ToFunc<FineFundamental>(pyfine);
            AddUniverse(coarse, fine);
        }
        
        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, Resolution? resolution = null)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, resolution);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, Resolution? resolution = null)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, resolution);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, Resolution? resolution, Func<IBaseData, IBaseDataBar> selector)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, Resolution? resolution, Func<IBaseData, TradeBar> selector)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, TimeSpan? resolution, Func<IBaseData, IBaseDataBar> selector)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, TimeSpan? resolution, Func<IBaseData, TradeBar> selector)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, IDataConsolidator consolidator, Func<IBaseData, IBaseDataBar> selector)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, consolidator, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, IDataConsolidator consolidator, Func<IBaseData, TradeBar> selector)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, consolidator, selector);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="first">The first indicator to plot</param>
        /// <param name="second">The second indicator to plot</param>
        /// <param name="third">The third indicator to plot</param>
        /// <param name="fourth">The fourth indicator to plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, Indicator first, Indicator second = null, Indicator third = null, Indicator fourth = null)
        {
            Plot(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="first">The first indicator to plot</param>
        /// <param name="second">The second indicator to plot</param>
        /// <param name="third">The third indicator to plot</param>
        /// <param name="fourth">The fourth indicator to plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, BarIndicator first, BarIndicator second = null, BarIndicator third = null, BarIndicator fourth = null)
        {
            Plot(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="first">The first indicator to plot</param>
        /// <param name="second">The second indicator to plot</param>
        /// <param name="third">The third indicator to plot</param>
        /// <param name="fourth">The fourth indicator to plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, TradeBarIndicator first, TradeBarIndicator second = null, TradeBarIndicator third = null, TradeBarIndicator fourth = null)
        {
            Plot(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, Indicator first, Indicator second = null, Indicator third = null, Indicator fourth = null)
        {
            PlotIndicator(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, BarIndicator first, BarIndicator second = null, BarIndicator third = null, BarIndicator fourth = null)
        {
            PlotIndicator(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, TradeBarIndicator first, TradeBarIndicator second = null, TradeBarIndicator third = null, TradeBarIndicator fourth = null)
        {
            PlotIndicator(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, Indicator first, Indicator second = null, Indicator third = null, Indicator fourth = null)
        {
            PlotIndicator(chart, waitForReady, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, BarIndicator first, BarIndicator second = null, BarIndicator third = null, BarIndicator fourth = null)
        {
            PlotIndicator(chart, waitForReady, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, TradeBarIndicator first, TradeBarIndicator second = null, TradeBarIndicator third = null, TradeBarIndicator fourth = null)
        {
            PlotIndicator(chart, waitForReady, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Gets the historical data for the specified symbol. The exact number of bars will be returned. 
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>A python dictionary with pandas DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject tickers, int periods, Resolution? resolution = null)
        {
            var symbols = GetSymbolsFromPyObject(tickers);
            if (symbols == null) return null;

            return CreatePandasDataFrame(symbols, History(symbols, periods, resolution));
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>A python dictionary with pandas DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject tickers, TimeSpan span, Resolution? resolution = null)
        {
            var symbols = GetSymbolsFromPyObject(tickers);
            if (symbols == null) return null;
            
            return CreatePandasDataFrame(symbols, History(symbols, span, resolution));
        }

        /// <summary>
        /// Gets the historical data for the specified symbol between the specified dates. The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>A python dictionary with pandas DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject tickers, DateTime start, DateTime end, Resolution? resolution = null)
        {
            var symbols = GetSymbolsFromPyObject(tickers);
            if (symbols == null) return null;

            return CreatePandasDataFrame(symbols, History(symbols, start, end, resolution));
        }

        /// <summary>
        /// Creates a pandas DataFrame from an enumerable of slice containing the requested historical data
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="history">an enumerable of slice containing the requested historical data</param>
        /// <returns>A python dictionary with pandas DataFrame containing the requested historical data</returns>
        private PyObject CreatePandasDataFrame(List<Symbol> symbols, IEnumerable<Slice> history)
        {
            // If pandas is null (cound not be imported), return null
            if (_pandas == null)
            {
                return null;
            }

            using (Py.GIL())
            {
                var pyDict = new PyDict();

                foreach (var symbol in symbols)
                {
                    var index = Securities[symbol].Type == SecurityType.Equity
                        ? history.Get<TradeBar>(symbol).Select(x => x.Time)
                        : history.Get<QuoteBar>(symbol).Select(x => x.Time);

                    var dataframe = new PyDict();
                    dataframe.SetItem("open", _pandas.Series(history.Get(symbol, Field.Open).ToList(), index));
                    dataframe.SetItem("high", _pandas.Series(history.Get(symbol, Field.High).ToList(), index));
                    dataframe.SetItem("low", _pandas.Series(history.Get(symbol, Field.Low).ToList(), index));
                    dataframe.SetItem("close", _pandas.Series(history.Get(symbol, Field.Close).ToList(), index));
                    dataframe.SetItem("volume", _pandas.Series(history.Get(symbol, Field.Volume).ToList(), index));

                    pyDict.SetItem(symbol.Value, _pandas.DataFrame(dataframe, columns: new[] { "open", "high", "low", "close", "volume" }.ToList()));
                }

                return pyDict;
            }
        }

        /// <summary>
        /// Gets the symbols/string from a PyObject
        /// </summary>
        /// <param name="pyObject">PyObject containing symbols</param>
        /// <returns>List of symbols</returns>
        private List<Symbol> GetSymbolsFromPyObject(PyObject pyObject)
        {
            using (Py.GIL())
            {
                if (PyString.IsStringType(pyObject))
                {
                    Security security;
                    if (Securities.TryGetValue(pyObject.ToString(), out security))
                    {
                        return new List<Symbol> { security.Symbol };
                    }
                    return null;
                }

                var symbols = new List<Symbol>();
                foreach (var item in pyObject)
                {
                    Security security;
                    if (Securities.TryGetValue(item.ToString(), out security))
                    {
                        symbols.Add(security.Symbol);
                    }
                }
                return symbols.Count == 0 ? null : symbols;
            }
        }

        /// <summary>
        /// Creates a type with a given name
        /// </summary>
        /// <param name="typeName">Name of the new type</param>
        /// <returns>Type object</returns>
        private Type CreateType(string typeName)
        {
            var an = new AssemblyName(typeName);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            return moduleBuilder.DefineType(typeName,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    typeof(PythonData))
                .CreateType();
        }

        /// <summary>
        /// Encapsulates a python method with a <see cref="System.Func{T, TResult}"/>
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="pyObject">The python method</param>
        /// <returns>A <see cref="System.Func{T, TResult}"/> that encapsulates the python method</returns>
        private Func<IEnumerable<T>, IEnumerable<Symbol>> ToFunc<T>(PyObject pyObject)
        {
            var testMod =
               "from clr import AddReference\n" +
               "AddReference(\"System\")\n" +
               "AddReference(\"System.Collections\")\n" +
               "AddReference(\"QuantConnect.Common\")\n" +
               "from System import Func\n" +
               "from System.Collections.Generic import IEnumerable\n" +
               "from QuantConnect import Symbol\n" +
               "from QuantConnect.Data.Fundamental import FineFundamental\n" +
               "from QuantConnect.Data.UniverseSelection import CoarseFundamental\n" +
               "def to_func(pyobject, type):\n" +
               "    return Func[IEnumerable[type], IEnumerable[Symbol]](pyobject)";

            using (Py.GIL())
            {
                dynamic toFunc = PythonEngine.ModuleFromString("x", testMod).GetAttr("to_func");
                return toFunc(pyObject, typeof(T))
                    .AsManagedObject(typeof(Func<IEnumerable<T>, IEnumerable<Symbol>>));
            }
        }
    }
}