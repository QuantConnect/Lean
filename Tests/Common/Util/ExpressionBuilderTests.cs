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
using System.Linq.Expressions;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class ExpressionBuilderTests
    {
        [Test]
        public void MakesPropertyOrFieldSelectorThatWorks()
        {
            const string DayOfYear = "DayOfYear";
            Expression<Func<DateTime, int>> expected = x => x.DayOfYear;

            var actual = ExpressionBuilder.MakePropertyOrFieldSelector<DateTime, int>(DayOfYear);

            DateTime now = DateTime.UtcNow;

            Assert.AreEqual(expected.Compile().Invoke(now), actual.Compile().Invoke(now));
        }
        [Test]
        public void NonGenericMakesPropertyOrFieldSelectorThatWorks()
        {
            const string DayOfYear = "DayOfYear";
            Expression<Func<DateTime, int>> expected = x => x.DayOfYear;

            var actual = ExpressionBuilder.MakePropertyOrFieldSelector(typeof (DateTime), DayOfYear) as Expression<Func<DateTime, int>>; 
            Assert.IsNotNull(actual);

            DateTime now = DateTime.UtcNow;
            Assert.AreEqual(expected.Compile().Invoke(now), actual.Compile().Invoke(now));
        }
    }
}
