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
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class StreamReaderExtensionsTests
    {
        [Test]
        public void GetDecimal()
        {
            var stream = "16.2".ToStream();

            var smartStream = new StreamReader(stream);
            var value = smartStream.GetDecimal();

            Assert.AreEqual(16.2, value);
        }

        [Test]
        public void GetNegativeDecimal()
        {
            var stream = "-16.2,-88".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(-16.2, smartStream.GetDecimal());
            Assert.AreEqual(-88, smartStream.GetDecimal());
        }

        [Test]
        public void GetMultipleDecimals()
        {
            var stream = "16.2,0,12.2111111111,".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal());
            Assert.AreEqual(0, smartStream.GetDecimal());
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal());

        }

        [Test]
        public void GetMultipleDecimalsWithCarriageReturn()
        {
            var stream = "16.2,0\r12.2111111111".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal());
            Assert.AreEqual(0, smartStream.GetDecimal());
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal());
        }

        [Test]
        public void GetMultipleDecimalsWithLineFeed()
        {
            var stream = "16.2,0\n12.2111111111".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal());
            Assert.AreEqual(0, smartStream.GetDecimal());
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal());
        }

        [Test]
        public void GetMultipleDecimalsWithCarriageReturnAndLineFeed()
        {
            var stream = "16.2,0\r\n12.2111111111".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal());
            Assert.AreEqual(0, smartStream.GetDecimal());
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal());
        }

        [Test]
        public void GetDecimalEmptyString()
        {
            var stream = "".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(0, smartStream.GetDecimal());

            stream = ",".ToStream();
            smartStream = new StreamReader(stream);

            Assert.AreEqual(0, smartStream.GetDecimal());
        }

        [Test]
        public void GetDateTime()
        {
            var stream = "20190102 02:13".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(new DateTime(2019, 1, 2, 2, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
        }

        [Test]
        public void GetMultipleDateTime()
        {
            var stream = "20190102 02:13,20190203 05:13".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(new DateTime(2019, 1, 2, 2, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
            Assert.AreEqual(new DateTime(2019, 2, 3, 5, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
        }

        [Test]
        public void GetMultipleDateTimeWithCarriageReturn()
        {
            var stream = "20190102 02:13\r20190203 05:13".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(new DateTime(2019, 1, 2, 2, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
            Assert.AreEqual(new DateTime(2019, 2, 3, 5, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
        }

        [Test]
        public void GetMultipleDateTimeWithLineFeed()
        {
            var stream = "20190102 02:13\n20190203 05:13".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(new DateTime(2019, 1, 2, 2, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
            Assert.AreEqual(new DateTime(2019, 2, 3, 5, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
        }

        [Test]
        public void GetMultipleDateTimeWithCarriageReturnAndLineFeed()
        {
            var stream = "20190102 02:13\r\n20190203 05:13".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(new DateTime(2019, 1, 2, 2, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
            Assert.AreEqual(new DateTime(2019, 2, 3, 5, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
        }

        [Test]
        public void GetDecimalsAndDateTimes()
        {
            var stream = $"20190102 02:13,0,19{Environment.NewLine}20190203 05:13,15,1000{Environment.NewLine}".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(new DateTime(2019, 1, 2, 2, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
            Assert.AreEqual(0, smartStream.GetDecimal());
            Assert.AreEqual(19, smartStream.GetDecimal());

            Assert.AreEqual(new DateTime(2019, 2, 3, 5, 13, 0),
                smartStream.GetDateTime(DateFormat.TwelveCharacter));
            Assert.AreEqual(15, smartStream.GetDecimal());
            Assert.AreEqual(1000, smartStream.GetDecimal());
        }

        [Test]
        public void GetInt()
        {
            var stream = $"20190,0,19{Environment.NewLine}201900{Environment.NewLine}".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(20190, smartStream.GetInt32());
            Assert.AreEqual(0, smartStream.GetInt32());
            Assert.AreEqual(19, smartStream.GetInt32());
            Assert.AreEqual(201900, smartStream.GetInt32());
        }

        [Test]
        public void GetNegativeInt()
        {
            var stream = $"-20190,0,-19{Environment.NewLine}-201900{Environment.NewLine}".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(-20190, smartStream.GetInt32());
            Assert.AreEqual(0, smartStream.GetInt32());
            Assert.AreEqual(-19, smartStream.GetInt32());
            Assert.AreEqual(-201900, smartStream.GetInt32());
        }

        [Test]
        public void GetIntWithCarriageReturnAndLineFeed()
        {
            var stream = "20190,0,19\r\n201900\r\n".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(20190, smartStream.GetInt32());
            Assert.AreEqual(0, smartStream.GetInt32());
            Assert.AreEqual(19, smartStream.GetInt32());
            Assert.AreEqual(201900, smartStream.GetInt32());
        }

        [Test]
        public void GetIntWithCarriageReturn()
        {
            var stream = "20190,0,19\r201900\r".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(20190, smartStream.GetInt32());
            Assert.AreEqual(0, smartStream.GetInt32());
            Assert.AreEqual(19, smartStream.GetInt32());
            Assert.AreEqual(201900, smartStream.GetInt32());
        }

        [Test]
        public void GetIntWithLineFeed()
        {
            var stream = "20190,0,19\n201900\n".ToStream();
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(20190, smartStream.GetInt32());
            Assert.AreEqual(0, smartStream.GetInt32());
            Assert.AreEqual(19, smartStream.GetInt32());
            Assert.AreEqual(201900, smartStream.GetInt32());
        }

        [Test]
        public void Performance()
        {
            var streamReaderMilliSeconds = 0L;
            var streamReaderCount = 0;
            var getLineReaderMilliSeconds = 0L;
            var getLineReaderCount = 0;
            var stopWatch = new Stopwatch();
            {
                var config = new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    true,
                    false
                );
                var date = new DateTime(2013, 10, 07);
                var reader = new TextSubscriptionDataSourceReader(
                    new ZipDataCacheProvider(new DefaultDataProvider()),
                    config,
                    date,
                    false);
                var source = typeof(TradeBar).GetBaseDataInstance().GetSource(config, date, false);
                // warmup
                streamReaderCount = reader.Read(source).Count();
                streamReaderCount = 0;

                // start test
                stopWatch.Start();
                for (int i = 0; i < 200; i++)
                {
                    streamReaderCount += reader.Read(source).Count();
                }
                stopWatch.Stop();
                streamReaderMilliSeconds = stopWatch.ElapsedMilliseconds;
            }

            {
                var config = new SubscriptionDataConfig(
                    typeof(TradeBarTest),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    true,
                    false
                );
                var date = new DateTime(2013, 10, 07);
                var reader = new TextSubscriptionDataSourceReader(
                    new ZipDataCacheProvider(new DefaultDataProvider()),
                    config,
                    date,
                    false);
                var source = typeof(TradeBarTest).GetBaseDataInstance().GetSource(config, date, false);
                // warmup
                getLineReaderCount = reader.Read(source).Count();
                getLineReaderCount = 0;

                // start test
                stopWatch.Start();
                for (int i = 0; i < 200; i++)
                {
                    getLineReaderCount += reader.Read(source).Count();
                }
                stopWatch.Stop();
                getLineReaderMilliSeconds = stopWatch.ElapsedMilliseconds;
            }
            Console.WriteLine($"StreamReader: {streamReaderMilliSeconds}ms. Count {streamReaderCount}");
            Console.WriteLine($"GetLine Reader: {getLineReaderMilliSeconds}ms. Count {getLineReaderCount}");

            // its 50% faster but lets leave some room to avoid noise
            Assert.IsTrue((streamReaderMilliSeconds * 1.85d) < getLineReaderMilliSeconds);
            Assert.AreEqual(getLineReaderCount, streamReaderCount);
        }

        /// <summary>
        /// Since this class does not implement <see cref="BaseData.Reader(SubscriptionDataConfig,StreamReader,DateTime,bool)"/>
        /// directly it will fallback to get line reader
        /// </summary>
        private class TradeBarTest : TradeBar
        {

        }
    }
}
