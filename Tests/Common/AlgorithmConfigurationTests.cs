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
using QuantConnect.Brokerages;
using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class AlgorithmConfigurationTests
    {
        [TestCase("BTC", BrokerageName.Binance, AccountType.Cash)]
        [TestCase("USDT", BrokerageName.GDAX, AccountType.Cash)]
        [TestCase("EUR", BrokerageName.Bitfinex, AccountType.Margin)]
        [TestCase("AUD", BrokerageName.Atreyu, AccountType.Margin)]
        public void CreatesConfiguration(string currency, BrokerageName brokerageName, AccountType accountType)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetAccountCurrency(currency);
            algorithm.SetBrokerageModel(brokerageName, accountType);

            var algorithmConfiguration = AlgorithmConfiguration.Create(algorithm);

            Assert.AreEqual(currency, algorithmConfiguration.AccountCurrency);
            Assert.AreEqual(brokerageName, algorithmConfiguration.BrokerageName);
            Assert.AreEqual(accountType, algorithmConfiguration.AccountType);
        }
    }
}
