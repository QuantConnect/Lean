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

using System.IO;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DataMonitorTests
    {
        [Test]
        public void NoReportWithoutDataRequests()
        {
            using var monitor = new DataMonitor();

            Assert.IsNull(monitor.Report);

            monitor.Exit();

            Assert.IsNull(monitor.Report);
        }

        [Test]
        public void ReportNamesTheFailedRequestsFileOnExit()
        {
            var succeededRequestFiles = Directory.GetFiles(Globals.ResultsDestinationFolder, "succeeded-data-requests-*.txt").Length;
            var reportFiles = Directory.GetFiles(Globals.ResultsDestinationFolder, "*data-monitor-report*.json").Length;
            using var monitor = new DataMonitor();
            var failedPath = Path.Combine(Globals.DataFolder, "equity", "usa", "minute", "spy", "20130101_trade.zip");

            monitor.OnNewDataRequest(this, new DataProviderNewDataRequestEventArgs(Path.Combine(Globals.DataFolder, "equity", "usa", "daily", "spy.zip"), true, string.Empty));
            monitor.OnNewDataRequest(this, new DataProviderNewDataRequestEventArgs(Path.Combine(Globals.DataFolder, "equity", "usa", "universes", "etf", "spy.csv"), false, "not found"));
            monitor.OnNewDataRequest(this, new DataProviderNewDataRequestEventArgs(failedPath, false, "not found"));

            Assert.IsNull(monitor.Report);

            monitor.Exit();

            var report = monitor.Report;
            Assert.IsNotNull(report);
            Assert.AreEqual(1, report.SucceededDataRequestsCount);
            Assert.AreEqual(2, report.FailedDataRequestsCount);
            Assert.AreEqual(1, report.FailedUniverseDataRequestsCount);

            // the report names the request files written next to it, the result handler stores the report itself
            Assert.That(report.FailedDataRequestsFile, Does.StartWith("failed-data-requests-"));
            var failedRequests = File.ReadAllLines(Path.Combine(Globals.ResultsDestinationFolder, report.FailedDataRequestsFile));
            Assert.AreEqual(2, failedRequests.Length);
            Assert.AreEqual(failedPath.Substring(Globals.DataFolder.Length), failedRequests[1]);
            Assert.AreEqual(reportFiles, Directory.GetFiles(Globals.ResultsDestinationFolder, "*data-monitor-report*.json").Length);

            // succeeded requests are only counted by default, 'data-monitor-store-succeeded-requests' lists them
            Assert.IsNull(report.SucceededDataRequestsFile);
            Assert.AreEqual(succeededRequestFiles, Directory.GetFiles(Globals.ResultsDestinationFolder, "succeeded-data-requests-*.txt").Length);
        }
    }
}
