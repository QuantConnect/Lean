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

using System;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class KnowSureThingTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new KnowSureThing(5, 5, 10, 5, 15, 5, 25, 10, 9, MovingAverageType.Simple);
        }

        protected override string TestFileName => "spy_with_kst.csv";

        protected override string TestColumnName => "kst";

        [Test]
        public void ComparesWithExternalDataSignal()
        {
            TestHelper.TestIndicator(
                CreateIndicator() as KnowSureThing,
                TestFileName,
                "signal",
                ind => (double)ind.SignalLine.Current.Value
            );
        }
    }
}
