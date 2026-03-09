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

using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Util;
using System;

namespace QuantConnect.Tests.Optimizer.Objectives
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ConstraintTests
    {
        [TestCase(101.0)]
        [TestCase(1000.0)]
        public void Meet(decimal value)
        {
            var constraint = new Constraint("Profit", ComparisonOperatorTypes.Greater, 100m);
            Assert.IsTrue(constraint.IsMet(BacktestResult.Create(value).ToJson()));
        }

        [TestCase(1.0)]
        [TestCase(99.9)]
        [TestCase(100d)]
        public void Fails(decimal value)
        {
            var constraint = new Constraint("Profit", ComparisonOperatorTypes.Greater, 100m);
            Assert.IsFalse(constraint.IsMet(BacktestResult.Create(value).ToJson()));
        }

        [Test]
        public void ThrowIfTargetNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var constraint = new Constraint("Drawdown", ComparisonOperatorTypes.Less, null);
            });
        }

        [TestCase("")]
        [TestCase(null)]
        public void ThrowIfResultNullOrEmpty(string json)
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
        public void ParseName(string targetName)
        {
            var target = new Constraint(targetName, ComparisonOperatorTypes.Equals, 100);

            Assert.AreEqual("['Statistics'].['Drawdown']", target.Target);
        }

        [Test]
        public void FromJson()
        {
            var json = "{\"operator\": \"equals\",\"target\": \"pin ocho.Gepetto\",\"targetValue\": 11}";

            var constraint = (Constraint)JsonConvert.DeserializeObject(json, typeof(Constraint));

            Assert.AreEqual("['pin ocho'].['Gepetto']", constraint.Target);
            Assert.AreEqual(ComparisonOperatorTypes.Equals, constraint.Operator);
            Assert.AreEqual(11, constraint.TargetValue.Value);
        }

        [Test]
        public void RoundTrip()
        {
            var origin =  new Constraint("['Statistics'].['Drawdown']", ComparisonOperatorTypes.Equals, 100);
            
            var json = JsonConvert.SerializeObject(origin);
            var actual = JsonConvert.DeserializeObject<Constraint>(json);
            
            Assert.NotNull(actual);
            Assert.AreEqual(origin.Target, actual.Target);
            Assert.AreEqual(origin.Operator, actual.Operator);
            Assert.AreEqual(origin.TargetValue, actual.TargetValue);
        }
    }
}
