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
        private static readonly DateTime Noon = new(2014, 6, 24, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        private Future _future;
        private LocalTimeKeeper _timeKeeper;
        private FutureSettlementModel _model;
        private FutureHolding _futureHoldings;
        private SecurityPortfolioManager _portfolio;

        [SetUp]
        public void Setup()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(null, securities);
            _portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());
            _model = new FutureSettlementModel();
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(Symbols.Fut_SPY_Feb19_2016.ID.Market, Symbols.Fut_SPY_Feb19_2016, SecurityType.Future);
            _future = new Future(Symbols.Fut_SPY_Feb19_2016,
                entry.ExchangeHours,
                _portfolio.CashBook[Currencies.USD],
                SymbolProperties.GetDefault(Currencies.USD),
                _portfolio.CashBook,
                RegisteredSecurityDataTypesProvider.Null,
                new FutureCache());
            _future.FeeModel = new ConstantFeeModel(0);
            _future.SettlementModel = _model;
            _futureHoldings = (FutureHolding)_future.Holdings;
            securities.Add(_future);
            _timeKeeper = new LocalTimeKeeper(Noon, entry.ExchangeHours.TimeZone);
            _future.SetLocalTimeKeeper(_timeKeeper);
        }

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
            _future.Holdings.SetHoldings(averagePrice, quantity);
            SetPrice(_future, futurePriceStep1);
            _portfolio.InvalidateTotalPortfolioValue();

            var expectedTpv = _portfolio.TotalPortfolioValue;
            var startCash = _portfolio.CashBook[Currencies.USD].Amount;
            Assert.AreEqual(0, _futureHoldings.SettledProfit);

            // advance time
            _timeKeeper.UpdateTime(_timeKeeper.LocalTime.AddDays(1));
            _model.Scan(new ScanSettlementModelParameters(_portfolio, _future, _timeKeeper.LocalTime));
            _portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(_portfolio.TotalPortfolioValue, expectedTpv);
            var expectedCash = startCash + _future.Holdings.UnrealizedProfit;
            Assert.AreEqual(expectedCash, _portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(_future.Holdings.UnrealizedProfit, _futureHoldings.SettledProfit);
            Assert.AreEqual(0, _futureHoldings.UnsettledProfit);

            // we call it again, nothing should change
            SetPrice(_future, futurePriceStep2);
            _portfolio.InvalidateTotalPortfolioValue();
            _model.Scan(new ScanSettlementModelParameters(_portfolio, _future, _timeKeeper.LocalTime));

            // price movement does affect TPV not cash
            expectedTpv = expectedTpv + (futurePriceStep2 - futurePriceStep1) * quantity;
            Assert.AreEqual(expectedTpv, _portfolio.TotalPortfolioValue);
            Assert.AreEqual(expectedCash, _portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreNotEqual(0, _futureHoldings.UnsettledProfit);

            // advance time
            _timeKeeper.UpdateTime(_timeKeeper.LocalTime.AddDays(1));
            _model.Scan(new ScanSettlementModelParameters(_portfolio, _future, _timeKeeper.LocalTime));
            _portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(expectedTpv, _portfolio.TotalPortfolioValue);
            Assert.AreEqual(startCash + _future.Holdings.UnrealizedProfit, _portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(_future.Holdings.UnrealizedProfit, _futureHoldings.SettledProfit);
            Assert.AreEqual(0, _futureHoldings.UnsettledProfit);
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
            _future.Holdings.SetHoldings(averagePrice, quantity);
            SetPrice(_future, futurePrice);
            _portfolio.InvalidateTotalPortfolioValue();

            var expectedTpv = _portfolio.TotalPortfolioValue;
            var startCash = _portfolio.CashBook[Currencies.USD].Amount;
            // advance time
            _timeKeeper.UpdateTime(_timeKeeper.LocalTime.AddDays(1));
            _model.Scan(new ScanSettlementModelParameters(_portfolio, _future, _timeKeeper.LocalTime));
            _portfolio.InvalidateTotalPortfolioValue();

            var expectedSettledCash = _future.Holdings.UnrealizedProfit;
            var expectedCash = startCash + expectedSettledCash;
            Assert.AreEqual(_portfolio.TotalPortfolioValue, expectedTpv);
            Assert.AreEqual(expectedCash, _portfolio.CashBook[Currencies.USD].Amount);

            // we change the holdings quantity
            var fillPrice = futurePrice * 0.9m;
            var fillQuantity = -(quantity - newQuantity);
            var absoluteQuantityClosed = Math.Min(Math.Abs(fillQuantity), _future.Holdings.AbsoluteQuantity);
            var closedQuantity = Math.Sign(-fillQuantity) * absoluteQuantityClosed;

            // let's get the profit/loss if we closed our position, which we will sum later on since it already has the right profit/loss sign
            Assert.AreEqual(Math.Sign(closedQuantity), Math.Sign(quantity));
            var funds = new CashAmount(_future.Holdings.TotalCloseProfit(includeFees: false, exitPrice: fillPrice, _future.Holdings.AveragePrice, closedQuantity), Currencies.USD);
            var fill = new OrderEvent(1, _future.Symbol, _timeKeeper.LocalTime, OrderStatus.Filled, Extensions.GetOrderDirection(fillQuantity), fillPrice, fillQuantity, OrderFee.Zero);
            _future.SettlementModel.ApplyFunds(new ApplyFundsSettlementModelParameters(_portfolio, _future, _timeKeeper.LocalTime.ConvertToUtc(_timeKeeper.TimeZone), funds, fill));

            // if we change side the cash adjustment will go to 0, until we scan again
            var settledProfit = 0m;
            expectedCash = startCash + funds.Amount;
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
                    expectedCash = startCash + funds.Amount + settledProfit;
                }
            }

            var futureHoldings = (FutureHolding)_future.Holdings;
            Assert.AreEqual(settledProfit, futureHoldings.SettledProfit);
            Assert.AreEqual(expectedCash, _portfolio.CashBook[Currencies.USD].Amount);
        }

        [TestCase(10, 10, -10, -10)]
        [TestCase(10, 10, -5, -15)]
        [TestCase(5, 15, -5, -15)]
        public void DifferentAveragePrice(decimal fillQuantityA, decimal fillQuantityB, decimal fillQuantityC, decimal fillQuantityD)
        {
            var startTpv = _portfolio.TotalPortfolioValue;
            var startCash = _portfolio.CashBook[Currencies.USD].Amount;

            var initialAveragePrice = 1400;
            var futureSettlementPrice = 1450;
            var secondAveragePrice = initialAveragePrice * 1.5m;

            var exitFillPrice = futureSettlementPrice * 0.9m;

            SetPrice(_future, futureSettlementPrice);
            _future.PortfolioModel.ProcessFill(_portfolio, _future, new OrderEvent(1, _future.Symbol, _timeKeeper.LocalTime, OrderStatus.Filled, Extensions.GetOrderDirection(fillQuantityA), initialAveragePrice, fillQuantityA, OrderFee.Zero));
            _portfolio.InvalidateTotalPortfolioValue();

            var profit = _future.Holdings.TotalCloseProfit(includeFees: false, exitPrice: _future.Price, _future.Holdings.AveragePrice, _future.Holdings.Quantity);
            Assert.AreEqual(0, _futureHoldings.SettledProfit);
            Assert.AreEqual(profit, _futureHoldings.UnsettledProfit);

            // advance time
            _timeKeeper.UpdateTime(_timeKeeper.LocalTime.AddDays(1));
            _model.Scan(new ScanSettlementModelParameters(_portfolio, _future, _timeKeeper.LocalTime));
            _portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(profit, _futureHoldings.SettledProfit);
            Assert.AreEqual(0, _futureHoldings.UnsettledProfit);

            // we double our position with a different average price, after we scan
            _future.PortfolioModel.ProcessFill(_portfolio, _future, new OrderEvent(2, _future.Symbol, _timeKeeper.LocalTime, OrderStatus.Filled, Extensions.GetOrderDirection(fillQuantityB), secondAveragePrice, fillQuantityB, OrderFee.Zero));
            _portfolio.InvalidateTotalPortfolioValue();

            // let's get the profit/loss if we closed our position, which we will sum later on since it already has the right profit/loss sign
            var averagePrice = _futureHoldings.AveragePrice;
            _future.PortfolioModel.ProcessFill(_portfolio, _future, new OrderEvent(3, _future.Symbol, _timeKeeper.LocalTime, OrderStatus.Filled, Extensions.GetOrderDirection(fillQuantityC), exitFillPrice, fillQuantityC, OrderFee.Zero));
            _portfolio.InvalidateTotalPortfolioValue();

            // settled profit was reset because we closed the position
            var currentlyStettledProfit = profit * ((fillQuantityA + fillQuantityC) / fillQuantityA);
            Assert.AreEqual(currentlyStettledProfit, _futureHoldings.SettledProfit);
            var closeProfit = new CashAmount(_future.Holdings.TotalCloseProfit(includeFees: false, exitPrice: exitFillPrice, entryPrice: averagePrice, -fillQuantityC), Currencies.USD);
            Assert.AreEqual(startCash + closeProfit.Amount + currentlyStettledProfit, _portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(startTpv + closeProfit.Amount + _futureHoldings.UnsettledProfit + currentlyStettledProfit, _portfolio.TotalPortfolioValue);
            Assert.AreEqual(_futureHoldings.UnsettledProfit + currentlyStettledProfit, _future.Holdings.TotalCloseProfit(includeFees: false, exitPrice: _future.Price, averagePrice, _future.Holdings.Quantity));

            // finally let's close the entire position
            averagePrice = _futureHoldings.AveragePrice;
            _future.PortfolioModel.ProcessFill(_portfolio, _future, new OrderEvent(4, _future.Symbol, _timeKeeper.LocalTime, OrderStatus.Filled, Extensions.GetOrderDirection(fillQuantityD), exitFillPrice, fillQuantityD, OrderFee.Zero));
            _portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(0, _futureHoldings.SettledProfit);
            Assert.AreEqual(0, _futureHoldings.UnsettledProfit);
            var closeProfit2 = new CashAmount(_future.Holdings.TotalCloseProfit(includeFees: false, exitPrice: exitFillPrice, averagePrice, -fillQuantityD), Currencies.USD);
            Assert.AreEqual(startCash + closeProfit.Amount + closeProfit2.Amount, _portfolio.CashBook[Currencies.USD].Amount);
            Assert.AreEqual(startTpv + closeProfit.Amount + closeProfit2.Amount, _portfolio.TotalPortfolioValue);
        }

        private static void SetPrice(Security security, decimal price)
        {
            security.SetMarketPrice(new Tick(Noon, security.Symbol, string.Empty, Exchange.UNKNOWN, quantity: 1, price));
        }
    }
}
