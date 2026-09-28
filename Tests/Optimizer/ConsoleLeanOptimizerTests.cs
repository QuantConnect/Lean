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

using System.IO;
using NUnit.Framework;
using QuantConnect.Optimizer.Launcher;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture]
    public class ConsoleLeanOptimizerTests
    {
        [TestCase("QuantConnect.Lean.Launcher.dll")]
        [TestCase("QuantConnect.Lean.Launcher.DLL")]
        public void LaunchesAssemblyThroughTheDotnetHost(string fileName)
        {
            var leanLocation = Path.Combine("Lean", "Launcher", "bin", "Debug", fileName);

            var startInfo = ConsoleLeanOptimizer.CreateLeanStartInfo(leanLocation, "--algorithm-id \"backtest-id\"");

            Assert.AreEqual("dotnet", startInfo.FileName);
            Assert.AreEqual($"\"{leanLocation}\" --algorithm-id \"backtest-id\"", startInfo.Arguments);
            Assert.AreEqual(Path.GetFullPath(Path.Combine("Lean", "Launcher", "bin", "Debug")), startInfo.WorkingDirectory);
        }

        [TestCase("QuantConnect.Lean.Launcher")]
        [TestCase("QuantConnect.Lean.Launcher.exe")]
        public void LaunchesExecutableDirectly(string fileName)
        {
            var directory = Directory.CreateTempSubdirectory().FullName;
            try
            {
                // an explicitly configured executable is used as is, even with the assembly next to it
                File.WriteAllText(Path.Combine(directory, "QuantConnect.Lean.Launcher.dll"), string.Empty);
                var leanLocation = Path.Combine(directory, fileName);

                var startInfo = ConsoleLeanOptimizer.CreateLeanStartInfo(leanLocation, "--algorithm-id \"backtest-id\"");

                Assert.AreEqual(leanLocation, startInfo.FileName);
                Assert.AreEqual("--algorithm-id \"backtest-id\"", startInfo.Arguments);
                Assert.AreEqual(directory, startInfo.WorkingDirectory);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
