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
using System;

namespace QuantConnect.Tests.Optimizer.Objectives
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class TargetTests
    {
        [Test]
        public void MoveAheadNoTarget()
        {
            var target = new Target("Profit", new Maximization(), null);
            Assert.IsTrue(target.MoveAhead(BacktestResult.Create(profit: 10m).ToJson()));
            Assert.AreEqual(10m, target.Current);
            Assert.IsFalse(target.MoveAhead(BacktestResult.Create(profit: 5m).ToJson()));
            Assert.AreEqual(10m, target.Current);
            Assert.IsTrue(target.MoveAhead(BacktestResult.Create(profit: 15m).ToJson()));
            Assert.AreEqual(15m, target.Current);
        }

        [Test]
        public void MoveAheadReachFirst()
        {
            var target = new Target("Profit", new Minimization(), 100);
            bool reached = false;
            target.Reached += (s, e) =>
            {
                reached = true;
            };

            Assert.IsTrue(target.MoveAhead(BacktestResult.Create(profit: 10m).ToJson()));
            target.CheckCompliance();

            Assert.AreEqual(10m, target.Current);
            Assert.IsTrue(reached);
        }

        [Test]
        public void MoveAheadReachLast()
        {
            var target = new Target("Profit", new Minimization(), 100);
            bool reached = false;
            target.Reached += (s, e) =>
            {
                reached = true;
            };

            for (var profit = 500m; profit > 0; profit -= 50)
            {
                Assert.IsTrue(target.MoveAhead(BacktestResult.Create(profit: profit).ToJson()));
                Assert.AreEqual(profit, target.Current);
                target.CheckCompliance();
            }
            
            Assert.IsTrue(reached);
        }

        [TestCase("")]
        [TestCase(null)]
        public void ThrowIfNullOrEmpty(string json)
        {
            var target = new Target("Profit", new Maximization(), null);

            Assert.Throws<ArgumentNullException>(() =>
            {
                target.MoveAhead(json);
            });
        }

        [Test]
        public void IgnoreBadJson()
        {
            var target = new Target("Profit", new Maximization(), null);
            
            Assert.IsFalse(target.MoveAhead("{\"Profit\":10}"));
            Assert.AreEqual(null, target.Current);
        }

        [TestCase("Sharpe Ratio")]
        [TestCase("Statistics.Sharpe Ratio")]
        [TestCase("['Statistics'].['Sharpe Ratio']")]
        public void ParseTargetName(string targetName)
        {
            var target = new Target(targetName, new Minimization(), 100);

            Assert.AreEqual("['Statistics'].['Sharpe Ratio']", target.Target);
        }

        [TestCase("null")]
        [TestCase("11")]
        public void FromJson(string value)
        {
            var json = $"{{\"extremum\": \"max\",\"target\": \"pin ocho.Gepetto\",\"targetValue\": {value}}}";

            var target = (Target)JsonConvert.DeserializeObject(json, typeof(Target));

            Assert.AreEqual("['pin ocho'].['Gepetto']", target.Target);
            Assert.AreEqual(typeof(Maximization), target.Extremum.GetType());

            if (value == "null")
            {
                Assert.IsNull(target.TargetValue);
            }
            else
            {
                Assert.AreEqual(11, target.TargetValue);
            }
        }

        [Test]
        public void RoundTrip()
        {
            var origin = new Target("['Statistics'].['Drawdown']", new Maximization(), 100);

            var json = JsonConvert.SerializeObject(origin);
            var actual = JsonConvert.DeserializeObject<Target>(json);

            Assert.NotNull(actual);
            Assert.AreEqual(origin.Target, actual.Target);
            Assert.AreEqual(origin.Extremum.GetType(), actual.Extremum.GetType());
            Assert.AreEqual(origin.TargetValue, actual.TargetValue);
        }
    }
}
