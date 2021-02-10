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
