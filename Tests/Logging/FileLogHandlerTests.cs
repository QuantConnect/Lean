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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Logging
{
    [TestFixture]
    public class FileLogHandlerTests
    {
        [Test]
        public void WritesMessageToFile()
        {
            const string file = "log2.txt";
            File.Delete(file);

            var debugMessage = "*debug message*" + DateTime.UtcNow.ToStringInvariant("o");
            using (var log = new FileLogHandler(file))
            {
                log.Debug(debugMessage);
            }

            var contents = File.ReadAllText(file);
            Assert.IsNotNull(contents);
            Assert.IsTrue(contents.Contains(debugMessage));

            File.Delete(file);
        }

        [Test]
        public void UsesGlobalFilePath()
        {
            var previous = Log.FilePath;
            Directory.CreateDirectory("filePathTest");
            Log.FilePath = Path.Combine("filePathTest", "log2.txt");
            File.Delete(Log.FilePath);

            var debugMessage = "*debug message*" + DateTime.UtcNow.ToStringInvariant("o");
            using (var log = new FileLogHandler())
            {
                log.Debug(debugMessage);
            }

            var contents = File.ReadAllText(Log.FilePath);
            Log.FilePath = previous;

            Assert.IsNotNull(contents);
            Assert.IsTrue(contents.Contains(debugMessage));

            File.Delete(Log.FilePath);
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void TestLoggingSpeeds(int x)
        {
            var start = DateTime.Now;
            const string file = "log2.txt";
            List<Thread> threads = new List<Thread>();

            //Delete the file if it exists.
            File.Delete(file);

            using (var log = new FileLogHandler(file))
            {
                //Spin off x threads that will use the log handler
                for (int i = 0; i < x; i++)
                {
                    var threadNumber = i;
                    Thread thread = new Thread(() => {
                        for (int j = 0; j < 1000; j++)
                        {
                            var debugMessage = $"debug message {j} for thread {threadNumber}";
                            log.Debug(debugMessage);
                        }
                    })
                    {
                        IsBackground = true,
                        Name = $"LogTestThread{i}"
                    };

                    thread.Start();
                    threads.Add(thread);
                }

                //Wait for all threads to complete
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }

            var end = DateTime.Now;
            var time = start - end;

            Console.WriteLine(time);
        }
    }
}
