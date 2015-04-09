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
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using QuantConnect.Data;
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
            securities.Add("CASH", subscriptions.Add(SecurityType.Base, "CASH"), leverage: 10);
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
                Dictionary<int, List<BaseData>> updateData = new Dictionary<int, List<BaseData>>();
                updateData.Add(0, new List<BaseData> {new IndicatorDataPoint("CASH", time, i + 1)});
                securities.Update(time, updateData);

                portfolio.ProcessFill(fill);
                Assert.AreEqual(equity[i + 1], portfolio.TotalPortfolioValue);
            }
        }
    }
}
