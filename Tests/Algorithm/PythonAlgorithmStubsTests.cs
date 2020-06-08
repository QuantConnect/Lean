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

using NUnit.Framework;
using System.Diagnostics;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class PythonAlgorithmStubsTests
    {
        [Test]
        public void BasicTemplateAlgorithmTypeChecks()
        {
            var mypyProcess = new Process()
            {
                StartInfo = new ProcessStartInfo("mypy", "--implicit-reexport --check-untyped-defs --python-version 3.6 --disallow-any-explicit --ignore-missing-imports ../../../Algorithm.Python/BasicTemplateAlgorithm.py")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            mypyProcess.Start();
            mypyProcess.WaitForExit();

            Assert.AreEqual(0, mypyProcess.ExitCode);
        }
    }
}
