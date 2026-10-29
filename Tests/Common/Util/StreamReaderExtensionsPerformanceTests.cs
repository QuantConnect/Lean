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
    [TestFixture, NonParallelizable]
    public class StreamReaderExtensionsPerformanceTests
    {
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
                using var zipCache = new ZipDataCacheProvider(TestGlobals.DataProvider);
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
                using var zipCache = new ZipDataCacheProvider(TestGlobals.DataProvider);
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
