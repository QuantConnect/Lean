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

using System;
using NUnit.Framework;
using Python.Runtime;
using static QLNet.NumericHaganPricer;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    // TODO: Rename to PandasPythonTests, dedicate class to python tests under ./PandasTests directory
    public class PandasIndexingTests
    {
        private dynamic _module;
        private dynamic _pandasIndexingTests;
        private dynamic _pandasDataFrameTests;

        [SetUp]
        public void Setup()
        {
            using (Py.GIL())
            {
                _module = Py.Import("PandasIndexingTests");
                _pandasIndexingTests = _module.PandasIndexingTests();
                _pandasDataFrameTests = _module.PandasDataFrameTests();
            }
        }

        [Test]
        public void IndexingDataFrameWithList()
        {
            using (Py.GIL())
            {
                Assert.DoesNotThrow((() => _pandasIndexingTests.test_indexing_dataframe_with_list()));
            }
        }

        [Test]
        public void ContainsUserMappedTickers()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_contains_user_mapped_ticker();
                var test = result.As<bool>();

                Assert.IsTrue(test);
            }
        }

        [TestCase("SPY WhatEver")]
        [TestCase("Sharpe ratio")]
        public void ContainsUserDefinedColumnsWithSpaces(string columnName)
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_contains_user_defined_columns_with_spaces(columnName);
                var test = result.As<bool>();

                Assert.IsTrue(test);
            }
        }

        [Test]
        public void ExpectedException()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_expected_exception();
                var exception = result.As<string>();

                Assert.IsTrue(exception.Contains("No key found for either mapped or original key.", StringComparison.InvariantCulture), exception);
            }
        }

        [Test]
        public void KeyErrorDescribesMissingColumn()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_keyerror_describes_missing_column();
                var exception = result.As<string>();

                // Backwards compatible legacy wording plus the new self-describing details
                StringAssert.Contains("No key found for either mapped or original key.", exception);
                StringAssert.Contains("'volume'", exception);
                StringAssert.Contains("The DataFrame has columns", exception);
                StringAssert.Contains("'lastprice'", exception);
                StringAssert.Contains("index levels ['symbol', 'time']", exception);
            }
        }

        [Test]
        public void KeyErrorDescribesIndexLevelKey()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_keyerror_describes_index_level_key();
                var exception = result.As<string>();

                StringAssert.Contains("Original Key: ['symbol', 'lastprice']", exception);
                StringAssert.Contains("'symbol' is an index level, not a column", exception);
                StringAssert.Contains("reset_index", exception);
            }
        }

        [Test]
        public void KeyErrorDescribesSymbolInIndex()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_keyerror_describes_symbol_in_index();
                var exception = result.As<string>();

                StringAssert.Contains("is a value of the 'symbol' index level", exception);
                StringAssert.Contains("df.loc", exception);
            }
        }

        [Test]
        public void KeyErrorDescribesMissingSymbol()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_keyerror_describes_missing_symbol();
                var exception = result.As<string>();

                StringAssert.Contains("is a known Symbol but is not present in this object", exception);
                StringAssert.Contains("df.get(key)", exception);
            }
        }

        [Test]
        public void GetWithSymbolReturnsSubFrame()
        {
            using (Py.GIL())
            {
                Assert.IsTrue(_pandasDataFrameTests.test_get_symbol_returns_subframe().As<bool>());
                Assert.IsTrue(_pandasDataFrameTests.test_get_ticker_returns_subframe().As<bool>());
            }
        }

        [Test]
        public void GetWithMissingSymbolReturnsNone()
        {
            using (Py.GIL())
            {
                Assert.IsTrue(_pandasDataFrameTests.test_get_missing_symbol_returns_none().As<bool>());
            }
        }

        [Test]
        public void GetWithColumnKeepsPandasSemantics()
        {
            using (Py.GIL())
            {
                Assert.IsTrue(_pandasDataFrameTests.test_get_column_keeps_pandas_semantics().As<bool>());
            }
        }

        [Test]
        public void ColumnEqualsOnlyMatchingString()
        {
            using (Py.GIL())
            {
                PyObject result = _pandasDataFrameTests.test_column_equals_only_matching_string();
                var test = result.As<bool>();

                Assert.IsTrue(test);
            }
        }
    }
}
