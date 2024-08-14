using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
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
