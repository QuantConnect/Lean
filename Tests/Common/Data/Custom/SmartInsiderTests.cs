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

using NUnit.Framework;
using QuantConnect.Data.Custom.SmartInsider;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class SmartInsiderTests
    {
        [Test]
        public void ErrorGetsMappedToSatisfyStockVesting()
        {
            var intentionLine = "20200101 01:02:03	BIXYZ	Downwards Revision	20190101	20190101	USXYZ		1	Some Random Industry																	US	Off Market Agreement	Issuer	Missing Lookup Formula for BuybackHoldingTypeId 10.00										";
            var transactionLine = "20200101 01:02:03	BIXYZ	Downwards Revision	20190101	20190101	USXYZ		1	Some Random Industry																			Off Market Agreement	Issuer	Missing Lookup Formula for BuybackHoldingTypeId 10.00																														";

            var intention = new SmartInsiderIntention(intentionLine);
            var transaction = new SmartInsiderTransaction(transactionLine);

            Assert.AreEqual(new DateTime(2020, 1, 1, 1, 2, 3), intention.Time);
            Assert.AreEqual(new DateTime(2020, 1, 1, 1, 2, 3), transaction.Time);

            Assert.IsTrue(intention.ExecutionHolding.HasValue);
            Assert.IsTrue(transaction.ExecutionHolding.HasValue);
            Assert.AreEqual(SmartInsiderExecutionHolding.SatisfyStockVesting, intention.ExecutionHolding);
            Assert.AreEqual(SmartInsiderExecutionHolding.SatisfyStockVesting, transaction.ExecutionHolding);
        }

        [TestCase("2019-01-01  23:59:59")]
        [TestCase("01/01/2019  23:59:59")]
        public void ParsesOldAndNewTransactionDateTimeValues(string date)
        {
            var expected = new DateTime(2019, 1, 1, 23, 59, 59);
            var actual = SmartInsiderEvent.ParseDate(date);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ParseDateThrowsOnInvalidDateTimeValue()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                SmartInsiderEvent.ParseDate("05/21/2019 00:00:00");
            });
        }

        [Test]
        public void SerializeRoundTripSmartInsiderIntention()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(SmartInsiderIntention), underlyingSymbol, QuantConnect.Market.USA);

            var item = new SmartInsiderIntention
            {
                Symbol = symbol,
                LastUpdate = time,
                Time = time,
                TransactionID = "123",
                EventType = SmartInsiderEventType.Intention,
                Execution = SmartInsiderExecution.Market,
                ExecutionEntity = SmartInsiderExecutionEntity.Issuer,
                ExecutionHolding = SmartInsiderExecutionHolding.NotReported,
                Amount = null
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<SmartInsiderIntention>(serialized, settings);

            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual("123", deserialized.TransactionID);
            Assert.AreEqual(SmartInsiderEventType.Intention, deserialized.EventType);
            Assert.AreEqual(SmartInsiderExecution.Market, deserialized.Execution);
            Assert.AreEqual(SmartInsiderExecutionEntity.Issuer, deserialized.ExecutionEntity);
            Assert.AreEqual(SmartInsiderExecutionHolding.NotReported, deserialized.ExecutionHolding);
            Assert.AreEqual(null, deserialized.Amount);
            Assert.AreEqual(time, deserialized.LastUpdate);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
        }

        [Test]
        public void SerializeRoundTripSmartInsiderTransaction()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(SmartInsiderTransaction), underlyingSymbol, QuantConnect.Market.USA);

            var item = new SmartInsiderTransaction
            {
                Symbol = symbol,
                LastUpdate = time,
                Time = time,
                TransactionID = "123",
                EventType = SmartInsiderEventType.Transaction,
                Execution = SmartInsiderExecution.Market,
                ExecutionEntity = SmartInsiderExecutionEntity.Issuer,
                ExecutionHolding = SmartInsiderExecutionHolding.SatisfyEmployeeTax,
                Amount = 1234
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<SmartInsiderTransaction>(serialized, settings);

            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual("123", deserialized.TransactionID);
            Assert.AreEqual(SmartInsiderEventType.Transaction, deserialized.EventType);
            Assert.AreEqual(SmartInsiderExecution.Market, deserialized.Execution);
            Assert.AreEqual(SmartInsiderExecutionEntity.Issuer, deserialized.ExecutionEntity);
            Assert.AreEqual(SmartInsiderExecutionHolding.SatisfyEmployeeTax, deserialized.ExecutionHolding);
            Assert.AreEqual(1234, deserialized.Amount);
            Assert.AreEqual(time, deserialized.LastUpdate);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
        }

        [Test]
        public void ToLineDoesNotOutputRawNullValues()
        {
            var intentionLine = "20200101 01:02:03	BIXYZ		20190101	20190101	USXYZ		1	Some Random Industry																	US													";
            var transactionLine = "20200101 01:02:03	BIXYZ		20190101	20190101	USXYZ		1	Some Random Industry																																																			";

            var intention = new SmartInsiderIntention(intentionLine);
            var transaction = new SmartInsiderTransaction(transactionLine);

            Assert.IsNull(intention.EventType);
            Assert.IsNull(intention.Execution);
            Assert.IsNull(intention.ExecutionEntity);
            Assert.IsNull(intention.ExecutionHolding);
            Assert.IsNull(transaction.EventType);
            Assert.IsNull(transaction.Execution);
            Assert.IsNull(transaction.ExecutionEntity);
            Assert.IsNull(transaction.ExecutionHolding);

            var intentionLineSerialized = intention.ToLine().Split('\t');
            var transactionLineSerialized = transaction.ToLine().Split('\t');

            Assert.AreNotEqual(intentionLineSerialized[2], "null");
            Assert.AreNotEqual(intentionLineSerialized[26], "null");
            Assert.AreNotEqual(intentionLineSerialized[27], "null");
            Assert.AreNotEqual(intentionLineSerialized[28], "null");
            Assert.AreNotEqual(transactionLineSerialized[2], "null");
            Assert.AreNotEqual(transactionLineSerialized[27], "null");
            Assert.AreNotEqual(transactionLineSerialized[28], "null");
            Assert.AreNotEqual(transactionLineSerialized[29], "null");

            Assert.IsTrue(string.IsNullOrWhiteSpace(intentionLineSerialized[2]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(intentionLineSerialized[26]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(intentionLineSerialized[27]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(intentionLineSerialized[28]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(transactionLineSerialized[2]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(transactionLineSerialized[27]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(transactionLineSerialized[28]));
            Assert.IsTrue(string.IsNullOrWhiteSpace(transactionLineSerialized[29]));
        }

        [Test]
        public void ParseFromRawDataUnexpectedEventTypes()
        {
            var realRawIntentionLine = "\"BI12345\"\t\"Some new event\"\t2020-07-27\t2009-11-11\t\"US1234567890\"\t\"\"\t12345\t\"https://smartinsidercompanypage.com\"\t\"Consumer Staples\"\t" +
                                       "\"Personal Care, Drug and Grocery Stores\"\t\"Personal Care, Drug and Grocery Stores\"\t\"Personal Products\"\t12345678\t\"Some Company Corp\"\t\"Some-Comapny C\"\t" +
                                       "\"\"\t\"\"\t\"\"\t\"\"\t\"Com\"\t\"US\"\t\"SCC\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\t\t-999\t\"Some unexpected event.\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"\"\t\"" +
                                       "\"\t2020-07-27\t\"\"\t\"\"\t\"\"\t2020-07-27  13:57:37\t\"US\"\t\"https://smartinsiderdatapage.com\"\t\"UnexpectedEvent\"\t\"UnexpectedIssuer\"\t\"UnexpectedReported\"\t\"\"\t\"\"\t\t" +
                                       "\"\"\t\t\t\"\"\t\t\t\"\"";

            var tsv = realRawIntentionLine.Split('\t')
                .Take(60)
                .Select(x => x.Replace("\"", ""))
                .ToList();

            // Remove in descending order to maintain index order
            // while we delete lower indexed values
            tsv.RemoveAt(46); // ShowOriginal
            tsv.RemoveAt(36); // PreviousClosePrice
            tsv.RemoveAt(14); // ShortCompanyName
            tsv.RemoveAt(7);  // CompanyPageURL

            var filteredRawIntentionLine = string.Join("\t", tsv);

            var intention = new SmartInsiderIntention();
            Assert.DoesNotThrow(() => intention.FromRawData(filteredRawIntentionLine));

            Assert.IsTrue(intention.EventType.HasValue);
            Assert.AreEqual(SmartInsiderEventType.NotSpecified, intention.EventType);
            Assert.AreEqual(SmartInsiderExecution.Error, intention.Execution);
            Assert.AreEqual(SmartInsiderExecutionEntity.Error, intention.ExecutionEntity);
            Assert.AreEqual(SmartInsiderExecutionHolding.Error, intention.ExecutionHolding);
        }
    }
}
