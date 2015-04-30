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
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityPortfolioManagerTests
    {
        [Test]
        public void TestCashFills()
        {
            // this test asserts the portfolio behaves according to the Test_Cash algo, see TestData\CashTestingStrategy.csv
            // also "https://www.dropbox.com/s/oiliumoyqqj1ovl/2013-cash.csv?dl=1"

            const string fillsFile = "TestData\\test_cash_fills.xml";
            const string equityFile = "TestData\\test_cash_equity.xml";

            var fills = XDocument.Load(fillsFile).Descendants("OrderEvent").Select(x => new OrderEvent(
                x.Get<int>("OrderId"),
                x.Get<string>("Symbol"),
                x.Get<OrderStatus>("Status"),
                x.Get<decimal>("FillPrice"),
                x.Get<int>("FillQuantity"))
                ).ToList();

            var equity = XDocument.Load(equityFile).Descendants("decimal")
                .Select(x => decimal.Parse(x.Value, CultureInfo.InvariantCulture))
                .ToList();

            Assert.AreEqual(fills.Count + 1, equity.Count);

            // we're going to process fills and very our equity after each fill
            var subscriptions = new SubscriptionManager();
            var securities = new SecurityManager();
            securities.Add("CASH", new Security(subscriptions.Add(SecurityType.Base, "CASH"), leverage: 10));
            var transactions = new SecurityTransactionManager(securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            portfolio.SetCash(equity[0]);

            for (int i = 0; i < fills.Count; i++)
            {
                // before processing the fill we must deduct the cost
                var fill = fills[i];
                var time = DateTime.Today.AddDays(i);

                // the value of 'CASH' increments for each fill, the original test algo did this monthly
                // the time doesn't really matter though
                var updateData = new Dictionary<int, List<BaseData>>();
                updateData.Add(0, new List<BaseData> {new IndicatorDataPoint("CASH", time, i + 1)});
                securities.Update(time, updateData);

                portfolio.ProcessFill(fill);
                Assert.AreEqual(equity[i + 1], portfolio.TotalPortfolioValue, "Failed on " + i);
            }
        }

        [Test]
        public void ForexCashFills()
        {
            // this test asserts the portfolio behaves according to the Test_Cash algo, but for a Forex security, 
            // see TestData\CashTestingStrategy.csv; also "https://www.dropbox.com/s/oiliumoyqqj1ovl/2013-cash.csv?dl=1"

            const string fillsFile = "TestData\\test_forex_fills.xml";
            const string equityFile = "TestData\\test_forex_equity.xml";
            const string mchQuantityFile = "TestData\\test_forex_fills_mch_quantity.xml";
            const string jwbQuantityFile = "TestData\\test_forex_fills_jwb_quantity.xml";

            var fills = XDocument.Load(fillsFile).Descendants("OrderEvent").Select(x => new OrderEvent(
                x.Get<int>("OrderId"),
                x.Get<string>("Symbol"),
                x.Get<OrderStatus>("Status"),
                x.Get<decimal>("FillPrice"),
                x.Get<int>("FillQuantity"))
                ).ToList();

            var equity = XDocument.Load(equityFile).Descendants("decimal")
                .Select(x => decimal.Parse(x.Value, CultureInfo.InvariantCulture))
                .ToList();

            var mchQuantity = XDocument.Load(mchQuantityFile).Descendants("decimal")
                .Select(x => decimal.Parse(x.Value, CultureInfo.InvariantCulture))
                .ToList();

            var jwbQuantity = XDocument.Load(jwbQuantityFile).Descendants("decimal")
                .Select(x => decimal.Parse(x.Value, CultureInfo.InvariantCulture))
                .ToList();

            Assert.AreEqual(fills.Count + 1, equity.Count);

            // we're going to process fills and very our equity after each fill
            var subscriptions = new SubscriptionManager();
            var securities = new SecurityManager();
            var transactions = new SecurityTransactionManager(securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            portfolio.SetCash(equity[0]);
            portfolio.CashBook.Add("MCH", mchQuantity[0], 0);
            portfolio.CashBook.Add("JWB", jwbQuantity[0], 0);

            var jwbCash = portfolio.CashBook["JWB"];
            var mchCash = portfolio.CashBook["MCH"];
            var usdCash = portfolio.CashBook["USD"];

            var mchJwbSecurity = new QuantConnect.Securities.Forex.Forex(jwbCash, subscriptions.Add(SecurityType.Forex, "MCHJWB"), leverage: 10);
            var mchUsdSecurity = new QuantConnect.Securities.Forex.Forex(usdCash, subscriptions.Add(SecurityType.Forex, "MCHUSD"), leverage: 10);
            var usdJwbSecurity = new QuantConnect.Securities.Forex.Forex(mchCash, subscriptions.Add(SecurityType.Forex, "USDJWB"), leverage: 10);
            
            // no fee model
            mchJwbSecurity.TransactionModel = new SecurityTransactionModel();
            mchUsdSecurity.TransactionModel = new SecurityTransactionModel();
            usdJwbSecurity.TransactionModel = new SecurityTransactionModel();

            securities.Add(mchJwbSecurity);
            securities.Add(usdJwbSecurity);
            securities.Add(mchUsdSecurity);

            portfolio.CashBook.EnsureCurrencyDataFeeds(securities, subscriptions);

            for (int i = 0; i < fills.Count; i++)
            {
                // before processing the fill we must deduct the cost
                var fill = fills[i];
                var time = DateTime.Today.AddDays(i);

                // the value of 'MCJWB' increments for each fill, the original test algo did this monthly
                // the time doesn't really matter though
                decimal mchJwb = i + 1;
                decimal mchUsd = (i + 1)/(i + 2m);
                decimal usdJwb = i + 2;
                Assert.AreEqual((double)mchJwb, (double)(mchUsd*usdJwb), 1e-10);
                //Console.WriteLine("Step: " + i + " -- MCHJWB: " + mchJwb);

                var updateData = new Dictionary<int, List<BaseData>>();
                updateData.Add(0, new List<BaseData> {new IndicatorDataPoint("MCHJWB", time, mchJwb)});
                updateData.Add(1, new List<BaseData> {new IndicatorDataPoint("MCHUSD", time, mchUsd)});
                updateData.Add(2, new List<BaseData> {new IndicatorDataPoint("JWBUSD", time, usdJwb)});

                securities.Update(time, updateData);
                portfolio.CashBook.Update(updateData);
                portfolio.ProcessFill(fill);
                //Console.WriteLine("-----------------------");
                //Console.WriteLine(fill);

                //Console.WriteLine("Post step: " + i);
                //foreach (var cash in portfolio.CashBook)
                //{
                //    Console.WriteLine(cash.Value);
                //}
                //Console.WriteLine("CashValue: " + portfolio.CashBook.TotalValueInAccountCurrency);

                Console.WriteLine(i + 1 + "   " + portfolio.TotalPortfolioValue.ToString("C"));
                //Assert.AreEqual((double) equity[i + 1], (double)portfolio.TotalPortfolioValue, 2e-2);
                Assert.AreEqual((double) mchQuantity[i + 1], (double)portfolio.CashBook["MCH"].Quantity);
                Assert.AreEqual((double) jwbQuantity[i + 1], (double)portfolio.CashBook["JWB"].Quantity);

                //Console.WriteLine();
                //Console.WriteLine();
            }
        }

        [Test]
        public void ComputeMarginProperlyAsSecurityPriceFluctuates()
        {
            const int quantity = 1000;
            var securities = new SecurityManager();
            var transactions = new SecurityTransactionManager(securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            portfolio.CashBook["USD"].Quantity = quantity;

            var config = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Equity, "AAPL", Resolution.Minute, true, true, true, true, true, 0);
            securities.Add(new Security(config, 1, false));

            var time = DateTime.Now;
            const decimal buyPrice = 1m;
            securities["AAPL"].SetMarketPrice(time, new TradeBar(time, "AAPL", buyPrice, buyPrice, buyPrice, buyPrice, 1));

            var order = new MarketOrder("AAPL", quantity, time) {Price = buyPrice};
            var fill = new OrderEvent(order){FillPrice = buyPrice, FillQuantity = quantity};

            Assert.AreEqual(portfolio.CashBook["USD"].Quantity, fill.FillPrice*fill.FillQuantity);

            portfolio.ProcessFill(fill);

            Assert.AreEqual(0, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity, portfolio.TotalPortfolioValue);

            // we shouldn't be able to place a trader
            var newOrder = new MarketOrder("AAPL", 1, time.AddSeconds(1)) {Price = buyPrice};
            bool sufficientCapital = transactions.GetSufficientCapitalForOrder(portfolio, newOrder);
            Assert.IsFalse(sufficientCapital);

            // now the stock doubles, so we should have margin remaining

            time = time.AddDays(1);
            const decimal highPrice = buyPrice * 2;
            securities["AAPL"].SetMarketPrice(time, new TradeBar(time, "AAPL", highPrice, highPrice, highPrice, highPrice, 1));

            Assert.AreEqual(quantity, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity * 2, portfolio.TotalPortfolioValue);

            // we shouldn't be able to place a trader
            var anotherOrder = new MarketOrder("AAPL", 1, time.AddSeconds(1)) { Price = highPrice };
            sufficientCapital = transactions.GetSufficientCapitalForOrder(portfolio, anotherOrder);
            Assert.IsTrue(sufficientCapital);

            // now the stock plummets, so we should have negative margin remaining

            time = time.AddDays(1);
            const decimal lowPrice = buyPrice/2;
            securities["AAPL"].SetMarketPrice(time, new TradeBar(time, "AAPL", lowPrice, lowPrice, lowPrice, lowPrice, 1));

            Assert.AreEqual(-quantity/2m, portfolio.MarginRemaining);
            Assert.AreEqual(quantity, portfolio.TotalMarginUsed);
            Assert.AreEqual(quantity/2m, portfolio.TotalPortfolioValue);


            // this would cause a margin call
            var marginCallOrders = portfolio.ScanForMarginCall();
            Assert.AreNotEqual(0, marginCallOrders.Count);
            Assert.AreEqual(-quantity, marginCallOrders[0].Quantity);
            Assert.GreaterOrEqual(-portfolio.MarginRemaining, marginCallOrders[0].Price*marginCallOrders[0].Quantity);
        }

        [Test]
        public void MarginComputesProperlyWithMultipleSecurities()
        {
            var securities = new SecurityManager();
            var transactions = new SecurityTransactionManager(securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            portfolio.CashBook["USD"].Quantity = 1000;
            portfolio.CashBook.Add("EUR",  1000, 1.1m);
            portfolio.CashBook.Add("GBP", -1000, 2.0m);

            var eurCash = portfolio.CashBook["EUR"];
            var gbpCash = portfolio.CashBook["GBP"];
            var usdCash = portfolio.CashBook["USD"];

            var time = DateTime.Now;
            var config1 = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Equity, "AAPL", Resolution.Minute, true, true, true, true, true, 0);
            securities.Add(new Security(config1, 2, false));
            securities["AAPL"].Holdings.SetHoldings(100, 100);
            securities["AAPL"].SetMarketPrice(time, new TradeBar{Time = time, Value = 100});
            //Console.WriteLine("AAPL TMU: " + securities["AAPL"].MarginModel.GetMaintenanceMargin(securities["AAPL"]));
            //Console.WriteLine("AAPL Value: " + securities["AAPL"].Holdings.HoldingsValue);

            //Console.WriteLine();

            var config2 = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "EURUSD", Resolution.Minute, true, true, true, true, true, 1);
            securities.Add(new QuantConnect.Securities.Forex.Forex(usdCash, config2, 100, false));
            securities["EURUSD"].Holdings.SetHoldings(1.1m, 1000);
            securities["EURUSD"].SetMarketPrice(time, new TradeBar { Time = time, Value = 1.1m });
            //Console.WriteLine("EURUSD TMU: " + securities["EURUSD"].MarginModel.GetMaintenanceMargin(securities["EURUSD"]));
            //Console.WriteLine("EURUSD Value: " + securities["EURUSD"].Holdings.HoldingsValue);

            //Console.WriteLine();

            var config3 = new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Forex, "EURGBP", Resolution.Minute, true, true, true, true, true, 2);
            securities.Add(new QuantConnect.Securities.Forex.Forex(gbpCash, config3, 100, false));
            securities["EURGBP"].Holdings.SetHoldings(1m, 1000);
            securities["EURGBP"].SetMarketPrice(time, new TradeBar { Time = time, Value = 1m });
            //Console.WriteLine("EURGBP TMU: " + securities["EURGBP"].MarginModel.GetMaintenanceMargin(securities["EURGBP"]));
            //Console.WriteLine("EURGBP Value: " + securities["EURGBP"].Holdings.HoldingsValue);

            //Console.WriteLine();

            //Console.WriteLine(portfolio.CashBook["USD"]);
            //Console.WriteLine(portfolio.CashBook["EUR"]);
            //Console.WriteLine(portfolio.CashBook["GBP"]);
            //Console.WriteLine("CashBook: " + portfolio.CashBook.TotalValueInAccountCurrency);

            //Console.WriteLine();

            //Console.WriteLine("Total Margin Used: " + portfolio.TotalMarginUsed);
            //Console.WriteLine("Total Free Margin: " + portfolio.MarginRemaining);
            //Console.WriteLine("Total Portfolio Value: " + portfolio.TotalPortfolioValue);


            var acceptedOrder = new MarketOrder("AAPL", 101, DateTime.Now) {Price = 100};
            var sufficientCapital = transactions.GetSufficientCapitalForOrder(portfolio, acceptedOrder);
            Assert.IsTrue(sufficientCapital);

            var rejectedOrder = new MarketOrder("AAPL", 102, DateTime.Now) { Price = 100 };
            sufficientCapital = transactions.GetSufficientCapitalForOrder(portfolio, rejectedOrder);
            Assert.IsFalse(sufficientCapital);
        }
    }
}
