/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class IchimokuKinkoHyoTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 0.1m;
            VolumeRenkoBarSize = 0.5m;
            return new IchimokuKinkoHyo();
        }

        protected override string TestFileName => "spy_with_ichimoku.csv";

        protected override string TestColumnName => "Tenkan";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double) ((IchimokuKinkoHyo) indicator).Tenkan.Current.Value, 1e-3);

        [Test]
        public void ComparesWithExternalDataTenkanMaximum()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "TenkanMaximum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).TenkanMaximum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataTenkanMinimum()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "TenkanMinimum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).TenkanMinimum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataKijunMaximum()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "KijunMaximum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).KijunMaximum.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataKijunMinimum()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "KijunMinimum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).KijunMinimum.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataKijun()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Kijun",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).Kijun.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataDelayedTenkanSenkouA()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "DelayedTenkanSenkouA",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedTenkanSenkouA.Current.Value)
                );
        }


        [Test]
        public void ComparesWithExternalDataDelayedKijunSenkouA()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "DelayedKijunSenkouA",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedKijunSenkouA.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataSenkouA()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "Senkou A",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).SenkouA.Current.Value)
                );
        }
        [Test]
        public void ComparesWithExternalDataSenkouBMaximum()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "SenkouBMaximum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).SenkouBMaximum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataSenkouBMinimum()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "SenkouBMinimum",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).SenkouBMinimum.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataDelayedMaximumSenkouB()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "DelayedMaximumSenkouB",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedMaximumSenkouB.Current.Value)
                );
        }

        [Test]
        public void ComparesWithExternalDataDelayedMinimumSenkouB()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "DelayedMinimumSenkouB",
                (ind, expected) => Assert.AreEqual(expected, (double)((IchimokuKinkoHyo)ind).DelayedMinimumSenkouB.Current.Value)
                );
        }
    }
}