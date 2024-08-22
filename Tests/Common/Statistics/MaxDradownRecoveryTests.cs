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
    internal class MaxDradownRecoveryTests
    {
        [Test]
        public void MaxDradownRecoveryTests_RepeatedDrawdownsSameLevelButOneLonger_ReturnLongerDrawdown()
        {

        }

        [Test]
        public void MaxDradownRecoveryTests_RepeatedDrawdownsSameLevelSameLength_ReturnOneOfThem()
        {

        }

        [Test]
        public void MaxDrawdownRecoveryTests_NoRecovery_ReturnZero()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 90 },
                { startDate.AddDays(2), 98 },
                { startDate.AddDays(3), 99 }
            };
            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        /// <summary>
        /// Tests fake recovery from 99 to 98 to 99. Max drawdown is 100 to 98, so this should have a recovery.
        /// </summary>
        [Test]
        public void MaxDrawdownRecoveryTests_BasicRecoveryWithin_ReturnRecoveryTime()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 99 },
                { startDate.AddDays(2), 98 },
                { startDate.AddDays(3), 99 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_BasicRecovery_ReturnRecoveryTime()
        {

        }

        [Test]
        public void MaxDradownRecoveryTests_FlatPrice_ReturnZero()
        {

        }

        [Test]
        public void MaxDradownRecoveryTests_SingleDataPoint_ReturnZero()
        {

        }

        [Test]
        public void MaxDradownRecoveryTests_EmptyDataSet_ReturnZero()
        {

        }

        [Test]
        public void MaxDradownRecoveryTests_LongRecoveryIntermediatePeaks_ReturnCorrectRecoveryTime()
        {

        }

        [Test]
        public void MaxDradownRecoveryTests_PriceCrashesThroughPreviousHigh_ReturnCorrectRecoveryTime()
        {

        }

    }
}
