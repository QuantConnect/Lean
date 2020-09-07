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

using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Memory;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace QuantConnect.Python
{
    /// <summary>
    /// Collection of methods that converts lists of objects in pandas.DataFrame
    /// </summary>
    public class PandasConverter
    {
        private static dynamic _pandas;

        /// <summary>
        /// Creates an instance of <see cref="PandasConverter"/>.
        /// </summary>
        public PandasConverter()
        {
            if (_pandas == null)
            {
                using (Py.GIL())
                {
                    _pandas = Py.Import("pandas");
                }
            }
        }

        /// <summary>
        /// Converts an enumerable of <see cref="Slice"/> in a pandas.DataFrame
        /// </summary>
        /// <param name="data">Enumerable of <see cref="Slice"/></param>
        /// <returns><see cref="PyObject"/> containing a pandas.DataFrame</returns>
        public PyObject GetDataFrame(IEnumerable<Slice> data)
        {
            /*
            var maxLevels = 0;
            var sliceDataDict = new Dictionary<Symbol, PandasData>();

            foreach (var slice in data)
            {
                foreach (var key in slice.Keys)
                {
                    var baseData = slice[key];

                    PandasData value;
                    if (!sliceDataDict.TryGetValue(key, out value))
                    {
                        sliceDataDict.Add(key, value = new PandasData(baseData));
                        maxLevels = Math.Max(maxLevels, value.Levels);
                    }

                    if (value.IsCustomData)
                    {
                        value.Add(baseData);
                    }
                    else
                    {
                        var ticks = slice.Ticks.ContainsKey(key) ? slice.Ticks[key] : null;
                        var tradeBars = slice.Bars.ContainsKey(key) ? slice.Bars[key] : null;
                        var quoteBars = slice.QuoteBars.ContainsKey(key) ? slice.QuoteBars[key] : null;
                        value.Add(ticks, tradeBars, quoteBars);
                    }
                }
            }

            using (Py.GIL())
            {
                if (sliceDataDict.Count == 0)
                {
                    return _pandas.DataFrame();
                }
                var dataFrames = sliceDataDict.Select(x => x.Value.ToPandasDataFrame(maxLevels));
                return _pandas.concat(dataFrames.ToArray(), Py.kw("sort", true));
            }
            */
            var allocator = new NativeMemoryAllocator();
            var recordBatchBuilder = new RecordBatch.Builder(allocator);

            var symbols = new List<string>();
            var times = new List<DateTimeOffset>();
            var tradeBarOpen = new List<double>();
            var tradeBarHigh = new List<double>();
            var tradeBarLow = new List<double>();
            var tradeBarClose = new List<double>();
            var tradeBarVolume = new List<double>();
           
            var quoteBarBidOpen = new List<double>();
            var quoteBarBidHigh = new List<double>();
            var quoteBarBidLow = new List<double>();
            var quoteBarBidClose = new List<double>();  
            var quoteBarBidVolume = new List<double>();   
            var quoteBarAskOpen = new List<double>();
            var quoteBarAskHigh = new List<double>();
            var quoteBarAskLow = new List<double>();
            var quoteBarAskClose = new List<double>();  
            var quoteBarAskVolume = new List<double>();   
            
            foreach (var slice in data)
            {
                foreach (var symbol in slice.Keys)
                {
                    //var ticks = slice.Ticks.ContainsKey(symbol) ? slice.Ticks[symbol] : null;
                    var tradeBar = slice.Bars.ContainsKey(symbol) ? slice.Bars[symbol] : null;
                    var quoteBar = slice.QuoteBars.ContainsKey(symbol) ? slice.QuoteBars[symbol] : null;

                    if (tradeBar != null)
                    {
                        tradeBarOpen.Add((double) tradeBar.Open);
                        tradeBarHigh.Add((double) tradeBar.High);
                        tradeBarLow.Add((double) tradeBar.Low);
                        tradeBarClose.Add((double) tradeBar.Close);
                        tradeBarVolume.Add((double) tradeBar.Volume);
                        symbols.Add(tradeBar.Symbol.ID.ToString());
                        times.Add(new DateTimeOffset(tradeBar.EndTime));
                    }

                    if (quoteBar != null)
                    {
                        if (quoteBar.Bid != null)
                        {
                            quoteBarBidOpen.Add((double) quoteBar.Bid.Open);
                            quoteBarBidHigh.Add((double) quoteBar.Bid.High);
                            quoteBarBidLow.Add((double) quoteBar.Bid.Low);
                            quoteBarBidClose.Add((double) quoteBar.Bid.Close);
                            quoteBarBidVolume.Add((double) quoteBar.LastBidSize);
                        }
                        else
                        {
                            quoteBarBidOpen.Add(double.NaN);
                            quoteBarBidHigh.Add(double.NaN);
                            quoteBarBidLow.Add(double.NaN);
                            quoteBarBidClose.Add(double.NaN);
                            quoteBarBidVolume.Add(double.NaN);
                        }

                        if (quoteBar.Ask != null)
                        {
                            quoteBarAskOpen.Add((double) quoteBar.Open);
                            quoteBarAskHigh.Add((double) quoteBar.High);
                            quoteBarAskLow.Add((double) quoteBar.Low);
                            quoteBarAskClose.Add((double) quoteBar.Close);
                            quoteBarAskVolume.Add((double) quoteBar.LastAskSize);
                        }
                        else
                        {
                            quoteBarAskOpen.Add(double.NaN);
                            quoteBarAskHigh.Add(double.NaN);
                            quoteBarAskLow.Add(double.NaN);
                            quoteBarAskClose.Add(double.NaN);
                            quoteBarAskVolume.Add(double.NaN);
                        }

                        //symbols.Add(quoteBar.Symbol.ID.ToString());
                        //times.Add(new DateTimeOffset(quoteBar.EndTime));
                    }
                }
            }

            recordBatchBuilder.Append("time", false, col => col.Timestamp(array => array.AppendRange(times)));
            recordBatchBuilder.Append("open", true, col => col.Double(array => array.AppendRange(tradeBarOpen)));
            recordBatchBuilder.Append("high", true, col => col.Double(array => array.AppendRange(tradeBarHigh)));
            recordBatchBuilder.Append("low", true, col => col.Double(array => array.AppendRange(tradeBarLow)));
            recordBatchBuilder.Append("close", true, col => col.Double(array => array.AppendRange(tradeBarClose)));
            recordBatchBuilder.Append("volume", true, col => col.Double(array => array.AppendRange(tradeBarVolume)));
            recordBatchBuilder.Append("symbol", true, col => col.String(array => array.AppendRange(symbols)));
            //recordBatchBuilder.Append("bidopen", true, col => col.Double(array => array.AppendRange(quoteBarBidOpen)));
            //recordBatchBuilder.Append("bidhigh", true, col => col.Double(array => array.AppendRange(quoteBarBidHigh)));
            //recordBatchBuilder.Append("bidlow", true, col => col.Double(array => array.AppendRange(quoteBarBidLow)));
            //recordBatchBuilder.Append("bidclose", true, col => col.Double(array => array.AppendRange(quoteBarBidClose)));
            //recordBatchBuilder.Append("bidsize", true, col => col.Double(array => array.AppendRange(quoteBarBidVolume)));
            //recordBatchBuilder.Append("askopen", true, col => col.Double(array => array.AppendRange(quoteBarAskOpen)));
            //recordBatchBuilder.Append("askhigh", true, col => col.Double(array => array.AppendRange(quoteBarAskHigh)));
            //recordBatchBuilder.Append("asklow", true, col => col.Double(array => array.AppendRange(quoteBarAskLow)));
            //recordBatchBuilder.Append("askclose", true, col => col.Double(array => array.AppendRange(quoteBarAskClose)));
            //recordBatchBuilder.Append("asksize", true, col => col.Double(array => array.AppendRange(quoteBarAskVolume)));

            var recordBatch = recordBatchBuilder.Build();
            var ms = new MemoryStream();
            using (var writer = new ArrowStreamWriter(ms, recordBatch.Schema))
            {
                writer.WriteRecordBatchAsync(recordBatch).SynchronouslyAwaitTask();
                
                var rawBytes = ms.ToArray();
                var arrowBuffer = new ArrowBuffer(rawBytes);

                using (Py.GIL())
                {
                    dynamic pa = PythonEngine.ImportModule("pyarrow");
                    var pinned = arrowBuffer.Memory.Pin();
                    unsafe
                    {
                        Console.WriteLine(recordBatch.Length);
                        Console.WriteLine(rawBytes.Length);
                        Console.WriteLine(arrowBuffer.Length);
                        
                        dynamic buf = pa.foreign_buffer(((long) (new IntPtr(pinned.Pointer))).ToPython(), arrowBuffer.Length.ToPython());
                        return pa.ipc.open_stream(buf).read_pandas();
                    }
                }
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

            using (Py.GIL())
            {
                // If sliceData is still null, data is an empty enumerable
                // returns an empty pandas.DataFrame
                if (sliceData == null)
                {
                    return _pandas.DataFrame();
                }
                return sliceData.ToPandasDataFrame();
            }
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
                    pyDict.SetItem(kvp.Key.ToLowerInvariant(), _pandas.Series(values, index));
                }

                return _pandas.DataFrame(pyDict, columns: data.Keys.Select(x => x.ToLowerInvariant()).OrderBy(x => x));
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