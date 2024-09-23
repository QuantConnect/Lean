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
    public class VortexTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            return new Vortex(14);
        }

        protected override string TestFileName => "spy_with_vtx.csv";

        protected override string TestColumnName => "plus_vtx";

        [Test]
        public override void ComparesAgainstExternalData()
        {
            const double epsilon = 0.0001;

            var vortex = CreateIndicator();

            TestHelper.TestIndicator(vortex, TestFileName, "plus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((Vortex)ind).PlusVortex.Current.Value, epsilon)
            );
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            const double epsilon = 0.0001;

            var vortex = CreateIndicator();


            TestHelper.TestIndicator(vortex, TestFileName, "plus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((Vortex)ind).PlusVortex.Current.Value, epsilon)
            );

            vortex.Reset();

            TestHelper.TestIndicator(vortex, TestFileName, "minus_vtx",
                (ind, expected) => Assert.AreEqual(expected, (double)((Vortex)ind).MinusVortex.Current.Value, epsilon)
            );
        }
    }
}
