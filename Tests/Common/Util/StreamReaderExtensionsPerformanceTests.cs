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
using System.Collections.Generic;
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
            using var zipCache = new ZipDataCacheProvider(TestGlobals.DataProvider);
            var date = new DateTime(2013, 10, 07);
            var (streamReader, streamReaderSource) = CreateReader(streamReaderType, tickType, zipCache, date);
            var (getLineReader, getLineReaderSource) = CreateReader(readLineReaderType, tickType, zipCache, date);

            // warmup
            streamReader.Read(streamReaderSource).Count();
            getLineReader.Read(getLineReaderSource).Count();

            var streamReaderTimes = new List<TimeSpan>();
            var streamReaderCount = 0;
            var getLineReaderTimes = new List<TimeSpan>();
            var getLineReaderCount = 0;
            var stopWatch = new Stopwatch();
            // alternate the readers so machine load and JIT tiering weigh on both alike
            for (int i = 0; i < 300; i++)
            {
                stopWatch.Restart();
                streamReaderCount += streamReader.Read(streamReaderSource).Count();
                streamReaderTimes.Add(stopWatch.Elapsed);

                stopWatch.Restart();
                getLineReaderCount += getLineReader.Read(getLineReaderSource).Count();
                getLineReaderTimes.Add(stopWatch.Elapsed);
            }
            // the median read leaves out the few a GC pause or a CPU throttle stretched by tens of milliseconds
            var streamReaderMedian = streamReaderTimes.Order().ElementAt(streamReaderTimes.Count / 2);
            var getLineReaderMedian = getLineReaderTimes.Order().ElementAt(getLineReaderTimes.Count / 2);
            Log.Trace($"StreamReader: median {streamReaderMedian.TotalMilliseconds}ms. Count {streamReaderCount}");
            Log.Trace($"GetLine Reader: median {getLineReaderMedian.TotalMilliseconds}ms. Count {getLineReaderCount}");

            // the stream reader isn't faster than the line reader anymore, this keeps the bound the test always enforced
            Assert.Less(streamReaderMedian, getLineReaderMedian * 2);
            Assert.AreEqual(getLineReaderCount, streamReaderCount);
        }

        private static (TextSubscriptionDataSourceReader, SubscriptionDataSource) CreateReader(Type dataType, TickType tickType,
            ZipDataCacheProvider zipCache, DateTime date)
        {
            var config = new SubscriptionDataConfig(
                dataType,
                Symbols.SPY,
                Resolution.Minute,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                true,
                false,
                tickType: tickType
            );
            var reader = new TextSubscriptionDataSourceReader(
                zipCache,
                config,
                date,
                false,
                null);
            return (reader, dataType.GetBaseDataInstance().GetSource(config, date, false));
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
