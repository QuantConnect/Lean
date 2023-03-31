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

using System;
using System.Collections.Generic;
using NUnit.Framework;

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class OptionStrategyPositionGroupBuyingPowerModelTests
    {
        private QCAlgorithm _algorithm;
        private SecurityPortfolioManager _portfolio;
        private QuantConnect.Securities.Equity.Equity _equity;
        private Option _callOption;
        private Option _putOption;

        [SetUp]
        public void Setup()
        {
            _algorithm = new();
            _algorithm.SetCash(100000);
            _portfolio = _algorithm.Portfolio;

            var tz = TimeZones.NewYork;

            _equity = new(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            _callOption = new(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_C_192_Feb19_2016,
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties("", Currencies.USD, 100, 0.01m, 1),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _callOption.Underlying = _equity;

            _putOption = new(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_P_192_Feb19_2016,
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties("", Currencies.USD, 100, 0.01m, 1),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _putOption.Underlying = _equity;

            _portfolio.Securities.Add(_equity.Symbol, _equity);
            _portfolio.Securities.Add(_callOption.Symbol, _callOption);
            _portfolio.Securities.Add(_putOption.Symbol, _putOption);
        }

        [TestCase(-10, true)]
        [TestCase(-12, true)]
        [TestCase(-20, false)]
        public void TestHasSufficientBuyingPowerForShortOrderWithoutInitialHoldings(decimal strategyQuantity, bool hasSufficientBuyingPower)
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _putOption.SetMarketPrice(new Tick { Value = price });
            _callOption.SetMarketPrice(new Tick { Value = price });

            var optionStrategy = OptionStrategies.Straddle(_callOption.Symbol.Canonical, 192, new DateTime(2016, 2, 22));
            var buyingPowerModel = new OptionStrategyPositionGroupBuyingPowerModel(optionStrategy);

            var groupOrderManager = new GroupOrderManager(1, 2, strategyQuantity);
            var orders = new List<Order>()
            {
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _callOption.Type,
                    _callOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager)),
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _putOption.Type,
                    _putOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager))
            };

            var positionGroup = _portfolio.Positions.CreatePositionGroup(orders);
            var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

            Assert.AreEqual(hasSufficientBuyingPower, hasSufficientBuyingPowerResult.IsSufficient);

            var positionGroupMaintenanceMargin = buyingPowerModel.GetMaintenanceMargin(
                new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));
            if (hasSufficientBuyingPower)
            {
                Assert.Less(positionGroupMaintenanceMargin, _portfolio.MarginRemaining);
            }
            else
            {
                Assert.GreaterOrEqual(positionGroupMaintenanceMargin, _portfolio.MarginRemaining);
            }
        }

        // Liquidating part of the position
        [TestCase(5, true)]
        // Liquidating the whole position
        [TestCase(10, true)]
        // Shorting more, but with margin left
        [TestCase(-2, true)]
        // Shorting even more to the point margin is no longer enough
        [TestCase(-10, false)]
        public void TestHasSufficientBuyingPowerForLiquidatingPartOfAStrategyPosition(decimal strategyQuantity, bool hasSufficientBuyingPower)
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;

            var initialMargin = _portfolio.MarginRemaining;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _putOption.SetMarketPrice(new Tick { Value = price });
            _putOption.Holdings.SetHoldings(1m, -10);
            _callOption.SetMarketPrice(new Tick { Value = price });
            _callOption.Holdings.SetHoldings(1.5m, -10);

            var optionStrategy = OptionStrategies.Straddle(_callOption.Symbol.Canonical, 192, new DateTime(2016, 2, 22));
            var buyingPowerModel = new OptionStrategyPositionGroupBuyingPowerModel(
                _callOption.Holdings.Quantity + strategyQuantity  == 0
                    // Liquidating
                    ? null
                    : optionStrategy);

            var groupOrderManager = new GroupOrderManager(1, 2, strategyQuantity);
            var orders = new List<Order>()
            {
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _callOption.Type,
                    _callOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager)),
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _putOption.Type,
                    _putOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager))
            };

            var positionGroup = _portfolio.Positions.CreatePositionGroup(orders);
            var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

            var positionGroupMaintenanceMargin = buyingPowerModel.GetMaintenanceMargin(
                new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

            Assert.AreEqual(hasSufficientBuyingPower, hasSufficientBuyingPowerResult.IsSufficient);

            if (hasSufficientBuyingPower)
            {
                Assert.Less(positionGroupMaintenanceMargin, initialMargin);
            }
            else
            {
                Assert.GreaterOrEqual(positionGroupMaintenanceMargin, initialMargin);
            }
        }
    }
}
