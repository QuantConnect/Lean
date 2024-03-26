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
 *
*/

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public partial class PandasConverterTests
    {
        [Test, TestCaseSource(nameof(TestDataFrameNonExceptionFunctions))]
        public void BackwardsCompatibilityDataFrameDataFrameNonExceptionFunctions(string method, string index, bool cache)
        {
            if(method == ".to_orc()")
            {
                if (OS.IsWindows)
                {
                    // not supported in windows
                    return;
                }
                // orc does not support serializing a non-default index for the index; you can .reset_index() to make the index into column(s)
                method = $".reset_index(){method}";
            }
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    $@"
def Test(df, symbol):
    df = df.lastprice.unstack(level=0){method}").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test, TestCaseSource(nameof(TestDataFrameParameterlessFunctions))]
        public void BackwardsCompatibilityDataFrameParameterlessFunctions(string method, string index, bool cache)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    $@"
def Test(df, symbol):
    df = df.lastprice.unstack(level=0){method}
    # If not DataFrame, return
    if not hasattr(df, 'columns'):
        return
    if df.iloc[-1][{index}] is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test, TestCaseSource(nameof(TestDataFrameOtherParameterFunctions))]
        public void BackwardsCompatibilityDataFrameOtherParameterFunctions(string method, string index, bool cache)
        {   
            // Cannot compare non identically indexed dataframes
            if (method == ".compare(other)" && _newerPandas)
            {
                return;
            }

            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    $@"
def Test(df, other, symbol):
    df = df{method}
    df = df.lastprice.unstack(level=0)
    # If not DataFrame, return
    if not hasattr(df, 'columns'):
        return
    if df.iloc[-1][{index}] is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        [Test, TestCaseSource(nameof(TestSeriesNonExceptionFunctions))]
        public void BackwardsCompatibilitySeriesNonExceptionFunctions(string method, string index, bool cache)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    $@"
def Test(df, symbol):
    series = df.lastprice
    series = series{method}").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test, TestCaseSource(nameof(TestSeriesParameterlessFunctions))]
        public void BackwardsCompatibilitySeriesParameterlessFunctions(string method, string index, bool cache)
        {
            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    $@"
def Test(df, symbol):
    series = df.lastprice
    series = series{method}
    # If not Series, return
    if not hasattr(series, 'index') or type(series) is tuple:
        return
    if series.loc[{index}].iloc[-1] is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), Symbols.SPY));
            }
        }

        [Test, TestCaseSource(nameof(TestSeriesOtherParameterFunctions))]
        public void BackwardsCompatibilitySeriesOtherParameterFunctions(string method, string index, bool cache)
        {
            // Cannot compare non identically indexed dataframes
            if (method == ".compare(other)" && _newerPandas)
            {
                return;
            }

            if (cache) SymbolCache.Set("SPY", Symbols.SPY);

            using (Py.GIL())
            {
                dynamic test = PyModule.FromString("testModule",
                    $@"
def Test(df, other, symbol):
    series, other = other.lastprice, df.lastprice
    series = series{method}
    # If not Series, return
    if not hasattr(series, 'index') or type(series) is tuple:
        return
    if series.loc[{index}].iloc[-1] is 0:
        raise Exception('Data is zero')").GetAttr("Test");

                Assert.DoesNotThrow(() => test(GetTestDataFrame(Symbols.SPY), GetTestDataFrame(Symbols.AAPL), Symbols.SPY));
            }
        }

        private static TestCaseData[] TestDataFrameNonExceptionFunctions
        {
            get
            {
                var functions = new[]
                {
                    ".agg('mean', axis=0)",
                    ".aggregate('mean', axis=0)",
                    ".clip(100, 200)",
                    ".fillna(value=999)",
                    ".first('2S')",
                    ".isin([100])",
                    ".last('2S')",
                    ".melt()"
                };
                
                if (!IsNewerPandas()){
                    var additionalFunctions = new[]
                    {
                    ".clip_lower(100)",
                    ".clip_upper(200)",
                    };
                    functions.Concat(additionalFunctions);
                }

                var testCases = functions.SelectMany(x => new[]
                {
                    new TestCaseData(x, "'SPY'", true),
                    new TestCaseData(x, "symbol", false),
                    new TestCaseData(x, "str(symbol.ID)", false)
                }).ToList();

                testCases.AddRange(_parameterlessFunctions["DataFrame"]);
                return testCases.ToArray();
            }
        }

        private static TestCaseData[] TestSeriesNonExceptionFunctions
        {
            get
            {
                var functions = new[]
                {
                    ".add_suffix('lean')",
                    ".add_prefix('lean')",
                    ".agg('mean', axis=0)",
                    ".aggregate('mean', axis=0)",
                    ".clip(100, 200)",
                    ".fillna(value=999)",
                    ".isin([100])",
                    ".searchsorted(200)",
                    ".value_counts()"
                };

                if (!IsNewerPandas()){
                    var additionalFunctions = new[]
                    {
                    ".clip_lower(100)",
                    ".clip_upper(200)",
                    };
                    functions.Concat(additionalFunctions);
                }


                var testCases = functions.SelectMany(x => new[]
                {
                    new TestCaseData(x, "'SPY'", true),
                    new TestCaseData(x, "symbol", false),
                    new TestCaseData(x, "str(symbol.ID)", false)
                })
                .ToList();

                testCases.AddRange(_parameterlessFunctions["Series"]);
                return testCases.ToArray();
            }
        }

        private static TestCaseData[] TestDataFrameParameterlessFunctions => _parameterlessFunctions["DataFrameParameterless"];

        private static TestCaseData[] TestSeriesParameterlessFunctions => _parameterlessFunctions["SeriesParameterless"];

        private static TestCaseData[] TestDataFrameOtherParameterFunctions
        {
            get
            {
                var functions = new[]
                {
                    "+",
                    "-",
                    "/",
                    "*",
                    "%",
                    "**",
                    "//"
                }
                .SelectMany(x => new[]
                {
                    new TestCaseData($" {x} other", "'SPY'", true),
                    new TestCaseData($" {x} other", "symbol", false),
                    new TestCaseData($" {x} other", "str(symbol.ID)", false)
                }).ToList();

                functions.AddRange(_parameterlessFunctions["DataFrameOtherParameter"]);
                return functions.ToArray();
            }
        }

        private static TestCaseData[] TestSeriesOtherParameterFunctions
        {
            get
            {
                var functions = new[]
                {
                    "+",
                    "-",
                    "/",
                    "*",
                    "%",
                    "**",
                    "//"
                }
                .SelectMany(x => new[]
                {
                    new TestCaseData($" {x} other", "'SPY'", true),
                    new TestCaseData($" {x} other", "symbol", false),
                    new TestCaseData($" {x} other", "str(symbol.ID)", false)
                }).ToList();

                functions.AddRange(_parameterlessFunctions["SeriesOtherParameter"]);
                return functions.ToArray();
            }
        }

        private static Dictionary<string, TestCaseData[]> _parameterlessFunctions = GetParameterlessFunctions();

        private static Dictionary<string, TestCaseData[]> GetParameterlessFunctions()
        {
            // Initialize the Python engine and begin allow thread
            PythonInitializer.Initialize();

            var functionsByType = new Dictionary<string, TestCaseData[]>();

            using (Py.GIL())
            {
                var module = PyModule.FromString("Test",
                    @"import pandas
from inspect import getmembers, isfunction, signature

skipped = [ 'boxplot', 'hist', 'plot',        # <- Graphics
    'agg', 'aggregate', 'align', 'bool','combine', 'corrwith', 'dot', 'drop',
    'equals', 'ewm', 'fillna', 'filter', 'groupby', 'join', 'mask', 'melt',
    'pivot', 'pivot_table', 'reindex_like', 'rename', 'reset_index', 'select_dtypes',
    'slice_shift', 'swaplevel', 'to_clipboard', 'to_excel', 'to_feather', 'to_gbq',
    'to_hdf', 'to_list', 'tolist', 'to_parquet', 'to_period', 'to_pickle', 'to_sql', 'to_xml',
    'to_stata', 'to_timestamp', 'to_xarray', 'tshift', 'update', 'value_counts', 'where']

newPandas = int(pandas.__version__.split('.')[0]) >= 1

def getSimpleExceptionTestFunctions(cls):
    functions = [ 'describe', 'mode']
    if not newPandas:
        functions.append('get_dtype_counts')
        functions.append('get_ftype_counts')
    for name, member in getmembers(cls):
        if isfunction(member) and name.startswith('to') and name not in skipped:
            functions.append(name)
    return functions

DataFrame = getSimpleExceptionTestFunctions(pandas.DataFrame)
Series = getSimpleExceptionTestFunctions(pandas.Series)
skipped += set(DataFrame + Series)

def getParameterlessFunctions(cls):
    functions = list()
    for name, member in getmembers(cls):
        if isfunction(member) and not name.startswith('_') and name not in skipped:
            parameters = signature(member).parameters
            count = 0
            for parameter in parameters.values():
                if parameter.default is parameter.empty:
                    count += 1
                else:
                    break
            if count < 2:
                functions.append(name)
    return functions

def getOtherParameterFunctions(cls):
    functions = list()
    for name, member in getmembers(pandas.DataFrame):
        if isfunction(member) and not name.startswith('_') and name not in skipped:
            parameters = signature(member).parameters
            for parameter in parameters.values():
                if parameter.name == 'other':
                    functions.append(name)
                    break
    return functions

DataFrameParameterless = getParameterlessFunctions(pandas.DataFrame)
SeriesParameterless = getParameterlessFunctions(pandas.Series)
DataFrameOtherParameter = getOtherParameterFunctions(pandas.DataFrame)
SeriesOtherParameter = getOtherParameterFunctions(pandas.Series)
");
                Func<string, string, TestCaseData[]> converter = (s, p) =>
                {
                    var list = (List<string>)module.GetAttr(s).AsManagedObject(typeof(List<string>));
                    return list.SelectMany(x => new[]
                    {
                        new TestCaseData($".{x}{p}", "'SPY'", true),
                        new TestCaseData($".{x}{p}", "symbol", false),
                        new TestCaseData($".{x}{p}", "str(symbol.ID)", false)
                    }
                    ).ToArray();
                };

                functionsByType.Add("DataFrame", converter("DataFrame", "()"));
                functionsByType.Add("Series", converter("Series", "()"));
                functionsByType.Add("DataFrameParameterless", converter("DataFrameParameterless", "()"));
                functionsByType.Add("SeriesParameterless", converter("SeriesParameterless", "()"));
                functionsByType.Add("DataFrameOtherParameter", converter("DataFrameOtherParameter", "(other)"));
                functionsByType.Add("SeriesOtherParameter", converter("SeriesOtherParameter", "(other)"));
            }

            return functionsByType;
        }
    }
}
