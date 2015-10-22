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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class BaseDataExchangeTests
    {
        [Test]
        public void FiresCorrectHandlerBySymbol()
        {
            var dataQueue = new ConcurrentQueue<BaseData>();
            var exchange = CreateExchange(dataQueue);
            exchange.SleepInterval = 1;

            var firedHandler = false;
            var firedWrongHandler = false;
            exchange.SetHandler("SPY", spy =>
            {
                firedHandler = true;
            });
            exchange.SetHandler("EURUSD", eurusd =>
            {
                firedWrongHandler = true;
            });

            dataQueue.Enqueue(new Tick{Symbol = "SPY"});

            exchange.Start();

            Thread.Sleep(10);

            Assert.IsTrue(firedHandler);
            Assert.IsFalse(firedWrongHandler);
        }

        [Test]
        public void RemovesHandlerBySymbol()
        {
            var dataQueue = new ConcurrentQueue<BaseData>();
            var exchange = CreateExchange(dataQueue);

            var firedHandler = false;
            exchange.SetHandler("SPY", spy =>
            {
                firedHandler = true;
            });
            exchange.RemoveHandler("SPY");

            dataQueue.Enqueue(new Tick {Symbol = "SPY"});

            exchange.Start();

            Thread.Sleep(10);

            Assert.IsFalse(firedHandler);
        }

        [Test, Category("TravisExclude")]
        public void EndsQueueConsumption()
        {
            var dataQueue = new ConcurrentQueue<BaseData>();
            var exchange = CreateExchange(dataQueue);
            
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    dataQueue.Enqueue(new Tick {Symbol = "SPY", Time = DateTime.UtcNow});
                }
            });

            BaseData last = null;
            exchange.SetHandler("SPY", spy =>
            {
                last = spy;
            });

            exchange.Start();

            Thread.Sleep(1);

            Thread.Sleep(25);

            exchange.Stop();

            var endTime = DateTime.UtcNow;

            Assert.IsNotNull(last);
            Assert.IsTrue(last.Time <= endTime);
        }

        [Test]
        public void DefaultErrorHandlerDoesNotStopQueueConsumption()
        {
            var dataQueue = new ConcurrentQueue<BaseData>();
            var exchange = CreateExchange(dataQueue);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    dataQueue.Enqueue(new Tick { Symbol = "SPY", Time = DateTime.UtcNow });
                }
            });

            var first = true;
            BaseData last = null;
            exchange.SetHandler("SPY", spy =>
            {
                if (first)
                {
                    first = false;
                    throw new Exception("This exception should be swalloed by the exchange!");
                }
                last = spy;
            });

            exchange.Start();

            Thread.Sleep(50);

            exchange.Stop();

            Assert.IsNotNull(last);
        }

        [Test]
        public void SetErrorHandlerExitsOnTrueReturn()
        {
            var dataQueue = new ConcurrentQueue<BaseData>();
            var exchange = CreateExchange(dataQueue);

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);
                    dataQueue.Enqueue(new Tick { Symbol = "SPY", Time = DateTime.UtcNow });
                }
            });

            var first = true;
            BaseData last = null;
            exchange.SetHandler("SPY", spy =>
            {
                if (first)
                {
                    first = false;
                    throw new Exception();
                }
                last = spy;
            });

            exchange.SetErrorHandler(error => true);

            exchange.Start();

            Thread.Sleep(25);

            exchange.Stop();

            Assert.IsNull(last);
        }

        private static BaseDataExchange CreateExchange(ConcurrentQueue<BaseData> dataQueue)
        {
            var dataQueueHandler = new FuncDataQueueHandler(q =>
            {
                BaseData data;
                int count = 0;
                var list = new List<BaseData>();
                while (++count < 10 && dataQueue.TryDequeue(out data)) list.Add(data);
                return list;
            });
            var exchange = new BaseDataExchange();
            exchange.AddEnumerator(GetNextTicksEnumerator(dataQueueHandler));
            return exchange;
        }

        private static IEnumerator<BaseData> GetNextTicksEnumerator(IDataQueueHandler dataQueueHandler)
        {
            while (true)
            {
                int ticks = 0;
                foreach (var data in dataQueueHandler.GetNextTicks())
                {
                    ticks++;
                    yield return data;
                }
                if (ticks == 0) Thread.Sleep(1);
            }
        }
    }
}
