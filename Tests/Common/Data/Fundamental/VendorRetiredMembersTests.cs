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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    /// <summary>
    /// Data points Morningstar retired keep their members so algorithms compile, but the members warn
    /// at build time and throw with the alternative at run time; the enumeration paths stay safe.
    /// </summary>
    [TestFixture]
    public class VendorRetiredMembersTests
    {
#pragma warning disable CS0618
        [Test]
        public void RetiredPeriodThrowsAndNamesTheAlternative()
        {
            var field = new DividendCoverageRatio();
            var error = Assert.Throws<NotSupportedException>(() => _ = field.ThreeMonths);
            Assert.IsTrue(error.Message.Contains("was retired by Morningstar"), error.Message);
            Assert.IsTrue(error.Message.Contains("use DividendCoverageRatio.TwelveMonths"), error.Message);
        }

        [Test]
        public void WholeRetiredPropertyThrowsButStaysEnumerable()
        {
            var field = new NormalizedDilutedEPSGrowth();
            var error = Assert.Throws<NotSupportedException>(() => _ = field.Value);
            Assert.IsTrue(error.Message.Contains("no replacement is available"), error.Message);

            // the paths a serializer or a period walk hits must not throw
            Assert.IsFalse(field.HasValue);
            Assert.IsEmpty(field.GetPeriodValues());
        }
#pragma warning restore CS0618

        [Test]
        public void RetiredMembersCarryTheObsoleteWarningAndStayOutOfSerialization()
        {
            var member = typeof(DividendCoverageRatio).GetProperty("ThreeMonths");
            var obsolete = member.GetCustomAttributes(typeof(ObsoleteAttribute), false).Cast<ObsoleteAttribute>().Single();
            Assert.IsTrue(obsolete.Message.Contains("use DividendCoverageRatio.TwelveMonths"), obsolete.Message);
            Assert.IsTrue(member.GetCustomAttributes(typeof(Newtonsoft.Json.JsonIgnoreAttribute), false).Any());
        }
    }
}
