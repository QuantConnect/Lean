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
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture]
    public class FutureSettlementModelTests
    {
        private static readonly DateTime Noon = new (2014, 6, 24, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new (Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        [TestCase(1400, 10, 1300, 1200)]
        [TestCase(1400, -10, 1300, 1200)]
        [TestCase(1300, 10, 1400, 1500)]
        [TestCase(1300, -10, 1400, 1500)]
        [TestCase(1400, 10, 1300, 1500)]
        [TestCase(1400, -10, 1300, 1500)]
        [TestCase(1300, 10, 1400, 1200)]
        [TestCase(1300, -10, 1400, 1200)]
        public void DailySettlement(decimal averagePrice, decimal quantity, decimal futurePriceStep1, decimal futurePriceStep2)
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            var model = new FutureSettlementModel();
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(Symbols.Fut_SPY_Feb19_2016.ID.Market, Symbols.Fut_SPY_Feb19_2016, SecurityType.Future);
            var future = new Future(Symbols.Fut_SPY_Feb19_2016,
                entry.ExchangeHours,
                portfolio.CashBook[Currencies.USD],
                SymbolProperties.GetDefault(Currencies.USD),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new FutureCache());
            future.FeeModel = new ConstantFeeModel(0);
            future.SettlementModel = model;
            var futureHoldings = (FutureHolding)future.Holdings;
            securities.Add(future);
            var timeKeeper = new LocalTimeKeeper(Noon, entry.ExchangeHours.TimeZone);
            future.SetLocalTimeKeeper(timeKeeper);

            future.Holdings.SetHoldings(averagePrice, quantity);
            SetPrice(future, futurePriceStep1);
            portfolio.InvalidateTotalPortfolioValue();

            var expectedTpv = portfolio.TotalPortfolioValue;
            var startCash = portfolio.CashBook[Currencies.USD].Amount;
            Assert.AreEqual(0, futureHoldings.SettledProfit);

            // advance time
            timeKeeper.UpdateTime(timeKeeper.LocalTime.AddDays(1));
            model.Scan(new ScanSettlementModelParameters(portfolio, future, timeKeeper.LocalTime));
            portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(portfolio.TotalPortfolioValue, expectedTpv);
            var expectedCash = startCash + future.Holdings.UnrealizedProfit;
            Assert.AreEqual(expectedCash, portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(future.Holdings.UnrealizedProfit, futureHoldings.SettledProfit);
            Assert.AreEqual(0, futureHoldings.UnsettledProfit);

            // we call it again, nothing should change
            SetPrice(future, futurePriceStep2);
            portfolio.InvalidateTotalPortfolioValue();
            model.Scan(new ScanSettlementModelParameters(portfolio, future, timeKeeper.LocalTime));

            // price movement does affect TPV not cash
            expectedTpv = expectedTpv + (futurePriceStep2 - futurePriceStep1) * quantity;
            Assert.AreEqual(expectedTpv, portfolio.TotalPortfolioValue);
            Assert.AreEqual(expectedCash, portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreNotEqual(0, futureHoldings.UnsettledProfit);

            // advance time
            timeKeeper.UpdateTime(timeKeeper.LocalTime.AddDays(1));
            model.Scan(new ScanSettlementModelParameters(portfolio, future, timeKeeper.LocalTime));
            portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(expectedTpv, portfolio.TotalPortfolioValue);
            Assert.AreEqual(startCash + future.Holdings.UnrealizedProfit, portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(future.Holdings.UnrealizedProfit, futureHoldings.SettledProfit);
            Assert.AreEqual(0, futureHoldings.UnsettledProfit);
        }

        [TestCase(1400, 10, 1300, 0)]
        [TestCase(1400, -10, 1300, 0)]
        [TestCase(1300, 10, 1400, 0)]
        [TestCase(1300, -10, 1400, 0)]
        [TestCase(1400, 10, 1300, 1)]
        [TestCase(1400, -10, 1300, 1)]
        [TestCase(1300, 10, 1400, 1)]
        [TestCase(1300, -10, 1400, 1)]
        [TestCase(1400, 10, 1300, -1)]
        [TestCase(1400, -10, 1300, -1)]
        [TestCase(1300, 10, 1400, -1)]
        [TestCase(1300, -10, 1400, -1)]
        [TestCase(1400, 10, 1300, -20)]
        [TestCase(1300, 10, 1400, -20)]
        [TestCase(1400, -10, 1300, 20)]
        [TestCase(1300, -10, 1400, 20)]
        public void HoldingsQuantityChange(decimal averagePrice, decimal quantity, decimal futurePrice, decimal newQuantity)
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            var model = new FutureSettlementModel();
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(Symbols.Fut_SPY_Feb19_2016.ID.Market, Symbols.Fut_SPY_Feb19_2016, SecurityType.Future);
            var future = new Future(Symbols.Fut_SPY_Feb19_2016,
                entry.ExchangeHours,
                portfolio.CashBook[Currencies.USD],
                SymbolProperties.GetDefault(Currencies.USD),
                portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new FutureCache());
            future.SettlementModel = model;
            future.FeeModel = new ConstantFeeModel(0);
            securities.Add(future);
            var timeKeeper = new LocalTimeKeeper(Noon, entry.ExchangeHours.TimeZone);
            future.SetLocalTimeKeeper(timeKeeper);

            future.Holdings.SetHoldings(averagePrice, quantity);
            SetPrice(future, futurePrice);
            portfolio.InvalidateTotalPortfolioValue();

            var expectedTpv = portfolio.TotalPortfolioValue;
            var startCash = portfolio.CashBook[Currencies.USD].Amount;
            // advance time
            timeKeeper.UpdateTime(timeKeeper.LocalTime.AddDays(1));
            model.Scan(new ScanSettlementModelParameters(portfolio, future, timeKeeper.LocalTime));
            portfolio.InvalidateTotalPortfolioValue();

            var expectedSettledCash = future.Holdings.UnrealizedProfit;
            var expectedCash = startCash + expectedSettledCash;
            Assert.AreEqual(portfolio.TotalPortfolioValue, expectedTpv);
            Assert.AreEqual(expectedCash, portfolio.CashBook[Currencies.USD].Amount);

            // we change the holdings quantity
            var fillPrice = futurePrice * 0.9m;
            var fillQuantity = -(quantity - newQuantity);
            var absoluteQuantityClosed = Math.Min(Math.Abs(fillQuantity), future.Holdings.AbsoluteQuantity);
            var closedQuantity = Math.Sign(-fillQuantity) * absoluteQuantityClosed;

            var funds = new CashAmount(future.Holdings.TotalCloseProfit(includeFees: false, exitPrice: fillPrice, future.Holdings.AveragePrice, -closedQuantity), Currencies.USD);
            var fill = new OrderEvent(1, future.Symbol, timeKeeper.LocalTime, OrderStatus.Filled, Extensions.GetOrderDirection(fillQuantity), fillPrice, fillQuantity, OrderFee.Zero);
            future.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(portfolio, future, timeKeeper.LocalTime.ConvertToUtc(timeKeeper.TimeZone), funds, fill));

            // if we change side the cash adjustment will go to 0, until we scan again
            var settledProfit = 0m;
            expectedCash = startCash - funds.Amount;
            if (Math.Sign(newQuantity) == Math.Sign(quantity))
            {
                // if we increase the position the cash adjustment will remain the same, until we scan again
                if (newQuantity < 0 && newQuantity < quantity)
                {
                    settledProfit = expectedSettledCash;
                }
                else if (newQuantity > 0 && newQuantity > quantity)
                {
                    settledProfit = expectedSettledCash;
                }
                else
                {
                    // we reduced the position
                    settledProfit = expectedSettledCash * (newQuantity / quantity);
                    expectedCash = startCash - funds.Amount + settledProfit;
                }
            }

            var futureHoldings = (FutureHolding)future.Holdings;
            Assert.AreEqual(settledProfit, futureHoldings.SettledProfit);
            Assert.AreEqual(expectedCash, portfolio.CashBook[Currencies.USD].Amount);
        }

        private static void SetPrice(Security security, decimal price)
        {
            security.SetMarketPrice(new Tick(Noon, security.Symbol, string.Empty, Exchange.UNKNOWN, quantity: 1, price));
        }
    }
}
