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
using Python.Runtime;

using QuantConnect.Python;

namespace QuantConnect.Tests.Common.Python
{
    [TestFixture]
    public class PythonInitializerTests
    {
        private const string ShutdownChildProcess = "LEAN_PYTHON_SHUTDOWN_CHILD_PROCESS";
        private const string ShutdownChildProcessCompleted = "LEAN_PYTHON_SHUTDOWN_COMPLETED";

        [Test]
        public void AlgorithmLocationIsAlwaysBeforeOtherPaths()
        {
            PythonInitializer.Initialize();
            PythonInitializer.ResetAlgorithmLocationPath();

            var testDirectory = Directory.CreateDirectory("TestDir").FullName.Replace('\\', '/');
            var algorithmDirectory = Directory.CreateDirectory("AlgoDir").FullName.Replace('\\', '/');

            PythonInitializer.AddAlgorithmLocationPath(algorithmDirectory);
            PythonInitializer.AddPythonPaths(new string[] { testDirectory });
            
            var paths = GetPythonPaths().ToList();

            Directory.Delete("TestDir", true);
            Directory.Delete("AlgoDir", true);

            var algorithmDirectoryIndex = paths.IndexOf(algorithmDirectory);
            var testDirectoryIndex = paths.IndexOf(testDirectory);

            Assert.AreNotEqual(-1, algorithmDirectoryIndex, string.Join(", ", paths));
            Assert.Less(algorithmDirectoryIndex, testDirectoryIndex);
        }

        [Test]
        [NonParallelizable]
        public void ShutdownCompletesWithoutLeakingGil()
        {
            if (Environment.GetEnvironmentVariable(ShutdownChildProcess) == "1")
            {
                PythonInitializer.Initialize();
                Assert.IsTrue(new Isolator().ExecuteWithTimeLimit(
                    TimeSpan.FromSeconds(10), PythonInitializer.Shutdown, -1));
                Assert.IsFalse(PythonEngine.IsInitialized);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                TestContext.Progress.WriteLine(ShutdownChildProcessCompleted);
                return;
            }

            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = TestContext.CurrentContext.TestDirectory
            };
            startInfo.ArgumentList.Add("vstest");
            startInfo.ArgumentList.Add(typeof(PythonInitializerTests).Assembly.Location);
            startInfo.ArgumentList.Add($"--Tests:{typeof(PythonInitializerTests).FullName}.{nameof(ShutdownCompletesWithoutLeakingGil)}");
            startInfo.ArgumentList.Add("--Logger:console;verbosity=detailed");
            startInfo.Environment[ShutdownChildProcess] = "1";
            startInfo.Environment["PYTHONNET_PYDLL"] = Runtime.PythonDLL;

            using var process = Process.Start(startInfo);
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit((int)TimeSpan.FromMinutes(1).TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                Assert.Fail("Timed out waiting for the Python shutdown child process.");
            }

            var output = standardOutput.GetAwaiter().GetResult() + standardError.GetAwaiter().GetResult();
            Assert.AreEqual(0, process.ExitCode, output);
            StringAssert.Contains(ShutdownChildProcessCompleted, output);
            StringAssert.DoesNotContain("GIL must always be released", output);
            StringAssert.DoesNotContain("Py.GILState.Finalize", output);
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
