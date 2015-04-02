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
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

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
            var serializer = new XmlSerializer(typeof (List<OrderEvent>));
            List<OrderEvent> fills;
            using (var stream = File.OpenRead(fillsFile))
            {
                fills = (List<OrderEvent>) serializer.Deserialize(stream);
            }

            serializer = new XmlSerializer(typeof (List<decimal>));
            List<decimal> equity;
            using (var stream = File.OpenRead(equityFile))
            {
                equity = (List<decimal>) serializer.Deserialize(stream);
            }

            Assert.AreEqual(fills.Count + 1, equity.Count);

            // we're going to process fills and very our equity after each fill
            var securities = new SecurityManager();
            securities.Add("CASH", SecurityType.Base, leverage: 10);
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
                securities.Update(time, new IndicatorDataPoint("CASH", time, i + 1));

                portfolio.ProcessFill(fill);
                Assert.AreEqual(equity[i + 1], portfolio.TotalPortfolioValue);
            }
        }
    }
}
