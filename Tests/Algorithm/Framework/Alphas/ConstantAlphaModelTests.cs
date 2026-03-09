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
using System.Reflection;
using static System.FormattableString;

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

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void ConstructorWithWeightOnlySetsWeightCorrectly(Language language)
        {
            IAlphaModel alpha;
            if (language == Language.CSharp)
            {
                alpha = new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1), weight: 0.1);
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString("test_module",
                    @"
from AlgorithmImports import *

def test_constructor():
    model = ConstantAlphaModel(InsightType.Price, InsightDirection.Up, timedelta(1), weight=0.1)
    return model
                    ");

                    alpha = testModule.GetAttr("test_constructor").Invoke().As<ConstantAlphaModel>();
                }
            }

            var magnitude = GetPrivateField(alpha, "_magnitude");
            var confidence = GetPrivateField(alpha, "_confidence");
            var weight = GetPrivateField(alpha, "_weight");

            Assert.IsNull(magnitude);
            Assert.IsNull(confidence);
            Assert.AreEqual(0.1, weight);
        }

        private static object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        protected override IEnumerable<Insight> ExpectedInsights()
        {
            return Enumerable.Range(0, 360).Select(x => new Insight(Symbols.SPY, _period, _type, _direction, _magnitude, _confidence));
        }

        protected override string GetExpectedModelName(IAlphaModel model)
        {
            return Invariant($"{nameof(ConstantAlphaModel)}({_type},{_direction},{_period},{_magnitude})");
        }
    }
}
