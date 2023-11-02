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
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class BaseFundamentalDataProviderTests
    {
        [Test]
        public void NoValueNull()
        {
            Assert.IsTrue(BaseFundamentalDataProvider.IsNone(null));
            Assert.IsTrue(BaseFundamentalDataProvider.IsNone(null, null));
        }

        [Test]
        public void NoValueDouble()
        {
            var noValue = BaseFundamentalDataProvider.GetDefault<double>();

            Assert.AreEqual(double.NaN, noValue);
            Assert.IsTrue(BaseFundamentalDataProvider.IsNone(noValue));
        }

        [Test]
        public void NoValueDecimal()
        {
            var noValue = BaseFundamentalDataProvider.GetDefault<decimal>();

            Assert.AreEqual(0, noValue);
            Assert.IsTrue(BaseFundamentalDataProvider.IsNone(noValue));
        }

        [Test]
        public void DatetimeNoTz()
        {
            var noValue = BaseFundamentalDataProvider.GetDefault<DateTime>();

            Assert.AreEqual(DateTime.MinValue, noValue);
            Assert.AreEqual(DateTimeKind.Unspecified, noValue.Kind);
            Assert.IsTrue(BaseFundamentalDataProvider.IsNone(noValue));
        }
    }
}
