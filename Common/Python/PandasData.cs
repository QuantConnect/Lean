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
using System.Reflection;

namespace QuantConnect.Python
{
    /// <summary>
    /// Organizes a list of data to create pandas.DataFrames 
    /// </summary>
    public class PandasData
    {
        private readonly Type _type;
        private readonly Symbol _symbol;
        private readonly List<DateTime> _timeIndex;
        private readonly Dictionary<string, List<object>> _series;

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
                columns = "askprice,asksize,bidprice,bidsize,lastprice,quantity,exchange,suspicious";
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
            // C# custom data
            else if (baseData is BaseData)
            {
                _type = baseData.GetType();
                var properties = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                columns = "Value," + string.Join(",", properties.Select(x => x.Name));
            }

            _series = columns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(k => k, v => new List<object>());
            _symbol = baseData.Symbol;
            _timeIndex = new List<DateTime>();

            Levels = 2;
            if (_symbol.SecurityType == SecurityType.Future) Levels = 3;
            if (_symbol.SecurityType == SecurityType.Option) Levels = 5;

            // Add first data-point to series
            Add(baseData);
        }

        /// <summary>
        /// Initializes an instance of <see cref="PandasData"/> with a sample list of <see cref="Tick"/> object
        /// </summary>
        /// <param name="ticks">List of <see cref="Tick"/> objects that contains information to be saved in an instance of <see cref="PandasData"/></param>
        public PandasData(IEnumerable<Tick> ticks)
        {
            _series = "askprice,asksize,bidprice,bidsize,lastprice,quantity,exchange,suspicious".Split(',')
                .ToDictionary(k => k, v => new List<object>());
            _symbol = ticks.FirstOrDefault().Symbol;
            _timeIndex = new List<DateTime>();

            Levels = 2;
            if (_symbol.SecurityType == SecurityType.Future) Levels = 3;
            if (_symbol.SecurityType == SecurityType.Option) Levels = 5;

            // Add first list of ticks to series
            Add(ticks);
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
                _series["open"].Add((double)bar.Open);
                _series["high"].Add((double)bar.High);
                _series["low"].Add((double)bar.Low);
                _series["close"].Add((double)bar.Close);

                var tradeBar = bar as TradeBar;
                if (tradeBar != null)
                {
                    _series["volume"].Add((double)tradeBar.Volume);
                }

                var quoteBar = bar as QuoteBar;
                if (quoteBar != null)
                {
                    _series["asksize"].Add((double)quoteBar.LastAskSize);
                    _series["bidsize"].Add((double)quoteBar.LastBidSize);

                    if (quoteBar.Ask != null)
                    {
                        _series["askopen"].Add((double)quoteBar.Ask.Open);
                        _series["askhigh"].Add((double)quoteBar.Ask.High);
                        _series["asklow"].Add((double)quoteBar.Ask.Low);
                        _series["askclose"].Add((double)quoteBar.Ask.Close);
                    }
                    else
                    {
                        _series["askopen"].Add(double.NaN);
                        _series["askhigh"].Add(double.NaN);
                        _series["asklow"].Add(double.NaN);
                        _series["askclose"].Add(double.NaN);
                    }

                    if (quoteBar.Bid != null)
                    {
                        _series["bidopen"].Add((double)quoteBar.Bid.Open);
                        _series["bidhigh"].Add((double)quoteBar.Bid.High);
                        _series["bidlow"].Add((double)quoteBar.Bid.Low);
                        _series["bidclose"].Add((double)quoteBar.Bid.Close);
                    }
                    else
                    {
                        _series["bidopen"].Add(double.NaN);
                        _series["bidhigh"].Add(double.NaN);
                        _series["bidlow"].Add(double.NaN);
                        _series["bidclose"].Add(double.NaN);
                    }
                }
            }

            var tick = baseData as Tick;
            if (tick != null)
            {
                if (tick.TickType == TickType.Quote)
                {
                    _series["askprice"].Add((double)tick.AskPrice);
                    _series["asksize"].Add((double)tick.AskSize);
                    _series["bidprice"].Add((double)tick.BidPrice);
                    _series["bidsize"].Add((double)tick.BidSize);
                }
                _series["exchange"].Add(tick.Exchange);
                _series["suspicious"].Add(tick.Suspicious);
                _series["lastprice"].Add((double)tick.LastPrice);
                _series["quantity"].Add((double)tick.Quantity);
            }

            var data = baseData as DynamicData;
            if (data != null)
            {
                foreach (var kvp in data.GetStorageDictionary())
                {
                    var value = kvp.Value;
                    if (value is decimal) value = Convert.ToDouble(value);
                    _series[kvp.Key].Add(value);
                }
                _series["value"].Add((double)data.Value);
            }

            if (_type != null)
            {
                foreach (var kvp in _series)
                {
                    var item = _type.GetProperty(kvp.Key).GetValue(baseData);
                    kvp.Value.Add(item);
                }
            }
        }

        /// <summary>
        /// Adds a list of object to the end of the lists
        /// </summary>
        /// <param name="ticks">List of <see cref="Tick"/> objects that contains information to be saved in an instance of <see cref="PandasData"/></param>
        public void Add(IEnumerable<Tick> ticks)
        {
            foreach (var tick in ticks)
            {
                Add(tick);
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
                foreach (var kvp in _series)
                {
                    if (kvp.Value.Count != tuples.Length) continue;
                    pandasSeries.Add(kvp.Key, pandas.Series(kvp.Value, index));
                }
                return pandasSeries;
            }
        }
    }
}