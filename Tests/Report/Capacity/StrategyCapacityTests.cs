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
    [TestFixture]
    public class StrategyCapacityTests
    {
        [TestCase(nameof(SpyBondPortfolioRebalance), 2940000)]
        [TestCase(nameof(BeastVsPenny), 177000)]
        [TestCase(nameof(MonthlyRebalanceHourly), 10900000)]
        [TestCase(nameof(MonthlyRebalanceDaily), 10900000)]
        [TestCase(nameof(IntradayMinuteScalping), 26300000)]
        [TestCase("IntradayMinuteScalpingBTCETH", 132000)]
        [TestCase("IntradayMinuteScalpingEURUSD", 2380000)]
        [TestCase("IntradayMinuteScalpingGBPJPY", 2240000)]
        [TestCase("IntradayMinuteScalpingTRYJPY", 2270000)]
        [TestCase("CheeseMilkHourlyRebalance", 51900)]
        [TestCase(nameof(IntradayMinuteScalpingFuturesES), 20200000)]
        [TestCase(nameof(EmaPortfolioRebalance100), 203)]
        public void TestCapacity(string strategy, int expectedCapacity)
        {
            var backtest = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText(Path.Combine("Report", "Capacity", "Strategies", $"{strategy}.json")), new OrderJsonConverter());
            var capacityReportElement = new EstimatedCapacityReportElement("estimated capacity", "key", backtest, null);

            capacityReportElement.Render();

            Assert.AreEqual(expectedCapacity, (double)(decimal)capacityReportElement.Result);
        }
    }
}
