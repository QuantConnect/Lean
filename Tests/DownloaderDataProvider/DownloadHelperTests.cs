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

using System;
using NUnit.Framework;
using QuantConnect.DownloaderDataProvider.Launcher;

namespace QuantConnect.Tests.DownloaderDataProvider
{
    [TestFixture]
    public class DownloadHelperTests
    {
        [TestCase("2020/01/01", "2024/01/01", 3, 0)]
        public void CalculateETAShouldDownloadAllSymbols(DateTime downloadStartDate, DateTime downloadEndDate, int amountDownloadSymbol, int alreadyDownloadedSymbol)
        {
            var totalDataPerSymbolInSeconds = (downloadEndDate - downloadStartDate).TotalSeconds;
            var totalDataInSeconds = totalDataPerSymbolInSeconds * amountDownloadSymbol;
            
            var startUtcTime = DateTime.UtcNow;
            var endDateTime = downloadStartDate;
            while (alreadyDownloadedSymbol != amountDownloadSymbol)
            {
                do
                {
                    endDateTime = endDateTime.AddDays(1);

                    var utcNow = DateTime.UtcNow;
                    var progressSoFar = (endDateTime - downloadStartDate).TotalSeconds + totalDataPerSymbolInSeconds * alreadyDownloadedSymbol;
                    var eta = Program.CalculateETA(utcNow, startUtcTime, totalDataInSeconds, progressSoFar);

                    if (endDateTime < downloadEndDate)
                    {
                        Assert.Greater(eta.TotalSeconds, 0);
                    }
                } while (endDateTime != downloadEndDate);

                endDateTime = downloadStartDate;
                alreadyDownloadedSymbol++;
            }

            Assert.That(amountDownloadSymbol, Is.EqualTo(alreadyDownloadedSymbol));
        }

        [TestCase("2020/01/01", "2024/01/01", "2020/1/10", 1, 0, 1, 161)]
        [TestCase("2019/01/01", "2023/01/01", "2021/2/10", 2, 1, 10, 3)]
        [TestCase("2021/01/01", "2022/01/01", "2021/5/10", 3, 2, 5, 1)]
        public void CalculateETAShouldReturnCorrectETA(
            DateTime downloadStartDate,
            DateTime downloadEndDate,
            DateTime currentDownloadedDataFromDataDownloader,
            int amountOfDownloadSymbol,
            int alreadyDownloadedSymbol,
            int minusUtcNowSecond,
            int expectedTotalSeconds)
        {
            var mockUtcTimeNow = new DateTime(2024, 04, 26, 1, 1, 10);

            DateTime runUtcTime = mockUtcTimeNow.AddSeconds(-minusUtcNowSecond);

            var totalDataPerSymbolInSeconds = (downloadEndDate - downloadStartDate).TotalSeconds;
            var totalDataInSeconds = totalDataPerSymbolInSeconds * amountOfDownloadSymbol;

            var progressSoFar = (currentDownloadedDataFromDataDownloader - downloadStartDate).TotalSeconds + totalDataPerSymbolInSeconds * alreadyDownloadedSymbol;

            var eta = Program.CalculateETA(mockUtcTimeNow, runUtcTime, totalDataInSeconds, progressSoFar);

            Assert.That(expectedTotalSeconds, Is.EqualTo((int)eta.TotalSeconds));
        }
    }
}
