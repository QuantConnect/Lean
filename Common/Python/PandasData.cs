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
using QuantConnect.Util;
using System;
using System.Collections;
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
        private static dynamic _pandas;
        private readonly static HashSet<string> _baseDataProperties = typeof(BaseData).GetProperties().ToHashSet(x => x.Name.ToLowerInvariant());

        private readonly int _levels;
        private readonly bool _isCustomData;
        private readonly Symbol _symbol;
        private readonly Dictionary<string, Tuple<List<DateTime>, List<object>>> _series;

        private readonly IEnumerable<MemberInfo> _members;

        /// <summary>
        /// Gets true if this is a custom data request, false for normal QC data
        /// </summary>
        public bool IsCustomData => _isCustomData;

        /// <summary>
        /// Implied levels of a multi index pandas.Series (depends on the security type)
        /// </summary>
        public int Levels => _levels;

        /// <summary>
        /// Initializes an instance of <see cref="PandasData"/>
        /// </summary>
        public PandasData(object data)
        {
            if (_pandas == null)
            {
                using (Py.GIL())
                {
                    _pandas = Py.Import("pandas");
                }
            }

            var enumerable = data as IEnumerable;
            if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    data = item;
                }
            }

            var type = data.GetType() as Type;
            _isCustomData = type.Namespace != "QuantConnect.Data.Market";
            _members = Enumerable.Empty<MemberInfo>();
            _symbol = (data as IBaseData)?.Symbol;

            _levels = 2;
            if (_symbol.SecurityType == SecurityType.Future) _levels = 3;
            if (_symbol.SecurityType == SecurityType.Option) _levels = 5;

            var columns = new List<string>
            {
                   "open",    "high",    "low",    "close", "lastprice",  "volume",
                "askopen", "askhigh", "asklow", "askclose",  "askprice", "asksize", "quantity", "suspicious",
                "bidopen", "bidhigh", "bidlow", "bidclose",  "bidprice", "bidsize", "exchange", "openinterest"
            };

            if (_isCustomData)
            {
                var keys = (data as DynamicData)?.GetStorageDictionary().Select(x => x.Key);

                // C# types that are not DynamicData type
                if (keys == null)
                {
                    var members = type.GetMembers().Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);

                    var duplicateKeys = members.GroupBy(x => x.Name.ToLowerInvariant()).Where(x => x.Count() > 1).Select(x => x.Key);
                    foreach (var duplicateKey in duplicateKeys)
                    {
                        throw new ArgumentException($"PandasData.ctor(): More than one \'{duplicateKey}\' member was found in \'{type.FullName}\' class.");
                    }

                    keys = members.Select(x => x.Name.ToLowerInvariant()).Except(_baseDataProperties).Concat(new[] { "value" });
                    _members = members.Where(x => keys.Contains(x.Name.ToLowerInvariant()));
                }

                columns.Add("value");
                columns.AddRange(keys);
            }

            _series = columns.Distinct().ToDictionary(k => k, v => Tuple.Create(new List<DateTime>(), new List<object>()));
        }

        /// <summary>
        /// Adds security data object to the end of the lists
        /// </summary>
        /// <param name="baseData"><see cref="IBaseData"/> object that contains security data</param>
        public void Add(object baseData)
        {
            foreach (var member in _members)
            {
                var key = member.Name.ToLowerInvariant();
                var endTime = (baseData as IBaseData).EndTime;
                AddToSeries(key, endTime, (member as FieldInfo)?.GetValue(baseData));
                AddToSeries(key, endTime, (member as PropertyInfo)?.GetValue(baseData));
            }

            var storage = (baseData as DynamicData)?.GetStorageDictionary();
            if (storage != null)
            {
                var endTime = (baseData as IBaseData).EndTime;
                var value = (baseData as IBaseData).Value;
                AddToSeries("value", endTime, value);

                foreach (var kvp in storage)
                {
                    AddToSeries(kvp.Key, endTime, kvp.Value);
                }
            }
            else
            {
                var ticks = new List<Tick> { baseData as Tick };
                var tradeBar = baseData as TradeBar;
                var quoteBar = baseData as QuoteBar;
                Add(ticks, tradeBar, quoteBar);
            }
        }

        /// <summary>
        /// Adds Lean data objects to the end of the lists
        /// </summary>
        /// <param name="ticks">List of <see cref="Tick"/> object that contains tick information of the security</param>
        /// <param name="tradeBar"><see cref="TradeBar"/> object that contains trade bar information of the security</param>
        /// <param name="quoteBar"><see cref="QuoteBar"/> object that contains quote bar information of the security</param>
        public void Add(IEnumerable<Tick> ticks, TradeBar tradeBar, QuoteBar quoteBar)
        {
            if (tradeBar != null)
            {
                var time = tradeBar.EndTime;
                AddToSeries("open", time, tradeBar.Open);
                AddToSeries("high", time, tradeBar.High);
                AddToSeries("low", time, tradeBar.Low);
                AddToSeries("close", time, tradeBar.Close);
                AddToSeries("volume", time, tradeBar.Volume);
            }
            if (quoteBar != null)
            {
                var time = quoteBar.EndTime;
                if (tradeBar == null)
                {
                    AddToSeries("open", time, quoteBar.Open);
                    AddToSeries("high", time, quoteBar.High);
                    AddToSeries("low", time, quoteBar.Low);
                    AddToSeries("close", time, quoteBar.Close);
                }
                if (quoteBar.Ask != null)
                {
                    AddToSeries("askopen", time, quoteBar.Ask.Open);
                    AddToSeries("askhigh", time, quoteBar.Ask.High);
                    AddToSeries("asklow", time, quoteBar.Ask.Low);
                    AddToSeries("askclose", time, quoteBar.Ask.Close);
                    AddToSeries("asksize", time, quoteBar.LastAskSize);
                }
                if (quoteBar.Bid != null)
                {
                    AddToSeries("bidopen", time, quoteBar.Bid.Open);
                    AddToSeries("bidhigh", time, quoteBar.Bid.High);
                    AddToSeries("bidlow", time, quoteBar.Bid.Low);
                    AddToSeries("bidclose", time, quoteBar.Bid.Close);
                    AddToSeries("bidsize", time, quoteBar.LastBidSize);
                }
            }
            if (ticks != null)
            {
                foreach (var tick in ticks)
                {
                    if (tick == null) continue;

                    var time = tick.EndTime;
                    var column = tick.TickType == TickType.OpenInterest
                        ? "openinterest"
                        : "lastprice";

                    if (tick.TickType == TickType.Quote)
                    {
                        AddToSeries("askprice", time, tick.AskPrice);
                        AddToSeries("asksize", time, tick.AskSize);
                        AddToSeries("bidprice", time, tick.BidPrice);
                        AddToSeries("bidsize", time, tick.BidSize);
                    }
                    AddToSeries("exchange", time, tick.Exchange);
                    AddToSeries("suspicious", time, tick.Suspicious);
                    AddToSeries("quantity", time, tick.Quantity);
                    AddToSeries(column, time, tick.LastPrice);
                }
            }
        }

        /// <summary>
        /// Get the pandas.DataFrame of the current <see cref="PandasData"/> state
        /// </summary>
        /// <param name="levels">Number of levels of the multi index</param>
        /// <returns>pandas.DataFrame object</returns>
        public PyObject ToPandasDataFrame(int levels = 2)
        {
            var empty = new PyString(string.Empty);
            var list = Enumerable.Repeat<PyObject>(empty, 5).ToList();
            list[3] = _symbol.ToString().ToPython();

            if (_symbol.SecurityType == SecurityType.Future)
            {
                list[0] = _symbol.ID.Date.ToPython();
                list[3] = _symbol.Value.ToPython();
            }
            if (_symbol.SecurityType == SecurityType.Option)
            {
                list[0] = _symbol.ID.Date.ToPython();
                list[1] = _symbol.ID.StrikePrice.ToPython();
                list[2] = _symbol.ID.OptionRight.ToString().ToPython();
                list[3] = _symbol.Value.ToPython();
            }

            // Create the index labels
            var names = "expiry,strike,type,symbol,time";
            if (levels == 2)
            {
                names = "symbol,time";
                list.RemoveRange(0, 3);
            }
            if (levels == 3)
            {
                names = "expiry,symbol,time";
                list.RemoveRange(1, 2);
            }

            Func<object, bool> filter = x =>
            {
                var isNaNOrZero = x is double && ((double)x).IsNaNOrZero();
                var isNullOrWhiteSpace = x is string && string.IsNullOrWhiteSpace((string)x);
                var isFalse = x is bool && !(bool)x;
                return x == null || isNaNOrZero || isNullOrWhiteSpace || isFalse;
            };
            Func<DateTime, PyTuple> selector = x =>
            {
                list[list.Count - 1] = x.ToPython();
                return new PyTuple(list.ToArray());
            };
            // creating the pandas MultiIndex is expensive so we keep a cash
            var indexCache = new Dictionary<List<DateTime>, dynamic>(new ListComparer<DateTime>());
            using (Py.GIL())
            {
                // Returns a dictionary keyed by column name where values are pandas.Series objects
                var pyDict = new PyDict();
                var splitNames = names.Split(',');
                foreach (var kvp in _series)
                {
                    var values = kvp.Value.Item2;
                    if (values.All(filter)) continue;

                    dynamic index;
                    if (!indexCache.TryGetValue(kvp.Value.Item1, out index))
                    {
                        var tuples = kvp.Value.Item1.Select(selector).ToArray();
                        index = _pandas.MultiIndex.from_tuples(tuples, names: splitNames);
                        indexCache[kvp.Value.Item1] = index;
                    }

                    pyDict.SetItem(kvp.Key, _pandas.Series(values, index));
                }
                _series.Clear();
                return _pandas.DataFrame(pyDict);
            }
        }

        /// <summary>
        /// Adds data to dictionary
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="time"><see cref="DateTime"/> object to add to the value associated with the specific key</param>
        /// <param name="input"><see cref="Object"/> to add to the value associated with the specific key</param>
        private void AddToSeries(string key, DateTime time, object input)
        {
            if (input == null) return;

            Tuple<List<DateTime>, List<object>> value;
            if (_series.TryGetValue(key, out value))
            {
                value.Item1.Add(time);
                value.Item2.Add(input is decimal ? input.ConvertInvariant<double>() : input);
            }
            else
            {
                throw new ArgumentException($"PandasData.AddToSeries(): {key} key does not exist in series dictionary.");
            }
        }
    }
}