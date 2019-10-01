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
using System.Globalization;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        [TestCase(typeof(string), "123", typeof(long), 123L)]
        [TestCase(typeof(string), "123", typeof(int), 123)]
        [TestCase(typeof(string), "123", typeof(decimal), 123)]
        [TestCase(typeof(long), "123", typeof(decimal), 123)]
        [TestCase(typeof(string), null, typeof(decimal), 0)]
        public void ConvertInvariant(Type sourceType, string sourceString, Type conversionType, object expected)
        {
            // we can't put a decimal in the attribute, so this ensure the runtime types are correct
            expected = Convert.ChangeType(expected, conversionType, CultureInfo.InvariantCulture);
            Assert.IsInstanceOf(conversionType, expected);

            var source = Convert.ChangeType(sourceString, sourceType, CultureInfo.InvariantCulture);
            if (sourceString == null)
            {
                Assert.IsNull(source);
            }
            else
            {
                Assert.IsInstanceOf(sourceType, source);
            }

            var converted = ((IConvertible)source).ConvertInvariant(conversionType);
            Assert.AreEqual(expected, converted);
            Assert.IsInstanceOf(conversionType, converted);
        }

        [Test]
        public void ConvertInvariant_ThrowsFormatException_WhenConvertingEmptyString()
        {
            const string input = "";
            Assert.Throws<FormatException>(
                () => input.ConvertInvariant<decimal>()
            );
        }

        [Test]
        public void ConvertInvariantString()
        {
            var source = 123L;
            const string expected = "123";
            var actual = source.ConvertInvariant<string>();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Convertible_ToStringInvariant_Equals_ToStringInvariantCulture()
        {
            IConvertible convertible = 1;
            var formatted = convertible.ToStringInvariant();
            Assert.AreEqual(convertible.ToString(CultureInfo.InvariantCulture), formatted);
        }

        [Test]
        public void Formattable_ToStringInvariant_DoesNotRequire_FormatParameter()
        {
            var format = (string) null;
            IFormattable formattable = 1;
            var formatted = formattable.ToStringInvariant(format);
            Assert.AreEqual(formattable.ToString(format, CultureInfo.InvariantCulture), formatted);
        }

        [Test]
        [TestCase(TypeCode.DateTime, "07/28/2019", "yyyy-MM-dd")]
        public void Formattable_ToStringInvariant_UsesProvided_FormatParameter(
            TypeCode typeCode,
            string value,
            string format
            )
        {
            var formattable = (IFormattable) Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture);
            var formatted = formattable.ToStringInvariant(format);
            var expected = formattable.ToString(format, CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, formatted, $"Failed on type code: {typeCode}");
        }

        [Test]
        public void Formattable_ToStringInvariant_RespectsFieldWidth_InFormatParameter()
        {
            // BEHAVIOR CHANGE --
            var value = 5.678m;
            var format = "-20:P2";
            var sameBehavior = $"{value,-20:P2}";
            var expected = string.Format(CultureInfo.InvariantCulture, "{0,-20:P2}", value);

            // the usage of the InvariantCulture add a space between the number of the percent sign
            //Assert.AreEqual("567.80%            ", sameBehavior); // this passes in windows but failed in travis
            Assert.AreEqual("567.80 %            ", expected);

            var actual = value.ToStringInvariant(format);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void StartsWithInvariant_IsTheSameAs_StartsWith_WithInvariantCulture_DefaultingToCaseSensitive()
        {
            var str = "aBcD";
            var beginning = "aB";
            Assert.IsTrue(str.StartsWithInvariant(beginning));

            beginning = "AB";
            Assert.IsFalse(str.StartsWithInvariant(beginning));
            Assert.IsTrue(str.StartsWithInvariant(beginning, ignoreCase: true));
        }

        [Test]
        public void EndsWithInvariant_IsTheSameAs_EndsWith_WithInvariantCulture_DefaultingToCaseSensitive()
        {
            var str = "aBcD";
            var ending = "cD";
            Assert.IsTrue(str.EndsWithInvariant(ending));

            ending = "CD";
            Assert.IsFalse(str.EndsWithInvariant(ending));
            Assert.IsTrue(str.EndsWithInvariant(ending, ignoreCase: true));
        }

        [Test]
        public void ToIso8601Invariant_IsTheSameAs_ToString_O_WithInvariantCulture()
        {
            var date = DateTime.UtcNow;
            var expected = date.ToString("O", CultureInfo.InvariantCulture);
            var actual = date.ToIso8601Invariant();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void IndexOfInvariant_IsTheSameAs_IndexOf_WithStringComparisonInvariantCulture(bool ignoreCase)
        {
            var comparison = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            var str = "abcdefg";
            var substring1 = "de";
            var expected1 = str.IndexOf(substring1, comparison);
            var actual1 = str.IndexOfInvariant(substring1, ignoreCase);
            Assert.AreEqual(expected1, actual1);

            var substring2 = "dE";
            var expected2 = str.IndexOf(substring2, comparison);
            var actual2 = str.IndexOfInvariant(substring2, ignoreCase);
            Assert.AreEqual(expected2, actual2);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void IfNotNullOrEmpty_ReturnsSpecifiedDefaultValue_WhenNullOrEmpty(string str)
        {
            int defaultValue = 42;
            var actual = str.IfNotNullOrEmpty(defaultValue, int.Parse);
            Assert.AreEqual(defaultValue, actual);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void IfNotNullOrEmpty_ReturnsDefaultValue_WhenNullOrEmpty(string str)
        {
            var actual = str.IfNotNullOrEmpty(int.Parse);
            Assert.AreEqual(default(int), actual);
        }

        [Test]
        public void IfNotNullOrEmpty_ReturnsResultOfFunc_WhenNotEmpty()
        {
            var actual = "42".IfNotNullOrEmpty(int.Parse);
            Assert.AreEqual(42, actual);
        }

        [Test]
        public void IfNotNullOrEmpty_ReturnsResultOfFunc_WhenNotEmptyAndDefaultValueSpecified()
        {
            var defaultValue = -42;
            var actual = "42".IfNotNullOrEmpty(defaultValue, int.Parse);
            Assert.AreEqual(42, actual);
        }
    }
}