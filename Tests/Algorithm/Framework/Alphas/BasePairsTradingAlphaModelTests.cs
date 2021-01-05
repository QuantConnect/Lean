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

using System.Collections.Generic;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class BasePairsTradingAlphaModelTests : CommonAlphaModelTests
    {
        private const int _lookback = 15;
        private const Resolution _resolution = Resolution.Minute;

        protected override int MaxSliceCount => 1500;

        protected override IAlphaModel CreateCSharpAlphaModel()
        {
            return new BasePairsTradingAlphaModel(_lookback, _resolution);
        }

        protected override void InitializeAlgorithm(QCAlgorithm algorithm)
        {
            algorithm.SetUniverseSelection(new ManualUniverseSelectionModel(
                Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                Symbol.Create("BAC", SecurityType.Equity, Market.USA)));
        }

        protected override string GetExpectedModelName(IAlphaModel model)
        {
            return $"{nameof(BasePairsTradingAlphaModel)}({_lookback},{_resolution},1)";
        }

        protected override IAlphaModel CreatePythonAlphaModel()
        {
            using (Py.GIL())
            {
                dynamic model = Py.Import("BasePairsTradingAlphaModel").GetAttr("BasePairsTradingAlphaModel");
                var instance = model(_lookback, _resolution);
                return new AlphaModelPythonWrapper(instance);
            }
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            Assert.Ignore("The CommonAlphaModelTests need to be refactored to support multiple securities with different prices for each security");
            return null;
        }
    }
}