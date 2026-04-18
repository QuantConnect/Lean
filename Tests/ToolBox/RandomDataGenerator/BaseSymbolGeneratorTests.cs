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

using Moq;
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.ToolBox.RandomDataGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class BaseSymbolGeneratorTests
    {
        [Test]
        [TestCase(2, 5)]
        [TestCase(3, 3)]
        [TestCase(1, 4)]
        public void NextUpperCaseString_CreatesString_WithinSpecifiedMinMaxLength(int min, int max)
        {
            var symbolGenerator = new Mock<BaseSymbolGenerator>(Mock.Of<RandomDataGeneratorSettings>(), new RandomValueGenerator()).Object;
            var str = symbolGenerator.NextUpperCaseString(min, max);
            Assert.LessOrEqual(min, str.Length);
            Assert.GreaterOrEqual(max, str.Length);
        }

        [Test]
        public void NextUpperCaseString_CreatesUpperCaseString()
        {
            var symbolGenerator = new Mock<BaseSymbolGenerator>(Mock.Of<RandomDataGeneratorSettings>(), new RandomValueGenerator()).Object;
            var str = symbolGenerator.NextUpperCaseString(10, 10);
            Assert.IsTrue(str.All(char.IsUpper));
        }

        [Test]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        public void ThrowsArgumentException_ForDerivativeSymbols(SecurityType securityType)
        {
            var symbolGenerator = new Mock<BaseSymbolGenerator>(Mock.Of<RandomDataGeneratorSettings>(), new RandomValueGenerator()).Object;
            Assert.Throws<ArgumentException>(() =>
                symbolGenerator.NextSymbol(securityType, Market.USA)
            );
        }

        [Test]
        public void ThrowIsSettingsAreNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                BaseSymbolGenerator.Create(null, Mock.Of<IRandomValueGenerator>());
            });
        }

        [Test]
        public void ThrowIsRundomValueGeneratorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                BaseSymbolGenerator.Create(new RandomDataGeneratorSettings(), null);
            });
        }

        internal static IEnumerable<Symbol> GenerateAsset(BaseSymbolGenerator instance)
        {
            var generateAsset = typeof(BaseSymbolGenerator).GetMethod("GenerateAsset", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IEnumerable<Symbol>)generateAsset.Invoke(instance, new[] { (object)null });
        }

        internal static IEnumerable<Symbol> GenerateAssetWithTicker(BaseSymbolGenerator instance, string ticker)
        {
            var generateAsset = typeof(BaseSymbolGenerator).GetMethod("GenerateAsset", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IEnumerable<Symbol>)generateAsset.Invoke(instance, new object[] { ticker });
        }

    }
}
