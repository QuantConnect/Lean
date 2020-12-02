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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ComparisonOperatorTests
    {
        private static TestCaseData[] Equal => new[]
        {
            new TestCaseData(1, 1),
            new TestCaseData(1.0, 1.0),
            new TestCaseData(1.0m, 1.0m)
        };

        private static TestCaseData[] Greater => new[]
        {
            new TestCaseData(2, 1),
            new TestCaseData(2.0, 1.5),
            new TestCaseData(2.0m, 1.1m)
        };

        [Test, TestCaseSource(nameof(Equal))]
        public void ShouldBeEqual(IComparable arg1, IComparable arg2)
        {
            Assert.IsTrue(ComparisonOperator.Compare(ComparisonOperatorTypes.Equals, arg1, arg2));
            Assert.IsTrue(ComparisonOperatorTypes.Equals.Compare(arg1, arg2));
        }

        [Test, TestCaseSource(nameof(Greater))]
        public void ShouldBeNotEqual(IComparable arg1, IComparable arg2)
        {
            Assert.IsTrue(ComparisonOperator.Compare(ComparisonOperatorTypes.NotEqual, arg1, arg2));
            Assert.IsTrue(ComparisonOperatorTypes.NotEqual.Compare(arg1, arg2));
        }

        [Test, TestCaseSource(nameof(Greater))]
        public void ShouldBeGreater(IComparable arg1, IComparable arg2)
        {
            Assert.IsTrue(ComparisonOperator.Compare(ComparisonOperatorTypes.Greater, arg1, arg2));
            Assert.IsTrue(ComparisonOperatorTypes.Greater.Compare(arg1, arg2));
        }

        [Test]
        [TestCaseSource(nameof(Greater))]
        [TestCaseSource(nameof(Equal))]
        public void ShouldBeGreaterOrEqual(IComparable arg1, IComparable arg2)
        {
            Assert.IsTrue(ComparisonOperator.Compare(ComparisonOperatorTypes.GreaterOrEqual, arg1, arg2));
            Assert.IsTrue(ComparisonOperatorTypes.GreaterOrEqual.Compare(arg1, arg2));
        }

        [Test, TestCaseSource(nameof(Greater))]
        public void ShouldBLess(IComparable arg1, IComparable arg2)
        {
            Assert.IsTrue(ComparisonOperator.Compare(ComparisonOperatorTypes.Less, arg2, arg1));
            Assert.IsTrue(ComparisonOperatorTypes.Less.Compare(arg2, arg1));
        }

        [Test]
        [TestCaseSource(nameof(Greater))]
        [TestCaseSource(nameof(Equal))]
        public void ShouldBeLessOrEqual(IComparable arg1, IComparable arg2)
        {
            Assert.IsTrue(ComparisonOperator.Compare(ComparisonOperatorTypes.LessOrEqual, arg2, arg1));
            Assert.IsTrue(ComparisonOperatorTypes.LessOrEqual.Compare(arg2, arg1));
        }
    }
}
