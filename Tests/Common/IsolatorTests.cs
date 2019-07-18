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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class IsolatorTests
    {
        [Test]
        public void WorksCorrectlyUsingWorker()
        {
            using (var worker = new WorkerThread())
            {
                var isolator = new Isolator();
                var executed = false;
                var result = isolator.ExecuteWithTimeLimit(
                    TimeSpan.FromMilliseconds(100),
                    () =>
                    {
                        executed = true;
                    },
                    5000,
                    workerThread:worker
                );
                Assert.IsTrue(result);
                Assert.IsTrue(executed);
            }
        }

        [TestCase(Language.Python, true)]
        [TestCase(Language.Python, false)]
        [TestCase(Language.CSharp, true)]
        [TestCase(Language.CSharp, false)]
        public void TimeOutWorkCorrectly(Language language, bool useWorker)
        {
            var worker = useWorker ? new WorkerThread() : null;
            using (worker)
            {
                var isolator = new Isolator();
                bool result = false;
                try
                {
                    if (language == Language.CSharp)
                    {
                        result = isolator.ExecuteWithTimeLimit(
                            TimeSpan.FromMilliseconds(100),
                            () => Thread.Sleep(10000), // 10s sleep
                            5000,
                            workerThread: worker
                        );
                    }
                    else
                    {
                        result = isolator.ExecuteWithTimeLimit(
                            TimeSpan.FromMilliseconds(100),
                            () =>
                            {
                                using (Py.GIL())
                                {
                                    // 10s sleep
                                    PythonEngine.RunSimpleString("import time; time.sleep(10)");
                                }
                            },
                            5000,
                            workerThread: worker
                        );
                    }

                    Assert.Fail($"Was expecting {nameof(TimeoutException)}");
                }
                catch (TimeoutException)
                {
                    Assert.IsFalse(result);
                }
            }
        }
    }
}