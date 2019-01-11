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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ListComparerTests
    {
        [Test]
        public void DateTimeEquals()
        {
            var comparer = new ListComparer<DateTime>();
            var list1 = new List<DateTime> { new DateTime(2019) };
            var list2 = new List<DateTime> { new DateTime(2019) };

            Assert.IsTrue(comparer.Equals(list1, list2));
            Assert.AreEqual(comparer.GetHashCode(list2), comparer.GetHashCode(list1));
        }

        [Test]
        public void DateTimeDifferent()
        {
            var comparer = new ListComparer<DateTime>();
            var list1 = new List<DateTime> { new DateTime(2019) };
            var list2 = new List<DateTime> { new DateTime(2017) };

            Assert.IsFalse(comparer.Equals(list1, list2));
            Assert.AreNotEqual(comparer.GetHashCode(list2), comparer.GetHashCode(list1));
        }

        [Test]
        public void EmptyLists()
        {
            var comparer = new ListComparer<DateTime>();
            var list1 = new List<DateTime>();
            var list2 = new List<DateTime>();

            Assert.IsTrue(comparer.Equals(list1, list2));
            Assert.AreEqual(comparer.GetHashCode(list2), comparer.GetHashCode(list1));
        }
    }
}
