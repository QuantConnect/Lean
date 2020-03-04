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
using QuantConnect.Lean.Engine.Alphas;

namespace QuantConnect.Tests.Engine.Alphas
{
    [TestFixture]
    public class DefaultAlphaHandlerTests
    {
        private DefaultAlphaHandlerTestable _defaultAlphaHandler;
        private Mock<IInsightManager> _insightManager;
        private const string AlgorithmId = "MyAlgorithm";
        private const string InsightFileName = "alpha-results.json";
        private const string InsightsDestinationFolderKey = "insights-destination-folder";
        
        [SetUp]
        public void SetUp()
        {
            _insightManager = new Mock<IInsightManager>();
            _defaultAlphaHandler = new DefaultAlphaHandlerTestable(_insightManager.Object, AlgorithmId);
        }

        [TestCase(true, "IGNORED")]
        [TestCase(false, "./NewDirectory")]
        [Test]
        public void TestStoreInsightsWithDifferentDirectories(bool useDefaultDirectory, string alternateDirectory)
        {
            var insights = new []
            {
                Insight.Price(Symbol.Create("ES", SecurityType.Future, Market.USA), DateTime.Now.AddMinutes(1), InsightDirection.Down),
                Insight.Price(Symbol.Create("GC", SecurityType.Future, Market.USA), DateTime.Now.AddMinutes(5), InsightDirection.Up)
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
                Config.Set(InsightsDestinationFolderKey, alternateDirectory);
            }
            
            _defaultAlphaHandler.ExecuteStoreInsights();
            
            var fullPath = Path.Combine(insightDirectory, InsightFileName);
            Assert.True(File.Exists(fullPath));

            var fileContents = File.ReadAllText(fullPath);
            var deserializedInsights = JsonConvert.DeserializeObject<List<Insight>>(fileContents);
            
            // There are no comparators implemented for Insights, so ToString() comparison is sufficient
            CollectionAssert.AreEquivalent(insights.Select(x => x.ToString()), deserializedInsights.Select(x => x.ToString()));
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
