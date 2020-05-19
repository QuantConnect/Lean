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
            Assert.AreEqual(intention.ExecutionHolding, SmartInsiderExecutionHolding.SatisfyStockVesting);
            Assert.AreEqual(transaction.ExecutionHolding, SmartInsiderExecutionHolding.SatisfyStockVesting);
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
    }
}
