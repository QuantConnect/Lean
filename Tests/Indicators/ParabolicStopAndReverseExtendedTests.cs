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
    public class ParabolicStopAndReverseExtendedTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new ParabolicStopAndReverseExtended();
        }

        protected override string TestFileName => "spy_sarext.txt";

        protected override string TestColumnName => "SAREXT"; 

        [Test]
        public void ComparesWithExternalDataWithParams1() 
        {
            var sarext = new ParabolicStopAndReverseExtended(sarStart : 0.0m, offsetOnReverse : 0.0m, 
                afStartShort : 0.03m, afIncrementShort : 0.02m, afMaxShort : 0.4m, 
                afStartLong : 0.02m, afIncrementLong : 0.01m, afMaxLong : 0.3m);
            TestHelper.TestIndicator(
                sarext,
                TestFileName,
                "SAREXT_PARAM1",
                (ind, expected) => Assert.AreEqual(expected, (double)sarext.Current.Value, delta: 1e-4)
            );
        }
        
        [Test]
        public void ComparesWithExternalDataWithParams2() 
        {
            var sarext = new ParabolicStopAndReverseExtended(sarStart : 100m, offsetOnReverse : 0.0m, 
                afStartShort : 0.03m, afIncrementShort : 0.02m, afMaxShort : 0.4m, 
                afStartLong : 0.02m, afIncrementLong : 0.01m, afMaxLong : 0.3m);
            TestHelper.TestIndicator(
                sarext,
                TestFileName,
                "SAREXT_PARAM2",
                (ind, expected) => Assert.AreEqual(expected, (double)sarext.Current.Value, delta: 1e-4)
            );
        }

        [Test]
        public void ComparesWithExternalDataWithParams3() 
        {
            var sarext = new ParabolicStopAndReverseExtended(sarStart : -95m, offsetOnReverse : 0.0m, 
                afStartShort : 0.03m, afIncrementShort : 0.02m, afMaxShort : 0.4m, 
                afStartLong : 0.02m, afIncrementLong : 0.01m, afMaxLong : 0.3m);
            TestHelper.TestIndicator(
                sarext,
                TestFileName,
                "SAREXT_PARAM3",
                (ind, expected) => Assert.AreEqual(expected, (double)sarext.Current.Value, delta: 1e-4)
            );
        }

        [Test]
        public void ComparesWithExternalDataWithParams4() 
        {
            var sarext = new ParabolicStopAndReverseExtended(sarStart : 100m, offsetOnReverse : 0.02m, 
                afStartShort : 0.03m, afIncrementShort : 0.02m, afMaxShort : 0.4m, 
                afStartLong : 0.02m, afIncrementLong : 0.01m, afMaxLong : 0.3m);
            TestHelper.TestIndicator(
                sarext,
                TestFileName,
                "SAREXT_PARAM4",
                (ind, expected) => Assert.AreEqual(expected, (double)sarext.Current.Value, delta: 1e-4)
            );
        }
    }
}