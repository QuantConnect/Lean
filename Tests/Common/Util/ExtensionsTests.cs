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

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void IsSubclassOfGenericWorksWorksForNonGenericType()
        {
            Assert.IsTrue(typeof(Derived2).IsSubclassOfGeneric(typeof(Derived1)));
        }

        [Test]
        public void IsSubclassOfGenericWorksForGenericTypeWithParameter()
        {
            Assert.IsTrue(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<int>)));
            Assert.IsFalse(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<bool>)));
        }

        [Test]
        public void IsSubclassOfGenericWorksForGenericTypeDefinitions()
        {
            Assert.IsTrue(typeof(Derived1).IsSubclassOfGeneric(typeof(Super<>)));
            Assert.IsTrue(typeof(Derived2).IsSubclassOfGeneric(typeof(Super<>)));
        }

        private class Super<T>
        {
        }

        private class Derived1 : Super<int>
        {
        }

        private class Derived2 : Derived1
        {
        }
    }
}
