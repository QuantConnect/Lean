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
using QuantConnect.Python;
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Fundamental;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Brokerages;
using QuantConnect.Scheduling;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Commands;
using QuantConnect.Api;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        private readonly Dictionary<IntPtr, PythonIndicator> _pythonIndicators = new Dictionary<IntPtr, PythonIndicator>();

        /// <summary>
        /// PandasConverter for this Algorithm
        /// </summary>
        public virtual PandasConverter PandasConverter { get; private set; }

        /// <summary>
        /// Sets pandas converter
        /// </summary>
        public void SetPandasConverter()
        {
            PandasConverter = new PandasConverter();
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time).
        /// This method is meant for custom data types that require a ticker, but have no underlying Symbol.
        /// Examples of data sources that meet this criteria are U.S. Treasury Yield Curve Rates and Trading Economics data
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        [DocumentationAttribute(AddingData)]
        public Security AddData(PyObject type, string ticker, Resolution? resolution = null)
        {
            return AddData(type, ticker, resolution, null, false, 1m);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// The data is added with a default time zone of NewYork (Eastern Daylight Savings Time).
        /// This adds a Symbol to the `Underlying` property in the custom data Symbol object.
        /// Use this method when adding custom data with a ticker from the past, such as "AOL"
        /// before it became "TWX", or if you need to filter using custom data and place trades on the
        /// Symbol associated with the custom data.
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the data</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>
        /// We include three optional unused object parameters so that pythonnet chooses the intended method
        /// correctly. Previously, calling the overloaded method that accepts a string would instead call this method.
        /// Adding the three unused parameters makes it choose the correct method when using a string or Symbol. This is
        /// due to pythonnet's method precedence, as viewable here: https://github.com/QuantConnect/pythonnet/blob/9e29755c54e6008cb016e3dd9d75fbd8cd19fcf7/src/runtime/methodbinder.cs#L215
        /// </remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData(PyObject type, Symbol underlying, Resolution? resolution = null)
        {
            return AddData(type, underlying, resolution, null, false, 1m);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// This method is meant for custom data types that require a ticker, but have no underlying Symbol.
        /// Examples of data sources that meet this criteria are U.S. Treasury Yield Curve Rates and Trading Economics data
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        [DocumentationAttribute(AddingData)]
        public Security AddData(PyObject type, string ticker, Resolution? resolution, DateTimeZone timeZone, bool fillForward = false, decimal leverage = 1.0m)
        {
            return AddData(type.CreateType(), ticker, resolution, timeZone, fillForward, leverage);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// This adds a Symbol to the `Underlying` property in the custom data Symbol object.
        /// Use this method when adding custom data with a ticker from the past, such as "AOL"
        /// before it became "TWX", or if you need to filter using custom data and place trades on the
        /// Symbol associated with the custom data.
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="underlying">The underlying symbol for the custom data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>
        /// We include three optional unused object parameters so that pythonnet chooses the intended method
        /// correctly. Previously, calling the overloaded method that accepts a string would instead call this method.
        /// Adding the three unused parameters makes it choose the correct method when using a string or Symbol. This is
        /// due to pythonnet's method precedence, as viewable here: https://github.com/QuantConnect/pythonnet/blob/9e29755c54e6008cb016e3dd9d75fbd8cd19fcf7/src/runtime/methodbinder.cs#L215
        /// </remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData(PyObject type, Symbol underlying, Resolution? resolution, DateTimeZone timeZone, bool fillForward = false, decimal leverage = 1.0m)
        {
            return AddData(type.CreateType(), underlying, resolution, timeZone, fillForward, leverage);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// This method is meant for custom data types that require a ticker, but have no underlying Symbol.
        /// Examples of data sources that meet this criteria are U.S. Treasury Yield Curve Rates and Trading Economics data
        /// </summary>
        /// <param name="dataType">Data source type</param>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        [DocumentationAttribute(AddingData)]
        public Security AddData(Type dataType, string ticker, Resolution? resolution, DateTimeZone timeZone, bool fillForward = false, decimal leverage = 1.0m)
        {
            // NOTE: Invoking methods on BaseData w/out setting the symbol may provide unexpected behavior
            var baseInstance = dataType.GetBaseDataInstance();
            if (!baseInstance.RequiresMapping())
            {
                var symbol = new Symbol(
                    SecurityIdentifier.GenerateBase(dataType, ticker, Market.USA, baseInstance.RequiresMapping()),
                    ticker);
                return AddDataImpl(dataType, symbol, resolution, timeZone, fillForward, leverage);
            }
            // If we need a mappable ticker and we can't find one in the SymbolCache, throw
            Symbol underlying;
            if (!SymbolCache.TryGetSymbol(ticker, out underlying))
            {
                throw new InvalidOperationException($"The custom data type {dataType.Name} requires mapping, but the provided ticker is not in the cache. " +
                                                    $"Please add this custom data type using a Symbol or perform this call after " +
                                                    $"a Security has been added using AddEquity, AddForex, AddCfd, AddCrypto, AddFuture, AddOption or AddSecurity. " +
                                                    $"An example use case can be found in CustomDataAddDataRegressionAlgorithm");
            }

            return AddData(dataType, underlying, resolution, timeZone, fillForward, leverage);
        }

        /// <summary>
        /// AddData a new user defined data source, requiring only the minimum config options.
        /// This adds a Symbol to the `Underlying` property in the custom data Symbol object.
        /// Use this method when adding custom data with a ticker from the past, such as "AOL"
        /// before it became "TWX", or if you need to filter using custom data and place trades on the
        /// Symbol associated with the custom data.
        /// </summary>
        /// <param name="dataType">Data source type</param>
        /// <param name="underlying"></param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        /// <remarks>
        /// We include three optional unused object parameters so that pythonnet chooses the intended method
        /// correctly. Previously, calling the overloaded method that accepts a string would instead call this method.
        /// Adding the three unused parameters makes it choose the correct method when using a string or Symbol. This is
        /// due to pythonnet's method precedence, as viewable here: https://github.com/QuantConnect/pythonnet/blob/9e29755c54e6008cb016e3dd9d75fbd8cd19fcf7/src/runtime/methodbinder.cs#L215
        /// </remarks>
        [DocumentationAttribute(AddingData)]
        public Security AddData(Type dataType, Symbol underlying, Resolution? resolution = null, DateTimeZone timeZone = null, bool fillForward = false, decimal leverage = 1.0m)
        {
            var symbol = QuantConnect.Symbol.CreateBase(dataType, underlying, underlying.ID.Market);
            return AddDataImpl(dataType, symbol, resolution, timeZone, fillForward, leverage);
        }

        /// <summary>
        /// AddData a new user defined data source including symbol properties and exchange hours,
        /// all other vars are not required and will use defaults.
        /// This overload reflects the C# equivalent for custom properties and market hours
        /// </summary>
        /// <param name="type">Data source type</param>
        /// <param name="ticker">Key/Ticker for data</param>
        /// <param name="properties">The properties of this new custom data</param>
        /// <param name="exchangeHours">The Exchange hours of this symbol</param>
        /// <param name="resolution">Resolution of the Data Required</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        [DocumentationAttribute(AddingData)]
        public Security AddData(PyObject type, string ticker, SymbolProperties properties, SecurityExchangeHours exchangeHours, Resolution? resolution = null, bool fillForward = false, decimal leverage = 1.0m)
        {
            // Get the right key for storage of base type symbols
            var dataType = type.CreateType();
            var key = SecurityIdentifier.GenerateBaseSymbol(dataType, ticker);

            // Add entries to our Symbol Properties DB and MarketHours DB
            SetDatabaseEntries(key, properties, exchangeHours);

            // Then add the data
            return AddData(dataType, ticker, resolution, null, fillForward, leverage);
        }

        /// <summary>
        /// Creates and adds a new Future Option contract to the algorithm.
        /// </summary>
        /// <param name="futureSymbol">The Future canonical symbol (i.e. Symbol returned from <see cref="AddFuture"/>)</param>
        /// <param name="optionFilter">Filter to apply to option contracts loaded as part of the universe</param>
        /// <returns>The new Option security, containing a Future as its underlying.</returns>
        /// <exception cref="ArgumentException">The symbol provided is not canonical.</exception>
        [DocumentationAttribute(AddingData)]
        public void AddFutureOption(Symbol futureSymbol, PyObject optionFilter)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> optionFilterUniverse;
            if (!optionFilter.TryConvertToDelegate(out optionFilterUniverse))
            {
                throw new ArgumentException("Option contract universe filter provided is not a function");
            }

            AddFutureOption(futureSymbol, optionFilterUniverse);
        }

        /// <summary>
        /// Adds the provided final Symbol with/without underlying set to the algorithm.
        /// This method is meant for custom data types that require a ticker, but have no underlying Symbol.
        /// Examples of data sources that meet this criteria are U.S. Treasury Yield Curve Rates and Trading Economics data
        /// </summary>
        /// <param name="dataType">Data source type</param>
        /// <param name="symbol">Final symbol that includes underlying (if any)</param>
        /// <param name="resolution">Resolution of the Data required</param>
        /// <param name="timeZone">Specifies the time zone of the raw data</param>
        /// <param name="fillForward">When no data available on a tradebar, return the last data that was generated</param>
        /// <param name="leverage">Custom leverage per security</param>
        /// <returns>The new <see cref="Security"/></returns>
        private Security AddDataImpl(Type dataType, Symbol symbol, Resolution? resolution, DateTimeZone timeZone, bool fillForward, decimal leverage)
        {
            var alias = symbol.ID.Symbol;
            SymbolCache.Set(alias, symbol);

            if (timeZone != null)
            {
                // user set time zone
                MarketHoursDatabase.SetEntryAlwaysOpen(symbol.ID.Market, alias, SecurityType.Base, timeZone);
            }

            //Add this new generic data as a tradeable security:
            var config = SubscriptionManager.SubscriptionDataConfigService.Add(
                dataType,
                symbol,
                resolution,
                fillForward,
                isCustomData: true,
                extendedMarketHours: true);
            var security = Securities.CreateSecurity(symbol, config, leverage, addToSymbolCache: false);

            return AddToUserDefinedUniverse(security, new List<SubscriptionDataConfig> { config });
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>)
        /// </summary>
        /// <param name="pyObject">Defines an initial coarse selection</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject pyObject)
        {
            Func<IEnumerable<Fundamental>, object> fundamentalSelector;
            Universe universe;

            if (pyObject.TryCreateType(out var type))
            {
                return AddUniverse(pyObject, null, null);
            }
            // TODO: to be removed when https://github.com/QuantConnect/pythonnet/issues/62 is solved
            else if (pyObject.TryConvert(out universe))
            {
                return AddUniverse(universe);
            }
            else if (pyObject.TryConvert(out universe, allowPythonDerivative: true))
            {
                return AddUniverse(new UniversePythonWrapper(pyObject));
            }
            else if (pyObject.TryConvertToDelegate(out fundamentalSelector))
            {
                return AddUniverse(FundamentalUniverse.USA(fundamentalSelector));
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
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>)
        /// </summary>
        /// <param name="pyObject">Defines an initial coarse selection or a universe</param>
        /// <param name="pyfine">Defines a more detailed selection with access to more data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject pyObject, PyObject pyfine)
        {
            Func<IEnumerable<CoarseFundamental>, object> coarseFunc;
            Func<IEnumerable<FineFundamental>, object> fineFunc;

            try
            {
                // this is due to a pythonNet limitation even if defining 'AddUniverse(IDateRule, PyObject)'
                // it will chose this method instead
                IDateRule dateRule;
                using (Py.GIL())
                {
                    dateRule = pyObject.As<IDateRule>();
                }

                if (pyfine.TryConvertToDelegate(out coarseFunc))
                {
                    return AddUniverse(dateRule, coarseFunc.ConvertToUniverseSelectionSymbolDelegate());
                }
            }
            catch (InvalidCastException)
            {
                // pass
            }

            if (pyObject.TryCreateType(out var type))
            {
                return AddUniverse(pyObject, null, pyfine);
            }
            else if (pyObject.TryConvert(out Universe universe) && pyfine.TryConvertToDelegate(out fineFunc))
            {
                return AddUniverse(universe, fineFunc.ConvertToUniverseSelectionSymbolDelegate());
            }
            else if (pyObject.TryConvertToDelegate(out coarseFunc) && pyfine.TryConvertToDelegate(out fineFunc))
            {
                return AddUniverse(coarseFunc.ConvertToUniverseSelectionSymbolDelegate(),
                    fineFunc.ConvertToUniverseSelectionSymbolDelegate());
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"QCAlgorithm.AddUniverse: {pyObject.Repr()} or {pyfine.Repr()} is not a valid argument.");
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
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(string name, Resolution resolution, PyObject pySelector)
        {
            var selector = pySelector.ConvertToDelegate<Func<DateTime, object>>();
            return AddUniverse(name, resolution, selector.ConvertToUniverseSelectionStringDelegate());
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="pySelector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(string name, PyObject pySelector)
        {
            var selector = pySelector.ConvertToDelegate<Func<DateTime, object>>();
            return AddUniverse(name, selector.ConvertToUniverseSelectionStringDelegate());
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
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, PyObject pySelector)
        {
            var selector = pySelector.ConvertToDelegate<Func<DateTime, object>>();
            return AddUniverse(securityType, name, resolution, market, universeSettings, selector.ConvertToUniverseSelectionStringDelegate());
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject T, string name, PyObject selector)
        {
            return AddUniverse(T.CreateType(), null, name, null, null, null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Market.USA and UniverseSettings
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject T, string name, Resolution resolution, PyObject selector)
        {
            return AddUniverse(T.CreateType(), null, name, resolution, null, null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, and Market.USA
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject T, string name, Resolution resolution, UniverseSettings universeSettings, PyObject selector)
        {
            return AddUniverse(T.CreateType(), null, name, resolution, null, universeSettings, selector);
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
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject T, string name, UniverseSettings universeSettings, PyObject selector)
        {
            return AddUniverse(T.CreateType(), null, name, null, null, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject T, SecurityType securityType, string name, Resolution resolution, string market, PyObject selector)
        {
            return AddUniverse(T.CreateType(), securityType, name, resolution, market, null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <param name="T">The data type</param>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(PyObject T, SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, PyObject selector)
        {
            return AddUniverse(T.CreateType(), securityType, name, resolution, market, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <param name="dataType">The data type</param>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="pySelector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(Type dataType, SecurityType? securityType = null, string name = null, Resolution? resolution = null, string market = null, UniverseSettings universeSettings = null, PyObject pySelector = null)
        {
            if (market.IsNullOrEmpty())
            {
                market = Market.USA;
            }
            securityType ??= SecurityType.Equity;
            Func<IEnumerable<BaseData>, IEnumerable<Symbol>> wrappedSelector = null;
            if (pySelector != null)
            {
                var selector = pySelector.ConvertToDelegate<Func<IEnumerable<IBaseData>, object>>();
                wrappedSelector = baseDatas =>
                {
                    var result = selector(baseDatas);
                    if (ReferenceEquals(result, Universe.Unchanged))
                    {
                        return Universe.Unchanged;
                    }
                    return ((object[])result).Select(x => x is Symbol symbol ? symbol : QuantConnect.Symbol.Create((string)x, securityType.Value, market, baseDataType: dataType));
                };
            }
            return AddUniverseSymbolSelector(dataType, name, resolution, market, universeSettings, wrappedSelector);
        }

        /// <summary>
        /// Creates a new universe selection model and adds it to the algorithm. This universe selection model will chain to the security
        /// changes of a given <see cref="Universe"/> selection output and create a new <see cref="OptionChainUniverse"/> for each of them
        /// </summary>
        /// <param name="universe">The universe we want to chain an option universe selection model too</param>
        /// <param name="optionFilter">The option filter universe to use</param>
        [DocumentationAttribute(Universes)]
        public void AddUniverseOptions(PyObject universe, PyObject optionFilter)
        {
            Func<OptionFilterUniverse, OptionFilterUniverse> convertedOptionChain;
            Universe universeToChain;

            if (universe.TryConvert(out universeToChain) && optionFilter.TryConvertToDelegate(out convertedOptionChain))
            {
                AddUniverseOptions(universeToChain, convertedOptionChain);
            }
            else
            {
                using (Py.GIL())
                {
                    throw new ArgumentException($"QCAlgorithm.AddChainedEquityOptionUniverseSelectionModel: {universe.Repr()} or {optionFilter.Repr()} is not a valid argument.");
                }
            }
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(ConsolidatingData)]
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
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(ConsolidatingData)]
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
        /// <param name="pyObject">The python object that it is trying to register with, could be consolidator or a timespan</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(ConsolidatingData)]
        public void RegisterIndicator(Symbol symbol, PyObject indicator, PyObject pyObject, PyObject selector = null)
        {
            // First check if this is just a regular IDataConsolidator
            IDataConsolidator dataConsolidator;
            if (pyObject.TryConvert(out dataConsolidator))
            {
                RegisterIndicator(symbol, indicator, dataConsolidator, selector);
                return;
            }

            try
            {
                dataConsolidator = new DataConsolidatorPythonWrapper(pyObject);
            }
            catch
            {
                // Finally, since above didn't work, just try it as a timespan
                // Issue #4668 Fix
                using (Py.GIL())
                {
                    try
                    {
                        // tryConvert does not work for timespan
                        TimeSpan? timeSpan = pyObject.As<TimeSpan>();
                        if (timeSpan != default(TimeSpan))
                        {
                            RegisterIndicator(symbol, indicator, timeSpan, selector);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Invalid third argument, should be either a valid consolidator or timedelta object. The following exception was thrown: ", e);
                    }
                }
            }

            RegisterIndicator(symbol, indicator, dataConsolidator, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(ConsolidatingData)]
        public void RegisterIndicator(Symbol symbol, PyObject indicator, IDataConsolidator consolidator, PyObject selector = null)
        {
            // TODO: to be removed when https://github.com/QuantConnect/pythonnet/issues/62 is solved
            var convertedIndicator = ConvertPythonIndicator(indicator);
            switch (convertedIndicator)
            {
                case PythonIndicator pythonIndicator:
                    RegisterIndicator(symbol, pythonIndicator, consolidator,
                        selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());
                    break;

                case IndicatorBase<IndicatorDataPoint> dataPointIndicator:
                    RegisterIndicator(symbol, dataPointIndicator, consolidator,
                        selector?.ConvertToDelegate<Func<IBaseData, decimal>>());
                    break;

                case IndicatorBase<IBaseDataBar> baseDataBarIndicator:
                    RegisterIndicator(symbol, baseDataBarIndicator, consolidator,
                        selector?.ConvertToDelegate<Func<IBaseData, IBaseDataBar>>());
                    break;

                case IndicatorBase<TradeBar> tradeBarIndicator:
                    RegisterIndicator(symbol, tradeBarIndicator, consolidator,
                        selector?.ConvertToDelegate<Func<IBaseData, TradeBar>>());
                    break;

                case IndicatorBase<IBaseData> baseDataIndicator:
                    RegisterIndicator(symbol, baseDataIndicator, consolidator,
                        selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());
                    break;

                case IndicatorBase<BaseData> baseDataIndicator:
                    RegisterIndicator(symbol, baseDataIndicator, consolidator,
                        selector?.ConvertToDelegate<Func<IBaseData, BaseData>>());
                    break;

                default:
                    // Shouldn't happen, ConvertPythonIndicator will wrap the PyObject in a PythonIndicator instance if it can't convert it
                    throw new ArgumentException($"Indicator type {indicator.GetPythonType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(HistoricalData)]
        public void WarmUpIndicator(Symbol symbol, PyObject indicator, Resolution? resolution = null, PyObject selector = null)
        {
            // TODO: to be removed when https://github.com/QuantConnect/pythonnet/issues/62 is solved
            WarmUpIndicator([symbol], indicator, resolution, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol or symbols to retrieve historical data for</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(HistoricalData)]
        public void WarmUpIndicator(PyObject symbol, PyObject indicator, Resolution? resolution = null, PyObject selector = null)
        {
            // TODO: to be removed when https://github.com/QuantConnect/pythonnet/issues/62 is solved
            var symbols = symbol.ConvertToSymbolEnumerable();
            WarmUpIndicator(symbols, indicator, resolution, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        private void WarmUpIndicator(IEnumerable<Symbol> symbols, PyObject indicator, Resolution? resolution = null, PyObject selector = null)
        {
            // TODO: to be removed when https://github.com/QuantConnect/pythonnet/issues/62 is solved
            var convertedIndicator = ConvertPythonIndicator(indicator);
            switch (convertedIndicator)
            {
                case PythonIndicator pythonIndicator:
                    WarmUpIndicator(symbols, pythonIndicator, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());
                    break;

                case IndicatorBase<IndicatorDataPoint> dataPointIndicator:
                    WarmUpIndicator(symbols, dataPointIndicator, resolution, selector?.ConvertToDelegate<Func<IBaseData, decimal>>());
                    break;

                case IndicatorBase<IBaseDataBar> baseDataBarIndicator:
                    WarmUpIndicator(symbols, baseDataBarIndicator, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseDataBar>>());
                    break;

                case IndicatorBase<TradeBar> tradeBarIndicator:
                    WarmUpIndicator(symbols, tradeBarIndicator, resolution, selector?.ConvertToDelegate<Func<IBaseData, TradeBar>>());
                    break;

                case IndicatorBase<IBaseData> baseDataIndicator:
                    WarmUpIndicator(symbols, baseDataIndicator, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());
                    break;

                case IndicatorBase<BaseData> baseDataIndicator:
                    WarmUpIndicator(symbols, baseDataIndicator, resolution, selector?.ConvertToDelegate<Func<IBaseData, BaseData>>());
                    break;

                default:
                    // Shouldn't happen, ConvertPythonIndicator will wrap the PyObject in a PythonIndicator instance if it can't convert it
                    throw new ArgumentException($"Indicator type {indicator.GetPythonType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(HistoricalData)]
        public void WarmUpIndicator(Symbol symbol, PyObject indicator, TimeSpan period, PyObject selector = null)
        {
            WarmUpIndicator([symbol], indicator, period, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol or symbols to retrieve historical data for</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(Indicators)]
        [DocumentationAttribute(HistoricalData)]
        public void WarmUpIndicator(PyObject symbol, PyObject indicator, TimeSpan period, PyObject selector = null)
        {
            var symbols = symbol.ConvertToSymbolEnumerable();
            WarmUpIndicator(symbols, indicator, period, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbols">The symbols to retrieve historical data for</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        private void WarmUpIndicator(IEnumerable<Symbol> symbols, PyObject indicator, TimeSpan period, PyObject selector = null)
        {
            var convertedIndicator = ConvertPythonIndicator(indicator);
            switch (convertedIndicator)
            {
                case PythonIndicator pythonIndicator:
                    WarmUpIndicator(symbols, pythonIndicator, period, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());
                    break;

                case IndicatorBase<IndicatorDataPoint> dataPointIndicator:
                    WarmUpIndicator(symbols, dataPointIndicator, period, selector?.ConvertToDelegate<Func<IBaseData, decimal>>());
                    break;

                case IndicatorBase<IBaseDataBar> baseDataBarIndicator:
                    WarmUpIndicator(symbols, baseDataBarIndicator, period, selector?.ConvertToDelegate<Func<IBaseData, IBaseDataBar>>());
                    break;

                case IndicatorBase<TradeBar> tradeBarIndicator:
                    WarmUpIndicator(symbols, tradeBarIndicator, period, selector?.ConvertToDelegate<Func<IBaseData, TradeBar>>());
                    break;

                case IndicatorBase<IBaseData> baseDataIndicator:
                    WarmUpIndicator(symbols, baseDataIndicator, period, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());
                    break;

                case IndicatorBase<BaseData> baseDataIndicator:
                    WarmUpIndicator(symbols, baseDataIndicator, period, selector?.ConvertToDelegate<Func<IBaseData, BaseData>>());
                    break;

                default:
                    // Shouldn't happen, ConvertPythonIndicator will wrap the PyObject in a PythonIndicator instance if it can't convert it
                    throw new ArgumentException($"Indicator type {indicator.GetPythonType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Plot a chart using string series name, with value.
        /// </summary>
        /// <param name="series">Name of the plot series</param>
        /// <param name="pyObject">PyObject with the value to plot</param>
        /// <seealso cref="Plot(string,decimal)"/>
        [DocumentationAttribute(Charting)]
        public void Plot(string series, PyObject pyObject)
        {
            using (Py.GIL())
            {
                if (pyObject.TryConvert(out IndicatorBase indicator, true))
                {
                    Plot(series, indicator);
                }
                else
                {
                    try
                    {
                        var value = (((dynamic)pyObject).Value as PyObject).GetAndDispose<decimal>();
                        Plot(series, value);
                    }
                    catch
                    {
                        var pythonType = pyObject.GetPythonType().Repr();
                        throw new ArgumentException($"QCAlgorithm.Plot(): The last argument should be a QuantConnect Indicator object, {pythonType} was provided.");
                    }
                }
            }
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
        [DocumentationAttribute(Charting)]
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
        [DocumentationAttribute(Charting)]
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
        [DocumentationAttribute(Charting)]
        public void Plot(string chart, TradeBarIndicator first, TradeBarIndicator second = null, TradeBarIndicator third = null, TradeBarIndicator fourth = null)
        {
            Plot(chart, new[] { first, second, third, fourth }.Where(x => x != null).ToArray());
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        [DocumentationAttribute(Charting)]
        [DocumentationAttribute(Indicators)]
        public void PlotIndicator(string chart, PyObject first, PyObject second = null, PyObject third = null, PyObject fourth = null)
        {
            var array = GetIndicatorArray(first, second, third, fourth);
            PlotIndicator(chart, array[0], array[1], array[2], array[3]);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        [DocumentationAttribute(Charting)]
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(Indicators)]
        public FilteredIdentity FilteredIdentity(Symbol symbol, TimeSpan resolution, PyObject selector = null, PyObject filter = null, string fieldName = null)
        {
            var name = $"{symbol}({fieldName ?? "close"}_{resolution.ToStringInvariant(null)})";
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
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>A python dictionary with pandas DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject tickers, int periods, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            if (tickers.TryConvert<Universe>(out var universe))
            {
                resolution ??= universe.Configuration.Resolution;
                var requests = CreateBarCountHistoryRequests(new[] { universe.Symbol }, universe.DataType, periods, resolution, fillForward, extendedMarketHours,
                    dataMappingMode, dataNormalizationMode, contractDepthOffset);
                // we pass in 'BaseDataCollection' type so we clean up the data frame if we can
                return GetDataFrame(History(requests.Where(x => x != null)), flatten, typeof(BaseDataCollection));
            }
            if (tickers.TryCreateType(out var type))
            {
                var requests = CreateBarCountHistoryRequests(Securities.Keys, type, periods, resolution, fillForward, extendedMarketHours,
                    dataMappingMode, dataNormalizationMode, contractDepthOffset);
                return GetDataFrame(History(requests.Where(x => x != null)), flatten, type);
            }

            var symbols = tickers.ConvertToSymbolEnumerable().ToArray();
            var dataType = Extensions.GetCustomDataTypeFromSymbols(symbols);

            return GetDataFrame(
                History(symbols, periods, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode, contractDepthOffset),
                flatten,
                dataType);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>A python dictionary with pandas DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject tickers, TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            return History(tickers, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset, flatten);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>A python dictionary with a pandas DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject tickers, DateTime start, DateTime end, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            if (tickers.TryConvert<Universe>(out var universe))
            {
                resolution ??= universe.Configuration.Resolution;
                var requests = CreateDateRangeHistoryRequests(new[] { universe.Symbol }, universe.DataType, start, end, resolution, fillForward, extendedMarketHours,
                    dataMappingMode, dataNormalizationMode, contractDepthOffset);
                // we pass in 'BaseDataCollection' type so we clean up the data frame if we can
                return GetDataFrame(History(requests.Where(x => x != null)), flatten, typeof(BaseDataCollection));
            }
            if (tickers.TryCreateType(out var type))
            {
                var requests = CreateDateRangeHistoryRequests(Securities.Keys, type, start, end, resolution, fillForward, extendedMarketHours,
                    dataMappingMode, dataNormalizationMode, contractDepthOffset);
                return GetDataFrame(History(requests.Where(x => x != null)), flatten, type);
            }

            var symbols = tickers.ConvertToSymbolEnumerable().ToArray();
            var dataType = Extensions.GetCustomDataTypeFromSymbols(symbols);

            return GetDataFrame(
                History(symbols, start, end, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode, contractDepthOffset),
                flatten,
                dataType);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject type, PyObject tickers, DateTime start, DateTime end, Resolution? resolution = null,
            bool? fillForward = null, bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null,
            DataNormalizationMode? dataNormalizationMode = null, int? contractDepthOffset = null, bool flatten = false)
        {
            var symbols = tickers.ConvertToSymbolEnumerable().ToArray();
            var requestedType = type.CreateType();
            var requests = CreateDateRangeHistoryRequests(symbols, requestedType, start, end, resolution, fillForward, extendedMarketHours,
                dataMappingMode, dataNormalizationMode, contractDepthOffset);
            return GetDataFrame(History(requests.Where(x => x != null)), flatten, requestedType);
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
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject type, PyObject tickers, int periods, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            var symbols = tickers.ConvertToSymbolEnumerable().ToArray();
            var requestedType = type.CreateType();
            CheckPeriodBasedHistoryRequestResolution(symbols, resolution, requestedType);

            var requests = CreateBarCountHistoryRequests(symbols, requestedType, periods, resolution, fillForward, extendedMarketHours,
                dataMappingMode, dataNormalizationMode, contractDepthOffset);

            return GetDataFrame(History(requests.Where(x => x != null)), flatten, requestedType);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="tickers">The symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject type, PyObject tickers, TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            return History(type, tickers, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset, flatten);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject type, Symbol symbol, DateTime start, DateTime end, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            return History(type.CreateType(), symbol, start, end, resolution, fillForward, extendedMarketHours, dataMappingMode,
                dataNormalizationMode, contractDepthOffset, flatten);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols between the specified dates. The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        private PyObject History(Type type, Symbol symbol, DateTime start, DateTime end, Resolution? resolution, bool? fillForward,
            bool? extendedMarketHours, DataMappingMode? dataMappingMode, DataNormalizationMode? dataNormalizationMode,
            int? contractDepthOffset, bool flatten)
        {
            var requests = CreateDateRangeHistoryRequests(new[] { symbol }, type, start, end, resolution, fillForward,
                extendedMarketHours, dataMappingMode, dataNormalizationMode, contractDepthOffset);
            if (requests.IsNullOrEmpty())
            {
                throw new ArgumentException($"No history data could be fetched. " +
                    $"This could be due to the specified security not being of the requested type. Symbol: {symbol} Requested Type: {type.Name}");
            }

            return GetDataFrame(History(requests), flatten, type);
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
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject type, Symbol symbol, int periods, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            var managedType = type.CreateType();
            resolution = GetResolution(symbol, resolution, managedType);
            CheckPeriodBasedHistoryRequestResolution(new[] { symbol }, resolution, managedType);

            var marketHours = GetMarketHours(symbol, managedType);
            var start = _historyRequestFactory.GetStartTimeAlgoTz(symbol, periods, resolution.Value, marketHours.ExchangeHours,
                marketHours.DataTimeZone, managedType, extendedMarketHours);
            return History(managedType, symbol, start, Time, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset, flatten);
        }

        /// <summary>
        /// Gets the historical data for the specified symbols over the requested span.
        /// The symbols must exist in the Securities collection.
        /// </summary>
        /// <param name="type">The data type of the symbols</param>
        /// <param name="symbol">The symbol to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="fillForward">True to fill forward missing data, false otherwise</param>
        /// <param name="extendedMarketHours">True to include extended market hours data, false otherwise</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security history request</param>
        /// <param name="dataNormalizationMode">The price scaling mode to use for the securities history</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 will use the front month, 1 will use the back month contract</param>
        /// <param name="flatten">Whether to flatten the resulting data frame.
        /// e.g. for universe requests, the each row represents a day of data, and the data is stored in a list in a cell of the data frame.
        /// If flatten is true, the resulting data frame will contain one row per universe constituent,
        /// and each property of the constituent will be a column in the data frame.</param>
        /// <returns>pandas.DataFrame containing the requested historical data</returns>
        [DocumentationAttribute(HistoricalData)]
        public PyObject History(PyObject type, Symbol symbol, TimeSpan span, Resolution? resolution = null, bool? fillForward = null,
            bool? extendedMarketHours = null, DataMappingMode? dataMappingMode = null, DataNormalizationMode? dataNormalizationMode = null,
            int? contractDepthOffset = null, bool flatten = false)
        {
            return History(type, symbol, Time - span, Time, resolution, fillForward, extendedMarketHours, dataMappingMode, dataNormalizationMode,
                contractDepthOffset, flatten);
        }

        /// <summary>
        /// Sets the specified function as the benchmark, this function provides the value of
        /// the benchmark at each date/time requested
        /// </summary>
        /// <param name="benchmark">The benchmark producing function</param>
        [DocumentationAttribute(TradingAndOrders)]
        [DocumentationAttribute(SecuritiesAndPortfolio)]
        [DocumentationAttribute(Indicators)]
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
        [DocumentationAttribute(Modeling)]
        public void SetBrokerageModel(PyObject model)
        {
            IBrokerageModel brokerageModel;
            if (!model.TryConvert(out brokerageModel))
            {
                brokerageModel = new BrokerageModelPythonWrapper(model);
            }

            SetBrokerageModel(brokerageModel);
        }

        /// <summary>
        /// Sets the implementation used to handle messages from the brokerage.
        /// The default implementation will forward messages to debug or error
        /// and when a <see cref="BrokerageMessageType.Error"/> occurs, the algorithm
        /// is stopped.
        /// </summary>
        /// <param name="handler">The message handler to use</param>
        [DocumentationAttribute(Modeling)]
        [DocumentationAttribute(Logging)]
        public void SetBrokerageMessageHandler(PyObject handler)
        {
            if (!handler.TryConvert(out IBrokerageMessageHandler brokerageMessageHandler))
            {
                brokerageMessageHandler = new BrokerageMessageHandlerPythonWrapper(handler);
            }

            SetBrokerageMessageHandler(brokerageMessageHandler);
        }

        /// <summary>
        /// Sets the risk free interest rate model to be used in the algorithm
        /// </summary>
        /// <param name="model">The risk free interest rate model to use</param>
        [DocumentationAttribute(Modeling)]
        public void SetRiskFreeInterestRateModel(PyObject model)
        {
            SetRiskFreeInterestRateModel(RiskFreeInterestRateModelPythonWrapper.FromPyObject(model));
        }

        /// <summary>
        /// Sets the security initializer function, used to initialize/configure securities after creation
        /// </summary>
        /// <param name="securityInitializer">The security initializer function or class</param>
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(Modeling)]
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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(MachineLearning)]
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
        [DocumentationAttribute(AddingData)]
        [DocumentationAttribute(MachineLearning)]
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
                        using var iterator = headers.GetIterator();
                        foreach (PyObject pyKey in iterator)
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
        /// Send a debug message to the web console:
        /// </summary>
        /// <param name="message">Message to send to debug console</param>
        /// <seealso cref="Log(PyObject)"/>
        /// <seealso cref="Error(PyObject)"/>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(Logging)]
        public void Log(PyObject message)
        {
            Log(message.ToSafeString());
        }

        /// <summary>
        /// Terminate the algorithm after processing the current event handler.
        /// </summary>
        /// <param name="message">Exit message to display on quitting</param>
        [DocumentationAttribute(Logging)]
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
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Resolution period, PyObject handler)
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
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Resolution period, TickType? tickType, PyObject handler)
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

            return Consolidate(symbol, period, tickType, handler.ConvertToDelegate<Action<BaseData>>());
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
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
        [DocumentationAttribute(ConsolidatingData)]
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

            return Consolidate(symbol, period, tickType, handler.ConvertToDelegate<Action<BaseData>>());
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Func<DateTime, CalendarInfo> calendar, PyObject handler)
        {
            return Consolidate(symbol, calendar, null, handler);
        }

        /// <summary>
        /// Schedules the provided training code to execute immediately
        /// </summary>
        /// <param name="trainingCode">The training code to be invoked</param>
        [DocumentationAttribute(MachineLearning)]
        [DocumentationAttribute(ScheduledEvents)]
        public ScheduledEvent Train(PyObject trainingCode)
        {
            return Schedule.TrainingNow(trainingCode);
        }

        /// <summary>
        /// Schedules the training code to run using the specified date and time rules
        /// </summary>
        /// <param name="dateRule">Specifies what dates the event should run</param>
        /// <param name="timeRule">Specifies the times on those dates the event should run</param>
        /// <param name="trainingCode">The training code to be invoked</param>
        [DocumentationAttribute(MachineLearning)]
        [DocumentationAttribute(ScheduledEvents)]
        public ScheduledEvent Train(IDateRule dateRule, ITimeRule timeRule, PyObject trainingCode)
        {
            return Schedule.Training(dateRule, timeRule, trainingCode);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="tickType">The tick type of subscription used as data source for consolidator. Specify null to use first subscription found.</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Func<DateTime, CalendarInfo> calendar, TickType? tickType, PyObject handler)
        {
            // resolve consolidator input subscription
            var type = GetSubscription(symbol, tickType).Type;

            if (type == typeof(TradeBar))
            {
                return Consolidate(symbol, calendar, tickType, handler.ConvertToDelegate<Action<TradeBar>>());
            }

            if (type == typeof(QuoteBar))
            {
                return Consolidate(symbol, calendar, tickType, handler.ConvertToDelegate<Action<QuoteBar>>());
            }

            return Consolidate(symbol, calendar, tickType, handler.ConvertToDelegate<Action<BaseData>>());
        }

        /// <summary>
        /// Gets the historical data of an indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">The target indicator</param>
        /// <param name="symbol">The symbol or symbols to retrieve historical data for</param>
        /// <param name="period">The number of bars to request</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of an indicator</returns>
        public IndicatorHistory IndicatorHistory(PyObject indicator, PyObject symbol, int period, Resolution? resolution = null, PyObject selector = null)
        {
            var symbols = symbol.ConvertToSymbolEnumerable();
            var convertedIndicator = ConvertPythonIndicator(indicator);

            switch (convertedIndicator)
            {
                case PythonIndicator pythonIndicator:
                    return IndicatorHistory(pythonIndicator, symbols, period, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());

                case IndicatorBase<IndicatorDataPoint> dataPointIndicator:
                    return IndicatorHistory(dataPointIndicator, symbols, period, resolution, selector?.ConvertToDelegate<Func<IBaseData, decimal>>());

                case IndicatorBase<IBaseDataBar> baseDataBarIndicator:
                    return IndicatorHistory(baseDataBarIndicator, symbols, period, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseDataBar>>());

                case IndicatorBase<TradeBar> tradeBarIndicator:
                    return IndicatorHistory(tradeBarIndicator, symbols, period, resolution, selector?.ConvertToDelegate<Func<IBaseData, TradeBar>>());

                case IndicatorBase<IBaseData> baseDataIndicator:
                    return IndicatorHistory(baseDataIndicator, symbols, period, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());

                case IndicatorBase<BaseData> baseDataIndicator:
                    return IndicatorHistory(baseDataIndicator, symbols, period, resolution, selector?.ConvertToDelegate<Func<IBaseData, BaseData>>());

                default:
                    // Shouldn't happen, ConvertPythonIndicator will wrap the PyObject in a PythonIndicator instance if it can't convert it
                    throw new ArgumentException($"Indicator type {indicator.GetPythonType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Gets the historical data of an indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">The target indicator</param>
        /// <param name="symbol">The symbol or symbols to retrieve historical data for</param>
        /// <param name="span">The span over which to retrieve recent historical data</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of an indicator</returns>
        public IndicatorHistory IndicatorHistory(PyObject indicator, PyObject symbol, TimeSpan span, Resolution? resolution = null, PyObject selector = null)
        {
            return IndicatorHistory(indicator, symbol, Time - span, Time, resolution, selector);
        }

        /// <summary>
        /// Gets the historical data of an indicator for the specified symbol. The exact number of bars will be returned.
        /// The symbol must exist in the Securities collection.
        /// </summary>
        /// <param name="indicator">The target indicator</param>
        /// <param name="symbol">The symbol or symbols to retrieve historical data for</param>
        /// <param name="start">The start time in the algorithm's time zone</param>
        /// <param name="end">The end time in the algorithm's time zone</param>
        /// <param name="resolution">The resolution to request</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame of historical data of an indicator</returns>
        public IndicatorHistory IndicatorHistory(PyObject indicator, PyObject symbol, DateTime start, DateTime end, Resolution? resolution = null, PyObject selector = null)
        {
            var symbols = symbol.ConvertToSymbolEnumerable();
            var convertedIndicator = ConvertPythonIndicator(indicator);

            switch (convertedIndicator)
            {
                case PythonIndicator pythonIndicator:
                    return IndicatorHistory(pythonIndicator, symbols, start, end, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());

                case IndicatorBase<IndicatorDataPoint> dataPointIndicator:
                    return IndicatorHistory(dataPointIndicator, symbols, start, end, resolution, selector?.ConvertToDelegate<Func<IBaseData, decimal>>());

                case IndicatorBase<IBaseDataBar> baseDataBarIndicator:
                    return IndicatorHistory(baseDataBarIndicator, symbols, start, end, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseDataBar>>());

                case IndicatorBase<TradeBar> tradeBarIndicator:
                    return IndicatorHistory(tradeBarIndicator, symbols, start, end, resolution, selector?.ConvertToDelegate<Func<IBaseData, TradeBar>>());

                case IndicatorBase<IBaseData> baseDataIndicator:
                    return IndicatorHistory(baseDataIndicator, symbols, start, end, resolution, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());

                case IndicatorBase<BaseData> baseDataIndicator:
                    return IndicatorHistory(baseDataIndicator, symbols, start, end, resolution, selector?.ConvertToDelegate<Func<IBaseData, BaseData>>());

                default:
                    // Shouldn't happen, ConvertPythonIndicator will wrap the PyObject in a PythonIndicator instance if it can't convert it
                    throw new ArgumentException($"Indicator type {indicator.GetPythonType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Gets the historical data of an indicator and convert it into pandas.DataFrame
        /// </summary>
        /// <param name="indicator">The target indicator</param>
        /// <param name="history">Historical data used to calculate the indicator</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>pandas.DataFrame containing the historical data of <paramref name="indicator"/></returns>
        public IndicatorHistory IndicatorHistory(PyObject indicator, IEnumerable<Slice> history, PyObject selector = null)
        {
            var convertedIndicator = ConvertPythonIndicator(indicator);

            switch (convertedIndicator)
            {
                case PythonIndicator pythonIndicator:
                    return IndicatorHistory(pythonIndicator, history, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());

                case IndicatorBase<IndicatorDataPoint> dataPointIndicator:
                    return IndicatorHistory(dataPointIndicator, history, selector?.ConvertToDelegate<Func<IBaseData, decimal>>());

                case IndicatorBase<IBaseDataBar> baseDataBarIndicator:
                    return IndicatorHistory(baseDataBarIndicator, history, selector?.ConvertToDelegate<Func<IBaseData, IBaseDataBar>>());

                case IndicatorBase<TradeBar> tradeBarIndicator:
                    return IndicatorHistory(tradeBarIndicator, history, selector?.ConvertToDelegate<Func<IBaseData, TradeBar>>());

                case IndicatorBase<IBaseData> baseDataIndicator:
                    return IndicatorHistory(baseDataIndicator, history, selector?.ConvertToDelegate<Func<IBaseData, IBaseData>>());

                case IndicatorBase<BaseData> baseDataIndicator:
                    return IndicatorHistory(baseDataIndicator, history, selector?.ConvertToDelegate<Func<IBaseData, BaseData>>());

                default:
                    // Shouldn't happen, ConvertPythonIndicator will wrap the PyObject in a PythonIndicator instance if it can't convert it
                    throw new ArgumentException($"Indicator type {indicator.GetPythonType().Name} is not supported.");
            }
        }

        /// <summary>
        /// Liquidate your portfolio holdings
        /// </summary>
        /// <param name="symbols">List of symbols to liquidate in Python</param>
        /// <param name="asynchronous">Flag to indicate if the symbols should be liquidated asynchronously</param>
        /// <param name="tag">Custom tag to know who is calling this</param>
        /// <param name="orderProperties">Order properties to use</param>
        [DocumentationAttribute(TradingAndOrders)]
        public List<OrderTicket> Liquidate(PyObject symbols, bool asynchronous = false, string tag = "Liquidated", IOrderProperties orderProperties = null)
        {
            return Liquidate(symbols.ConvertToSymbolEnumerable(), asynchronous, tag, orderProperties);
        }

        /// <summary>
        /// Register a command type to be used
        /// </summary>
        /// <param name="type">The command type</param>
        public void AddCommand(PyObject type)
        {
            // create a test instance to validate interface is implemented accurate
            var testInstance = new CommandPythonWrapper(type);

            var wrappedType = Extensions.CreateType(type);
            _registeredCommands[wrappedType.Name] = (CallbackCommand command) =>
            {
                var commandWrapper = new CommandPythonWrapper(type, command.Payload);
                return commandWrapper.Run(this);
            };
        }


        /// <summary>
        /// Get the option chains for the specified symbols at the current time (<see cref="Time"/>)
        /// </summary>
        /// <param name="symbols">
        /// The symbols for which the option chain is asked for.
        /// It can be either the canonical options or the underlying symbols.
        /// </param>
        /// <param name="flatten">
        /// Whether to flatten the resulting data frame.
        /// See <see cref="History(PyObject, int, Resolution?, bool?, bool?, DataMappingMode?, DataNormalizationMode?, int?, bool)"/>
        /// </param>
        /// <returns>The option chains</returns>
        [DocumentationAttribute(AddingData)]
        public OptionChains OptionChains(PyObject symbols, bool flatten = false)
        {
            return OptionChains(symbols.ConvertToSymbolEnumerable(), flatten);
        }

        /// <summary>
        /// Get an authenticated link to execute the given command instance
        /// </summary>
        /// <param name="command">The target command</param>
        /// <returns>The authenticated link</returns>
        public string Link(PyObject command)
        {
            var payload = ConvertCommandToPayload(command, out var typeName);
            return CommandLink(typeName, payload);
        }

        /// <summary>
        /// Broadcast a live command
        /// </summary>
        /// <param name="command">The target command</param>
        /// <returns><see cref="RestResponse"/></returns>
        public RestResponse BroadcastCommand(PyObject command)
        {
            var payload = ConvertCommandToPayload(command, out var typeName);
            return SendBroadcast(typeName, payload);
        }

        /// <summary>
        /// Convert the command to a dictionary payload
        /// </summary>
        /// <param name="command">The target command</param>
        /// <param name="typeName">The type of the command</param>
        /// <returns>The dictionary payload</returns>
        private Dictionary<string, object> ConvertCommandToPayload(PyObject command, out string typeName)
        {
            using var _ = Py.GIL();

            var strResult = CommandPythonWrapper.Serialize(command);
            using var pyType = command.GetPythonType();
            typeName = Extensions.CreateType(pyType).Name;

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(strResult);
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
                    .Select(
                        x =>
                        {
                            if (x == null) return null;

                            Type type;
                            return x.GetPythonType().TryConvert(out type)
                                ? x.AsManagedObject(type)
                                : WrapPythonIndicator(x);
                        }
                    ).ToArray();

                var types = array.Where(x => x != null).Select(x => GetIndicatorBaseType(x.GetType())).Distinct();

                if (types.Count() > 1)
                {
                    throw new Exception("QCAlgorithm.GetIndicatorArray(). All indicators must be of the same type: data point, bar or tradebar.");
                }

                return array;
            }
        }

        /// <summary>
        /// Converts the given PyObject into an indicator
        /// </summary>
        private IndicatorBase ConvertPythonIndicator(PyObject pyIndicator)
        {
            IndicatorBase convertedIndicator;
            if (pyIndicator.TryConvert(out PythonIndicator pythonIndicator))
            {
                convertedIndicator = WrapPythonIndicator(pyIndicator, pythonIndicator);
            }
            else if (!pyIndicator.TryConvert(out convertedIndicator))
            {
                convertedIndicator = WrapPythonIndicator(pyIndicator);
            }

            return convertedIndicator;
        }

        /// <summary>
        /// Wraps a custom python indicator and save its reference to _pythonIndicators dictionary
        /// </summary>
        /// <param name="pyObject">The python implementation of <see cref="IndicatorBase{IBaseDataBar}"/></param>
        /// <param name="convertedPythonIndicator">The C# converted <paramref name="pyObject"/> to avoid re-conversion</param>
        /// <returns><see cref="PythonIndicator"/> that wraps the python implementation</returns>
        private PythonIndicator WrapPythonIndicator(PyObject pyObject, PythonIndicator convertedPythonIndicator = null)
        {
            PythonIndicator pythonIndicator;

            if (!_pythonIndicators.TryGetValue(pyObject.Handle, out pythonIndicator))
            {
                if (convertedPythonIndicator == null)
                {
                    pyObject.TryConvert(out pythonIndicator);
                }
                else
                {
                    pythonIndicator = convertedPythonIndicator;
                }

                if (pythonIndicator == null)
                {
                    pythonIndicator = new PythonIndicator(pyObject);
                }
                else
                {
                    pythonIndicator.SetIndicator(pyObject);
                }

                // Save to prevent future additions
                _pythonIndicators.Add(pyObject.Handle, pythonIndicator);
            }

            return pythonIndicator;
        }

        /// <summary>
        /// Converts an enumerable of Slice into a Python Pandas data frame
        /// </summary>
        protected PyObject GetDataFrame(IEnumerable<Slice> data, bool flatten, Type dataType = null)
        {
            var history = PandasConverter.GetDataFrame(RemoveMemoizing(data), flatten, dataType);
            return flatten ? history : TryCleanupCollectionDataFrame(dataType, history);
        }

        /// <summary>
        /// Converts an enumerable of BaseData into a Python Pandas data frame
        /// </summary>
        protected PyObject GetDataFrame<T>(IEnumerable<T> data, bool flatten)
            where T : IBaseData
        {
            var history = PandasConverter.GetDataFrame(RemoveMemoizing(data), flatten: flatten);
            return flatten ? history : TryCleanupCollectionDataFrame(typeof(T), history);
        }

        private IEnumerable<T> RemoveMemoizing<T>(IEnumerable<T> data)
        {
            var memoizingEnumerable = data as MemoizingEnumerable<T>;
            if (memoizingEnumerable != null)
            {
                // we don't need the internal buffer which will just generate garbage, so we disable it
                // the user will only have access to the final pandas data frame object
                memoizingEnumerable.Enabled = false;
            }
            return data;
        }

        private PyObject TryCleanupCollectionDataFrame(Type dataType, PyObject history)
        {
            if (dataType != null && dataType.IsAssignableTo(typeof(BaseDataCollection)))
            {
                // clear out the first symbol level since it doesn't make sense, it's the universe generic symbol
                // let's directly return the data property which is where all the data points are in a BaseDataCollection, save the user some pain
                dynamic dynamic = history;
                using (Py.GIL())
                {
                    if (!dynamic.empty)
                    {
                        using var columns = new PySequence(dynamic.columns);
                        using var dataKey = "data".ToPython();
                        if (columns.Contains(dataKey))
                        {
                            history = dynamic["data"];
                        }
                        else
                        {
                            dynamic.index = dynamic.index.droplevel("symbol");
                            history = dynamic;
                        }
                    }
                }
            }
            return history;
        }
    }
}
