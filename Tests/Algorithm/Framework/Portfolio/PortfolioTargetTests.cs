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

using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class PortfolioTargetTests
    {
        [Test]
        public void PercentInvokesBuyingPowerModelAndAddsInExistingHoldings()
        {
            const decimal bpmQuantity = 100;
            const decimal holdings = 50;
            const decimal targetPercent = 0.5m;

            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Initialize();
            algorithm.PostInitialize();
            var security = algorithm.Securities.Single().Value;
            security.SetMarketPrice(new Tick{Value = 1m});
            security.Holdings.SetHoldings(1m, holdings);

            var buyingPowerMock = new Mock<IBuyingPowerModel>();
            buyingPowerMock.Setup(bpm => bpm.GetMaximumOrderQuantityForTargetBuyingPower(It.IsAny<GetMaximumOrderQuantityForTargetBuyingPowerParameters>()))
                .Returns(new GetMaximumOrderQuantityResult(bpmQuantity, null, false));
            security.BuyingPowerModel = buyingPowerMock.Object;
            buyingPowerMock.Setup(bpm => bpm.GetLeverage(It.IsAny<Security>())).Returns(1);

            var target = PortfolioTarget.Percent(algorithm, security.Symbol, targetPercent);

            Assert.AreEqual(security.Symbol, target.Symbol);
            Assert.AreEqual(bpmQuantity + holdings, target.Quantity);
        }

        [Test]
        public void PercentReturnsNullIfPriceIsZero()
        {
            const decimal holdings = 50;
            const decimal targetPercent = 1m;

            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Initialize();
            algorithm.PostInitialize();
            var security = algorithm.Securities.Single().Value;
            security.SetMarketPrice(new Tick { Value = 0m });
            security.Holdings.SetHoldings(1m, holdings);

            var target = PortfolioTarget.Percent(algorithm, security.Symbol, targetPercent);

            Assert.IsNull(target);
        }

        [Test]
        public void PercentReturnsNullIfBuyingPowerModelError()
        {
            const decimal holdings = 50;
            const decimal targetPercent = 1m;

            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Initialize();
            algorithm.PostInitialize();
            var security = algorithm.Securities.Single().Value;
            security.SetMarketPrice(new Tick { Value = 1m });
            security.Holdings.SetHoldings(1m, holdings);

            var buyingPowerMock = new Mock<IBuyingPowerModel>();
            buyingPowerMock.Setup(bpm => bpm.GetMaximumOrderQuantityForTargetBuyingPower(It.IsAny<GetMaximumOrderQuantityForTargetBuyingPowerParameters>()))
                .Returns(new GetMaximumOrderQuantityResult(0, "The portfolio does not have enough margin available."));
            security.BuyingPowerModel = buyingPowerMock.Object;
            buyingPowerMock.Setup(bpm => bpm.GetLeverage(It.IsAny<Security>())).Returns(1);

            var target = PortfolioTarget.Percent(algorithm, security.Symbol, targetPercent);

            Assert.IsNull(target);
        }

        [Test]
        public void PercentReturnsNullForCanonicalFutureSymbol()
        {
            var algorithm = new AlgorithmStub();
            algorithm.SetFinishedWarmingUp();
            var future = algorithm.AddFuture(Futures.Indices.SP500EMini);
            // continuous contract data gives the canonical security a price, but it is still not tradable
            future.SetMarketPrice(new Tick { Value = 100m });

            var target = PortfolioTarget.Percent(algorithm, future.Symbol, 1m);

            Assert.IsNull(target);
            Assert.IsTrue(algorithm.ErrorMessages.Any(x => x.Contains("canonical")),
                "Expected an error message explaining the canonical symbol is not tradable");
        }

        [TestCase(-3, true)]
        [TestCase(3, true)]
        [TestCase(2, false)]
        [TestCase(-2, false)]
        [TestCase(0.01, true)]
        [TestCase(-0.01, true)]
        [TestCase(0.1, false)]
        [TestCase(-0.1, false)]
        [TestCase(0, false)]
        public void PercentIgnoresExtremeValuesBasedOnSettings(double value, bool shouldBeNull)
        {
            var algorithm = PerformanceBenchmarkAlgorithms.SingleSecurity_Second;
            algorithm.Settings.MaxAbsolutePortfolioTargetPercentage = 2;
            algorithm.Settings.MinAbsolutePortfolioTargetPercentage = 0.1m;
            algorithm.Initialize();
            algorithm.PostInitialize();
            var security = algorithm.Securities.Single().Value;
            security.SetMarketPrice(new Tick { Value = 1m });

            var buyingPowerMock = new Mock<IBuyingPowerModel>();
            buyingPowerMock.Setup(bpm => bpm.GetMaximumOrderQuantityForTargetBuyingPower(It.IsAny<GetMaximumOrderQuantityForTargetBuyingPowerParameters>()))
                .Returns(new GetMaximumOrderQuantityResult(1, null, false));
            buyingPowerMock.Setup(bpm => bpm.GetLeverage(It.IsAny<Security>())).Returns(1);
            security.BuyingPowerModel = buyingPowerMock.Object;

            var target = PortfolioTarget.Percent(algorithm, security.Symbol, value);

            if (shouldBeNull)
            {
                Assert.IsNull(target);
            }
            else
            {
                Assert.IsNotNull(target);
            }
        }
    }
}
