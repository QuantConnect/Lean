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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public class PandasConverter
    {
        dynamic _pandas;

        /// <summary>
        /// Creates an instance of <see cref="PandasConverter"/>.
        /// </summary>
        /// <param name="pandas"></param>
        public PandasConverter(PyObject pandas = null)
        {
            try
            {
                if (pandas == null)
                {
                    using (Py.GIL())
                    {
                        pandas = Py.Import("pandas");
                    }
                }

                _pandas = pandas;
            }
            catch (PythonException pythonException)
            {
                Logging.Log.Error($"PandasConverter: Failed to import pandas module: {pythonException}");
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="Slice"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame(IEnumerable<Slice> data)
        {
            var maxLevels = 0;
            var sliceDataDict = new Dictionary<Symbol, PandasData>();

            foreach (var slice in data)
            {
                foreach (var key in slice.Keys)
                {
                    PandasData value;

                    var ticks = slice[key] as List<Tick>;
                    if (ticks == null)
                    {
                        if (!sliceDataDict.TryGetValue(key, out value))
                        {
                            sliceDataDict.Add(key, value = new PandasData(slice[key]));
                            maxLevels = Math.Max(maxLevels, value.Levels);
                        }
                        value.Add(slice[key]);
                    }
                    else
                    {
                        if (!sliceDataDict.TryGetValue(key, out value))
                        {
                            sliceDataDict.Add(key, value = new PandasData(ticks.First()));
                            maxLevels = Math.Max(maxLevels, value.Levels);
                        }
                        foreach (var tick in ticks)
                        {
                            value.Add(tick);
                        }
                    }
                }
            }

            using (Py.GIL())
            {
                if (sliceDataDict.Count == 0)
                {
                    return _pandas.DataFrame();
                }
                var dataFrames = sliceDataDict.Select(x => x.Value.ToPandasDataFrame(_pandas, maxLevels));
                return _pandas.concat(dataFrames.ToArray());
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="IBaseData"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame<T>(IEnumerable<T> data)
            where T : IBaseData
        {
            PandasData sliceData = null;
            foreach (var datum in data)
            {
                if (sliceData == null)
                {
                    sliceData = new PandasData(datum);
                }
                sliceData.Add(datum);
            }

            // If sliceData is still null, data is an empty enumerable
            // returns an empty pandas.DataFrame
            if (sliceData == null)
            {
                using (Py.GIL())
                {
                    return _pandas.DataFrame();
                }
            }
            return sliceData.ToPandasDataFrame(_pandas);
        }

        /// <summary>
        /// Converts a dictionary with a list of <see cref="IndicatorDataPoint"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Dictionary with a list of <see cref="IndicatorDataPoint"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetIndicatorDataFrame(IDictionary<string, List<IndicatorDataPoint>> data)
        {
            using (Py.GIL())
            {
                var pyDict = new PyDict();

                foreach (var kvp in data)
                {
                    var index = new List<DateTime>();
                    var values = new List<double>();

                    foreach (var item in kvp.Value)
                    {
                        index.Add(item.EndTime);
                        values.Add((double)item.Value);
                    }
                    pyDict.SetItem(kvp.Key.ToLower(), _pandas.Series(values, index));
                }

                return _pandas.DataFrame(pyDict, columns: data.Keys.Select(x => x.ToLower()).OrderBy(x => x));
            }
        }

        /// <summary>
        /// Returns a string that represent the current object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _pandas == null
                ? "pandas module was not imported."
                : _pandas.Repr();
        }
    }

    /// <summary>
    /// Organizes a list of data to create pandas.DataFrames 
    /// </summary>
    public class PandasData
    {
        private readonly Symbol _symbol;
        private readonly List<DateTime> _timeIndex;
        private readonly Dictionary<string, List<double>> _doubleSeries;
        private readonly Dictionary<string, List<string>> _stringSeries = new Dictionary<string, List<string>>();

        /// <summary>
        /// Implied levels of a multi index pandas.Series (depends on the security type)
        /// </summary>
        public int Levels { get; }

        /// <summary>
        /// Initializes an instance of <see cref="PandasData"/> with a sample <see cref="IBaseData"/> object
        /// </summary>
        /// <param name="baseData"><see cref="IBaseData"/> object that contains information to be saved in an instance of <see cref="PandasData"/></param>
        public PandasData(IBaseData baseData)
        {
            var columns = "open,high,low,close";

            if (baseData is Tick)
            {
                columns = "askprice,asksize,bidprice,bidsize,lastprice,quantity";
                _stringSeries.Add("exchange", new List<string>());
            }
            else if (baseData is TradeBar)
            {
                columns += ",volume";
            }
            else if (baseData is QuoteBar)
            {
                columns += ",askopen,askhigh,asklow,askclose,asksize,bidopen,bidhigh,bidlow,bidclose,bidsize";
            }
            else if (baseData is DynamicData)
            {
                // We get the fields of DynamicData from the storage dictionary
                // and add the field named 'value' since it is the reference value
                columns = "value," + string.Join(",", ((DynamicData)baseData).GetStorageDictionary().Keys);
            }
            
            _doubleSeries = columns.Split(',').ToDictionary(k => k, v => new List<double>());
            _symbol = baseData.Symbol;
            _timeIndex = new List<DateTime>();

            Levels = 2;
            if (_symbol.SecurityType == SecurityType.Future) Levels = 3;
            if (_symbol.SecurityType == SecurityType.Option) Levels = 5;
        }

        /// <summary>
        /// Adds an object to the end of the lists
        /// </summary>
        /// <param name="baseData"><see cref="IBaseData"/> object that contains information to be saved in an instance of <see cref="PandasData"/></param>
        public void Add(IBaseData baseData)
        {
            _timeIndex.Add(baseData.EndTime);

            var bar = baseData as IBaseDataBar;
            if (bar != null)
            {
                _doubleSeries["open"].Add((double)bar.Open);
                _doubleSeries["high"].Add((double)bar.High);
                _doubleSeries["low"].Add((double)bar.Low);
                _doubleSeries["close"].Add((double)bar.Close);

                var tradeBar = bar as TradeBar;
                if (tradeBar != null)
                {
                    _doubleSeries["volume"].Add((double)tradeBar.Volume);
                }

                var quoteBar = bar as QuoteBar;
                if (quoteBar != null)
                {
                    _doubleSeries["asksize"].Add((double)quoteBar.LastAskSize);
                    _doubleSeries["bidsize"].Add((double)quoteBar.LastBidSize);

                    if (quoteBar.Ask != null)
                    {
                        _doubleSeries["askopen"].Add((double)quoteBar.Ask.Open);
                        _doubleSeries["askhigh"].Add((double)quoteBar.Ask.High);
                        _doubleSeries["asklow"].Add((double)quoteBar.Ask.Low);
                        _doubleSeries["askclose"].Add((double)quoteBar.Ask.Close);
                    }
                    else
                    {
                        _doubleSeries["askopen"].Add(double.NaN);
                        _doubleSeries["askhigh"].Add(double.NaN);
                        _doubleSeries["asklow"].Add(double.NaN);
                        _doubleSeries["askclose"].Add(double.NaN);
                    }

                    if (quoteBar.Bid != null)
                    {
                        _doubleSeries["bidopen"].Add((double)quoteBar.Bid.Open);
                        _doubleSeries["bidhigh"].Add((double)quoteBar.Bid.High);
                        _doubleSeries["bidlow"].Add((double)quoteBar.Bid.Low);
                        _doubleSeries["bidclose"].Add((double)quoteBar.Bid.Close);
                    }
                    else
                    {
                        _doubleSeries["bidopen"].Add(double.NaN);
                        _doubleSeries["bidhigh"].Add(double.NaN);
                        _doubleSeries["bidlow"].Add(double.NaN);
                        _doubleSeries["bidclose"].Add(double.NaN);
                    }
                }
            }

            var tick = baseData as Tick;
            if (tick != null)
            {
                if (tick.TickType == TickType.Quote)
                {
                    _doubleSeries["askprice"].Add((double)tick.AskPrice);
                    _doubleSeries["asksize"].Add((double)tick.AskSize);
                    _doubleSeries["bidprice"].Add((double)tick.BidPrice);
                    _doubleSeries["bidsize"].Add((double)tick.BidSize);
                }
                else
                {
                    _stringSeries["exchange"].Add(tick.Exchange);
                }
                _doubleSeries["lastprice"].Add((double)tick.LastPrice);
                _doubleSeries["quantity"].Add((double)tick.Quantity);
            }

            var data = baseData as DynamicData;
            if (data != null)
            {
                foreach (var kvp in data.GetStorageDictionary())
                {
                    _doubleSeries[kvp.Key].Add(Convert.ToDouble(kvp.Value));
                }
                _doubleSeries["value"].Add((double)data.Value);
            }
        }

        /// <summary>
        /// Get the pandas.DataFrame of the current <see cref="PandasData"/> state 
        /// </summary>
        /// <param name="pandas">pandas module</param>
        /// <param name="levels">Number of levels of the multi index</param>
        /// <returns>pandas.DataFrame object</returns>
        public PyObject ToPandasDataFrame(dynamic pandas, int levels = 2)
        {
            var seriesDict = GetPandasSeries(pandas, levels);
            using (Py.GIL())
            {
                var pyDict = new PyDict();
                foreach (var series in seriesDict)
                {
                    pyDict.SetItem(series.Key, series.Value);
                }
                return pandas.DataFrame(pyDict);
            }
        }

        /// <summary>
        /// Get the pandas.Series of the current <see cref="PandasData"/> state 
        /// </summary>
        /// <param name="pandas">pandas module</param>
        /// <param name="levels">Number of levels of the multi index</param>
        /// <returns>Dictionary keyed by column name where values are pandas.Series objects</returns>
        private Dictionary<string, dynamic> GetPandasSeries(dynamic pandas, int levels = 2)
        {
            var pyObjectArray = new PyObject[levels];
            pyObjectArray[levels - 2] = _symbol.ToString().ToPython();

            if (_symbol.SecurityType == SecurityType.Future)
            {
                pyObjectArray[0] = _symbol.ID.Date.ToPython();
                pyObjectArray[1] = _symbol.Value.ToPython();
            }
            if (_symbol.SecurityType == SecurityType.Option)
            {
                pyObjectArray[0] = _symbol.ID.Date.ToPython();
                pyObjectArray[1] = _symbol.ID.StrikePrice.ToPython();
                pyObjectArray[2] = _symbol.ID.OptionRight.ToString().ToPython();
                pyObjectArray[3] = _symbol.Value.ToPython();
            }

            // Set null to python empty string
            for (var i = 0; i < levels - 1; i++)
            {
                if (pyObjectArray[i] == null)
                {
                    pyObjectArray[i] = new PyString(string.Empty);
                }
            }

            // Create the index labels
            var names = "symbol,time";
            if (levels == 3) names = "expiry,symbol,time";
            if (levels == 5) names = "expiry,strike,type,symbol,time";

            using (Py.GIL())
            {
                // Create a pandas multi index
                var tuples = _timeIndex.Select(x =>
                {
                    pyObjectArray[levels - 1] = x.ToPython();
                    return new PyTuple(pyObjectArray);
                }).ToArray();

                var index = pandas.MultiIndex.from_tuples(tuples, names: names.Split(','));

                // Returns a dictionary keyed by column name where values are pandas.Series objects
                var pandasSeries = new Dictionary<string, dynamic>();
                foreach (var kvp in _doubleSeries)
                {
                    if (kvp.Value.Count != tuples.Length) continue;
                    pandasSeries.Add(kvp.Key, pandas.Series(kvp.Value, index));
                }
                foreach (var kvp in _stringSeries)
                {
                    if (kvp.Value.Count != tuples.Length) continue;
                    pandasSeries.Add(kvp.Key, pandas.Series(kvp.Value, index));
                }
                return pandasSeries;
            }
        }
    }
}