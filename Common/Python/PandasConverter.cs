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
using System.Threading.Tasks;
using Apache.Arrow.Types;
using QuantConnect.Securities;

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
            var mhdb = MarketHoursDatabase.FromDataFolder();

            var tradeBarSymbols = new StringArray.Builder();
            var tradeBarTimes = new TimestampArray.Builder();
            var tradeBarOpen = new DoubleArray.Builder();
            var tradeBarHigh = new DoubleArray.Builder();
            var tradeBarLow = new DoubleArray.Builder();
            var tradeBarClose = new DoubleArray.Builder();
            var tradeBarVolume = new DoubleArray.Builder();

            var quoteBarSymbols = new StringArray.Builder();
            var quoteBarTimes = new TimestampArray.Builder();
            var quoteBarBidOpen = new DoubleArray.Builder();
            var quoteBarBidHigh = new DoubleArray.Builder();
            var quoteBarBidLow = new DoubleArray.Builder();
            var quoteBarBidClose = new DoubleArray.Builder();  
            var quoteBarBidVolume = new DoubleArray.Builder();   
            var quoteBarAskOpen = new DoubleArray.Builder();
            var quoteBarAskHigh = new DoubleArray.Builder();
            var quoteBarAskLow = new DoubleArray.Builder();
            var quoteBarAskClose = new DoubleArray.Builder();  
            var quoteBarAskVolume = new DoubleArray.Builder();

            var tickSymbols = new StringArray.Builder();
            var tickExchange = new StringArray.Builder();
            var hasExchange = false;
            var tickSuspicious = new BooleanArray.Builder();
            var hasSuspicious = false;

            var tickHasTrades = false;
            var tickTimes = new TimestampArray.Builder();
            var tickValue = new DoubleArray.Builder();
            var tickQuantity = new DoubleArray.Builder();
            var tickHasQuotes = false;
            var tickBidPrice = new DoubleArray.Builder();
            var tickBidSize = new DoubleArray.Builder();
            var tickAskPrice = new DoubleArray.Builder();
            var tickAskSize = new DoubleArray.Builder();

            var openInterestTimes = new TimestampArray.Builder();
            var openInterestSymbols = new StringArray.Builder();
            var openInterestValue = new DoubleArray.Builder();
            
            foreach (var slice in data)
            {
                foreach (var symbol in slice.Keys)
                {
                    var tradeBar = slice.Bars.ContainsKey(symbol) ? slice.Bars[symbol] : null;
                    var quoteBar = slice.QuoteBars.ContainsKey(symbol) ? slice.QuoteBars[symbol] : null;
                    var ticks = slice.Ticks.ContainsKey(symbol) ? slice.Ticks[symbol] : null;
                    
                    var sid = symbol.ID.ToString();
                    
                    if (tradeBar != null)
                    {
                        tradeBarOpen.Append((double) tradeBar.Open);
                        tradeBarHigh.Append((double) tradeBar.High);
                        tradeBarLow.Append((double) tradeBar.Low);
                        tradeBarClose.Append((double) tradeBar.Close);
                        tradeBarVolume.Append((double) tradeBar.Volume);

                        tradeBarSymbols.Append(sid);
                        tradeBarTimes.Append(new DateTimeOffset(tradeBar.EndTime, mhdb.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType).
                    }

                    if (quoteBar != null)
                    {
                        if (quoteBar.Bid != null)
                        {
                            quoteBarBidOpen.Append((double) quoteBar.Bid.Open);
                            quoteBarBidHigh.Append((double) quoteBar.Bid.High);
                            quoteBarBidLow.Append((double) quoteBar.Bid.Low);
                            quoteBarBidClose.Append((double) quoteBar.Bid.Close);
                            quoteBarBidVolume.Append((double) quoteBar.LastBidSize);
                        }
                        else
                        {
                            quoteBarBidOpen.Append(double.NaN);
                            quoteBarBidHigh.Append(double.NaN);
                            quoteBarBidLow.Append(double.NaN);
                            quoteBarBidClose.Append(double.NaN);
                            quoteBarBidVolume.Append(double.NaN);
                        }

                        if (quoteBar.Ask != null)
                        {
                            quoteBarAskOpen.Append((double) quoteBar.Ask.Open);
                            quoteBarAskHigh.Append((double) quoteBar.Ask.High);
                            quoteBarAskLow.Append((double) quoteBar.Ask.Low);
                            quoteBarAskClose.Append((double) quoteBar.Ask.Close);
                            quoteBarAskVolume.Append((double) quoteBar.LastAskSize);
                        }
                        else
                        {
                            quoteBarAskOpen.Append(double.NaN);
                            quoteBarAskHigh.Append(double.NaN);
                            quoteBarAskLow.Append(double.NaN);
                            quoteBarAskClose.Append(double.NaN);
                            quoteBarAskVolume.Append(double.NaN);
                        }

                        quoteBarSymbols.Append(quoteBar.Symbol.ID.ToString());
                        quoteBarTimes.Append(new DateTimeOffset(quoteBar.EndTime));
                    }

                    if (ticks != null)
                    {
                        foreach (var tick in ticks)
                        {
                            if (tick.TickType == TickType.Trade || tick.TickType == TickType.Quote)
                            {
                                tickSymbols.Append(sid);
                                tickTimes.Append(new DateTimeOffset(tick.EndTime));
                            }

                            if (tick.TickType == TickType.Trade)
                            {
                                tickHasTrades = true;
                                
                                tickValue.Append((double) tick.Value);
                                tickQuantity.Append((double) tick.Quantity);

                                if (tick.Suspicious && !hasSuspicious)
                                {
                                    hasSuspicious = true;
                                }
                                if (!string.IsNullOrWhiteSpace(tick.Exchange) && !hasExchange)
                                {
                                    hasExchange = true;
                                }
                                
                                tickSuspicious.Append(tick.Suspicious);
                                tickExchange.Append(tick.Exchange);

                                tickBidPrice.Append(double.NaN);
                                tickBidSize.Append(double.NaN);
                                tickAskPrice.Append(double.NaN);
                                tickAskSize.Append(double.NaN);
                            }
                            else if (tick.TickType == TickType.Quote)
                            {
                                tickHasQuotes = true;
                                
                                tickValue.Append(double.NaN);
                                tickQuantity.Append(double.NaN);
                                
                                tickSuspicious.Append(tick.Suspicious);
                                tickExchange.Append(tick.Exchange);

                                tickBidPrice.Append((double) tick.BidPrice);
                                tickBidSize.Append((double) tick.BidSize);
                                tickAskPrice.Append((double) tick.AskPrice);
                                tickAskSize.Append((double) tick.AskSize);
                            }
                            else
                            {
                                openInterestTimes.Append(new DateTimeOffset(tick.EndTime));
                                openInterestSymbols.Append(sid);
                                openInterestValue.Append((double)tick.Value);
                            }
                        }
                    }
                }
            }

            var recordBatches = new List<RecordBatch>();
            
            if (tradeBarTimes.Length != 0)
            {
                var tradeBarRecordBatchBuilder = new RecordBatch.Builder(allocator);
                
                tradeBarRecordBatchBuilder.Append("time", false, tradeBarTimes.Build());
                tradeBarRecordBatchBuilder.Append("symbol", true, tradeBarSymbols.Build());
                tradeBarRecordBatchBuilder.Append("open", true, tradeBarOpen.Build(allocator));
                tradeBarRecordBatchBuilder.Append("high", true, tradeBarHigh.Build(allocator));
                tradeBarRecordBatchBuilder.Append("low", true, tradeBarLow.Build(allocator));
                tradeBarRecordBatchBuilder.Append("close", true, tradeBarClose.Build(allocator));
                tradeBarRecordBatchBuilder.Append("volume", true, tradeBarVolume.Build(allocator));
                
                recordBatches.Add(tradeBarRecordBatchBuilder.Build());
            }

            if (quoteBarTimes.Length != 0)
            {
                var quoteBarRecordBatchBuilder = new RecordBatch.Builder(allocator);
                
                quoteBarRecordBatchBuilder.Append("time", false, quoteBarTimes.Build());
                quoteBarRecordBatchBuilder.Append("symbol", true, quoteBarSymbols.Build());
                quoteBarRecordBatchBuilder.Append("bidopen", true, quoteBarBidOpen.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidhigh", true, quoteBarBidHigh.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidlow", true, quoteBarBidLow.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidclose", true, quoteBarBidClose.Build(allocator));
                quoteBarRecordBatchBuilder.Append("bidsize", true, quoteBarBidVolume.Build(allocator));
                quoteBarRecordBatchBuilder.Append("askopen", true, quoteBarAskOpen.Build(allocator));
                quoteBarRecordBatchBuilder.Append("askhigh", true, quoteBarAskHigh.Build(allocator));
                quoteBarRecordBatchBuilder.Append("asklow", true, quoteBarAskLow.Build(allocator));
                quoteBarRecordBatchBuilder.Append("askclose", true, quoteBarAskClose.Build(allocator));
                quoteBarRecordBatchBuilder.Append("asksize", true, quoteBarAskVolume.Build(allocator));
                
                recordBatches.Add(quoteBarRecordBatchBuilder.Build());
            }

            if (tickTimes.Length != 0)
            {
                var tickRecordBatchBuilder = new RecordBatch.Builder();

                tickRecordBatchBuilder.Append("time", false, tickTimes.Build());
                tickRecordBatchBuilder.Append("symbol", false, tickSymbols.Build());

                if (tickHasTrades || tickHasQuotes)
                {
                    if (hasSuspicious)
                    {
                        tickRecordBatchBuilder.Append("suspicious", true, tickSuspicious.Build());
                    }
                    if (hasExchange)
                    {
                        tickRecordBatchBuilder.Append("exchange", true, tickExchange.Build());
                    }

                    if (tickHasTrades)
                    {
                        tickRecordBatchBuilder.Append("lastprice", tickHasQuotes, tickValue.Build());
                        tickRecordBatchBuilder.Append("quantity", tickHasQuotes, tickQuantity.Build());
                    }

                    if (tickHasQuotes)
                    {
                        tickRecordBatchBuilder.Append("bidprice", tickHasTrades, tickBidPrice.Build());
                        tickRecordBatchBuilder.Append("bidsize", tickHasTrades, tickBidSize.Build());
                        tickRecordBatchBuilder.Append("askprice", tickHasTrades, tickAskPrice.Build());
                        tickRecordBatchBuilder.Append("asksize", tickHasTrades, tickAskSize.Build());
                    }
                }
                
                recordBatches.Add(tickRecordBatchBuilder.Build());
            }

            if (openInterestTimes.Length != 0)
            {
                var openInterestBatchBuilder = new RecordBatch.Builder();

                openInterestBatchBuilder.Append("time", false, openInterestTimes.Build());
                openInterestBatchBuilder.Append("symbol", false, openInterestSymbols.Build());
                openInterestBatchBuilder.Append("openinterest", false, openInterestValue.Build());
                
                recordBatches.Add(openInterestBatchBuilder.Build());
            }

            if (recordBatches.Count == 0)
            {
                return null;
            }

            using (Py.GIL())
            {
                dynamic pa = PythonEngine.ImportModule("pyarrow");
                var dataFrames = new List<dynamic>();
                foreach (var recordBatch in recordBatches)
                {
                    using (var ms = new MemoryStream(0))
                    using (var writer = new ArrowStreamWriter(ms, recordBatch.Schema))
                    {
                        writer.WriteRecordBatchAsync(recordBatch).SynchronouslyAwaitTask();
                        using (var arrowBuffer = new ArrowBuffer(ms.GetBuffer()))
                        {
                            unsafe
                            {
                                var pinned = arrowBuffer.Memory.Pin();

                                dynamic buf = pa.foreign_buffer(
                                    ((long) new IntPtr(pinned.Pointer)).ToPython(),
                                    arrowBuffer.Length.ToPython()
                                );
                                dynamic df = pa.ipc.open_stream(buf).read_pandas(
                                    Py.kw("split_blocks", true),
                                    Py.kw("self_destruct", true)
                                );
                                df.set_index("symbol", Py.kw("inplace", true));
                                df.set_index("time", Py.kw("append", true), Py.kw("inplace", true));

                                dataFrames.Add(df);
                            }
                        }
                    }
                }

                dynamic final_df = null;
                foreach (var dataFrame in dataFrames)
                {
                    if (final_df == null)
                    {
                        final_df = dataFrame;
                        continue;
                    }

                    final_df = final_df.join(dataFrame, Py.kw("how", "outer"));
                }

                final_df.sort_index(Py.kw("inplace", true));
                return final_df;
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