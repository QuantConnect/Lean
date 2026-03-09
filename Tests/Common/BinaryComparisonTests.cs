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

using System.Linq.Expressions;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class BinaryComparisonTests
    {
        [Test]
        [TestCase(ExpressionType.Equal, true, false)]
        [TestCase(ExpressionType.NotEqual, false, true)]
        [TestCase(ExpressionType.LessThan, false, true)]
        [TestCase(ExpressionType.LessThanOrEqual, true, true)]
        [TestCase(ExpressionType.GreaterThan, false, false)]
        [TestCase(ExpressionType.GreaterThanOrEqual, true, false)]
        public void EvaluatesComparison(ExpressionType type, bool expected1, bool expected2)
        {
            const int left1 = 1;
            const int right1 = 1;
            const int left2 = 2;
            const int right2 = 3;

            var comparison = BinaryComparison.FromExpressionType(type);

            var actual1 = comparison.Evaluate(left1, right1);
            Assert.AreEqual(expected1, actual1);

            var actual2 = comparison.Evaluate(left2, right2);
            Assert.AreEqual(expected2, actual2);
        }

        [Test]
        [TestCase(ExpressionType.Equal, true, false)]
        [TestCase(ExpressionType.NotEqual, false, true)]
        [TestCase(ExpressionType.LessThan, false, false)]
        [TestCase(ExpressionType.LessThanOrEqual, true, false)]
        [TestCase(ExpressionType.GreaterThan, false, true)]
        [TestCase(ExpressionType.GreaterThanOrEqual, true, true)]
        public void EvaluatesFlippedOperandsComparison(ExpressionType type, bool expected1, bool expected2)
        {
            const int left1 = 1;
            const int right1 = 1;
            const int left2 = 2;
            const int right2 = 3;

            var comparison = BinaryComparison.FromExpressionType(type).FlipOperands();

            var actual1 = comparison.Evaluate(left1, right1);
            Assert.AreEqual(expected1, actual1);

            var actual2 = comparison.Evaluate(left2, right2);
            Assert.AreEqual(expected2, actual2);
        }
    }
}
