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
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class MacdAlphaModelTests : CommonAlphaModelTests
    {
        protected override IAlphaModel CreateCSharpAlphaModel() => new MacdAlphaModel();

        protected override IAlphaModel CreatePythonAlphaModel()
        {
            using (Py.GIL())
            {
                dynamic model = Py.Import("MacdAlphaModel").GetAttr("MacdAlphaModel");
                var instance = model();
                return new AlphaModelPythonWrapper(instance);
            }
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            var period = TimeSpan.FromDays(12);
            return new[]
            {
                Insight.Price(Symbols.SPY, period, InsightDirection.Up),
                Insight.Price(Symbols.SPY, period, InsightDirection.Down),
                Insight.Price(Symbols.SPY, period, InsightDirection.Up)
            };
        }

        protected override string GetExpectedModelName(IAlphaModel model)
        {
            return $"{nameof(MacdAlphaModel)}(12,26,9,Exponential,Daily)";
        }

        [Test]
        public void MacdAlphaModelWarmsUpProperly()
        {
            SetUpHistoryProvider();
            Algorithm.SetStartDate(2013, 10, 08);

            // Create a MacdAlphaModel for the test
            var model = new TestMacdAlphaModel();

            // Set the alpha model
            Algorithm.SetAlpha(model);
            Algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

            var changes = SecurityChangesTests.CreateNonInternal(AddedSecurities, RemovedSecurities);
            Algorithm.OnFrameworkSecuritiesChanged(changes);

            // Get the dictionary of macd indicators
            var symbolData = model.GetSymbolData();

            // Check the symbolData dictionary is not empty
            Assert.NotZero(symbolData.Count);

            // Check all MACD indicators from the alpha are ready and have at least
            // one datapoint
            foreach (var item in symbolData)
            {
                var macd = item.Value.MACD;

                Assert.IsTrue(macd.IsReady);
                Assert.NotZero(macd.Samples);
            }
        }

        [Test]
        public void PythonMacdAlphaModelWarmsUpProperly()
        {
            using (Py.GIL())
            {
                SetUpHistoryProvider();
                Algorithm.SetStartDate(2013, 10, 08);
                Algorithm.SetUniverseSelection(new ManualUniverseSelectionModel());

                // Create and set alpha model
                dynamic model = Py.Import("MacdAlphaModel").GetAttr("MacdAlphaModel");
                var instance = model();
                Algorithm.SetAlpha(instance);

                var changes = SecurityChangesTests.CreateNonInternal(AddedSecurities, RemovedSecurities);
                Algorithm.OnFrameworkSecuritiesChanged(changes);

                // Get the dictionary of macd indicators
                var symbolData = instance.symbolData;

                // Check the dictionary is not empty
                Assert.NotZero(symbolData.Length());

                // Check all MACD indicators from the alpha are ready and have at least
                // one datapoint
                foreach (var item in symbolData)
                {
                    var macd = symbolData[item].MACD;

                    Assert.IsTrue(macd.IsReady.IsTrue());
                    Assert.NotZero(((PyObject)macd.Samples).GetAndDispose<int>());
                }
            }
        }
    }
}
