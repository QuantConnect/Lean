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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class LinqExtensionsTests
    {
        [Test]
        public void ToArray_PerformsProjection()
        {
            var enumerable = Enumerable.Range(0, 10);
            var array = enumerable.ToArray(i => Invariant($"{i}"));
            CollectionAssert.AreEqual(enumerable.Select(i => Invariant($"{i}")).ToArray(), array);
        }

        [Test]
        public void ToImmutableArray_PerformsProjection()
        {
            var enumerable = Enumerable.Range(0, 10);
            var array = enumerable.ToImmutableArray(i => Invariant($"{i}"));
            CollectionAssert.AreEqual(enumerable.Select(i => Invariant($"{i}")).ToArray(), array);
        }
    }
}
