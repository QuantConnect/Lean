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

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AverageDirectionalIndexTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new AverageDirectionalIndex(14);
        }

        protected override string TestFileName => "spy_with_adx.txt";

        protected override string TestColumnName => "ADX 14";

        [Test]
        public override void ComparesAgainstExternalData()
        {
            const double epsilon = .0001;
            var adx = CreateIndicator();

            TestHelper.TestIndicator(adx, TestFileName, "+DI14",
                (ind, expected) => Assert.AreEqual(expected, (double)((AverageDirectionalIndex)ind).PositiveDirectionalIndex.Current.Value, epsilon)
            );
            adx.Reset();

            TestHelper.TestIndicator(adx, TestFileName, "-DI14",
                (ind, expected) => Assert.AreEqual(expected, (double)((AverageDirectionalIndex)ind).NegativeDirectionalIndex.Current.Value, epsilon)
            );
        }
    }
}