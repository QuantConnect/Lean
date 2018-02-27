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

using System;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests.Algorithm.Framework
{
    [TestFixture]
    public class AlphaTests
    {
        [Test]
        public void SurvivesRoundTripSerializationUsingJsonConvert()
        {
            var time = new DateTime(2000, 01, 02, 03, 04, 05, 06);
            var alpha = new Alpha(time, Symbols.SPY, AlphaType.Volatility, AlphaDirection.Up, Time.OneMinute, 1, 2);
            var serialized = JsonConvert.SerializeObject(alpha);
            var deserialized = JsonConvert.DeserializeObject<Alpha>(serialized);

            Assert.AreEqual(alpha.CloseTimeUtc, deserialized.CloseTimeUtc);
            Assert.AreEqual(alpha.Confidence, deserialized.Confidence);
            Assert.AreEqual(alpha.Direction, deserialized.Direction);
            Assert.AreEqual(alpha.EstimatedValue, deserialized.EstimatedValue);
            Assert.AreEqual(alpha.GeneratedTimeUtc, deserialized.GeneratedTimeUtc);
            Assert.AreEqual(alpha.Id, deserialized.Id);
            Assert.AreEqual(alpha.Magnitude, deserialized.Magnitude);
            Assert.AreEqual(alpha.Period, deserialized.Period);
            Assert.AreEqual(alpha.Score.Direction, deserialized.Score.Direction);
            Assert.AreEqual(alpha.Score.Magnitude, deserialized.Score.Magnitude);
            Assert.AreEqual(alpha.Score.UpdatedTimeUtc, deserialized.Score.UpdatedTimeUtc);
            Assert.AreEqual(alpha.Score.IsFinalScore, deserialized.Score.IsFinalScore);
            Assert.AreEqual(alpha.Symbol, deserialized.Symbol);
            Assert.AreEqual(alpha.Type, deserialized.Type);
        }
    }
}
