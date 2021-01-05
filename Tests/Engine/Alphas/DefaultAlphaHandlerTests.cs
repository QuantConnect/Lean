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
using System.Linq;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Alphas;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.Alphas
{
    [TestFixture]
    public class DefaultAlphaHandlerTests
    {
        private DefaultAlphaHandlerTestable _defaultAlphaHandler;
        private Mock<IInsightManager> _insightManager;
        private const string AlgorithmId = "MyAlgorithm";
        private const string InsightFileName = "alpha-results.json";
        private const string ResultsDestinationFolderKey = "results-destination-folder";
        
        [SetUp]
        public void SetUp()
        {
            _insightManager = new Mock<IInsightManager>();
            _defaultAlphaHandler = new DefaultAlphaHandlerTestable(_insightManager.Object, AlgorithmId);
        }

        [TearDown]
        public void TearDown()
        {
            _defaultAlphaHandler.Exit();
        }

        [TestCase(true, "IGNORED")]
        [TestCase(false, "./NewDirectory")]
        [Test]
        public void TestStoreInsightsWithDifferentDirectories(bool useDefaultDirectory, string alternateDirectory)
        {
            // Arrange
            var insights = new []
            {
                Insight.Price(Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME), DateTime.Now.AddMinutes(1), InsightDirection.Down),
                Insight.Price(Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX), DateTime.Now.AddMinutes(5), InsightDirection.Up)
            };
            _insightManager.Setup(x => x.AllInsights).Returns(insights);
            
            var topDirectory = useDefaultDirectory
                ? Directory.GetCurrentDirectory()
                : Path.Combine(Directory.GetCurrentDirectory(), alternateDirectory);
            
            // Make sure the directory doesn't exist
            var insightDirectory = Path.Combine(topDirectory, AlgorithmId);
            if (Directory.Exists(insightDirectory))
            {
                Directory.Delete(insightDirectory, true);
            }

            if (!useDefaultDirectory)
            {
                Config.Set(ResultsDestinationFolderKey, alternateDirectory);
            }

            var packet = new AlgorithmNodePacket(PacketType.LiveNode);
            var algorithm = new Mock<IAlgorithm>();
            var messagingHandler = new Mock<IMessagingHandler>();
            var api = new Mock<IApi>();

            _defaultAlphaHandler.Initialize(packet, algorithm.Object, messagingHandler.Object, api.Object, new BacktestingTransactionHandler());
            
            // Act
            _defaultAlphaHandler.ExecuteStoreInsights();
            
            // Assert
            var fullPath = Path.Combine(insightDirectory, InsightFileName);
            Assert.True(File.Exists(fullPath));

            var fileContents = File.ReadAllText(fullPath);
            var deserializedInsights = JsonConvert.DeserializeObject<List<Insight>>(fileContents);
            
            // There are no comparators implemented for Insights, so ToString() comparison is sufficient
            CollectionAssert.AreEquivalent(insights.Select(x => x.ToString()), deserializedInsights.Select(x => x.ToString()));
            
            Directory.Delete(insightDirectory, true);
        }
        
        private class DefaultAlphaHandlerTestable : DefaultAlphaHandler
        {
            private readonly IInsightManager _insightManager;
            private readonly string _algorithmId;

            public DefaultAlphaHandlerTestable(IInsightManager insightManager, string algorithmId)
            {
                _insightManager = insightManager;
                _algorithmId = algorithmId;
            }

            protected override IInsightManager InsightManager => _insightManager;
            protected override string AlgorithmId => _algorithmId;

            public void ExecuteStoreInsights()
            {
                base.StoreInsights();
            }
        }
    }
}
