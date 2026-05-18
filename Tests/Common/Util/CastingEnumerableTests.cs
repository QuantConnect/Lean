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
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class CastingEnumerableTests
    {
        private class BaseClass { }
        private class DerivedClass : BaseClass { }

        [Test]
        public void CastsOnEnumeration()
        {
            var list = new List<BaseClass> { new DerivedClass(), new DerivedClass(), new DerivedClass() };
            var casting = new CastingEnumerable<BaseClass, DerivedClass>(list);

            Assert.DoesNotThrow(() =>
            {
                foreach (var item in casting) { }
            });

            CollectionAssert.AreEqual(list, casting);
            Assert.AreEqual(list.Count, casting.Count);
        }

        [Test]
        public void CastsOnIndexing()
        {
            var list = new List<BaseClass> { new DerivedClass(), new DerivedClass(), new DerivedClass() };
            var casting = new CastingEnumerable<BaseClass, DerivedClass>(list);

            DerivedClass casted0 = null;
            DerivedClass casted1 = null;
            DerivedClass casted2 = null;

            Assert.DoesNotThrow(() =>
            {
                casted0 = casting[0];
                casted1 = casting[1];
                casted2 = casting[2];
            });

            Assert.AreEqual(list[0], casted0);
            Assert.AreEqual(list[1], casted1);
            Assert.AreEqual(list[2], casted2);
        }
    }
}
