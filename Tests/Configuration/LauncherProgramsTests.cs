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
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Configuration
{
    [TestFixture, NonParallelizable]
    public class LauncherProgramsTests
    {
        private const string Lean = "QuantConnect.Lean.Launcher.Program, QuantConnect.Lean.Launcher";
        private const string Report = "QuantConnect.Report.Program, QuantConnect.Report";
        private const string Downloader = "QuantConnect.DownloaderDataProvider.Launcher.Program, QuantConnect.DownloaderDataProvider.Launcher";

        private ILogHandler _logHandler;
        private bool _debuggingEnabled;

        [SetUp]
        public void SetUp()
        {
            _logHandler = Log.LogHandler;
            _debuggingEnabled = Log.DebuggingEnabled;

            // Each launcher fails right away once it carries on past parsing its arguments
            Config.Set("log-handler", "NonExistentLogHandler");
            Config.Set("result-handler", "NonExistentResultHandler");
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            Log.LogHandler = _logHandler;
            Log.DebuggingEnabled = _debuggingEnabled;
        }

        [TestCase(Lean, "--help")]
        [TestCase(Lean, "-h")]
        [TestCase(Lean, "-?")]
        [TestCase(Lean, "--version")]
        [TestCase(Lean, "-V")]
        [TestCase(Lean, "--close-automatically", "true", "--help")]
        [TestCase(Report, "--help")]
        [TestCase(Report, "--version")]
        [TestCase(Downloader, "--help")]
        [TestCase(Downloader, "--version")]
        public void ExitsWithoutRunningWhenHelpOrVersionIsRequested(string program, params string[] args)
        {
            Assert.DoesNotThrow(() => RunMain(program, args));
        }

        [TestCase(Lean, "--close-automatically", "true")]
        [TestCase(Report, "--close-automatically", "true")]
        [TestCase(Downloader, "--data-type", "Trade")]
        public void RunsWhenOptionsAreGiven(string program, params string[] args)
        {
            Assert.Throws<AggregateException>(() => RunMain(program, args));
        }

        private static void RunMain(string program, string[] args)
        {
            var main = Type.GetType(program, throwOnError: true).GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var task = Task.Run(() => main.Invoke(null, new object[] { args }));
            Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(30)), "The launcher did not exit");
        }
    }
}
