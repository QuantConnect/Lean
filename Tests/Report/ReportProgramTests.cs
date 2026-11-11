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
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Report
{
    [TestFixture, NonParallelizable, Platform("Linux")]
    public class ReportProgramTests
    {
        private string _directory;
        private string _path;
        private ILogHandler _logHandler;
        private bool _debuggingEnabled;

        [SetUp]
        public void SetUp()
        {
            _logHandler = Log.LogHandler;
            _debuggingEnabled = Log.DebuggingEnabled;
            _path = Environment.GetEnvironmentVariable("PATH");
            _directory = Directory.CreateTempSubdirectory("report program ").FullName;
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
            Log.LogHandler = _logHandler;
            Log.DebuggingEnabled = _debuggingEnabled;
            Environment.SetEnvironmentVariable("PATH", _path);
            Environment.ExitCode = 0;
            Directory.Delete(_directory, true);
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        public void ConvertsToPdfDestinationContainingSpaces(int converterExitCode, int expectedExitCode)
        {
            var argumentsFile = Path.Combine(_directory, "arguments.txt");
            var converter = Path.Combine(_directory, "xvfb-run");
            File.WriteAllText(converter, $"#!/bin/sh\nprintf '%s\\n' \"$@\" > '{argumentsFile}'\nexit {converterExitCode}\n");
            File.SetUnixFileMode(converter, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Environment.SetEnvironmentVariable("PATH", $"{_directory}{Path.PathSeparator}{_path}");

            var reportDirectory = Directory.CreateDirectory(Path.Combine(_directory, "my reports")).FullName;
            var destination = Path.Combine(reportDirectory, "report.html");
            Config.Set("backtest-data-source-file", Path.Combine("TestData", "test_report_data.json"));
            Config.Set("live-data-source-file", "");
            Config.Set("report-destination", destination);
            Config.Set("report-format", "PDF");
            Config.Set("close-automatically", "true");

            var main = Type.GetType("QuantConnect.Report.Program, QuantConnect.Report", throwOnError: true)
                .GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            main.Invoke(null, new object[] { Array.Empty<string>() });

            Assert.IsTrue(File.Exists(destination));
            CollectionAssert.AreEqual(
                new[] { "--server-args=-screen 0, 1600x1200x24+32", "wkhtmltopdf", destination, Path.Combine(reportDirectory, "report.pdf") },
                File.ReadAllLines(argumentsFile));
            Assert.AreEqual(expectedExitCode, Environment.ExitCode);
        }
    }
}
