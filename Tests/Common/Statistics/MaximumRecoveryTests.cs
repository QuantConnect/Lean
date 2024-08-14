using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QLNet;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    internal class MaximumRecoveryTests
    {
        [Test]
        public void MaximumRecoveryTests_RepeatedDrawdownsSameLevelButOneLonger_ReturnLongerDrawdown()
        {

        }

        [Test]
        public void MaximumRecoveryTests_RepeatedDrawdownsSameLevelSameLength_ReturnOneOfThem()
        {

        }

        [Test]
        public void MaximumRecoveryTests_NoRecovery_ReturnZero()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 90 },
                { startDate.AddDays(2), 98 },
                { startDate.AddDays(3), 99 }
            };
            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaximumRecoveryTime(equityOverTime);
            Assert.AreEqual(TimeSpan.Zero, maximumRecoveryTime);
        }

        [Test]
        public void MaximumRecoveryTests_BasicRecoveryWithin_ReturnRecoveryTime()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 99 },
                { startDate.AddDays(2), 98 },
                { startDate.AddDays(3), 99 }
            };
            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaximumRecoveryTime(equityOverTime);
            Assert.AreEqual(TimeSpan.FromDays(1), maximumRecoveryTime);

        }

        [Test]
        public void MaximumRecoveryTests_BasicRecovery_ReturnRecoveryTime()
        {

        }

        [Test]
        public void MaximumRecoveryTests_FlatPrice_ReturnZero()
        {

        }

        [Test]
        public void MaximumRecoveryTests_SingleDataPoint_ReturnZero()
        {

        }

        [Test]
        public void MaximumRecoveryTests_EmptyDataSet_ReturnZero()
        {

        }

        [Test]
        public void MaximumRecoveryTests_LongRecoveryIntermediatePeaks_ReturnCorrectRecoveryTime()
        {

        }

        [Test]
        public void MaximumRecoveryTests_PriceCrashesThroughPreviousHigh_ReturnCorrectRecoveryTime()
        {

        }

    }
}
