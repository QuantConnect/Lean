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

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time)
        /// </summary>
        /// <param name="typeName">Data source type</param>
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
        /// <param name="typeName">Data source type</param>
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
            if (_locked) return;

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
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, params IndicatorBase<IndicatorDataPoint>[] indicators)
        {
            Plot<IndicatorDataPoint>(chart, indicators);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, params IndicatorBase<IBaseDataBar>[] indicators)
        {
            Plot<IBaseDataBar>(chart, indicators);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, params IndicatorBase<TradeBar>[] indicators)
        {
            Plot<TradeBar>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, params IndicatorBase<IndicatorDataPoint>[] indicators)
        {
            PlotIndicator<IndicatorDataPoint>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, params IndicatorBase<IBaseDataBar>[] indicators)
        {
            PlotIndicator<IBaseDataBar>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, params IndicatorBase<TradeBar>[] indicators)
        {
            PlotIndicator<TradeBar>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase<IndicatorDataPoint>[] indicators)
        {
            PlotIndicator<IndicatorDataPoint>(chart, waitForReady, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase<IBaseDataBar>[] indicators)
        {
            PlotIndicator<IBaseDataBar>(chart, waitForReady, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase<TradeBar>[] indicators)
        {
            PlotIndicator<TradeBar>(chart, waitForReady, indicators);
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