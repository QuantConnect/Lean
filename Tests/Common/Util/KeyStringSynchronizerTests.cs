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
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class KeyStringSynchronizerTests
    {
        [Test]
        public void ReEntrancy()
        {
            var synchronizer = new KeyStringSynchronizer();

            var counter = 0;
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var task = new Task(() =>
            {
                var key = "someKey";
                synchronizer.Execute(
                    key,
                    singleExecution: true,
                    () =>
                    {
                        synchronizer.Execute(
                            key,
                            singleExecution: true,
                            () =>
                            {
                                counter++;
                            }
                        );
                    }
                );
                cancellationToken.Cancel();
            });
            task.Start();

            cancellationToken.Token.WaitHandle.WaitOne();

            Assert.AreEqual(1, counter);
        }

        [Test]
        public void ExecuteOnce()
        {
            var synchronizer = new KeyStringSynchronizer();

            var counter = 0;
            var endedCount = 0;
            var taskCount = 10;
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            for (var i = 0; i < taskCount; i++)
            {
                var task = new Task(() =>
                {
                    synchronizer.Execute(
                        new string("someKey"),
                        singleExecution: true,
                        () =>
                        {
                            Thread.Sleep(4000);
                            counter++;
                        }
                    );

                    if (Interlocked.Increment(ref endedCount) == taskCount)
                    {
                        // the end
                        cancellationToken.Cancel();
                    }
                });
                task.Start();
            }

            cancellationToken.Token.WaitHandle.WaitOne();

            Assert.AreEqual(1, counter);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExecuteMultipleTimes(bool shouldThorw)
        {
            var synchronizer = new KeyStringSynchronizer();

            var counter = 0;
            var endedCount = 0;
            var taskCount = 10;
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            for (var i = 0; i < taskCount; i++)
            {
                var task = new Task(() =>
                {
                    var exceptionCount = 0;
                    for (var i = 0; i < 5; i++)
                    {
                        try
                        {
                            synchronizer.Execute(
                                "someKey",
                                singleExecution: false,
                                () =>
                                {
                                    counter++;
                                    if (shouldThorw)
                                    {
                                        throw new Exception("This shouldn't matter");
                                    }
                                }
                            );
                        }
                        catch (Exception)
                        {
                            exceptionCount++;
                        }
                    }

                    Assert.AreEqual(shouldThorw ? 5 : 0, exceptionCount);

                    if (Interlocked.Increment(ref endedCount) == taskCount)
                    {
                        // the end
                        cancellationToken.Cancel();
                    }
                });
                task.Start();
            }

            cancellationToken.Token.WaitHandle.WaitOne();

            Assert.AreEqual(50, counter);
        }

        [Test]
        public void ExecuteFunc()
        {
            var synchronizer = new KeyStringSynchronizer();

            var counter = 0;
            var endedCount = 0;
            var taskCount = 10;
            using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            for (var i = 0; i < taskCount; i++)
            {
                var task = new Task(() =>
                {
                    var result = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        var newResult = synchronizer.Execute(
                            "someKey",
                            () =>
                            {
                                return ++counter;
                            }
                        );

                        Assert.Greater(newResult, result);
                        result = newResult;
                    }

                    if (Interlocked.Increment(ref endedCount) == taskCount)
                    {
                        // the end
                        cancellationToken.Cancel();
                    }
                });
                task.Start();
            }

            cancellationToken.Token.WaitHandle.WaitOne();

            Assert.AreEqual(50, counter);
        }
    }
}
