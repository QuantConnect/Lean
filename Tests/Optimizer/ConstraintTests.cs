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
using QuantConnect.Optimizer;
using System;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ConstraintTests
    {
        [TestCase(101d)]
        [TestCase(1000d)]
        public void Meet(double value)
        {
            var constraint = new Constraint("Profit", ComparisonOperatorTypes.Greater, 100m);
            Assert.IsTrue(constraint.IsMet(BacktestResult.Create(new decimal(value)).ToJson()));
        }

        [TestCase(1d)]
        [TestCase(99.9d)]
        [TestCase(100d)]
        public void Fails(double value)
        {
            var constraint = new Constraint("Profit", ComparisonOperatorTypes.Greater, 100m);
            Assert.IsFalse(constraint.IsMet(BacktestResult.Create(new decimal(value)).ToJson()));
        }


        [TestCase("")]
        [TestCase(null)]
        public void ThrowIfNullOrEmpty(string json)
        {
            var constraint = new Constraint("Drawdown", ComparisonOperatorTypes.Less, 10);

            Assert.Throws<ArgumentNullException>(() =>
            {
                constraint.IsMet(json);
            });
        }

        [Test]
        public void IgnoreBadJson()
        {
            var constraint = new Constraint("Drawdown", ComparisonOperatorTypes.Less, 0.1m);

            Assert.IsFalse(constraint.IsMet("{\"Drawdown\":\"10.0%\"}"));
        }

        [TestCase("Drawdown")]
        [TestCase("Statistics.Drawdown")]
        [TestCase("['Statistics'].['Drawdown']")]
        public void ParseTargetName(string targetName)
        {
            var target = new Target(targetName, new Minimization(), 100);

            Assert.AreEqual("['Statistics'].['Drawdown']", target.Target);
        }

        [Test]
        public void FromJson()
        {
            var json = "{\"operator\": \"equals\",\"target\": \"pin ocho.Gepetto\",\"target-value\": null}";

            var constraint = (Constraint)JsonConvert.DeserializeObject(json, typeof(Constraint));

            Assert.AreEqual("['pin ocho'].['Gepetto']", constraint.Target);
            Assert.IsNull(constraint.TargetValue);
            Assert.AreEqual(ComparisonOperatorTypes.Equals, constraint.Operator);
        }
    }
}
