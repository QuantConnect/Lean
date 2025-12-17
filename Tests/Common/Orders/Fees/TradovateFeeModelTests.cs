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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class TradovateFeeModelTests
    {
        private readonly IFeeModel _feeModel = new TradovateFeeModel();

        #region Micro E-mini Futures Fee Tests

        [TestCase(Futures.Indices.MicroDow30EMini, 0.79)]      // MYM
        [TestCase(Futures.Indices.MicroSP500EMini, 0.79)]      // MES
        [TestCase(Futures.Indices.MicroNASDAQ100EMini, 0.79)]  // MNQ
        [TestCase(Futures.Indices.MicroRussell2000EMini, 0.79)] // M2K
        public void MicroEminiFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        [Test]
        public void MicroEminiFuturesFee_MultipleContracts()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.MicroDow30EMini, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 10, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(10 * 0.79m, fee.Value.Amount);
        }

        #endregion

        #region E-mini Futures Fee Tests

        [TestCase(Futures.Indices.Dow30EMini, 1.29)]    // YM
        [TestCase(Futures.Indices.SP500EMini, 1.29)]    // ES
        [TestCase(Futures.Indices.NASDAQ100EMini, 1.29)] // NQ
        [TestCase(Futures.Indices.Russell2000EMini, 1.29)] // RTY
        public void EminiFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        [Test]
        public void EminiFuturesFee_MultipleContracts()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 5, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(5 * 1.29m, fee.Value.Amount);
        }

        #endregion

        #region Treasury Futures Fee Tests

        [TestCase(Futures.Financials.Y30TreasuryBond, 1.79)]  // ZB
        [TestCase(Futures.Financials.Y10TreasuryNote, 1.79)]  // ZN
        [TestCase(Futures.Financials.Y5TreasuryNote, 1.79)]   // ZF
        [TestCase(Futures.Financials.Y2TreasuryNote, 1.79)]   // ZT
        public void TreasuryFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        #endregion

        #region Energy Futures Fee Tests

        [TestCase(Futures.Energy.CrudeOilWTI, 1.79)]  // CL
        [TestCase(Futures.Energy.NaturalGas, 1.79)]   // NG
        public void EnergyFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        [TestCase(Futures.Energy.MicroCrudeOilWTI, 0.79)]  // MCL
        public void MicroEnergyFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        #endregion

        #region Metals Futures Fee Tests

        [TestCase(Futures.Metals.Gold, 1.79)]    // GC
        [TestCase(Futures.Metals.Silver, 1.79)]  // SI
        public void MetalsFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        [TestCase(Futures.Metals.MicroGold, 0.79)]   // MGC
        [TestCase(Futures.Metals.MicroSilver, 0.79)] // SIL
        public void MicroMetalsFuturesFee(string futureSymbol, double expectedFeePerContract)
        {
            var symbol = Symbols.CreateFutureSymbol(futureSymbol, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFeePerContract, fee.Value.Amount);
        }

        #endregion

        #region Default Fee Tests

        [Test]
        public void UnknownFuture_ReturnsDefaultFee()
        {
            // Use a future that might not be in the fee dictionary
            var symbol = Symbols.CreateFutureSymbol(Futures.Grains.Wheat, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            // Default fee should be applied for unknown contracts
            Assert.Greater(fee.Value.Amount, 0);
        }

        #endregion

        #region Order Type Fee Tests

        [Test]
        public void LimitOrder_SameFeeAsMarket()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, 44000m, 44000m));

            var marketOrder = new MarketOrder(symbol, 1, DateTime.UtcNow);
            var limitOrder = new LimitOrder(symbol, 1, 44000m, DateTime.UtcNow);

            var marketFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, marketOrder));
            var limitFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, limitOrder));

            Assert.AreEqual(marketFee.Value.Amount, limitFee.Value.Amount);
            Assert.AreEqual(marketFee.Value.Currency, limitFee.Value.Currency);
        }

        [Test]
        public void StopOrder_SameFeeAsMarket()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, 44000m, 44000m));

            var marketOrder = new MarketOrder(symbol, 1, DateTime.UtcNow);
            var stopOrder = new StopMarketOrder(symbol, 1, 43500m, DateTime.UtcNow);

            var marketFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, marketOrder));
            var stopFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, stopOrder));

            Assert.AreEqual(marketFee.Value.Amount, stopFee.Value.Amount);
            Assert.AreEqual(marketFee.Value.Currency, stopFee.Value.Currency);
        }

        [Test]
        public void TrailingStopOrder_SameFeeAsMarket()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, 44000m, 44000m));

            var marketOrder = new MarketOrder(symbol, 1, DateTime.UtcNow);
            var trailingStopOrder = new TrailingStopOrder(symbol, 1, 43500m, 500m, false, DateTime.UtcNow);

            var marketFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, marketOrder));
            var trailingStopFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, trailingStopOrder));

            Assert.AreEqual(marketFee.Value.Amount, trailingStopFee.Value.Amount);
            Assert.AreEqual(marketFee.Value.Currency, trailingStopFee.Value.Currency);
        }

        #endregion

        #region Sell Order Fee Tests

        [Test]
        public void SellOrder_SameFeeAsBuyOrder()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
            var security = CreateFutureSecurity(symbol);

            var buyOrder = new MarketOrder(symbol, 1, DateTime.UtcNow);
            var sellOrder = new MarketOrder(symbol, -1, DateTime.UtcNow);

            var buyFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, buyOrder));
            var sellFee = _feeModel.GetOrderFee(new OrderFeeParameters(security, sellOrder));

            Assert.AreEqual(buyFee.Value.Amount, sellFee.Value.Amount);
            Assert.AreEqual(buyFee.Value.Currency, sellFee.Value.Currency);
        }

        #endregion

        #region Currency Tests

        [Test]
        public void Fee_AlwaysInUSD()
        {
            var symbols = new[]
            {
                Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21)),
                Symbols.CreateFutureSymbol(Futures.Indices.MicroDow30EMini, new DateTime(2025, 3, 21)),
                Symbols.CreateFutureSymbol(Futures.Financials.Y30TreasuryBond, new DateTime(2025, 3, 21))
            };

            foreach (var symbol in symbols)
            {
                var security = CreateFutureSecurity(symbol);
                var order = new MarketOrder(symbol, 1, DateTime.UtcNow);

                var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

                Assert.AreEqual(Currencies.USD, fee.Value.Currency, $"Fee currency should be USD for {symbol}");
            }
        }

        #endregion

        #region Helper Methods

        private static Future CreateFutureSecurity(Symbol symbol)
        {
            return new Future(
                symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 1),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        #endregion
    }
}
