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
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class StreamReaderExtensionsTests
    {
        [TestCase("\r\n", "")]
        [TestCase("\n", "")]
        [TestCase("\r", "")]
        [TestCase(",", "")]
        [TestCase("-16", "-16")]
        [TestCase("16.2,", "16.2")]
        [TestCase("16.2", "16.2")]
        public void GetSimpleString(string input, string result)
        {
            var stream = input.ToStream();

            var smartStream = new StreamReader(stream);
            var value = smartStream.GetString();

            Assert.AreEqual(result, value);
        }

        [TestCase("He Llo\r\nHe Llo2\r\n", "He Llo", "He Llo2")]
        [TestCase("\nTT\n", "", "TT")]
        [TestCase("\rpl op\r", "", "pl op")]
        [TestCase(",He Llo,", "", "He Llo")]
        [TestCase("-16\rPe pe", "-16", "Pe pe")]
        [TestCase("16.2,,", "16.2", "")]
        [TestCase("16.2,8", "16.2", "8")]
        public void GetString(string input, string result, string result2)
        {
            var stream = input.ToStream();

            var smartStream = new StreamReader(stream);
            Assert.AreEqual(result, smartStream.GetString());
            Assert.AreEqual(result2, smartStream.GetString());
        }

        [Test]
        public void GetDecimal()
        {
            var stream = "16.2".ToStream();

            var smartStream = new StreamReader(stream);
            bool pastLineEnd;
            var value = smartStream.GetDecimal(out pastLineEnd);

            Assert.IsTrue(pastLineEnd);
            Assert.AreEqual(16.2, value);
        }

        [Test]
        public void GetNegativeDecimal()
        {
            var stream = "-16.2,-88".ToStream();

            var smartStream = new StreamReader(stream);

            bool pastLineEnd;
            Assert.AreEqual(-16.2, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);
            Assert.AreEqual(-88, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
        }

        [Test]
        public void GetMultipleDecimals()
        {
            var stream = "16.2,0,12.2111111111,".ToStream();
            bool pastLineEnd;
            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);
            Assert.AreEqual(0, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);

        }

        [Test]
        public void GetMultipleDecimalsWithCarriageReturn()
        {
            bool pastLineEnd;
            var stream = "16.2,0\r12.2111111111".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);
            Assert.AreEqual(0, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
        }

        [Test]
        public void GetMultipleDecimalsWithLineFeed()
        {
            bool pastLineEnd;
            var stream = "16.2,0\n12.2111111111".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);
            Assert.AreEqual(0, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
        }

        [Test]
        public void GetMultipleDecimalsWithCarriageReturnAndLineFeed()
        {
            bool pastLineEnd;
            var stream = "16.2,0\r\n12.2111111111".ToStream();

            var smartStream = new StreamReader(stream);

            Assert.AreEqual(16.2, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsFalse(pastLineEnd);
            Assert.AreEqual(0, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
            Assert.AreEqual(12.2111111111, smartStream.GetDecimal(out pastLineEnd));
            Assert.IsTrue(pastLineEnd);
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

        [Parallelizable(ParallelScope.None)]
        [TestCase(typeof(TradeBar), typeof(TradeBarTest), TickType.Trade)]
        [TestCase(typeof(QuoteBar), typeof(QuoteBarTest), TickType.Quote)]
        public void Performance(Type streamReaderType, Type readLineReaderType, TickType tickType)
        {
            var streamReaderMilliSeconds = 0L;
            var streamReaderCount = 0;
            var getLineReaderMilliSeconds = 0L;
            var getLineReaderCount = 0;
            var stopWatch = new Stopwatch();
            {
                var config = new SubscriptionDataConfig(
                    streamReaderType,
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    true,
                    false,
                    tickType: tickType
                );
                var zipCache = new ZipDataCacheProvider(TestGlobals.DataProvider);
                var date = new DateTime(2013, 10, 07);
                var reader = new TextSubscriptionDataSourceReader(
                    zipCache,
                    config,
                    date,
                    false,
                    null);
                var source = streamReaderType.GetBaseDataInstance().GetSource(config, date, false);
                // warmup
                streamReaderCount = reader.Read(source).Count();
                streamReaderCount = 0;

                // start test
                stopWatch.Start();
                for (int i = 0; i < 300; i++)
                {
                    streamReaderCount += reader.Read(source).Count();
                }
                stopWatch.Stop();
                streamReaderMilliSeconds = stopWatch.ElapsedMilliseconds;
                zipCache.DisposeSafely();
            }

            {
                var config = new SubscriptionDataConfig(
                    readLineReaderType,
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    true,
                    false,
                    tickType: tickType
                );
                var zipCache = new ZipDataCacheProvider(TestGlobals.DataProvider);
                var date = new DateTime(2013, 10, 07);
                var reader = new TextSubscriptionDataSourceReader(
                    zipCache,
                    config,
                    date,
                    false,
                    null);
                var source = readLineReaderType.GetBaseDataInstance().GetSource(config, date, false);
                // warmup
                getLineReaderCount = reader.Read(source).Count();
                getLineReaderCount = 0;

                // start test
                stopWatch.Start();
                for (int i = 0; i < 300; i++)
                {
                    getLineReaderCount += reader.Read(source).Count();
                }
                stopWatch.Stop();
                getLineReaderMilliSeconds = stopWatch.ElapsedMilliseconds;
                zipCache.DisposeSafely();
            }
            Log.Trace($"StreamReader: {streamReaderMilliSeconds}ms. Count {streamReaderCount}");
            Log.Trace($"GetLine Reader: {getLineReaderMilliSeconds}ms. Count {getLineReaderCount}");

            // its 50% faster but lets leave some room to avoid noise
            Assert.IsTrue((streamReaderMilliSeconds * 1.5d) < getLineReaderMilliSeconds);
            Assert.AreEqual(getLineReaderCount, streamReaderCount);
        }

        /// <summary>
        /// Since this class does not implement <see cref="BaseData.Reader(SubscriptionDataConfig,StreamReader,DateTime,bool)"/>
        /// directly it will fallback to get line reader
        /// </summary>
        private class TradeBarTest : TradeBar
        {
        }

        /// <summary>
        /// Since this class does not implement <see cref="BaseData.Reader(SubscriptionDataConfig,StreamReader,DateTime,bool)"/>
        /// directly it will fallback to get line reader
        /// </summary>
        private class QuoteBarTest : QuoteBar
        {
        }
    }
}
