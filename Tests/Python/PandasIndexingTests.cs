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

                Assert.IsTrue(exception.Contains("No key found for either mapped or original key. Mapped Key: ['AAPL']; Original Key: ['aapl']", StringComparison.InvariantCulture));
            }
        }
    }
}
