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

        [TestCase("SAREXT_PARAM1", 0.0, 0.0, 0.03, 0.02, 0.4, 0.02, 0.01, 0.3)]
        [TestCase("SAREXT_PARAM2", 100, 0.0, 0.03, 0.02, 0.4, 0.02, 0.01, 0.3)]
        [TestCase("SAREXT_PARAM3", -95, 0.0, 0.03, 0.02, 0.4, 0.02, 0.01, 0.3)]
        [TestCase("SAREXT_PARAM4", 100, 0.02, 0.03, 0.02, 0.4, 0.02, 0.01, 0.3)]
        public void ComparesWithExternalDataWithParams(string colName, decimal ss, decimal offset, 
            decimal afss, decimal afis, decimal afms, decimal afsl, decimal afil, decimal afml)  
        {
            var sarext = new ParabolicStopAndReverseExtended(sarStart : ss, offsetOnReverse : offset, 
                afStartShort : afss, afIncrementShort : afis, afMaxShort : afms, 
                afStartLong : afsl, afIncrementLong : afil, afMaxLong : afml);
            TestHelper.TestIndicator(
                sarext,
                TestFileName,
                colName,
                (ind, expected) => Assert.AreEqual(expected, (double)sarext.Current.Value, delta: 1e-4)
            );
        }
    }
}