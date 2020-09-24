using NUnit.Framework;
using Python.Runtime;
using System;
using System.Linq;
using QuantConnect.Research;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;


namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantbookEngineTests
    {
        private QuantBook qb;
        private readonly DateTime startDate = new DateTime(2013, 10, 7);
        private readonly DateTime endDate = new DateTime(2013, 10, 11);

        [SetUp]
        public void Setup()
        {
            qb = new QuantBook();
            qb.SetStartDate(startDate);
            qb.SetEndDate(endDate);
        }

        [TestCaseSource(nameof(TimeTestCases))]
        public void AlgorithmTimeTest(dynamic input)
        {
            Assert.AreEqual(qb.Time, startDate);

            qb.AddEquity("SPY", input[0]);
            qb.Step(input[1]);

            Assert.AreEqual(input[2], qb.Time);
            qb.Shutdown();
        }

        [Test]
        public void ConsolidatorTest()
        {
            var spy = qb.AddEquity("SPY", Resolution.Minute).Symbol;
            var consolidator = new QuoteBarConsolidator(10);
            qb.SubscriptionManager.AddConsolidator(spy, consolidator);

            Assert.IsTrue(consolidator.Consolidated == null);
            qb.Step(10);
            Assert.IsTrue(consolidator.Consolidated != null);
            Assert.AreEqual(153.0223, (double)consolidator.Consolidated.Value, 0.005);
            qb.Shutdown();
        }

        [Test]
        public void IndicatorTest()
        {
            var spy = qb.AddEquity("SPY", Resolution.Minute).Symbol;
            var indicator = qb.EMA(spy, 10);

            Assert.AreEqual(0, indicator.Current.Value);
            qb.Step(10);
            Assert.AreEqual(153.0310, (double)indicator.Current.Value, 0.005);
            qb.Shutdown();
        }

        [Test]
        public void UniverseTest()
        {
            //Have to adjust the dates for this test
            qb.SetStartDate(new DateTime(2014, 3, 24));
            qb.SetEndDate(new DateTime(2014, 4, 1));

            Assert.IsTrue(qb.UniverseManager.Count == 0);

            // subscriptions added via universe selection will have this resolution
            qb.UniverseSettings.Resolution = Resolution.Daily;

            //Go select spy
            qb.AddUniverse(coarse =>
            {
                return from c in coarse
                    let sym = c.Symbol.Value
                    where sym == "SPY"
                    select c.Symbol;
            });

            // Step to the 28th
            qb.Step(new DateTime(2014, 3, 28));
            Assert.IsTrue(qb.UniverseManager.Count == 1);
            Assert.IsNotNull(qb.SubscriptionManager.Subscriptions.Select(x => x.Symbol).Where(x => x.Value == "SPY"));
            qb.Shutdown();
        }

        // Time test cases: resolution, steps, and expected end time
        // QB Starts at 9:30.00
        private static readonly object[] TimeTestCases =
        {
            new object[] {Resolution.Tick, 1000, new DateTime(2013, 10, 7, 9, 30, 13).AddMilliseconds(3)},
            new object[] {Resolution.Second, 600,new DateTime(2013, 10, 7, 9, 40, 00)},
            new object[] {Resolution.Minute, 80, new DateTime(2013, 10, 7, 10, 50, 0)},
            new object[] {Resolution.Hour, 5, new DateTime(2013, 10, 7, 14, 0, 0)},
        };
    }
}
