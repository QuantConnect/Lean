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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Report.ReportElements;
using QuantConnect.Tests.Report.Capacity.Strategies;

namespace QuantConnect.Tests.Report.Capacity
{
    /// <summary>
    /// </summary>
    [TestFixture, Category("TravisExclude"), Ignore("Requires a ton of data to run these tests that doesn't exist in the repo")]

    public class StrategyCapacityTests
    {
        [TestCase(nameof(SpyBondPortfolioRebalance), 2900000)]
        [TestCase(nameof(BeastVsPenny), 370000)]
        [TestCase(nameof(MonthlyRebalanceHourly), 11000000)]
        [TestCase(nameof(MonthlyRebalanceDaily), 11000000)]
        [TestCase(nameof(IntradayMinuteScalping), 31000000)]
        [TestCase("IntradayMinuteScalpingBTCETH", 13000)]
        [TestCase("IntradayMinuteScalpingEURUSD", 4800000)]
        [TestCase("IntradayMinuteScalpingGBPJPY", 4700000)]
        [TestCase("IntradayMinuteScalpingTRYJPY", 4300000)]
        [TestCase(nameof(CheeseMilkHourlyRebalance), 57000)]
        [TestCase(nameof(IntradayMinuteScalpingFuturesES), 36000000)]
        [TestCase(nameof(EmaPortfolioRebalance100), 2600)]
        public void TestCapacity(string strategy, int expectedCapacity)
        {
            var backtest = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText(Path.Combine("Report", "Capacity", "Strategies", $"{strategy}.json")), new OrderJsonConverter());
            var capacityReportElement = new EstimatedCapacityReportElement("estimated capacity", "key", backtest, null);

            capacityReportElement.Render();

            Assert.AreEqual(expectedCapacity, (double)(decimal)capacityReportElement.Result);
        }
    }
}
