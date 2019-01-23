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

namespace QuantConnect.Tests.Algorithm.Framework.Alphas.Serialization
{
    [TestFixture]
    public class AlphaRuntimeStatisticsTests
    {
        [Test]
        public void HandlesSerializationRoundTrip()
        {
            var stats = new AlphaRuntimeStatistics
            {
                LongCount = 10,
                ShortCount = 11,
                TotalAccumulatedEstimatedAlphaValue = 100,
                TotalInsightsAnalysisCompleted = 7,
                TotalInsightsGenerated = 6,
                TotalInsightsClosed = 5
            };

            var time = DateTime.UtcNow;
            stats.MeanPopulationScore.SetScore(InsightScoreType.Direction, 0.5, time);
            stats.MeanPopulationScore.SetScore(InsightScoreType.Magnitude, 0.6, time);
            stats.RollingAveragedPopulationScore.SetScore(InsightScoreType.Direction, 0.55, time);
            stats.RollingAveragedPopulationScore.SetScore(InsightScoreType.Magnitude, 0.66, time);

            var json = JsonConvert.SerializeObject(stats);

            var deserialized = JsonConvert.DeserializeObject<AlphaRuntimeStatistics>(json);

            Assert.AreEqual(10, deserialized.LongCount);
            Assert.AreEqual(11, deserialized.ShortCount);
            Assert.AreEqual(100, deserialized.TotalAccumulatedEstimatedAlphaValue);
            Assert.AreEqual(7, deserialized.TotalInsightsAnalysisCompleted);
            Assert.AreEqual(6, deserialized.TotalInsightsGenerated);
            Assert.AreEqual(5, deserialized.TotalInsightsClosed);

            Assert.AreEqual(0.5, deserialized.MeanPopulationScore.Direction);
            Assert.AreEqual(0.6, deserialized.MeanPopulationScore.Magnitude);
            Assert.AreEqual(time, deserialized.MeanPopulationScore.UpdatedTimeUtc);
            Assert.AreEqual(0.55, deserialized.RollingAveragedPopulationScore.Direction);
            Assert.AreEqual(0.66, deserialized.RollingAveragedPopulationScore.Magnitude);
            Assert.AreEqual(time, deserialized.RollingAveragedPopulationScore.UpdatedTimeUtc);
        }
    }
}
