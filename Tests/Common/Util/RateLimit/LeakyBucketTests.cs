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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Util.RateLimit;

namespace QuantConnect.Tests.Common.Util.RateLimit
{
    [TestFixture]
    public class LeakyBucketTests
    {
        [Test]
        public void BucketIsInitializedWithAvailableEqualToCapacity()
        {
            var bucket = new LeakyBucket(10, 1, Time.OneSecond);
            Assert.AreEqual(10, bucket.AvailableTokens);
        }

        [Test]
        public void ConsumeBlocksUntilTokensAreAvailable()
        {
            var time = new DateTime(2000, 01, 01);
            var timeProvider = new ManualTimeProvider(time);

            const int refillAmount = 1;
            var refillInterval = TimeSpan.FromMinutes(1);
            var refillStrategy = new FixedIntervalRefillStrategy(timeProvider, refillAmount, refillInterval);

            // using spin wait strategy to ensure we update AvailableTokens as quickly as possible
            var sleepStrategy = new BusyWaitSleepStrategy();

            const int capacity = 10;
            var bucket = new LeakyBucket(capacity, sleepStrategy, refillStrategy, timeProvider);

            // first remove half the capacity
            bucket.Consume(capacity/2);

            // we've consumed half of the available tokens
            Assert.AreEqual(capacity/2, bucket.AvailableTokens);

            var taskStarted = new ManualResetEvent(false);
            var bucketConsumeCompleted = new ManualResetEvent(false);
            Task.Run(() =>
            {
                taskStarted.Set();

                // this will block until time advances
                bucket.Consume(capacity);
                bucketConsumeCompleted.Set();
            });

            taskStarted.WaitOne();

            // each loop we'll advance one refill increment and when the loop finishes
            // the bucket's consume operation will succeed
            var initialAmount = bucket.AvailableTokens;

            for (int i = 0; i < 5; i++)
            {
                timeProvider.Advance(refillInterval);

                // on the last loop, the bucket will consume all ten
                if (i != 4)
                {
                    var count = 0;
                    while (++count < 100 && (initialAmount + (1 + i) * refillAmount) != bucket.AvailableTokens)
                    {
                        Thread.Sleep(1);
                    }

                    // each time we advance the number of available tokens will increment by the refill amount
                    Assert.AreEqual(initialAmount + (1 + i) * refillAmount, bucket.AvailableTokens,
                        $"CurrentTime: {timeProvider.GetUtcNow():O}: Iteration: {i}"
                    );
                }
            }

            // now that we've advanced, bucket consumption should have completed
            // we provide for a small timeout to support non-multi-threaded machines
            Assert.IsTrue(bucketConsumeCompleted.WaitOne(1000), "Timeout waiting for consumer");
            Assert.AreEqual(0, bucket.AvailableTokens, $"There are still available tokens {bucket.AvailableTokens}");
        }
    }
}
