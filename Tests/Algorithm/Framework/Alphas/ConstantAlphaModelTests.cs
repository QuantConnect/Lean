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
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class ConstantAlphaModelTests : CommonAlphaModelTests
    {
        private InsightType _type = InsightType.Price;
        private InsightDirection _direction = InsightDirection.Up;
        private TimeSpan _period = Time.OneDay;
        private double? _magnitude = 0.025;
        private double? _confidence = null;

        protected override IAlphaModel CreateCSharpAlphaModel() => new ConstantAlphaModel(_type, _direction, _period, _magnitude, _confidence);

        protected override IAlphaModel CreatePythonAlphaModel()
        {
            using (Py.GIL())
            {
                dynamic model = Py.Import("ConstantAlphaModel").GetAttr("ConstantAlphaModel");
                var instance = model(_type, _direction, _period, _magnitude, _confidence);
                return new AlphaModelPythonWrapper(instance);
            }
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            return Enumerable.Range(0, 360).Select(x => new Insight(Symbols.SPY, _period, _type, _direction, _magnitude, _confidence));
        }
    }
}
