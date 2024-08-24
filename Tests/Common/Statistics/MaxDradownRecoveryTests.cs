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
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 90 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 90 },
                { startDate.AddDays(4), 99 },
                { startDate.AddDays(5), 100 },
            };
            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(2, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_RepeatedDrawdownsSameLevelButOneLongerLongerFirst_ReturnLongerDrawdown()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 90 },
                { startDate.AddDays(4), 99 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 90 },
                { startDate.AddDays(5), 100 },
            };
            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(2, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_RepeatedDrawdownsSameLevelSameLength_ReturnOneOfThem()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 90 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 90 },
                { startDate.AddDays(4), 100 },
            };
            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(1, maximumRecoveryTime);
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
        public void MaxDrawdownRecoveryTests_FakeRecovery_ReturnNoRecovery()
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
        public void MaxDradownRecoveryTests_LowThenHighThenLowWithoutRecovery_ReturnNoRecovery()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 50 },
                { startDate.AddDays(1), 100 },
                { startDate.AddDays(2), 98 },
                { startDate.AddDays(3), 99 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_LowThenHighThenLowWithRecovery_ReturnRecoveryOfTwo()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 50 },
                { startDate.AddDays(1), 100 },
                { startDate.AddDays(2), 98 },
                { startDate.AddDays(3), 99 },
                { startDate.AddDays(4), 100 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(2, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_BasicRecovery_ReturnRecoveryTime()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 98 },
                { startDate.AddDays(2), 99 },
                { startDate.AddDays(3), 100 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(2, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_FlatPrice_ReturnZero()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 100 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 100 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_SingleDataPoint_ReturnZero()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_EmptyDataSet_ReturnZero()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_LongRecoveryIntermediatePeaks_ReturnCorrectRecoveryTime()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 97 },
                { startDate.AddDays(2), 99 },
                { startDate.AddDays(3), 97 },
                { startDate.AddDays(4), 100 },
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(3, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_PriceCrashesThroughPreviousHigh_ReturnCorrectRecoveryTime()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 98 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 101 },
                { startDate.AddDays(4), 100 }, // Crashes through previous high here, but crash doesnt exceed max drawdown.
                { startDate.AddDays(5), 99 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(1, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_TwoMaxDrawdownsOneDoesntRecover_ReturnNoRecovery()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 98 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 101 },
                { startDate.AddDays(4), 100 },
                { startDate.AddDays(5), 98 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }

        [Test]
        public void MaxDradownRecoveryTests_NewDrawdownHigher_ReturnNoRecovery()
        {
            var startDate = DateTime.MinValue;
            var equityOverTime = new SortedDictionary<DateTime, decimal>
            {
                { startDate, 100 },
                { startDate.AddDays(1), 98 },
                { startDate.AddDays(2), 100 },
                { startDate.AddDays(3), 101 },
                { startDate.AddDays(4), 100 },
                { startDate.AddDays(5), 98 }
            };

            var maximumRecoveryTime = QuantConnect.Statistics.Statistics.MaxDrawdownRecoveryTime(equityOverTime);
            Assert.AreEqual(0, maximumRecoveryTime);
        }


    }
}
