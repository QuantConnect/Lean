using System;
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class GeneratedInsightsCollectionTests
    {
        [Test]
        public void CheckCloneRespectsDerivedTypes()
        {
            var insights = new List<DerivedInsight>
            {
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
            };

            var generatedInsightsCollection = new GeneratedInsightsCollection(DateTime.UtcNow, insights, clone: true);

            Assert.True(generatedInsightsCollection.Insights.TrueForAll(x => x.GetType() == typeof(DerivedInsight)));
        }

        public class DerivedInsight : Insight
        {
            public DerivedInsight(Symbol symbol, TimeSpan period, InsightType type, InsightDirection direction)
                : base(symbol, period, type, direction)
            {
            }

            public override Insight Clone()
            {
                return new DerivedInsight(Symbol, Period, Type, Direction);
            }
        }
    }
}