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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Threading;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DataFileLockTests
    {
        [Test]
        public void MultiThreadedDataFileLocking()
        {
            var file = "file1.txt";
            int concurrentThreads = 0;

            File.Delete(file);

            Parallel.ForEach(Enumerable.Range(1, 10), t =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                for (var i = 0; i < 100; i++)
                {
                    string lockName;

                    using (var acquired = new BasicFileLock(file))
                    {
                        Interlocked.Increment(ref concurrentThreads);
                        lockName = acquired.LockName;

                        Assert.LessOrEqual(concurrentThreads, 1);

                        try
                        {
                            using (var inStream = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.None))
                            {
                                using (var streamWriter = new StreamWriter(inStream))
                                {
                                    streamWriter.WriteLine(threadId.ToString());
                                    streamWriter.Flush();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                        Interlocked.Decrement(ref concurrentThreads);
                    }
                }
            });

            Assert.AreEqual(1000, File.ReadAllLines(file).Length);
        }
    }
}
