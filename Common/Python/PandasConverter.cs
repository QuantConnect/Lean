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
            var symbols = data.SelectMany(x => x.Keys).Distinct().OrderBy(x => x.Value);

            // If data contains derivatives and its underlying, 
            // we get the underlying to exclude it from the dataframe 
            Symbol underlying = null;
            var derivatives = symbols.Where(x => x.HasUnderlying);
            if (derivatives.Count() > 0)
            {
                underlying = derivatives.First().Underlying;
            }
            
            using (Py.GIL())
            {
                var dataFrame = _pandas.DataFrame();

                foreach (var symbol in symbols)
                {
                    if (symbol == underlying)
                    {
                        continue;
                    }

                    var items = new PyObject[]
                    {
                        dataFrame,
                        GetDataFrame(data.Get<QuoteBar>(symbol)),
                        GetDataFrame(data.Get<TradeBar>(symbol))
                    };

                    dataFrame = _pandas.concat(new PyList(items));
                }

                return dataFrame;
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="IBaseDataBar"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame<T>(IEnumerable<T> data)
            where T : IBaseDataBar
        {
            if (data.Count() == 0)
            {
                return _pandas.DataFrame();
            }

            using (Py.GIL())
            {
                var index = CreateIndex(data.First().Symbol, data.Select(x => x.Time));

                var pyDict = new PyDict();
                
                pyDict.SetItem("low", _pandas.Series(data.Select(x => (double)x.Low).ToList(), index));
                pyDict.SetItem("open", _pandas.Series(data.Select(x => (double)x.Open).ToList(), index));
                pyDict.SetItem("high", _pandas.Series(data.Select(x => (double)x.High).ToList(), index));
                pyDict.SetItem("close", _pandas.Series(data.Select(x => (double)x.Close).ToList(), index));

                if (typeof(T) == typeof(TradeBar))
                {
                    Func<IBaseDataBar, double> getVolume = x => { var bar = x as TradeBar; return (double)bar.Volume; };
                    pyDict.SetItem("volume", _pandas.Series(data.Select(x => getVolume(x)).ToList(), index));
                }

                if (typeof(T) == typeof(QuoteBar))
                {
                    Func<IBaseDataBar, QuoteBar> toQuoteBar = x => x as QuoteBar;                   
                    pyDict.SetItem("askopen", _pandas.Series(data.Select(x => { return toQuoteBar(x).Ask == null ? double.NaN : (double)toQuoteBar(x).Ask.Open; }).ToList(), index));
                    pyDict.SetItem("bidopen", _pandas.Series(data.Select(x => { return toQuoteBar(x).Bid == null ? double.NaN : (double)toQuoteBar(x).Bid.Open; }).ToList(), index));
                    pyDict.SetItem("askhigh", _pandas.Series(data.Select(x => { return toQuoteBar(x).Ask == null ? double.NaN : (double)toQuoteBar(x).Ask.High; }).ToList(), index));
                    pyDict.SetItem("bidhigh", _pandas.Series(data.Select(x => { return toQuoteBar(x).Bid == null ? double.NaN : (double)toQuoteBar(x).Bid.High; }).ToList(), index));
                    pyDict.SetItem("asklow", _pandas.Series(data.Select(x => { return toQuoteBar(x).Ask == null ? double.NaN : (double)toQuoteBar(x).Ask.Low; }).ToList(), index));
                    pyDict.SetItem("bidlow", _pandas.Series(data.Select(x => { return toQuoteBar(x).Bid == null ? double.NaN : (double)toQuoteBar(x).Bid.Low; }).ToList(), index));
                    pyDict.SetItem("askclose", _pandas.Series(data.Select(x => { return toQuoteBar(x).Ask == null ? double.NaN : (double)toQuoteBar(x).Ask.Close; }).ToList(), index));
                    pyDict.SetItem("bidclose", _pandas.Series(data.Select(x => { return toQuoteBar(x).Bid == null ? double.NaN : (double)toQuoteBar(x).Bid.Close; }).ToList(), index));
                    pyDict.SetItem("asksize", _pandas.Series(data.Select(x => (double)toQuoteBar(x).LastAskSize).ToList(), index));
                    pyDict.SetItem("bidsize", _pandas.Series(data.Select(x => (double)toQuoteBar(x).LastBidSize).ToList(), index));
                }

                return _pandas.DataFrame(pyDict);
            }
        }

        /// <summary>
        /// Creates the index of pandas.Series
        /// </summary>
        /// <param name="symbol"><see cref="Symbol"/> of the security</param>
        /// <param name="time">Time series axis</param>
        /// <returns><see cref="PyObject"/> containing a pandas.MultiIndex</returns>
        private PyObject CreateIndex(Symbol symbol, IEnumerable<DateTime> time)
        {
            var value = (symbol.HasUnderlying ? symbol.Value : symbol.ToString()).ToPython();
            var tuples = time.Select(x => new PyTuple(new PyObject[] { value, x.ToPython() }));
            var names = "symbol,time";

            if (symbol.SecurityType == SecurityType.Future)
            {
                tuples = time.Select(x => new PyTuple(new PyObject[] { symbol.ID.Date.ToPython(), value, x.ToPython() }));
                names = "expiry," + names;
            }

            if (symbol.SecurityType == SecurityType.Option)
            {
                tuples = time.Select(x => new PyTuple(new PyObject[] { symbol.ID.Date.ToPython(), symbol.ID.StrikePrice.ToPython(), symbol.ID.OptionRight.ToString().ToPython(), value, x.ToPython() }));
                names = "expiry,strike,type," + names;
            }

            return _pandas.MultiIndex.from_tuples(tuples.ToArray(), names: names.Split(','));
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
                    var index = kvp.Value.Select(x => x.EndTime).ToList();
                    var values = kvp.Value.Select(x => (double)x.Value).ToList();
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
}