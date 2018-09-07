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
using QuantConnect.Util;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private readonly Dictionary<IntPtr, PythonActivator> _pythonActivators = new Dictionary<IntPtr, PythonActivator>();

        public PandasConverter PandasConverter { get; private set; }

        /// <summary>
        /// Sets pandas converter
        /// </summary>
        public void SetPandasConverter()
        {
            PandasConverter = new PandasConverter();
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        public Security AddData(PyObject type, string symbol, Resolution resolution = Resolution.Minute)
        {
            return AddData(type, symbol, resolution, TimeZones.NewYork, false, 1m);
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
        /// <returns>The new <see cref="Security"/></returns>
        public Security AddData(PyObject type, string symbol, Resolution resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
        {
            return AddData(CreateType(type), symbol, resolution, timeZone, fillDataForward, leverage);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// </summary>
        /// <param name="dataType">Data source type</param>
        /// <param name="symbol">Key/Symbol for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillDataForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        public Security AddData(Type dataType, string symbol, Resolution resolution, DateTimeZone timeZone, bool fillDataForward = false, decimal leverage = 1.0m)
        {
            var marketHoursDbEntry = MarketHoursDatabase.SetEntryAlwaysOpen(Market.USA, symbol, SecurityType.Base, timeZone);

            //Add this to the data-feed subscriptions
            var symbolObject = new Symbol(SecurityIdentifier.GenerateBase(symbol, Market.USA), symbol);
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(Market.USA, symbol, SecurityType.Base, CashBook.AccountCurrency);

            //Add this new generic data as a tradeable security:
            var security = SecurityManager.CreateSecurity(dataType, Portfolio, SubscriptionManager, marketHoursDbEntry.ExchangeHours, marketHoursDbEntry.DataTimeZone,
                symbolProperties, SecurityInitializer, symbolObject, resolution, fillDataForward, leverage, true, false, true, LiveMode);

            AddToUserDefinedUniverse(security);
            return security;
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="pyObject">Defines an initial coarse selection</param>
        public void AddUniverse(PyObject pyObject)
        {
            Func<IEnumerable<CoarseFundamental>, object[]> coarse;
            Universe universe;

            if (pyObject.TryConvert(out universe))
            {
                AddUniverse(universe);
            }
            else if (pyObject.TryConvertToDelegate(out coarse))
            {
                AddUniverse(c => coarse(c.ToList()).Select(x => (Symbol)x));
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"QCAlgorithm.AddUniverse: {pyObject.Repr()} is not a valid argument.");
                }
            }
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse and fine fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="pycoarse">Defines an initial coarse selection</param>
        /// <param name="pyfine">Defines a more detailed selection with access to more data</param>
        public void AddUniverse(PyObject pycoarse, PyObject pyfine)
        {
            Func<IEnumerable<CoarseFundamental>, object[]> coarse;
            Func<IEnumerable<FineFundamental>, object[]> fine;

            if (pycoarse.TryConvertToDelegate(out coarse) && pyfine.TryConvertToDelegate(out fine))
            {
                AddUniverse(c => coarse(c.ToList()).Select(x => (Symbol)x), f => fine(f.ToList()).Select(x => (Symbol)x));
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"QCAlgorithm.AddUniverse: {pycoarse.Repr()} or {pyfine.Repr()} is not a valid argument.");
                }
            }
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The resolution this universe should be triggered on</param>
        /// <param name="pySelector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public void AddUniverse(string name, Resolution resolution, PyObject pySelector)
        {
            var selector = pySelector.ConvertToDelegate<Func<DateTime, object[]>>();
            AddUniverse(name, resolution, d => selector(d).Select(x => (string)x));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="pySelector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public void AddUniverse(string name, PyObject pySelector)
        {
            var selector = pySelector.ConvertToDelegate<Func<DateTime, object[]>>();
            AddUniverse(name, d => selector(d).Select(x => (string)x));
        }

        /// <summary>
        /// Creates a new user defined universe that will fire on the requested resolution during market hours.
        /// </summary>
        /// <param name="securityType">The security type of the universe</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The resolution this universe should be triggered on</param>
        /// <param name="market">The market of the universe</param>
        /// <param name="universeSettings">The subscription settings used for securities added from this universe</param>
        /// <param name="pySelector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public void AddUniverse(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, PyObject pySelector)
        {
            var selector = pySelector.ConvertToDelegate<Func<DateTime, object[]>>();
            AddUniverse(securityType, name, resolution, market, universeSettings, d => selector(d).Select(x => (string)x));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(PyObject T, string name, PyObject selector)
        {
            AddUniverse(CreateType(T), SecurityType.Equity, name, Resolution.Daily, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Market.USA and UniverseSettings
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(PyObject T, string name, Resolution resolution, PyObject selector)
        {
            AddUniverse(CreateType(T), SecurityType.Equity, name, resolution, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, and Market.USA
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(PyObject T, string name, Resolution resolution, UniverseSettings universeSettings, PyObject selector)
        {
            AddUniverse(CreateType(T), SecurityType.Equity, name, resolution, Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, and Market.USA
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(PyObject T, string name, UniverseSettings universeSettings, PyObject selector)
        {
            AddUniverse(CreateType(T), SecurityType.Equity, name, Resolution.Daily, Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(PyObject T, SecurityType securityType, string name, Resolution resolution, string market, PyObject selector)
        {
            AddUniverse(CreateType(T), securityType, name, resolution, market, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(PyObject T, SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, PyObject selector)
        {
            AddUniverse(CreateType(T), securityType, name, resolution, market, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <param name="dataType">The data type</param>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="pySelector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse(Type dataType, SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, PyObject pySelector)
        {
            var marketHoursDbEntry = MarketHoursDatabase.GetEntry(market, name, securityType);
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            var symbol = QuantConnect.Symbol.Create(name, securityType, market);
            var config = new SubscriptionDataConfig(dataType, symbol, resolution, dataTimeZone, exchangeTimeZone, false, false, true, true, isFilteredSubscription: false);

            var selector = pySelector.ConvertToDelegate<Func<IEnumerable<IBaseData>, object[]>>();

            AddUniverse(new FuncUniverse(config, universeSettings, SecurityInitializer, d => selector(d)
                .Select(x => x is Symbol ? (Symbol)x : QuantConnect.Symbol.Create((string)x, securityType, market))));
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, PyObject indicator, Resolution? resolution = null, PyObject selector = null)
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution), selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, PyObject indicator, TimeSpan? resolution = null, PyObject selector = null)
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution), selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, PyObject indicator, IDataConsolidator consolidator, PyObject selector = null)
        {
            IndicatorBase<IndicatorDataPoint> indicatorDataPoint;
            IndicatorBase<IBaseDataBar> indicatorDataBar;
            IndicatorBase<TradeBar> indicatorTradeBar;

            if (indicator.TryConvert(out indicatorDataPoint))
            {
                Func<IBaseData, decimal> func = null;
                selector?.TryConvert(out func);
                RegisterIndicator(symbol, indicatorDataPoint, consolidator, func);
                return;
            }
            else if (indicator.TryConvert(out indicatorDataBar))
            {
                Func<IBaseData, IBaseDataBar> func = null;
                selector?.TryConvert(out func);
                RegisterIndicator(symbol, indicatorDataBar, consolidator, func);
                return;
            }
            else if (indicator.TryConvert(out indicatorTradeBar))
            {
                Func<IBaseData, TradeBar> func = null;
                selector?.TryConvert(out func);
                RegisterIndicator(symbol, indicatorTradeBar, consolidator, func);
                return;
            }

            using (Py.GIL())
            {
                if (!indicator.HasAttr("Update"))
                {
                    throw new ArgumentException($"QCAlgorithm.RegisterIndicator(): Update method must be defined. Please checkout {indicator}");
                }
            }

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator);

            // attach to the DataConsolidated event so it updates our indicator
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                using (Py.GIL())
                {
                    indicator.InvokeMethod("Update", new[] { consolidated.ToPython() });
                }
            };
        }

        /// <summary>
        /// Plot a chart using string series name, with value.
        /// </summary>
        /// <param name="series">Name of the plot series</param>
        /// <param name="pyObject">PyObject with the value to plot</param>
        /// <seealso cref="Plot(string,decimal)"/>
        public void Plot(string series, PyObject pyObject)
        {
            IIndicator<IndicatorDataPoint> indicator;

            using (Py.GIL())
            {
                var pythonType = pyObject.GetPythonType();

                try
                {
                    var type = pythonType.As<Type>();
                    indicator = pyObject.AsManagedObject(type) as IIndicator<IndicatorDataPoint>;

                    if (indicator == null)
                    {
                        throw new ArgumentException();
                    }
                }
                catch
                {
                    throw new ArgumentException($"QCAlgorithm.Plot(): The last argument should be a QuantConnect Indicator object, {pythonType.Repr()} was provided.");
                }
            }

            Plot(series, indicator.Current.Value);
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
        public void PlotIndicator(string chart, PyObject first, PyObject second = null, PyObject third = null, PyObject fourth = null)
        {
            var array = GetIndicatorArray(first, second, third, fourth);
            PlotIndicator(chart, array[0], array[1], array[2], array[3]);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, PyObject first, PyObject second = null, PyObject third = null, PyObject fourth = null)
        {
            var array = GetIndicatorArray(first, second, third, fourth);
            PlotIndicator(chart, waitForReady, array[0], array[1], array[2], array[3]);
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
        public FilteredIdentity FilteredIdentity(Symbol symbol, PyObject selector = null, PyObject filter = null, string fieldName = null)
        {
            var resolution = GetSubscription(symbol).Resolution;
            return FilteredIdentity(symbol, resolution, selector, filter, fieldName);
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
        public FilteredIdentity FilteredIdentity(Symbol symbol, Resolution resolution, PyObject selector = null, PyObject filter = null, string fieldName = null)
        {
            var name = CreateIndicatorName(symbol, fieldName ?? "close", resolution);
            var pyselector = PythonUtil.ToFunc<IBaseData, IBaseDataBar>(selector);
            var pyfilter = PythonUtil.ToFunc<IBaseData, bool>(filter);
            var filteredIdentity = new FilteredIdentity(name, pyfilter);
            RegisterIndicator(symbol, filteredIdentity, resolution, pyselector);
            return filteredIdentity;
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
        public FilteredIdentity FilteredIdentity(Symbol symbol, TimeSpan resolution, PyObject selector = null, PyObject filter = null, string fieldName = null)
        {
            var name = string.Format("{0}({1}_{2})", symbol, fieldName ?? "close", resolution);
            var pyselector = PythonUtil.ToFunc<IBaseData, IBaseDataBar>(selector);
            var pyfilter = PythonUtil.ToFunc<IBaseData, bool>(filter);
            var filteredIdentity = new FilteredIdentity(name, pyfilter);
            RegisterIndicator(symbol, filteredIdentity, ResolveConsolidator(symbol, resolution), pyselector);
            return filteredIdentity;
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
            return PandasConverter.GetDataFrame(History(symbols, periods, resolution));
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
            return PandasConverter.GetDataFrame(History(symbols, span, resolution));
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
            return PandasConverter.GetDataFrame(History(symbols, start, end, resolution));
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject type, PyObject tickers, DateTime start, DateTime end, Resolution? resolution = null)
        {
            var symbols = GetSymbolsFromPyObject(tickers);

            var requests = symbols.Select(x =>
            {
                var security = Securities[x];
                var config = security.Subscriptions.OrderByDescending(s => s.Resolution)
                        .FirstOrDefault(s => s.Type.BaseType == CreateType(type).BaseType);
                if (config == null) return null;

                return CreateHistoryRequest(config, start, end, resolution);
            });

            return PandasConverter.GetDataFrame(History(requests.Where(x => x != null)).Memoize());
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject type, PyObject tickers, int periods, Resolution? resolution = null)
        {
            var symbols = GetSymbolsFromPyObject(tickers);

            var requests = symbols.Select(x =>
            {
                var security = Securities[x];
                var config = security.Subscriptions.OrderByDescending(s => s.Resolution)
                        .FirstOrDefault(s => s.Type.BaseType == CreateType(type).BaseType);
                if (config == null) return null;

                Resolution? res = resolution ?? security.Resolution;
                var start = GetStartTimeAlgoTz(x, periods, resolution).ConvertToUtc(TimeZone);
                return CreateHistoryRequest(config, start, UtcTime.RoundDown(res.Value.ToTimeSpan()), resolution);
            });

            return PandasConverter.GetDataFrame(History(requests.Where(x => x != null)).Memoize());
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject type, PyObject tickers, TimeSpan span, Resolution? resolution = null)
        {
            return History(type, tickers, Time - span, Time, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject type, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null)
        {
            var security = Securities[symbol];
            // verify the types match
            var requestedType = CreateType(type);
            var config = security.Subscriptions.OrderByDescending(s => s.Resolution)
                .FirstOrDefault(s => s.Type.BaseType == requestedType.BaseType);
            if (config == null)
            {
                var actualType = security.Subscriptions.Select(x => x.Type.Name).DefaultIfEmpty("[None]").FirstOrDefault();
                throw new ArgumentException("The specified security is not of the requested type. Symbol: " + symbol.ToString() + " Requested Type: " + requestedType.Name + " Actual Type: " + actualType);
            }

            var request = CreateHistoryRequest(config, start, end, resolution);
            return PandasConverter.GetDataFrame(History(request).Memoize());
        }

        /// <summary>
        /// Gets the historical data for the specified symbols. The exact number of bars will be returned for
        /// each symbol. This may result in some data start earlier/later than others due to when various
        /// exchanges are open. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="periods">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject type, Symbol symbol, int periods, Resolution? resolution = null)
        {
            if (resolution == Resolution.Tick) throw new ArgumentException("History functions that accept a 'periods' parameter can not be used with Resolution.Tick");

            var start = GetStartTimeAlgoTz(symbol, periods, resolution);
            var end = Time.RoundDown((resolution ?? Securities[symbol].Resolution).ToTimeSpan());
            return History(type, symbol, start, end, resolution);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        public PyObject History(PyObject type, Symbol symbol, TimeSpan span, Resolution? resolution = null)
        {
            return History(type, symbol, Time - span, Time, resolution);
        }

        /// <summary>
        /// Create a pandas dataframe from history request with information from a PyObject
        /// </summary>
        /// <param name="pyObject">PyObject containing elements to create a history request</param>
        public PyObject PandasDataFrameHistory(PyObject pyObject)
        {
            var requests = CreateHistoryRequests(pyObject);
            var history = HistoryProvider.GetHistory(requests, TimeZone).Memoize();
            return PandasConverter.GetDataFrame(history);
        }

        /// <summary>
        /// Get history requests with information to create pandas dataframe from local files
        /// </summary>
        /// <param name="pyObject">PyObject containing elements to create a history request</param>
        public IEnumerable<PandasHistoryRequest> GetPandasHistoryRequests(PyObject pyObject)
        {
            return CreateHistoryRequests(pyObject)
                .SelectMany(request =>
                {
                    var config = GetSubscription(request.Symbol, request.TickType);
                    var hours = request.ExchangeHours;
                    var resolution = request.Resolution;

                    var start = request.StartTimeUtc.ConvertFromUtc(config.DataTimeZone);
                    var end = request.EndTimeUtc.ConvertFromUtc(config.DataTimeZone);

                    var tradeableDays = QuantConnect.Time.EachTradeableDay(hours, start, end);
                    if (resolution == Resolution.Daily || resolution == Resolution.Hour)
                    {
                        tradeableDays = new[] { tradeableDays.Last() };
                    }

                    return tradeableDays
                        .Select(date => new PandasHistoryRequest(config, hours, request.StartTimeUtc, request.EndTimeUtc, resolution, date));
                });
        }

        /// <summary>
        /// Create history requests from a PyObject
        /// </summary>
        /// <param name="pyObject">PyObject containing elements to create a history request</param>
        private IEnumerable<HistoryRequest> CreateHistoryRequests(PyObject pyObject)
        {
            var dict = new PyDict(pyObject);
            var requests = new List<HistoryRequest>();

            using (Py.GIL())
            {
                var symbols = Securities.Keys;
                if (dict.HasKey("symbols"))
                {
                    symbols = GetSymbolsFromPyObject(dict["symbols"]).ToList();
                }

                Resolution? resolution = null;
                if (dict.HasKey("resolution"))
                {
                    resolution = (Resolution)dict["resolution"].AsManagedObject(typeof(int));
                }

                foreach (var symbol in symbols)
                {
                    var config = GetSubscription(symbol);

                    // Check whether the symbol has the requested data type
                    if (dict.HasKey("type"))
                    {
                        var requestedType = CreateType(dict["type"]);
                        config = Securities[symbol].Subscriptions
                            .OrderByDescending(s => s.Resolution)
                            .FirstOrDefault(s => s.Type.BaseType == requestedType.BaseType);

                        if (config == null)
                        {
                            continue;
                        }
                    }

                    resolution = GetResolution(symbol, resolution);

                    DateTime start;
                    var end = Time.RoundDown(resolution.Value.ToTimeSpan());

                    if (dict.HasKey("periods"))
                    {
                        var periods = (int)dict["periods"].AsManagedObject(typeof(int));
                        start = GetStartTimeAlgoTz(symbol, periods, resolution);
                    }
                    else if (dict.HasKey("span"))
                    {
                        var span = (TimeSpan)dict["span"].AsManagedObject(typeof(TimeSpan));
                        start = end - span;
                    }
                    else
                    {
                        start = (DateTime)dict["start"].AsManagedObject(typeof(DateTime));
                        end = (DateTime)dict["end"].AsManagedObject(typeof(DateTime));
                    }

                    requests.Add(CreateHistoryRequest(config, start, end, resolution));
                }
            }
            return requests;
        }

        /// <summary>
        /// Sets the specified function as the benchmark, this function provides the value of
        /// the benchmark at each date/time requested
        /// </summary>
        /// <param name="benchmark">The benchmark producing function</param>
        public void SetBenchmark(PyObject benchmark)
        {
            using (Py.GIL())
            {
                var pyBenchmark = PythonUtil.ToFunc<DateTime, decimal>(benchmark);
                if (pyBenchmark != null)
                {
                    SetBenchmark(pyBenchmark);
                    return;
                }
                SetBenchmark((Symbol)benchmark.AsManagedObject(typeof(Symbol)));
            }
        }

        /// <summary>
        /// Sets the brokerage to emulate in backtesting or paper trading.
        /// This can be used to set a custom brokerage model.
        /// </summary>
        /// <param name="model">The brokerage model to use</param>
        public void SetBrokerageModel(PyObject model)
        {
            SetBrokerageModel(new BrokerageModelPythonWrapper(model));
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation
        /// </summary>
        /// <param name="securityInitializer">The security initializer function or class</param>
        public void SetSecurityInitializer(PyObject securityInitializer)
        {
            var securityInitializer1 = PythonUtil.ToAction<Security>(securityInitializer);
            if (securityInitializer1 != null)
            {
                SetSecurityInitializer(securityInitializer1);
                return;
            }

            SetSecurityInitializer(new SecurityInitializerPythonWrapper(securityInitializer));
        }

        /// <summary>
        /// Downloads the requested resource as a <see cref="string"/>.
        /// The resource to download is specified as a <see cref="string"/> containing the URI.
        /// </summary>
        /// <param name="address">A string containing the URI to download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <returns>The requested resource as a <see cref="string"/></returns>
        public string Download(string address, PyObject headers) => Download(address, headers, null, null);

        /// <summary>
        /// Downloads the requested resource as a <see cref="string"/>.
        /// The resource to download is specified as a <see cref="string"/> containing the URI.
        /// </summary>
        /// <param name="address">A string containing the URI to download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <param name="userName">The user name associated with the credentials</param>
        /// <param name="password">The password for the user name associated with the credentials</param>
        /// <returns>The requested resource as a <see cref="string"/></returns>
        public string Download(string address, PyObject headers, string userName, string password)
        {
            var dict = new Dictionary<string, string>();

            if (headers != null)
            {
                using (Py.GIL())
                {
                    // In python algorithms, headers must be a python dictionary
                    // In order to convert it into a C# Dictionary
                    if (PyDict.IsDictType(headers))
                    {
                        foreach (PyObject pyKey in headers)
                        {
                            var key = (string)pyKey.AsManagedObject(typeof(string));
                            var value = (string)headers.GetItem(pyKey).AsManagedObject(typeof(string));
                            dict.Add(key, value);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"QCAlgorithm.Fetch(): Invalid argument. {headers.Repr()} is not a dict");
                    }
                }
            }
            return Download(address, dict, userName, password);
        }

        /// <summary>
        /// Gets Enumerable of <see cref="Symbol"/> from a PyObject
        /// </summary>
        /// <param name="pyObject">PyObject containing Symbol or Array of Symbol</param>
        /// <returns>Enumerable of Symbol</returns>
        private IEnumerable<Symbol> GetSymbolsFromPyObject(PyObject pyObject)
        {
            Symbol symbol;
            Symbol[] symbols;

            if (PyString.IsStringType(pyObject))
            {
                yield return pyObject.As<string>();
            }
            else if (pyObject.TryConvert(out symbol))
            {
                if (symbol == null) throw new ArgumentException(_symbolEmptyErrorMessage);
                yield return symbol;
            }
            else if (pyObject.TryConvert(out symbols))
            {
                foreach (var s in symbols)
                {
                    if (s == null) throw new ArgumentException(_symbolEmptyErrorMessage);
                    yield return s;
                }
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"Argument type should be Symbol or a list of Symbol. Object: {pyObject}.");
                }
            }
        }

        /// <summary>
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(PyObject)"/>
        /// <seealso cref="Error(PyObject)"/>
        public void Debug(PyObject message)
        {
            Debug(message.ToSafeString());
        }

        /// <summary>
        /// Send a string error message to the Console.
        /// </summary>
        /// <param name="message">Message to display in errors grid</param>
        /// <seealso cref="Debug(PyObject)"/>
        /// <seealso cref="Log(PyObject)"/>
        public void Error(PyObject message)
        {
            Error(message.ToSafeString());
        }

        /// <summary>
        /// Added another method for logging if user guessed.
        /// </summary>
        /// <param name="message">String message to log.</param>
        /// <seealso cref="Debug(PyObject)"/>
        /// <seealso cref="Error(PyObject)"/>
        public void Log(PyObject message)
        {
            Log(message.ToSafeString());
        }

        /// <summary>
        /// Terminate the algorithm after processing the current event handler.
        /// </summary>
        /// <param name="message">Exit message to display on quitting</param>
        public void Quit(PyObject message)
        {
            Quit(message.ToSafeString());
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        public IDataConsolidator Consolidate(Symbol symbol, Resolution period, PyObject handler)
        {
            return Consolidate(symbol, period.ToTimeSpan(), null, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="tickType">The tick type of subscription used as data source for consolidator. Specify null to use first subscription found.</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        public IDataConsolidator Consolidate(Symbol symbol, Resolution period, TickType? tickType, PyObject handler)
        {
            return Consolidate(symbol, period.ToTimeSpan(), tickType, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        public IDataConsolidator Consolidate(Symbol symbol, TimeSpan period, PyObject handler)
        {
            return Consolidate(symbol, period, null, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="tickType">The tick type of subscription used as data source for consolidator. Specify null to use first subscription found.</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        public IDataConsolidator Consolidate(Symbol symbol, TimeSpan period, TickType? tickType, PyObject handler)
        {
            // resolve consolidator input subscription
            var type = GetSubscription(symbol, tickType).Type;

            if (type == typeof(TradeBar))
            {
                return Consolidate(symbol, period, tickType, handler.ConvertToDelegate<Action<TradeBar>>());
            }

            if (type == typeof(QuoteBar))
            {
                return Consolidate(symbol, period, tickType, handler.ConvertToDelegate<Action<QuoteBar>>());
            }

            return Consolidate(symbol, period, null, handler.ConvertToDelegate<Action<BaseData>>());
        }

        /// <summary>
        /// Gets indicator base type
        /// </summary>
        /// <param name="type">Indicator type</param>
        /// <returns>Indicator base type</returns>
        private Type GetIndicatorBaseType(Type type)
        {
            if (type.BaseType == typeof(object))
            {
                return type;
            }
            return GetIndicatorBaseType(type.BaseType);
        }

        /// <summary>
        /// Converts the sequence of PyObject objects into an array of dynamic objects that represent indicators of the same type
        /// </summary>
        /// <returns>Array of dynamic objects with indicator</returns>
        private dynamic[] GetIndicatorArray(PyObject first, PyObject second = null, PyObject third = null, PyObject fourth = null)
        {
            using (Py.GIL())
            {
                var array = new[] { first, second, third, fourth }
                    .Select(x =>
                    {
                        if (x == null) return null;
                        var type = (Type)x.GetPythonType().AsManagedObject(typeof(Type));
                        return (dynamic)x.AsManagedObject(type);

                    }).ToArray();

                var types = array.Where(x => x != null).Select(x => GetIndicatorBaseType(x.GetType())).Distinct();

                if (types.Count() > 1)
                {
                    throw new Exception("QCAlgorithm.GetIndicatorArray(). All indicators must be of the same type: data point, bar or tradebar.");
                }

                return array;
            }
        }

        /// <summary>
        /// Creates a type with a given name, if PyObject is not a CLR type. Otherwise, convert it.
        /// </summary>
        /// <param name="pyObject">Python object representing a type.</param>
        /// <returns>Type object</returns>
        private Type CreateType(PyObject pyObject)
        {
            Type type;
            if (pyObject.TryConvert(out type) &&
                type != typeof(PythonQuandl) &&
                type != typeof(PythonData))
            {
                return type;
            }

            PythonActivator pythonType;
            if (!_pythonActivators.TryGetValue(pyObject.Handle, out pythonType))
            {
                AssemblyName an;
                using (Py.GIL())
                {
                    an = new AssemblyName(pyObject.Repr().Split('\'')[1]);
                }
                var typeBuilder = AppDomain.CurrentDomain
                    .DefineDynamicAssembly(an, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("MainModule")
                    .DefineType(an.Name, TypeAttributes.Class, typeof(DynamicData));

                pythonType = new PythonActivator(typeBuilder.CreateType(), pyObject);

                ObjectActivator.AddActivator(pythonType.Type, pythonType.Factory);

                // Save to prevent future additions
                _pythonActivators.Add(pyObject.Handle, pythonType);
            }
            return pythonType.Type;
        }
    }
}