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
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using Moq;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class CashModelAlgorithmTradingTests
    {
        private readonly Symbol _symbol = Symbols.BTCUSD;
        private readonly string _cashSymbol = "BTC";

        /*****************************************************/
        //  Isostatic market conditions tests.
        /*****************************************************/

        [Test]
        public void SetHoldings_ZeroToLong()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25 & Target 50%
            Update(algo.Portfolio.CashBook, security, 25);
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);
            Assert.AreEqual(2000, actual);
        }

        [Test]
        public void SetHoldings_ZeroToLong_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25 & Target 50%
            Update(algo.Portfolio.CashBook, security, 25);
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);
            // $1 in fees, so slightly less than 2k from SetHoldings_ZeroToLong
            Assert.AreEqual(1999.96, actual);
        }

        [Test]
        public void SetHoldings_ZeroToLong_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25 & Target 50%
            Update(algo.Portfolio.CashBook, security, 25);
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);
            // 10k in fees = 400 shares (400*25), so 400 less than 2k from SetHoldings_ZeroToLong
            Assert.AreEqual(1600, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25 & Target 50%
            Update(algo.Portfolio.CashBook, security, 25);
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25 & Target 50%
            Update(algo.Portfolio.CashBook, security, 25);
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25 & Target 50%
            Update(algo.Portfolio.CashBook, security, 25);
            var actual = algo.CalculateOrderQuantity(_symbol, -0.5m);
            // no shorting allowed
            Assert.AreEqual(0, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);
            Assert.AreEqual(1000, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);
            // 1000 - 0.04 fees = 999.96
            Assert.AreEqual(999.96, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);
            Assert.AreEqual(600, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);
            Assert.AreEqual(-1000, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);
            // 50% of TPV is 50K = 2000 shares, need to sell slightly less than 1000 including $1 fee
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);
            Assert.AreEqual(-999.96, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);
            Assert.AreEqual(-600, actual);
        }

        [Test]
        public void SetHoldings_LongToZero()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToZero_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToZero_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity(_symbol, 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToShort()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);

            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            Assert.AreEqual(100000, algo.Portfolio.TotalPortfolioValue);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            Assert.AreEqual(150000, algo.Portfolio.TotalPortfolioValue);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% security::
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares
            Assert.AreEqual(-500, actual);
        }

        [Test]
        public void SetHoldings_LongFixed_PriceRise_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);

            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% security::
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares, -$1 in fees
            Assert.AreEqual(-499.98, actual);
        }

        [Test]
        public void SetHoldings_LongFixed_PriceRise_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);

            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% security::
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares, -200 in fees
            Assert.AreEqual(-300, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is already 66% of holdings.
            //Calculate the order for 75% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares
            Assert.AreEqual(250, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_PriceRise_SmallConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 1);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is already 66% of holdings.
            //Calculate the order for 75% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares, -$1 in fees = 249.98
            Assert.AreEqual(249.98, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_PriceRise_HighConstantFeeStructure()
        {
            Security security;
            var algo = GetAlgorithm(out security, 10000);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
            //Half cash spent on 2000 shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 2000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. security is already 66% of holdings.
            //Calculate the order for 75% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares, -10k in fees = 50
            Assert.AreEqual(50, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);

            //75% cash spent on 3000 shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio.CashBook.Add(_cashSymbol, 3000, 25);

            //Price rises to $50.
            Update(algo.Portfolio.CashBook, security, 50);

            //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. security is 86% of holdings.
            //Calculate the order for 50% security:
            var actual = algo.CalculateOrderQuantity(_symbol, 0.5m);

            //Need to sell to 50% = 87.5k target from $150k = 62.5 / $50-share = 1250
            Assert.AreEqual(-1250, actual);
        }

        [Test]
        public void SetHoldings_LongToShort_PriceRise()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);
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
        public void OrderQuantityConversionTest()
        {
            Security security;
            var algo = GetAlgorithm(out security, 0);
            algo.SetFinishedWarmingUp();

            //Set price to $25
            Update(algo.Portfolio.CashBook, security, 25);

            algo.Portfolio.SetCash(150000);

            var mock = new Mock<IOrderProcessor>();
            var request = new Mock<Orders.SubmitOrderRequest>(null, null, null, null, null, null, null, null, null);
            mock.Setup(m => m.Process(It.IsAny<Orders.OrderRequest>())).Returns(new Orders.OrderTicket(null, request.Object));
            algo.Transactions.SetOrderProcessor(mock.Object);

            algo.Buy(_symbol, 1);
            algo.Buy(_symbol, 1.0);
            algo.Buy(_symbol, 1.0m);
            algo.Buy(_symbol, 1.0f);

            algo.Sell(_symbol, 1);
            algo.Sell(_symbol, 1.0);
            algo.Sell(_symbol, 1.0m);
            algo.Sell(_symbol, 1.0f);

            algo.Order(_symbol, 1);
            algo.Order(_symbol, 1.0);
            algo.Order(_symbol, 1.0m);
            algo.Order(_symbol, 1.0f);

            algo.MarketOrder(_symbol, 1);
            algo.MarketOrder(_symbol, 1.0);
            algo.MarketOrder(_symbol, 1.0m);
            algo.MarketOrder(_symbol, 1.0f);

            algo.MarketOnOpenOrder(_symbol, 1);
            algo.MarketOnOpenOrder(_symbol, 1.0);
            algo.MarketOnOpenOrder(_symbol, 1.0m);

            algo.MarketOnCloseOrder(_symbol, 1);
            algo.MarketOnCloseOrder(_symbol, 1.0);
            algo.MarketOnCloseOrder(_symbol, 1.0m);

            algo.LimitOrder(_symbol, 1, 1);
            algo.LimitOrder(_symbol, 1.0, 1);
            algo.LimitOrder(_symbol, 1.0m, 1);

            algo.StopMarketOrder(_symbol, 1, 1);
            algo.StopMarketOrder(_symbol, 1.0, 1);
            algo.StopMarketOrder(_symbol, 1.0m, 1);

            algo.SetHoldings(_symbol, 1);
            algo.SetHoldings(_symbol, 1.0);
            algo.SetHoldings(_symbol, 1.0m);
            algo.SetHoldings(_symbol, 1.0f);

            const int expected = 32;
            Assert.AreEqual(expected, algo.Transactions.LastOrderId);
        }

        private static QCAlgorithm GetAlgorithm(out Security security, decimal fee)
        {
            // Initialize algorithm
            var algo = new QCAlgorithm();
            algo.SetCash(100000);
            algo.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);
            security = algo.AddSecurity(SecurityType.Crypto, "BTCUSD");
            security.TransactionModel = new ConstantFeeTransactionModel(fee);
            return algo;
        }

        private void Update(CashBook cashBook, Security security, decimal close)
        {
            security.SetMarketPrice(new TradeBar
            {
                Time = DateTime.Now,
                Symbol = security.Symbol,
                Open = close,
                High = close,
                Low = close,
                Close = close
            });

            cashBook[_cashSymbol].ConversionRate = close;
        }
    }
}