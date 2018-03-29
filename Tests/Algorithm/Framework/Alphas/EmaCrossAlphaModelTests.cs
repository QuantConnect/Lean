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

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class EmaCrossAlphaModelTests : CommonAlphaModelTests
    {
        private InsightType _type = InsightType.Price;

        private int _fastPeriod = 10;
        private int _slowPeriod = 20;
        private TimeSpan _predictionInterval = TimeSpan.FromMinutes(10);

        protected override IAlphaModel CreateCSharpAlphaModel()
        {
            return new EmaCrossAlphaModel(_fastPeriod, _slowPeriod, _predictionInterval);
        }

        protected override IAlphaModel CreatePythonAlphaModel()
        {
            using (Py.GIL())
            {
                dynamic model = Py.Import("EmaCrossAlphaModel").GetAttr("EmaCrossAlphaModel");
                var instance = model(_fastPeriod, _slowPeriod, _predictionInterval);
                return new AlphaModelPythonWrapper(instance);
            }
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            return new[]
            {
                new Insight(Symbols.SPY, _type, InsightDirection.Down, _predictionInterval),
                new Insight(Symbols.SPY, _type, InsightDirection.Up, _predictionInterval)
            };
        }
    }
}
