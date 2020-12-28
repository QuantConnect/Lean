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

using QuantConnect.Statistics;
using Newtonsoft.Json;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class JsonRoundingConverterTests
    {
        [Test]
        public void MinMaxValueDeserializesSuccessfuly()
        {
            var portfolioStatistics = new PortfolioStatistics
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
