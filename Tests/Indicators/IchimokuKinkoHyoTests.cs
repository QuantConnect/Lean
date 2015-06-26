using System;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    class IchimokuKinkoHyoTests
    {
        [Test]
        public void ComparesWithExternalDataTenkanMaximum()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "TenkanMaximum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).TenkanMaximum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataTenkanMinimum()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "TenkanMinimum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).TenkanMinimum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataTenkan()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "Tenkan",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).Tenkan.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataKijunMaximum()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "KijunMaximum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).KijunMaximum.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataKijunMinimum()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "KijunMinimum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).KijunMinimum.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataKijun()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            ichimoku.Current.Time.ToString();
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "Kijun",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).Kijun.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataDelayedTenkanSenkouA()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "DelayedTenkanSenkouA",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedTenkanSenkouA.Current.Value)
                );
        }


        [Test]
        public void ComparesWithExternalDataDelayedKijunSenkouA()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "DelayedKijunSenkouA",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedKijunSenkouA.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataSenkouA()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "Senkou A",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).SenkouA.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataSenkouBMaximum()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "SenkouBMaximum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).SenkouBMaximum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataSenkouBMinimum()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "SenkouBMinimum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).SenkouBMinimum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataDelayedMaximumSenkouB()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "DelayedMaximumSenkouB",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedMaximumSenkouB.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataDelayedMinimumSenkouB()
        {
            var ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            TestHelper.TestIndicator(
                ichimoku,
                "spy_with_ichimoku.csv",
                "DelayedMinimumSenkouB",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedMinimumSenkouB.Current.Value)
                );
        }
    }
}
