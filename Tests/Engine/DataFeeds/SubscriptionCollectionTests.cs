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
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class SubscriptionCollectionTests
    {
        [Test]
        public void EnumerationWhileUpdatingDoesNotThrow()
        {
            var cts = new CancellationTokenSource();
            var subscriptions = new SubscriptionCollection();
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(10);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, DateTimeZone.Utc, DateTimeZone.Utc, true, false, false);
            var security = new Equity(Symbols.SPY, SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc), new Cash("USD", 0, 1), SymbolProperties.GetDefault("USD"));
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(DateTimeZone.Utc, start, end);
            var enumerator = new EnqueueableEnumerator<BaseData>();
            var subscriptionDataEnumerator = SubscriptionData.Enumerator(config, security, timeZoneOffsetProvider, enumerator);
            var subscription = new Subscription(null, security, config, subscriptionDataEnumerator, timeZoneOffsetProvider, start, end, false);

            var addTask = new TaskFactory().StartNew(() =>
            {
                Console.WriteLine("Add task started");

                while (DateTime.UtcNow < end)
                {
                    if (!subscriptions.Contains(config))
                    {
                        subscriptions.TryAdd(subscription);
                    }

                    Thread.Sleep(1);
                }

                Console.WriteLine("Add task ended");
            }, cts.Token);

            var removeTask = new TaskFactory().StartNew(() =>
            {
                Console.WriteLine("Remove task started");

                while (DateTime.UtcNow < end)
                {
                    Subscription removed;
                    subscriptions.TryRemove(config, out removed);

                    Thread.Sleep(1);
                }

                Console.WriteLine("Remove task ended");
            }, cts.Token);

            var readTask = new TaskFactory().StartNew(() =>
            {
                Console.WriteLine("Read task started");

                while (DateTime.UtcNow < end)
                {
                    foreach (var sub in subscriptions) { }

                    Thread.Sleep(1);
                }

                Console.WriteLine("Read task ended");
            }, cts.Token);

            Task.WaitAll(addTask, removeTask, readTask);
        }
    }
}
