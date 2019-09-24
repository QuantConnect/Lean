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

using System;
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Custom.PsychSignal;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class PsychSignalConsolidatorTests
    {
        [Test]
        public void ConsolidatesMockPsychSignalDataAccurately_Daily()
        {
            // Note: This data is not real, and was created for testing purposes only
            var consolidator = new PsychSignalConsolidator(TimeSpan.FromDays(1));
            var consolidated = false;

            consolidator.DataConsolidated += (_, data) =>
            {
                Assert.AreEqual(new DateTime(2019, 1, 1), data.Time);

                Assert.AreEqual(1m, data.BullIntensity.Open);
                Assert.AreEqual(2m, data.BullIntensity.High);
                Assert.AreEqual(0.5m, data.BullIntensity.Low);
                Assert.AreEqual(1.99m, data.BullIntensity.Close);

                Assert.AreEqual(0.5m, data.BearIntensity.Open);
                Assert.AreEqual(3m, data.BearIntensity.High);
                Assert.AreEqual(0.5m, data.BearIntensity.Low);
                Assert.AreEqual(1.23m, data.BearIntensity.Close);

                Assert.AreEqual(1239, data.BullScoredMessages);
                Assert.AreEqual(4326, data.BearScoredMessages);

                Assert.AreEqual(1m, data.BullBearMessageRatio.Open);
                Assert.AreEqual(5m, data.BullBearMessageRatio.High);
                Assert.AreEqual(1m, data.BullBearMessageRatio.Low);
                Assert.AreEqual(5m, data.BullBearMessageRatio.Close);

                Assert.AreEqual(5, data.BullMinusBear.Open);
                Assert.AreEqual(5, data.BullMinusBear.High);
                Assert.AreEqual(0, data.BullMinusBear.Low);
                Assert.AreEqual(0, data.BullMinusBear.Close);

                Assert.AreEqual(8005, data.TotalScoredMessages);

                consolidated = true;
            };

            var tick1 = new PsychSignalSentimentData()
            {
                Time = new DateTime(2019, 1, 1, 0, 1, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1m, // Open
                BearIntensity = 0.5m, // Open, low
                BullScoredMessages = 1234,
                BearScoredMessages = 4321,
                BullBearMessageRatio = 1m, // False, but let's just assume it's real for this test's simplicity
                BullMinusBear = 5,
                TotalScoredMessages = 8000
            };
            var tick2 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 1, 2, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 2m, // High
                BearIntensity = 0.75m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 4,
                TotalScoredMessages = 1
            };

            var tick3 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 2, 3, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 0.5m, // Low
                BearIntensity = 3m, // High
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 3,
                TotalScoredMessages = 1
            };

            var tick4 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 19, 2, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 0.75m,
                BearIntensity = 0.99m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 2,
                TotalScoredMessages = 1
            };

            var tick5 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 22, 30, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1m,
                BearIntensity = 1.2m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 1,
                TotalScoredMessages = 1
            };

            var tick6 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 23, 45, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1.99m, // Close
                BearIntensity = 1.23m, // Close
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 0,
                TotalScoredMessages = 1
            };

            var tick7 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 2, 0, 0, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 3m,
                BearIntensity = 0.5m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 10,
                TotalScoredMessages = 1
            };

            consolidator.Update(tick1);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick1.BullIntensity);

            consolidator.Update(tick2);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick2.BullIntensity);

            consolidator.Update(tick3);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick3.BullIntensity);

            consolidator.Update(tick4);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick4.BullIntensity);

            consolidator.Update(tick5);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick5.BullIntensity);

            consolidator.Update(tick6);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick6.BullIntensity);

            consolidator.Update(tick7);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick7.BullIntensity);

            Assert.IsTrue(consolidated);
        }

        [Test]
        public void ConsolidatesMockPsychSignalDataAccurately_Hourly()
        {
            // Note: This data is not real, and was created for testing purposes only
            var consolidator = new PsychSignalConsolidator(TimeSpan.FromHours(1));
            var consolidated = false;

            consolidator.DataConsolidated += (_, data) =>
            {
                Assert.AreEqual(new DateTime(2019, 1, 1), data.Time);

                Assert.AreEqual(1m, data.BullIntensity.Open);
                Assert.AreEqual(2m, data.BullIntensity.High);
                Assert.AreEqual(0.5m, data.BullIntensity.Low);
                Assert.AreEqual(1.99m, data.BullIntensity.Close);

                Assert.AreEqual(0.5m, data.BearIntensity.Open);
                Assert.AreEqual(3m, data.BearIntensity.High);
                Assert.AreEqual(0.5m, data.BearIntensity.Low);
                Assert.AreEqual(1.23m, data.BearIntensity.Close);

                Assert.AreEqual(1239, data.BullScoredMessages);
                Assert.AreEqual(4326, data.BearScoredMessages);

                Assert.AreEqual(1m, data.BullBearMessageRatio.Open);
                Assert.AreEqual(5m, data.BullBearMessageRatio.High);
                Assert.AreEqual(1m, data.BullBearMessageRatio.Low);
                Assert.AreEqual(5m, data.BullBearMessageRatio.Close);

                Assert.AreEqual(5, data.BullMinusBear.Open);
                Assert.AreEqual(5, data.BullMinusBear.High);
                Assert.AreEqual(0, data.BullMinusBear.Low);
                Assert.AreEqual(0, data.BullMinusBear.Close);

                Assert.AreEqual(8005, data.TotalScoredMessages);

                consolidated = true;
            };

            var tick1 = new PsychSignalSentimentData()
            {
                Time = new DateTime(2019, 1, 1, 0, 1, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1m, // Open
                BearIntensity = 0.5m, // Open, low
                BullScoredMessages = 1234,
                BearScoredMessages = 4321,
                BullBearMessageRatio = 1m, // False, but let's just assume it's real for this test's simplicity
                BullMinusBear = 5,
                TotalScoredMessages = 8000
            };
            var tick2 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 2, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 2m, // High
                BearIntensity = 0.75m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 4,
                TotalScoredMessages = 1
            };

            var tick3 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 3, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 0.5m, // Low
                BearIntensity = 3m, // High
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 3,
                TotalScoredMessages = 1
            };

            var tick4 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 2, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 0.75m,
                BearIntensity = 0.99m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 2,
                TotalScoredMessages = 1
            };

            var tick5 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 30, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1m,
                BearIntensity = 1.2m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 1,
                TotalScoredMessages = 1
            };

            var tick6 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 45, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1.99m, // Close
                BearIntensity = 1.23m, // Close
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 0,
                TotalScoredMessages = 1
            };

            var tick7 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 1, 1, 0),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 3m,
                BearIntensity = 0.5m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 10,
                TotalScoredMessages = 1
            };

            consolidator.Update(tick1);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick1.BullIntensity);

            consolidator.Update(tick2);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick2.BullIntensity);

            consolidator.Update(tick3);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick3.BullIntensity);

            consolidator.Update(tick4);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick4.BullIntensity);

            consolidator.Update(tick5);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick5.BullIntensity);

            consolidator.Update(tick6);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick6.BullIntensity);

            consolidator.Update(tick7);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick7.BullIntensity);

            Assert.IsTrue(consolidated);
        }

        [Test]
        public void ConsolidatesMockPsychSignalDataAccurately_Minute()
        {
            // Note: This data is not real, and was created for testing purposes only
            var consolidator = new PsychSignalConsolidator(TimeSpan.FromMinutes(1));
            var consolidated = false;

            consolidator.DataConsolidated += (_, data) =>
            {
                Assert.AreEqual(new DateTime(2019, 1, 1, 0, 5, 0), data.Time);

                Assert.AreEqual(1m, data.BullIntensity.Open);
                Assert.AreEqual(2m, data.BullIntensity.High);
                Assert.AreEqual(0.5m, data.BullIntensity.Low);
                Assert.AreEqual(1.99m, data.BullIntensity.Close);

                Assert.AreEqual(0.5m, data.BearIntensity.Open);
                Assert.AreEqual(3m, data.BearIntensity.High);
                Assert.AreEqual(0.5m, data.BearIntensity.Low);
                Assert.AreEqual(1.23m, data.BearIntensity.Close);

                Assert.AreEqual(1239, data.BullScoredMessages);
                Assert.AreEqual(4326, data.BearScoredMessages);

                Assert.AreEqual(1m, data.BullBearMessageRatio.Open);
                Assert.AreEqual(5m, data.BullBearMessageRatio.High);
                Assert.AreEqual(1m, data.BullBearMessageRatio.Low);
                Assert.AreEqual(5m, data.BullBearMessageRatio.Close);

                Assert.AreEqual(5, data.BullMinusBear.Open);
                Assert.AreEqual(5, data.BullMinusBear.High);
                Assert.AreEqual(0, data.BullMinusBear.Low);
                Assert.AreEqual(0, data.BullMinusBear.Close);

                Assert.AreEqual(8005, data.TotalScoredMessages);

                consolidated = true;
            };

            var tick1 = new PsychSignalSentimentData()
            {
                Time = new DateTime(2019, 1, 1, 0, 5, 1),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1m, // Open
                BearIntensity = 0.5m, // Open, low
                BullScoredMessages = 1234,
                BearScoredMessages = 4321,
                BullBearMessageRatio = 1m, // False, but let's just assume it's real for this test's simplicity
                BullMinusBear = 5,
                TotalScoredMessages = 8000
            };
            var tick2 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 5, 20),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 2m, // High
                BearIntensity = 0.75m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 4,
                TotalScoredMessages = 1
            };

            var tick3 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 5, 40),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 0.5m, // Low
                BearIntensity = 3m, // High
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 3,
                TotalScoredMessages = 1
            };

            var tick4 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 5, 55),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 0.75m,
                BearIntensity = 0.99m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 2,
                TotalScoredMessages = 1
            };

            var tick5 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 5, 57),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1m,
                BearIntensity = 1.2m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 1,
                TotalScoredMessages = 1
            };

            var tick6 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 0, 5, 59),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 1.99m, // Close
                BearIntensity = 1.23m, // Close
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 0,
                TotalScoredMessages = 1
            };

            var tick7 = new PsychSignalSentimentData
            {
                Time = new DateTime(2019, 1, 1, 1, 6, 15),
                Symbol = Symbol.Create("AAPL", SecurityType.Base, QuantConnect.Market.USA),
                BullIntensity = 3m,
                BearIntensity = 0.5m,
                BullScoredMessages = 1,
                BearScoredMessages = 1,
                BullBearMessageRatio = 5,
                BullMinusBear = 10,
                TotalScoredMessages = 1
            };

            consolidator.Update(tick1);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick1.BullIntensity);

            consolidator.Update(tick2);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick2.BullIntensity);

            consolidator.Update(tick3);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick3.BullIntensity);

            consolidator.Update(tick4);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick4.BullIntensity);

            consolidator.Update(tick5);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick5.BullIntensity);

            consolidator.Update(tick6);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick1.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick2.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick3.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick6.BullIntensity);

            consolidator.Update(tick7);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Open, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.High, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Low, tick7.BullIntensity);
            Assert.AreEqual(((PsychSignalConsolidated)consolidator.WorkingData).BullIntensity.Close, tick7.BullIntensity);

            Assert.IsTrue(consolidated);
        }
    }
}