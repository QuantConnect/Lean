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

using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Binance;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;
using System;
using QuantConnect.Util;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture, Explicit("This test requires a configured and testable Binance practice account")]
    public class BinanceBrokerageAdditionalTests
    {
        [Test]
        public void ParameterlessConstructorComposerUsage()
        {
            var brokerage = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>("BinanceBrokerage");
            Assert.IsNotNull(brokerage);
            Assert.True(brokerage.IsConnected);
        }

        [Test]
        public void ConnectedIfNoAlgorithm()
        {
            using var brokerage = CreateBrokerage(null);
            Assert.True(brokerage.IsConnected);
        }

        [Test]
        public void ConnectedIfAlgorithmIsNotNullAndClientNotCreated()
        {
            using var brokerage = CreateBrokerage(Mock.Of<IAlgorithm>());
            Assert.True(brokerage.IsConnected);
        }

        [Test]
        public void ConnectToUserDataStreamIfAlgorithmNotNullAndApiIsCreated()
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork));

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new BinanceBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions));

            using var brokerage =  CreateBrokerage(algorithm.Object);

            Assert.True(brokerage.IsConnected);
            
            var _ = brokerage.GetCashBalance();

            Assert.True(brokerage.IsConnected);

            brokerage.Disconnect();

            Assert.False(brokerage.IsConnected);
        }

        private static Brokerage CreateBrokerage(IAlgorithm algorithm)
        {
            var apiKey = Config.Get("binance-api-key");
            var apiSecret = Config.Get("binance-api-secret");
            var apiUrl = Config.Get("binance-api-url", "https://api.binance.com");
            var websocketUrl = Config.Get("binance-websocket-url", "wss://stream.binance.com:9443/ws");

            return new BinanceBrokerage(
                apiKey,
                apiSecret,
                apiUrl,
                websocketUrl,
                algorithm,
                new AggregationManager(),
                null
            );
        }
    }
}
