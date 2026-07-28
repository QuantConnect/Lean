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
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InteractiveBrokersOrderPropertiesTests
    {
        [Test]
        public void ExactPercentageDoesNotMakeAssignmentOrderSignificant()
        {
            var exactFirst = new InteractiveBrokersOrderProperties
            {
                ExactFaPercentage = 12.5m,
                FaPercentage = 25
            };
            var integerFirst = new InteractiveBrokersOrderProperties
            {
                FaPercentage = 25,
                ExactFaPercentage = 12.5m
            };

            Assert.AreEqual(25, exactFirst.FaPercentage);
            Assert.AreEqual(12.5m, exactFirst.ExactFaPercentage);
            Assert.AreEqual(integerFirst.FaPercentage, exactFirst.FaPercentage);
            Assert.AreEqual(integerFirst.ExactFaPercentage, exactFirst.ExactFaPercentage);
        }

        [Test]
        public void ClonePreservesExactPercentageIndependently()
        {
            var properties = new InteractiveBrokersOrderProperties
            {
                FaPercentage = 25,
                ExactFaPercentage = 12.5m
            };

            var clone = (InteractiveBrokersOrderProperties)properties.Clone();
            properties.FaPercentage = 50;
            properties.ExactFaPercentage = 33.25m;

            Assert.AreEqual(25, clone.FaPercentage);
            Assert.AreEqual(12.5m, clone.ExactFaPercentage);
            Assert.AreEqual(50, properties.FaPercentage);
            Assert.AreEqual(33.25m, properties.ExactFaPercentage);
        }
    }
}
