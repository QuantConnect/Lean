using QuantConnect.Statistics;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class JsonRoundingConverterTests
    {
        [Test]
        public void MinMaxValueDeserializesSuccessfuly()
        {
            var portfolioStatistics = new PortfolioStatistics()
            {
                AverageWinRate = decimal.MaxValue,
                AverageLossRate = decimal.MinValue,
                CompoundingAnnualReturn = decimal.MaxValue
            };

            var serializedValue = JsonConvert.SerializeObject(portfolioStatistics);
            var deserializedValue = JsonConvert.DeserializeObject<PortfolioStatistics>(serializedValue);

            Assert.AreEqual(portfolioStatistics.AverageWinRate, deserializedValue.AverageWinRate);
            Assert.AreEqual(portfolioStatistics.AverageLossRate, deserializedValue.AverageLossRate);
            Assert.AreEqual(portfolioStatistics.CompoundingAnnualReturn, deserializedValue.CompoundingAnnualReturn);

            Assert.AreEqual(portfolioStatistics.AnnualStandardDeviation, deserializedValue.AnnualStandardDeviation);
            Assert.AreEqual(portfolioStatistics.Expectancy, deserializedValue.Expectancy);
        }
    }
}
