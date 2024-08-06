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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ChandeKrollStopTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new ChandeKrollStop(5, 2.0m, 3);
        }

        protected override string TestFileName => "spy_with_ChandeKrollStop.csv";

        protected override string TestColumnName => "short_stop";

        protected override Action<IndicatorBase<IBaseDataBar>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double)((ChandeKrollStop)indicator).Short.Current.Value, 1e-6);


        [Test]
        public void CompareAgainstExternalDataForLongStop()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                TestFileName,
                "long_stop",
                (ind, expected) => Assert.AreEqual(expected, (double) ((ChandeKrollStop) ind).Long.Current.Value, 1e-6)
            );
        }
    }
}
