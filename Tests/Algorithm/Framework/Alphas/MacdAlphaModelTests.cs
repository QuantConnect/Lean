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
    public class MacdAlphaModelTests : CommonAlphaModelTests
    {
        private InsightType _type = InsightType.Price;
        private TimeSpan _consolidatorPeriod = TimeSpan.FromMinutes(10);
        private TimeSpan _insightPeriod = TimeSpan.FromMinutes(30);
        private decimal _bounceThresholdPercent = 0.01m;

        protected override IAlphaModel CreateCSharpAlphaModel()
        {
            return new MacdAlphaModel(_consolidatorPeriod, _insightPeriod, _bounceThresholdPercent);
        }

        protected override IAlphaModel CreatePythonAlphaModel()
        {
            using (Py.GIL())
            {
                dynamic model = Py.Import("MacdAlphaModel").GetAttr("MacdAlphaModel");
                var instance = model(_consolidatorPeriod, _insightPeriod, _bounceThresholdPercent);
                return new AlphaModelPythonWrapper(instance);
            }
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            return new[]
            {
                new Insight(Symbols.SPY, _type, InsightDirection.Flat, _insightPeriod),
                new Insight(Symbols.SPY, _type, InsightDirection.Up, _insightPeriod),
                new Insight(Symbols.SPY, _type, InsightDirection.Flat, _insightPeriod),
                new Insight(Symbols.SPY, _type, InsightDirection.Down, _insightPeriod)
            };
        }
    }
}
