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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using System;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Util;
using QuantConnect.Tests.Common.Data.UniverseSelection;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class EmaCrossAlphaModelTests : CommonAlphaModelTests
    {
        protected override IAlphaModel CreateCSharpAlphaModel() => new EmaCrossAlphaModel();

        protected override IAlphaModel CreatePythonAlphaModel()
        {
            using (Py.GIL())
            {
                dynamic model = Py.Import("EmaCrossAlphaModel").GetAttr("EmaCrossAlphaModel");
                var instance = model();
                return new AlphaModelPythonWrapper(instance);
            }
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            var period = TimeSpan.FromDays(12);

            return new[]
            {
                Insight.Price(Symbols.SPY, period, InsightDirection.Down),
                Insight.Price(Symbols.SPY, period, InsightDirection.Up)
            };
        }

        protected override string GetExpectedModelName(IAlphaModel model)
        {
            return $"{nameof(EmaCrossAlphaModel)}(12,26,Daily)";
        }

        [Test]
        public void WarmsUpProperly()
        {
            SetUpHistoryProvider();

            Algorithm.SetStartDate(2013, 10, 08);
            Algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

            // Create a EmaCrossAlphaModel for the test
            var model = new TestEmaCrossAlphaModel();

            // Set the alpha model
            Algorithm.SetAlpha(model);
            Algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

            var changes = SecurityChangesTests.CreateNonInternal(AddedSecurities, RemovedSecurities);
            Algorithm.OnFrameworkSecuritiesChanged(changes);

            // Get the dictionary of macd indicators
            var symbolData = model.GetSymbolData();

            // Check the symbolData dictionary is not empty
            Assert.NotZero(symbolData.Count);

            // Check all EmaCross indicators from the alpha are ready and have at least
            // one datapoint
            foreach (var item in symbolData)
            {
                var fast = item.Value.Fast;
                var slow = item.Value.Slow;

                Assert.IsTrue(fast.IsReady);
                Assert.NotZero(fast.Samples);

                Assert.IsTrue(slow.IsReady);
                Assert.NotZero(slow.Samples);
            }
        }

        [Test]
        public void PythonVersionWarmsUpProperly()
        {
            using (Py.GIL())
            {
                SetUpHistoryProvider();
                Algorithm.SetStartDate(2013, 10, 08);
                Algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

                // Create and set alpha model
                dynamic model = Py.Import("EmaCrossAlphaModel").GetAttr("EmaCrossAlphaModel");
                var instance = model();
                Algorithm.SetAlpha(instance);

                var changes = SecurityChangesTests.CreateNonInternal(AddedSecurities, RemovedSecurities);
                Algorithm.OnFrameworkSecuritiesChanged(changes);

                // Get the dictionary of ema cross indicators
                var symbolData = instance.symbol_data_by_symbol;

                // Check the dictionary is not empty
                Assert.NotZero(symbolData.Length());

                // Check all Ema Cross indicators from the alpha are ready and have at least
                // one datapoint
                foreach (var item in symbolData)
                {
                    var fast = symbolData[item].fast;
                    var slow = symbolData[item].slow;

                    Assert.IsTrue(fast.IsReady.IsTrue());
                    Assert.NotZero(((PyObject)fast.Samples).GetAndDispose<int>());

                    Assert.IsTrue(slow.IsReady.IsTrue());
                    Assert.NotZero(((PyObject)slow.Samples).GetAndDispose<int>());
                }
            }
        }
    }
}
