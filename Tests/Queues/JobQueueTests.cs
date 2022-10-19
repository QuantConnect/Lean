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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using Newtonsoft.Json;

using QuantConnect.Configuration;
using QuantConnect.Queues;
using QuantConnect.Python;

namespace QuantConnect.Tests.Queues
{
    [TestFixture]
    public class JobQueueTests
    {
        [Test]
        public void NextJobAddsAlgorithmLocationAtTheBeginning()
        {
            var algorithmDirectory = "Python/PandasTests";
            var algorithmFile = $"{algorithmDirectory}/PandasIndexingTests.py";
            Config.Set("algorithm-location", algorithmFile);
            Config.Set("algorithm-language", "Python");
            
            JobQueue jobQueue = new JobQueue();
            var job = jobQueue.NextJob(out _);

            var testDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            PythonInitializer.AddPythonPaths(new string[] { testDirectory });

            var paths = GetPythonPaths();

            Assert.IsTrue(paths.First().EndsWith(algorithmDirectory, StringComparison.Ordinal),
                $"Expected {paths.First()} to end with {algorithmDirectory}");
        }

        private static IEnumerable<string> GetPythonPaths()
        {
            using (Py.GIL())
            {
                using dynamic sys = Py.Import("sys");
                using var locals = new PyDict();
                locals.SetItem("sys", sys);

                // Filter out any already paths that already exist on our current PythonPath
                using var pythonCurrentPath = PythonEngine.Eval("sys.path", locals: locals);

                return pythonCurrentPath.As<List<string>>();
            }
        }
    }
}
