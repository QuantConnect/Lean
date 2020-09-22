using NUnit.Framework;
using Python.Runtime;
using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Research;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantbookEngineTests
    {

        [TestCaseSource(nameof(TimeTestCases))]
        public void AlgorithmTimeTest(dynamic input)
        {
            var qb = new QuantBook();
            var startDate = new DateTime(2014, 10, 7);
            var endDate = new DateTime(2014, 10, 11);

            qb.SetStartDate(startDate);
            qb.SetEndDate(endDate);

            Assert.AreEqual(qb.Time, startDate);

            qb.AddEquity("SPY", input[0]);

            qb.Step(input[1]);

            //Assert.AreEqual(qb.Time, input[2]);
            var test = qb.Time;
        }

        // Different requests and their expected values
        private static readonly object[] TimeTestCases =
        {
            //new object[] {Resolution.Tick, 1000, new DateTime(2014, 10, 7, 1, 0, 0)},
            new object[] {Resolution.Second, 500,new DateTime(2014, 10, 7, 9, 8, 20)},
            new object[] {Resolution.Minute, 80, new DateTime(2014, 10, 7, 10, 20, 0)},
            new object[] {Resolution.Hour, 5, new DateTime(2014, 10, 7, 1, 0, 0)},
            new object[] {Resolution.Daily, 2, new DateTime(2014, 10, 9)},
        };
    }
}
