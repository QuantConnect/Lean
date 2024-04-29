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

using System.Collections.Generic;

using NUnit.Framework;

using System;
using Newtonsoft.Json;
using QuantConnect.Packets;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class AlgorithmConfigurationTests
    {
        [TestCaseSource(nameof(AlgorithmConfigurationTestCases))]
        public void CreatesConfiguration(string currency, BrokerageName brokerageName, AccountType accountType,
            Dictionary<string, string> parameters)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetAccountCurrency(currency);
            algorithm.SetBrokerageModel(brokerageName, accountType);
            algorithm.SetParameters(parameters);


            var algorithmConfiguration = AlgorithmConfiguration.Create(algorithm, null);

            Assert.AreEqual(currency, algorithmConfiguration.AccountCurrency);
            Assert.AreEqual(brokerageName, algorithmConfiguration.Brokerage);
            Assert.AreEqual(accountType, algorithmConfiguration.AccountType);
            CollectionAssert.AreEquivalent(parameters, algorithmConfiguration.Parameters);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void JsonRoundtrip(bool backwardsCompatible)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetName("Backtest name");
            algorithm.AddTag("tag1");
            algorithm.AddTag("tag2");
            algorithm.SetAccountCurrency(Currencies.GBP);
            algorithm.SetBrokerageModel(BrokerageName.Coinbase, AccountType.Cash);
            algorithm.SetParameters(new Dictionary<string, string> { { "a", "A" }, { "b", "B" } });

            var backtestNode = new BacktestNodePacket
            {
                OutOfSampleDays = 30,
                OutOfSampleMaxEndDate = new DateTime(2023, 01, 01)
            };
            var algorithmConfiguration = AlgorithmConfiguration.Create(algorithm, backtestNode);

            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = false,
                        OverrideSpecifiedNames = true
                    }
                }
            };
            var serialized = JsonConvert.SerializeObject(algorithmConfiguration, settings);
            if (backwardsCompatible)
            {
                serialized = $"{{\"Name\":\"Backtest name\",\"Tags\":[\"tag1\",\"tag2\"],\"AccountCurrency\":\"GBP\",\"Brokerage\":32," +
                $"\"AccountType\":1,\"Parameters\":{{\"a\":\"A\",\"b\":\"B\"}},\"OutOfSampleMaxEndDate\":\"2023-01-01T00:00:00\"," +
                $"\"OutOfSampleDays\":30,\"StartDate\":\"1998-01-01 00:00:00\",\"EndDate\":\"{algorithm.EndDate.ToString(DateFormat.UI)}\",\"TradingDaysPerYear\":252}}";
            }
            else
            {
                Assert.AreEqual($"{{\"name\":\"Backtest name\",\"tags\":[\"tag1\",\"tag2\"],\"accountCurrency\":\"GBP\",\"brokerage\":32," +
                $"\"accountType\":1,\"parameters\":{{\"a\":\"A\",\"b\":\"B\"}},\"outOfSampleMaxEndDate\":\"2023-01-01T00:00:00\"," +
                $"\"outOfSampleDays\":30,\"startDate\":\"1998-01-01 00:00:00\",\"endDate\":\"{algorithm.EndDate.ToString(DateFormat.UI)}\",\"tradingDaysPerYear\":252}}", serialized);
            }

            var deserialize = JsonConvert.DeserializeObject<AlgorithmConfiguration>(serialized);

            Assert.AreEqual(algorithmConfiguration.Name, deserialize.Name);
            Assert.AreEqual(algorithmConfiguration.Parameters, deserialize.Parameters);
            Assert.AreEqual(algorithmConfiguration.AccountCurrency, deserialize.AccountCurrency);
            Assert.AreEqual(algorithmConfiguration.AccountType, deserialize.AccountType);
            Assert.AreEqual(algorithmConfiguration.Brokerage, deserialize.Brokerage);
            var expected = new DateTime(algorithm.EndDate.Year, algorithm.EndDate.Month, algorithm.EndDate.Day, algorithm.EndDate.Hour, algorithm.EndDate.Minute, algorithm.EndDate.Second);
            Assert.AreEqual(expected, deserialize.EndDate);
            Assert.AreEqual(algorithmConfiguration.OutOfSampleDays, deserialize.OutOfSampleDays);
            Assert.AreEqual(algorithmConfiguration.TradingDaysPerYear, deserialize.TradingDaysPerYear);
            Assert.AreEqual(algorithmConfiguration.OutOfSampleMaxEndDate, deserialize.OutOfSampleMaxEndDate);
            Assert.AreEqual(algorithmConfiguration.StartDate, deserialize.StartDate);
            Assert.AreEqual(algorithmConfiguration.Tags, deserialize.Tags);
        }

        private static TestCaseData[] AlgorithmConfigurationTestCases => new[]
        {
            new TestCaseData("BTC", BrokerageName.Binance, AccountType.Cash,
                new Dictionary<string, string> { { "param1", "param1 value" }, { "param2", "param2 value" } }),
            new TestCaseData("USDT", BrokerageName.Coinbase, AccountType.Cash,
                new Dictionary<string, string> { { "a", "A" }, { "b", "B" } }),
            new TestCaseData("EUR", BrokerageName.Bitfinex, AccountType.Margin,
                new Dictionary<string, string> { { "first", "1" }, { "second", "2" }, { "third", "3" } }),
            new TestCaseData("AUD", BrokerageName.Axos, AccountType.Margin,
                new Dictionary<string, string> { { "ema-slow", "20" }, { "ema-fast", "10" } })
        };
    }
}
