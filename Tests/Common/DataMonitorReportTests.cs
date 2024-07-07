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

using System.Collections.Generic;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class DataMonitorReportTests
    {
        [Test]
        public void TotalRequestsCountIsTheSumOfSucceededAndFailedRequests()
        {
            var report = new DataMonitorReport
            {
                SucceededDataRequestsCount = 6,
                FailedDataRequestsCount = 3
            };

            Assert.AreEqual(9, report.TotalRequestsCount);
        }

        [Test]
        public void TotalUniverseRequestsCountIsTheSumOfSucceededAndFailedUniverseRequests()
        {
            var report = new DataMonitorReport
            {
                SucceededUniverseDataRequestsCount = 6,
                FailedUniverseDataRequestsCount = 3
            };

            Assert.AreEqual(9, report.TotalUniverseDataRequestsCount);
        }

        [Test]
        public void FailedRequestsPercentageIsProperlyCalculatedAndRounded()
        {
            var report = new DataMonitorReport
            {
                SucceededDataRequestsCount = 6,
                FailedDataRequestsCount = 3
            };

            Assert.AreEqual(33d, report.FailedDataRequestsPercentage);
        }

        [Test]
        public void FailedUniverseRequestsPercentageIsProperlyCalculatedAndRounded()
        {
            var report = new DataMonitorReport
            {
                SucceededUniverseDataRequestsCount = 6,
                FailedUniverseDataRequestsCount = 3
            };

            Assert.AreEqual(33d, report.FailedUniverseDataRequestsPercentage);
        }
    }
}
