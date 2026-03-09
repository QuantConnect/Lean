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

using NodaTime;
using NUnit.Framework;

using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityPositionGroupBuyingPowerModelTests
    {
        private QCAlgorithm _algorithm;
        private SecurityPortfolioManager _portfolio;
        private Security _security;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            _algorithm.SetCash(100000);
            _portfolio = _algorithm.Portfolio;

            _security = new(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                // Only for testing with a lot size different than 1
                new SymbolProperties(string.Empty, Currencies.USD, 1, 0.01m, 0.01m, string.Empty),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());
            _security.SetMarketPrice(new Tick { Value = 200m });

            _algorithm.Securities.Add(_security);

        }

        [Test]
        public void GetsTheCorrectMaximumNumberOfLotsForTargetBuyingPower([Values(0.2, 0.75, 1)] decimal targetBuyingPower)
        {
            var buyingPowerModel = new SecurityPositionGroupBuyingPowerModel();
            var positionGroup = new PositionGroup(
                buyingPowerModel,
                -10,
                new Position(_security.Symbol, -10, 1)
            );

            var maxQuantityParameters = new GetMaximumOrderQuantityForTargetBuyingPowerParameters(_portfolio, _security, targetBuyingPower, 0);
            var maxQuantityResult = _security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(maxQuantityParameters);

            Assert.IsFalse(maxQuantityResult.IsError);
            Assert.AreNotEqual(maxQuantityResult.Quantity, 0);

            var maxLotsParameters = new GetMaximumLotsForTargetBuyingPowerParameters(_portfolio, positionGroup, targetBuyingPower, 0);
            var maxLotsResult = buyingPowerModel.GetMaximumLotsForTargetBuyingPower(maxLotsParameters);

            Assert.IsFalse(maxLotsResult.IsError);
            Assert.AreEqual(maxQuantityResult.Quantity, maxLotsResult.NumberOfLots * _security.SymbolProperties.LotSize);
        }

        [Test]
        public void GetsTheCorrectMaximumNumberOfLotsForDeltaBuyingPower([Values(0.2, 0.75, 1)] decimal targetBuyingPower)
        {
            var buyingPowerModel = new SecurityPositionGroupBuyingPowerModel();
            var positionGroup = new PositionGroup(
                buyingPowerModel,
                -10,
                new Position(_security.Symbol, -10, 1)
            );

            var deltaBuyingPower = _portfolio.TotalPortfolioValue * targetBuyingPower;

            var maxQuantityParameters = new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(_portfolio, _security, deltaBuyingPower, 0);
            var maxQuantityResult = _security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(maxQuantityParameters);

            Assert.IsFalse(maxQuantityResult.IsError);
            Assert.AreNotEqual(maxQuantityResult.Quantity, 0);

            var maxLotsarameters = new GetMaximumLotsForDeltaBuyingPowerParameters(_portfolio, positionGroup, deltaBuyingPower, 0);
            var maxLotsForDeltaResult = buyingPowerModel.GetMaximumLotsForDeltaBuyingPower(maxLotsarameters);

            Assert.IsFalse(maxLotsForDeltaResult.IsError);
            Assert.AreEqual(maxQuantityResult.Quantity, maxLotsForDeltaResult.NumberOfLots * _security.SymbolProperties.LotSize);
        }

        [Test]
        public void GetTheSameQuantityAndLotsForTargetAndDeltaBuyingPower([Values(0.2, 0.5, 0.75, 1)] decimal targetBuyingPower)
        {
            var buyingPowerModel = new SecurityPositionGroupBuyingPowerModel();
            var positionGroup = new PositionGroup(
                buyingPowerModel,
                -10,
                new Position(_security.Symbol, -10, 1)
            );

            // maximum quantity and lots for target buying power
            var maxQuantityForTargetParameters = new GetMaximumOrderQuantityForTargetBuyingPowerParameters(_portfolio, _security, targetBuyingPower, 0);
            var maxQuantityForTargetResult = _security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower(maxQuantityForTargetParameters);

            var maxLotsForTargetParameters = new GetMaximumLotsForTargetBuyingPowerParameters(_portfolio, positionGroup, targetBuyingPower, 0);
            var maxLotsForTargetResult = buyingPowerModel.GetMaximumLotsForTargetBuyingPower(maxLotsForTargetParameters);

            // maximum quantity and lots for delta buying power
            var deltaBuyingPower = _portfolio.TotalPortfolioValue * targetBuyingPower;

            var maxQuantityForDeltaParameters = new GetMaximumOrderQuantityForDeltaBuyingPowerParameters(_portfolio, _security, deltaBuyingPower, 0);
            var maxQuantityForDeltaResult = _security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower(maxQuantityForDeltaParameters);

            var maxLotsForDeltaParameters = new GetMaximumLotsForDeltaBuyingPowerParameters(_portfolio, positionGroup, deltaBuyingPower, 0);
            var maxLotsForDeltaResult = buyingPowerModel.GetMaximumLotsForDeltaBuyingPower(maxLotsForDeltaParameters);

            // maximum quantity should be the same, since the expected delta buying power is the same as the target buying power used
            Assert.IsFalse(maxQuantityForTargetResult.IsError);
            Assert.IsFalse(maxLotsForTargetResult.IsError);
            Assert.IsFalse(maxQuantityForDeltaResult.IsError);
            Assert.IsFalse(maxLotsForDeltaResult.IsError);
            Assert.AreNotEqual(maxQuantityForTargetResult.Quantity, 0);
            Assert.AreNotEqual(maxLotsForTargetResult.NumberOfLots, 0);
            Assert.AreEqual(maxQuantityForTargetResult.Quantity, maxQuantityForDeltaResult.Quantity);
            Assert.AreEqual(maxLotsForTargetResult.NumberOfLots, maxLotsForDeltaResult.NumberOfLots);
        }
    }
}
