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
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using Moq;
using QuantConnect.Brokerages;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class CashModelAlgorithmTradingTests
    {
        private static readonly Symbol _symbol = Symbols.BTCUSD;
        private static readonly string _cashSymbol = "BTC";

        /*****************************************************/
        //  Isostatic market conditions tests.
        /*****************************************************/

        [Test]
        public void SetHoldings_ZeroToLong()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(1995m, actual);
        }

        [Test]
        public void SetHoldings_ZeroToLong_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            // $100k total value * 0.5 target * 0.9975 FreePortfolioValuePercentage / 25 ~= 1995 - fees
            Assert.AreEqual(1994.96m, actual);
        }

        [Test]
        public void SetHoldings_ZeroToLong_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            // 10k in fees = 400 shares (400*25)
            // $100k total value * 0.5 target * 0.9975 FreePortfolioValuePercentage / 25 ~= 1995 - 400 because of fees
            Assert.AreEqual(1595m, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(992.5m, actual);
        }

        [Test]
        public void SetHoldings_LongToFullLong()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 1m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            // 100k total value * 1 target * 0.9975 setHoldings buffer - 50K holdings -10K fees / @ 25 ~= 1590m
            Assert.AreEqual(1590m, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            // 100k total value * 0.75 target * 0.9975 setHoldings buffer - 50K holdings / @ 25 ~= 992m
            Assert.AreEqual(992.46m, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(592.5m, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(-1005m, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);
            // 100k total value * 0.5 target * 0.9975 setHoldings buffer - 75K holdings / @ 25 ~= -1005m
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(-1004.96m, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(-605m, actual);
        }

        [Test]
        public void SetHoldings_LongToZero()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToZero_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToZero_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToShort()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            // no shorting allowed
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_LongToShort_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            // no shorting allowed
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_LongToShort_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            // no shorting allowed
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_HalfLongToFullShort()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            // no shorting allowed
            var actual = algo.CalculateOrderQuantity(_symbol, -1m);
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_HalfLongToFullShort_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            // no shorting allowed
            var actual = algo.CalculateOrderQuantity(_symbol, -1m);
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_HalfLongToFullShort_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            // no shorting allowed
            var actual = algo.CalculateOrderQuantity(_symbol, -1m);
            Assert.AreEqual(0, actual);
        }

        /*****************************************************/
        //  Rising market conditions tests.
        /*****************************************************/

        [Test]
        public void SetHoldings_LongFixed_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);

            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            Assert.AreEqual(100000, algo.Portfolio.TotalPortfolioValue);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);
            algo.Portfolio.InvalidateTotalPortfolioValue();

            Assert.AreEqual(150000, algo.Portfolio.TotalPortfolioValue);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% security::
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // 150k total value * 0.5 target * 0.9975 setHoldings buffer - 100K holdings / @ 50 = -503.75m
            Assert.AreEqual(-503.75m, actual);
        }

        [Test]
        public void SetHoldings_LongFixed_PriceRise_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);

            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% security::
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // 150k total value * 0.5 target * 0.9975 setHoldings buffer - 100K holdings / @ 50 = -503.75m - $1 in fees
            Assert.AreEqual(-503.73m, actual);
        }

        [Test]
        public void SetHoldings_LongFixed_PriceRise_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);

            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% security::
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // 150k total value * 0.5 target * 0.9975 setHoldings buffer - 100K holdings / @ 50 = -503.75m - -200 in fees
            Assert.AreEqual(-303.75m, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is already 66% of holdings.
            //Calculate the order for 75% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // 150k total value * 0.75 target * 0.9975 setHoldings buffer - 100K holdings / @ 50 = 244.375m
            Assert.AreEqual(244.375m, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_PriceRise_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is already 66% of holdings.
            //Calculate the order for 75% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // 150k total value * 0.75 target * 0.9975 setHoldings buffer - 100K holdings / @ 50 = 244.375m -$1 in fees
            Assert.AreEqual(244.355m, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_PriceRise_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is already 66% of holdings.
            //Calculate the order for 75% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // 150k total value * 0.75 target * 0.9975 setHoldings buffer - 100K holdings -10k in fees / @ 50 = 44.375m
            Assert.AreEqual(44.375m, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);

            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            algo.Settings.FreePortfolioValue =
                algo.Portfolio.TotalPortfolioValue * algo.Settings.FreePortfolioValuePercentage;

            //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. security is 86% of holdings.
            //Calculate the order for 50% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            Assert.IsTrue(security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio, security,
                new MarketOrder(_symbol, actual, DateTime.UtcNow)).IsSufficient);

            // $175k total value * 0.5 target * 0.9975 setHoldings buffer - $150k holdings / @ 50 = -1254.375m
            Assert.AreEqual(-1254.375m, actual);
        }

        [Test]
        public void SetHoldings_LongToShort_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is 66% of holdings.
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);

            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_ZeroToFullLong()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security));
            // 100000 * 0.9975 / 25 = 3990m
            Assert.AreEqual(3990m, actual);
            var hashSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio,
                security, new MarketOrder(_symbol, actual, DateTime.UtcNow));
            Assert.IsTrue(hashSufficientBuyingPower.IsSufficient);
        }

        [Test]
        public void SetHoldings_Long_TooBigOfATarget()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            var actual = algo.CalculateOrderQuantity(_symbol, 1m * security.BuyingPowerModel.GetLeverage(security) + 0.1m);

            Assert.AreEqual(4389m, actual);
            var hashSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio,
                security, new MarketOrder(_symbol, actual, DateTime.UtcNow));
            Assert.IsFalse(hashSufficientBuyingPower.IsSufficient);
        }

        [Test]
        public void SetHoldings_PriceRise_VolatilityCoveredByBuffer()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m);
            Assert.AreEqual(3990m, actual);

            //Price rises to 0.25%. We should be covered by buffer
            Update(algo.Portfolio.CashBook, security, security.Price * 1.0025m);

            var hashSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio,
                security, new MarketOrder(_symbol, actual, DateTime.UtcNow));
            Assert.IsTrue(hashSufficientBuyingPower.IsSufficient);
        }

        [Test]
        public void SetHoldings_PriceRise_VolatilityNotCoveredByBuffer()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);

            var actual = algo.CalculateOrderQuantity(_symbol, 1m);
            Assert.AreEqual(3990m, actual);

            // Price rises to 0.26%. We will not be covered by buffer
            Update(algo.Portfolio.CashBook, security, security.Price * 1.0026m);

            var hashSufficientBuyingPower = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(algo.Portfolio,
                security, new MarketOrder(_symbol, actual, DateTime.UtcNow));
            Assert.IsFalse(hashSufficientBuyingPower.IsSufficient);
        }


        [TestCase(SecurityType.Forex)]
        [TestCase(SecurityType.Crypto)]
        public void OrderQuantityConversionTest(SecurityType securityType)
        {
            var algo = GetAlgorithm(out var security, 0, securityType);
            var symbol = security.Symbol;
            algo.Portfolio.SetCash(150000);

            var mock = new Mock<ITransactionHandler>();
            var request = new Mock<SubmitOrderRequest>(null, null, null, null, null, null, null, null, null, null);
            mock.Setup(m => m.Process(It.IsAny<OrderRequest>())).Returns(new OrderTicket(null, request.Object));
            mock.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>())).Returns(new List<Order>());
            algo.Transactions.SetOrderProcessor(mock.Object);

            algo.Buy(symbol, 1);
            algo.Buy(symbol, 1.0);
            algo.Buy(symbol, 1.0m);
            algo.Buy(symbol, 1.0f);

            algo.Sell(symbol, 1);
            algo.Sell(symbol, 1.0);
            algo.Sell(symbol, 1.0m);
            algo.Sell(symbol, 1.0f);

            algo.Order(symbol, 1);
            algo.Order(symbol, 1.0);
            algo.Order(symbol, 1.0m);
            algo.Order(symbol, 1.0f);

            algo.MarketOrder(symbol, 1);
            algo.MarketOrder(symbol, 1.0);
            algo.MarketOrder(symbol, 1.0m);
            algo.MarketOrder(symbol, 1.0f);

            int expected = 32;
            if (securityType == SecurityType.Crypto)
            {
                expected -= 6;

                Assert.Throws<InvalidOperationException>(() => algo.MarketOnCloseOrder(symbol, 1));
                Assert.Throws<InvalidOperationException>(() => algo.MarketOnCloseOrder(symbol, 1.0));
                Assert.Throws<InvalidOperationException>(() => algo.MarketOnCloseOrder(symbol, 1.0m));

                Assert.Throws<InvalidOperationException>(() => algo.MarketOnOpenOrder(symbol, 1));
                Assert.Throws<InvalidOperationException>(() => algo.MarketOnOpenOrder(symbol, 1.0));
                Assert.Throws<InvalidOperationException>(() => algo.MarketOnOpenOrder(symbol, 1.0m));
            }
            else
            {
                algo.MarketOnOpenOrder(symbol, 1);
                algo.MarketOnOpenOrder(symbol, 1.0);
                algo.MarketOnOpenOrder(symbol, 1.0m);

                algo.MarketOnCloseOrder(symbol, 1);
                algo.MarketOnCloseOrder(symbol, 1.0);
                algo.MarketOnCloseOrder(symbol, 1.0m);
            }

            algo.LimitOrder(symbol, 1, 1);
            algo.LimitOrder(symbol, 1.0, 1);
            algo.LimitOrder(symbol, 1.0m, 1);

            algo.StopMarketOrder(symbol, 1, 1);
            algo.StopMarketOrder(symbol, 1.0, 1);
            algo.StopMarketOrder(symbol, 1.0m, 1);

            algo.SetHoldings(symbol, 1);
            algo.SetHoldings(symbol, 1.0);
            algo.SetHoldings(symbol, 1.0m);
            algo.SetHoldings(symbol, 1.0f);

            Assert.AreEqual(expected, algo.Transactions.LastOrderId);
        }

        private static QCAlgorithm GetAlgorithm(out Security security, decimal fee, SecurityType securityType = SecurityType.Crypto)
        {
            SymbolCache.Clear();
            // Initialize algorithm
            var algo = new QCAlgorithm();
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            algo.SetCash(100000);
            algo.SetBrokerageModel(BrokerageName.Coinbase, AccountType.Cash);
            algo.Transactions.SetOrderProcessor(new FakeOrderProcessor());
            algo.SetFinishedWarmingUp();
            var cashSymbol = string.Empty;
            if (securityType == SecurityType.Crypto)
            {
                cashSymbol = "BTC";
                security = algo.AddSecurity(securityType, "BTCUSD");
            }
            else if(securityType == SecurityType.Forex)
            {
                cashSymbol = "EUR";
                security = algo.AddSecurity(securityType, "EURUSD");
                // set BPM to cash, since it's not the default
                security.BuyingPowerModel = new CashBuyingPowerModel();
            }
            else
            {
                throw new NotImplementedException("Unexpected security type");
            }
            security.FeeModel = new ConstantFeeModel(fee);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25, cashSymbol);
            return algo;
        }

        private static void Update(CashBook cashBook, Security security, decimal close, string cashSymbol = "BTC")
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = new DateTime(2022, 3, 15, 8, 0, 0),
                Symbol = security.Symbol,
                Open = close,
                High = close,
                Low = close,
                Close = close
            });

            if (cashBook.TryGetValue(cashSymbol, out var cash))
            {
                cash.ConversionRate = close;
            }
        }
    }
}
