using NUnit.Framework;
using Python.Runtime;
using System;
using System.Linq;
using QuantConnect.Research;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;


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
            qb.TearDown();
        }

        [Test]
        public void EndTimePastEndDateTest()
        {
            qb.AddEquity("SPY", Resolution.Hour);
            qb.Step(new DateTime(2013, 12, 1)); // Beyond our end date of 10/11/2013
            Assert.IsTrue(qb.Time <= qb.EndDate); // Time should stop before end date (end of stream)
            qb.TearDown();
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
            qb.TearDown();
        }

        [Test]
        public void IndicatorTest()
        {
            var spy = qb.AddEquity("SPY", Resolution.Minute).Symbol;
            var indicator = qb.EMA(spy, 10);

            Assert.AreEqual(0, indicator.Current.Value);
            qb.Step(10);
            Assert.AreEqual(153.0310, (double)indicator.Current.Value, 0.005);
            qb.TearDown();
        }

        [Test]
        public void IndicatorExtensionsTest()
        {
            var spy = qb.AddEquity("SPY", Resolution.Minute).Symbol;
            var ema = qb.EMA(spy, 10); //Create EMA indicator
            var roc = new RateOfChange(10).Of(ema); //Attach ROC to EMA

            Assert.AreEqual(0, ema.Current.Value);
            Assert.AreEqual(0, roc.Current.Value);
            qb.Step(10);
            Assert.AreEqual(1, roc.Samples); //We should have only one sample since EMA just produced its first value
            qb.Step(9);
            Assert.AreEqual(0.0011738588880640296659611208, roc.Current.Value);
            qb.TearDown();
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
            Assert.IsTrue(qb.CurrentSlice.HasData && qb.CurrentSlice.ContainsKey("SPY"));
            qb.TearDown();
        }

        [Test]
        public void AddSecuritiesAfterStartedTest()
        {
            //TODO: Do we want this to be possible or not?
        }

        [Test]
        public void ScheduledEventsTest()
        {
            //TODO
        }

        // Time test cases: resolution, steps, and expected end time
        private static readonly object[] TimeTestCases =
        {
            new object[] {Resolution.Tick, 1000, new DateTime(2013, 10, 7, 9, 30, 13).AddMilliseconds(3)},
            new object[] {Resolution.Second, 600,new DateTime(2013, 10, 7, 9, 40, 00)},
            new object[] {Resolution.Minute, 80, new DateTime(2013, 10, 7, 10, 50, 0)},
            new object[] {Resolution.Hour, 5, new DateTime(2013, 10, 7, 14, 0, 0)},
            new object[] {Resolution.Daily, 2, new DateTime(2013, 10, 9)},
        };
    }
}
