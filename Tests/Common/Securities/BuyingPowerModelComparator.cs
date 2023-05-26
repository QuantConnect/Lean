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
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IBuyingPowerModel"/> that verifies consistency with
    /// the <see cref="SecurityPositionGroupBuyingPowerModel"/>
    /// </summary>
    public class BuyingPowerModelComparator : IBuyingPowerModel
    {
        public SecurityPortfolioManager Portfolio { get; }
        public IBuyingPowerModel SecurityModel { get; }
        public IPositionGroupBuyingPowerModel PositionGroupModel { get; }

        private bool reentry;

        public BuyingPowerModelComparator(
            IBuyingPowerModel securityModel,
            IPositionGroupBuyingPowerModel positionGroupModel,
            SecurityPortfolioManager portfolio = null,
            ITimeKeeper timeKeeper = null,
            IOrderProcessor orderProcessor = null
            )
        {
            Portfolio = portfolio;
            SecurityModel = securityModel;
            PositionGroupModel = positionGroupModel;

            if (portfolio == null)
            {
                var securities = new SecurityManager(timeKeeper ?? new TimeKeeper(DateTime.UtcNow));
                Portfolio = new SecurityPortfolioManager(securities, new SecurityTransactionManager(null, securities), new AlgorithmSettings());
            }
            if (orderProcessor != null)
            {
                Portfolio.Transactions.SetOrderProcessor(orderProcessor);
            }
        }

        public decimal GetLeverage(Security security)
        {
            return SecurityModel.GetLeverage(security);
        }

        public void SetLeverage(Security security, decimal leverage)
        {
            SecurityModel.SetLeverage(security, leverage);
        }

        public MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetMaintenanceMargin(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = true;
            var actual = PositionGroupModel.GetMaintenanceMargin(new PositionGroupMaintenanceMarginParameters(
                Portfolio, new PositionGroup(PositionGroupModel, parameters.Quantity, new Position(parameters.Security, parameters.Quantity))
            ));

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaintenanceMargin)}"
            );

            reentry = false;
            return expected;
        }

        public InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetInitialMarginRequirement(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = true;
            var actual = PositionGroupModel.GetInitialMarginRequirement(new PositionGroupInitialMarginParameters(
                Portfolio, new PositionGroup(PositionGroupModel, parameters.Quantity, new Position(parameters.Security, parameters.Quantity))
            ));

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetInitialMarginRequirement)}"
            );

            reentry = false;
            return expected;
        }

        public InitialMargin GetInitialMarginRequiredForOrder(InitialMarginRequiredForOrderParameters parameters)
        {
            reentry = true;
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetInitialMarginRequiredForOrder(parameters);
            if (reentry)
            {
                return expected;
            }

            var actual = PositionGroupModel.GetInitialMarginRequiredForOrder(new PositionGroupInitialMarginForOrderParameters(
                Portfolio, new PositionGroup(PositionGroupModel, parameters.Order.Quantity, new Position(parameters.Security, parameters.Order.Quantity)), parameters.Order
            ));

            Assert.AreEqual(expected.Value, actual.Value,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetInitialMarginRequiredForOrder)}"
            );

            reentry = false;
            return expected;
        }

        public HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientBuyingPowerForOrderParameters parameters
            )
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.HasSufficientBuyingPowerForOrder(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = true;
            var position = new Position(parameters.Security, parameters.Order.Quantity);
            var actual = PositionGroupModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(
                    Portfolio,
                    new PositionGroup(PositionGroupModel, position.GetGroupQuantity(), position),
                    new List<Order> { parameters.Order }
                )
            );

            Assert.AreEqual(expected.IsSufficient, actual.IsSufficient,
                $"{PositionGroupModel.GetType().Name}:{nameof(HasSufficientBuyingPowerForOrder)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Reason, actual.Reason,
                $"{PositionGroupModel.GetType().Name}:{nameof(HasSufficientBuyingPowerForOrder)}"
            );

            reentry = false;
            return expected;
        }

        public GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(
            GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters
            )
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetMaximumOrderQuantityForTargetBuyingPower(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = true;
            var security = parameters.Security;
            var positionGroup = Portfolio.Positions[new PositionGroupKey(PositionGroupModel, security)];
            var actual = PositionGroupModel.GetMaximumLotsForTargetBuyingPower(
                new GetMaximumLotsForTargetBuyingPowerParameters(
                    parameters.Portfolio,
                    positionGroup,
                    parameters.TargetBuyingPower,
                    parameters.MinimumOrderMarginPortfolioPercentage,
                    parameters.SilenceNonErrorReasons
                )
            );

            var lotSize = security.SymbolProperties.LotSize;
            Assert.AreEqual(expected.IsError, actual.IsError,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForTargetBuyingPower)}: " +
                $"ExpectedQuantity: {expected.Quantity} ActualQuantity: {actual.NumberOfLots * lotSize} {Environment.NewLine}" +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            // we're not comparing group quantities, which is the number of position lots, but rather the implied
            // position quantities resulting from having that many lots.
            var resizedPositionGroup = positionGroup.WithQuantity(
                Math.Sign(positionGroup.Quantity) == -1 ? -actual.NumberOfLots : actual.NumberOfLots, Portfolio.Positions);
            var position = resizedPositionGroup.GetPosition(security.Symbol);

            var bpmOrder = new MarketOrder(security.Symbol, expected.Quantity, parameters.Portfolio.Securities.UtcTime);
            var pgbpmOrder = new MarketOrder(security.Symbol, position.Quantity, parameters.Portfolio.Securities.UtcTime);

            var bpmOrderValue = bpmOrder.GetValue(security);
            var pgbpmOrderValue = pgbpmOrder.GetValue(security);

            var bpmOrderFees = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, bpmOrder)).Value.Amount;
            var pgbpmOrderFees = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, pgbpmOrder)).Value.Amount;

            var bpmMarginRequired = bpmOrderValue + bpmOrderFees;
            var pgbpmMarginRequired = pgbpmOrderValue + pgbpmOrderFees;

            Assert.AreEqual(expected.Quantity, position.Quantity,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForTargetBuyingPower)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Reason, actual.Reason,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForTargetBuyingPower)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            reentry = false;
            return expected;
        }

        public GetMaximumOrderQuantityResult GetMaximumOrderQuantityForDeltaBuyingPower(
            GetMaximumOrderQuantityForDeltaBuyingPowerParameters parameters
            )
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetMaximumOrderQuantityForDeltaBuyingPower(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = true;
            var security = parameters.Security;
            var positionGroup = Portfolio.Positions[new PositionGroupKey(PositionGroupModel, security)];
            var actual = PositionGroupModel.GetMaximumLotsForDeltaBuyingPower(
                new GetMaximumLotsForDeltaBuyingPowerParameters(
                    parameters.Portfolio,
                    positionGroup,
                    parameters.DeltaBuyingPower,
                    parameters.MinimumOrderMarginPortfolioPercentage,
                    parameters.SilenceNonErrorReasons
                )
            );

            var lotSize = security.SymbolProperties.LotSize;
            Assert.AreEqual(expected.IsError, actual.IsError,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForDeltaBuyingPower)}: " +
                $"ExpectedQuantity: {expected.Quantity} ActualQuantity: {actual.NumberOfLots * lotSize} {Environment.NewLine}" +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            // we're not comparing group quantities, which is the number of position lots, but rather the implied
            // position quantities resulting from having that many lots.
            var resizedPositionGroup = positionGroup.WithQuantity(
                Math.Sign(positionGroup.Quantity) == -1 ? -actual.NumberOfLots : actual.NumberOfLots, Portfolio.Positions);
            var position = resizedPositionGroup.GetPosition(security.Symbol);

            var bpmOrder = new MarketOrder(security.Symbol, expected.Quantity, parameters.Portfolio.Securities.UtcTime);
            var pgbpmOrder = new MarketOrder(security.Symbol, position.Quantity, parameters.Portfolio.Securities.UtcTime);

            var bpmMarginRequired = security.BuyingPowerModel.GetInitialMarginRequirement(security, bpmOrder.Quantity);
            var pgbpmMarginRequired = PositionGroupModel.GetInitialMarginRequiredForOrder(Portfolio, resizedPositionGroup, pgbpmOrder);

            var availableBuyingPower = security.BuyingPowerModel.GetBuyingPower(parameters.Portfolio, security, bpmOrder.Direction);

            Assert.AreEqual(expected.Quantity, position.Quantity,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForDeltaBuyingPower)}: " +
                $"ExpectedReason: {expected.Reason}{Environment.NewLine}" +
                $"ActualReason: {actual.Reason}"
            );

            Assert.AreEqual(expected.Reason, actual.Reason,
                $"{PositionGroupModel.GetType().Name}:{nameof(GetMaximumOrderQuantityForDeltaBuyingPower)}"
            );

            reentry = false;
            return expected;
        }

        public ReservedBuyingPowerForPosition GetReservedBuyingPowerForPosition(ReservedBuyingPowerForPositionParameters parameters)
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetReservedBuyingPowerForPosition(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = true;

            reentry = false;
            return expected;
        }

        public BuyingPower GetBuyingPower(BuyingPowerParameters parameters)
        {
            EnsureSecurityExists(parameters.Security);
            var expected = SecurityModel.GetBuyingPower(parameters);
            if (reentry)
            {
                return expected;
            }

            reentry = false;
            return expected;
        }

        private void EnsureSecurityExists(Security security)
        {
            if (!Portfolio.Securities.ContainsKey(security.Symbol))
            {
                var timeKeeper = (LocalTimeKeeper) typeof(Security).GetField("_localTimeKeeper", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(security);
                Portfolio.Securities[security.Symbol] = security;
                security.SetLocalTimeKeeper(timeKeeper);
            }
        }
    }
}
