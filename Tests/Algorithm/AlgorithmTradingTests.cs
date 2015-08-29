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

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmTradingTests
    {
        /*****************************************************/
        //  Isostatic market conditions tests.
        /*****************************************************/
        [Test]
        public void SetHoldings_ZeroToLong()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);
            Assert.AreEqual(2000, actual);
        }
        [Test]
        public void SetHoldings_ZeroToLong_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);
            // 10k in fees = 400 shares (400*25), so 400 less than 2k from SetHoldings_ZeroToLong
            Assert.AreEqual(1600, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_ZeroToShort_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25 & Target 50%
            Update(msft, 25);
            var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);
            Assert.AreEqual(-1600, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.75m);
            Assert.AreEqual(1000, actual);
        }

        [Test]
        public void SetHoldings_LongToLonger_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);
            //Calculate the new holdings:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.75m);
            Assert.AreEqual(600, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio["MSFT"].SetHoldings(25, 3000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);
            Assert.AreEqual(-1000, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio["MSFT"].SetHoldings(25, 3000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);
            Assert.AreEqual(-600, actual);
        }

        [Test]
        public void SetHoldings_LongToZero()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity("MSFT", 0m);
            Assert.AreEqual(-2000, actual);
        }

        [Test]
        public void SetHoldings_LongToZero_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);
            //Sell all 2000 held:
            var actual = algo.CalculateOrderQuantity("MSFT", 0m);
            Assert.AreEqual(-1600, actual);
        }

        [Test]
        public void SetHoldings_LongToShort()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Sell all 2000 held + -2000 to get to -50%
            var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);
            Assert.AreEqual(-4000, actual);
        }

        [Test]
        public void SetHoldings_LongToShort_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Sell all 2000 held + -2000 to get to -50%
            var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);
            Assert.AreEqual(-3600, actual);
        }

        [Test]
        public void SetHoldings_ShortToZero()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);
            //Buy 2000 to get to 0 holdings.
            var actual = algo.CalculateOrderQuantity("MSFT", 0m);
            Assert.AreEqual(2000, actual);
        }

        [Test]
        public void SetHoldings_ShortToZero_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);
            //Buy 2000 to get to 0 holdings.
            var actual = algo.CalculateOrderQuantity("MSFT", 0m);
            Assert.AreEqual(1600, actual);
        }

        [Test]
        public void SetHoldings_ShortToShorter()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);

            // Cash: 150k
            // MSFT: -50k
            // TPV:  100k

            // we should end with -3000 = -.75*(100k/25)

            var actual = algo.CalculateOrderQuantity("MSFT", -0.75m);
            Assert.AreEqual(-1000, actual);
        }

        [Test]
        public void SetHoldings_ShortToShorter_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);

            // Cash: 150k
            // MSFT: -50k
            // TPV:  100k

            // we should end with -3000 = -.75*(100k/25)

            var actual = algo.CalculateOrderQuantity("MSFT", -0.75m);
            Assert.AreEqual(-600, actual);
        }

        [Test]
        public void SetHoldings_ShortToLong()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);
            // TPV: 150k - 50k = 100k*.5=50k @ 25 = 2000, so we need 4000 since we start at -2k
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);
            Assert.AreEqual(4000, actual);
        }

        [Test]
        public void SetHoldings_ShortToLong_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);
            // TPV: 150k - 50k = 100k*.5=50k @ 25 = 2000, so we need 4000 since we start at -2k
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);
            Assert.AreEqual(3600, actual);
        }


        /*****************************************************/
        //  Rising market conditions tests.
        /*****************************************************/
        [Test]
        public void SetHoldings_LongFixed_PriceRise()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);

            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares
            Assert.AreEqual(-500, actual);
        }
        [Test]
        public void SetHoldings_LongFixed_PriceRise_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);

            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k.
            //Calculate the new holdings for 50% MSFT::
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

            //Need to sell $25k so 50% of $150k: $25k / $50-share = -500 shares, -200 in fees
            Assert.AreEqual(-300, actual);
        }


        [Test]
        public void SetHoldings_LongToLonger_PriceRise()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares
            Assert.AreEqual(250, actual);
        }
        [Test]
        public void SetHoldings_LongToLonger_PriceRise_HighConstantFeeStructure()
        {
            Security msft;
            var algo = GetAlgorithm(out msft, 10000);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
            //Calculate the order for 75% MSFT:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.75m);

            //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares, -10k in fees = 50
            Assert.AreEqual(50, actual);
        }

        [Test]
        public void SetHoldings_LongerToLong_PriceRise()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);

            //75% cash spent on 3000 MSFT shares.
            algo.Portfolio.SetCash(25000);
            algo.Portfolio["MSFT"].SetHoldings(25, 3000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. MSFT is 86% of holdings.
            //Calculate the order for 50% MSFT:
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

            //Need to sell to 50% = 87.5k target from $150k = 62.5 / $50-share = 1250
            Assert.AreEqual(-1250, actual);
        }


        [Test]
        public void SetHoldings_LongToShort_PriceRise()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Half cash spent on 2000 MSFT shares.
            algo.Portfolio.SetCash(50000);
            algo.Portfolio["MSFT"].SetHoldings(25, 2000);

            //Price rises to $50.
            Update(msft, 50);

            //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is 66% of holdings.
            var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);

            // Need to hold -75k from $100k = delta: $175k / $50-share = -3500 shares.
            Assert.AreEqual(-3500, actual);
        }

        [Test]
        public void SetHoldings_ShortToShorter_PriceRise()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);

            //Price rises to $50.
            Update(msft, 50);

            // Cash: 150k
            // MSFT: -(2000*50) = -100K
            // TPV: 50k
            Assert.AreEqual(50000, algo.Portfolio.TotalPortfolioValue);

            // we should end with -750 shares (-.75*50000/50)
            var actual = algo.CalculateOrderQuantity("MSFT", -0.75m);

            // currently -2000, so plus 1250
            Assert.AreEqual(1250, actual);
        }


        [Test]
        public void SetHoldings_ShortToLong_PriceRise_ZeroValue()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);

            //Price rises to $50: holdings now worthless.
            Update(msft, 50m);

            //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

            //We want to be 50% long, this is currently +2000 holdings + 50% 50k = $25k/ $50-share=500
            Assert.AreEqual(2500, actual);
        }

        [Test]
        public void SetHoldings_ShortToLong_PriceRise()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 2);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);

            //Price rises to $50
            Update(msft, 50m);

            // Cash: 150k
            // MSFT: -50*2000=100k
            // TPV: 50k
            Assert.AreEqual(50000, algo.Portfolio.TotalPortfolioValue);

            // 50k*0.5=25k = 500 end holdings
            var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

            // 500 will makes us 50% tpv, we hold -2000, so 2500 buy
            Assert.AreEqual(2500, actual);
        }



        /*****************************************************/
        //  Falling market conditions tests.
        /*****************************************************/
        [Test]
        public void SetHoldings_ShortFixed_PriceFall()
        {
            Security msft;
            var algo = GetAlgorithm(out msft);
            //Set price to $25
            Update(msft, 25);
            //Sold -2000 MSFT shares, +50k cash
            algo.Portfolio.SetCash(150000);
            algo.Portfolio["MSFT"].SetHoldings(25, -2000);

            Update(msft, 12.5m);

            // Cash: 150k
            // MSFT: -25k
            // TPV : 125k

            // -50% of 125 = (62.5k) @ 12.5/share = -5000

            // to get to -5000 we'll need to short another 3000
            var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);
            Assert.AreEqual(-3000, actual);
        }


        //[Test]
        //public void SetHoldings_LongToLonger_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on 2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio["MSFT"].SetHoldings(25, 2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is already 66% of holdings.
        //    //Calculate the order for 75% MSFT:
        //    var actual = algo.CalculateOrderQuantity("MSFT", 0.75m);

        //    //Need to buy to make position $112.5k == $12.5k / 50 = 250 shares
        //    Assert.AreEqual(250, actual);
        //}

        //[Test]
        //public void SetHoldings_LongerToLong_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));

        //    //75% cash spent on 3000 MSFT shares.
        //    algo.Portfolio.SetCash(25000);
        //    algo.Portfolio["MSFT"].SetHoldings(25, 3000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 3000 * 50 = $150k Holdings, $25k Cash: $175k. MSFT is 86% of holdings.
        //    //Calculate the order for 50% MSFT:
        //    var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

        //    //Need to sell to 50% = 87.5k target from $150k = 62.5 / $50-share = 1250
        //    Assert.AreEqual(-1250, actual);
        //}


        //[Test]
        //public void SetHoldings_LongToShort_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on 2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio["MSFT"].SetHoldings(25, 2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $100k Holdings, $50k Cash: $150k. MSFT is 66% of holdings.
        //    var actual = algo.CalculateOrderQuantity("MSFT", -0.5m);

        //    // Need to hold -75k from $100k = delta: $175k / $50-share = -3500 shares.
        //    Assert.AreEqual(-3500, actual);
        //}

        //[Test]
        //public void SetHoldings_ShortToShorter_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on -2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio["MSFT"].SetHoldings(25, -2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
        //    var actual = algo.CalculateOrderQuantity("MSFT", -0.75m);

        //    //Want to hold -75% of MSFT: 50k total, -37.5k / $50-share = -750 TOTAL. 
        //    // Currently -2000, so net order +1250.
        //    Assert.AreEqual(1250, actual);
        //}

        //[Test]
        //public void SetHoldings_ShortToLong_PriceRise()
        //{
        //    var algo = GetAlgorithm();
        //    //Set price to $25
        //    Update(msft, 25));
        //    //Half cash spent on -2000 MSFT shares.
        //    algo.Portfolio.SetCash(50000);
        //    algo.Portfolio["MSFT"].SetHoldings(25, -2000);

        //    //Price rises to $50.
        //    Update(msft, 50));

        //    //Now: 2000 * 50 = $0k Net Holdings, $50k Cash: $50k. MSFT is 0% of holdings.
        //    var actual = algo.CalculateOrderQuantity("MSFT", 0.5m);

        //    //We want to be 50% long, this is currently +2000 holdings + 50% 50k = $25k/ $50-share=500
        //    Assert.AreEqual(2500, actual);
        //}

















        private QCAlgorithm GetAlgorithm(out Security msft, decimal fee = 0)
        {
            //Initialize algorithm
            var algo = new QCAlgorithm();
            algo.AddSecurity(SecurityType.Equity, "MSFT");
            algo.SetCash(100000);
            algo.Securities["MSFT"].TransactionModel = new ConstantFeeTransactionModel(fee);
            msft = algo.Securities["MSFT"];
            return algo;
        }


        private void Update(Security security, decimal close)
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
        }
    }
}