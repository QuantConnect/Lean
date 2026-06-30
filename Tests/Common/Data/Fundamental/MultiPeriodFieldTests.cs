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
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class MultiPeriodFieldTests
    {
        private TestMultiPeriodField _field;

        [SetUp]
        public void SetUp()
        {
            _field = new TestMultiPeriodField();
            _field.ThreeMonths = 1;
            _field.OneYear = 5;
            _field.FiveYears = 2;
        }

        [Test]
        public void ReturnsDefaultPeriod()
        {
            Assert.IsTrue(_field.HasValue);

            Assert.AreEqual(5, (decimal)_field);
            Assert.AreEqual(5, _field.Value);
        }

        [Test]
        public void ReturnsRequestedPeriodWithDataAvailable()
        {
            Assert.IsTrue(_field.HasPeriodValue("3M"));

            Assert.AreEqual(1, _field.GetPeriodValue("3M"));
            Assert.AreEqual(1, _field.ThreeMonths);
        }

        [Test]
        public void ReturnsRequestedPeriodWithNoData()
        {
            Assert.IsFalse(_field.HasPeriodValue("3Y"));

            Assert.AreEqual(MultiPeriodField.NoValue, _field.GetPeriodValue("3Y"));
            Assert.AreEqual(MultiPeriodField.NoValue, _field.ThreeYears);
        }

        [Test]
        public void ReturnsCorrectPeriodNamesAndValues()
        {
            Assert.AreEqual(new[] { "1Y", "3M", "5Y" }, _field.GetPeriodNames());

            Assert.AreEqual(new[] { "1Y", "3M", "5Y" }, _field.GetPeriodValues().Keys);

            Assert.AreEqual(new[] { 5, 1, 2 }, _field.GetPeriodValues().Values);
        }

        [Test]
        public void ReturnsFirstPeriodIfNoDefaultAvailable()
        {
            var field = new TestMultiPeriodField();
            field.ThreeMonths = 1;
            field.FiveYears = 2;

            Assert.IsFalse(field.HasValue);

            Assert.AreEqual(1, (decimal)field);
            Assert.AreEqual(1, field.Value);
        }

        [Test]
        public void EmptyStore()
        {
            using var field = new TestMultiPeriodField();
            Assert.IsFalse(field.HasValue);
            Assert.AreEqual(MultiPeriodField.NoValue, field.Value);
            Assert.AreEqual(MultiPeriodField.NoValue, field.FiveYears);
            Assert.AreEqual(MultiPeriodField.NoValue, field.OneYear);
            Assert.AreEqual(Enumerable.Empty<string>(), field.GetPeriodNames());
            Assert.AreEqual(MultiPeriodField.NoValue, field.GetPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear));
            Assert.AreEqual(MultiPeriodField.NoValue, field.GetPeriodValue(QuantConnect.Data.Fundamental.Period.TenYears));
            Assert.AreEqual(0, field.GetPeriodValues().Count);
            Assert.IsFalse(field.HasPeriodValue(QuantConnect.Data.Fundamental.Period.OneYear));
            Assert.IsFalse(field.HasPeriodValue(QuantConnect.Data.Fundamental.Period.TenYears));
        }

        [Test]
        public void EmptyStoreToString()
        {
            using var field = new TestMultiPeriodField();
            Assert.AreEqual("", field.ToString());
        }

        [Test]
        public void ArithmeticOperatorsBetweenDoubleFields()
        {
            var left = new TestMultiPeriodField();
            left.OneYear = 10;
            var right = new TestMultiPeriodField();
            right.OneYear = 4;

            Assert.AreEqual(14m, left + right);
            Assert.AreEqual(6m, left - right);
            Assert.AreEqual(40m, left * right);
            Assert.AreEqual(2.5m, left / right);
            Assert.AreEqual(2m, left % right);
        }

        [Test]
        public void ArithmeticOperatorsBetweenLongFields()
        {
            var left = new TestMultiPeriodFieldLong();
            left.OneYear = 10;
            var right = new TestMultiPeriodFieldLong();
            right.OneYear = 4;

            Assert.AreEqual(14m, left + right);
            Assert.AreEqual(6m, left - right);
            Assert.AreEqual(40m, left * right);
            Assert.AreEqual(2.5m, left / right);
            Assert.AreEqual(2m, left % right);
        }

        [Test]
        public void ArithmeticOperatorsBetweenDoubleAndLongFields()
        {
            var doubleField = new TestMultiPeriodField();
            doubleField.OneYear = 10;
            var longField = new TestMultiPeriodFieldLong();
            longField.OneYear = 4;

            // double on the left, long on the right
            Assert.AreEqual(14m, doubleField + longField);
            Assert.AreEqual(6m, doubleField - longField);
            Assert.AreEqual(40m, doubleField * longField);
            Assert.AreEqual(2.5m, doubleField / longField);
            Assert.AreEqual(2m, doubleField % longField);

            // long on the left, double on the right
            Assert.AreEqual(14m, longField + doubleField);
            Assert.AreEqual(-6m, longField - doubleField);
            Assert.AreEqual(40m, longField * doubleField);
            Assert.AreEqual(0.4m, longField / doubleField);
            Assert.AreEqual(4m, longField % doubleField);
        }

        [Test]
        public void ArithmeticOperatorsWithScalar()
        {
            var field = new TestMultiPeriodField();
            field.OneYear = 10;

            // field implicitly converts to decimal, the built-in decimal operators apply
            Assert.AreEqual(13m, field + 3m);
            Assert.AreEqual(7m, field - 3m);
            Assert.AreEqual(30m, field * 3m);
            Assert.AreEqual(5m, field / 2m);
            Assert.AreEqual(1m, field % 3m);
        }

        [Test]
        public void ArithmeticOperatorsUseDefaultPeriodValue()
        {
            // No default period value available, falls back to first available period (3M = 1)
            var left = new TestMultiPeriodField();
            left.ThreeMonths = 1;
            var right = new TestMultiPeriodField();
            right.ThreeMonths = 1;

            Assert.IsFalse(left.HasValue);
            Assert.AreEqual(2m, left + right);
        }

        private class TestMultiPeriodField : MultiPeriodField
        {
            protected override string DefaultPeriod => "OneYear";

            public double ThreeMonths { get; set; } = NoValue;
            public double OneYear { get; set; } = NoValue;
            public double ThreeYears { get; set; } = NoValue;
            public double FiveYears { get; set; } = NoValue;
            public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(double), OneYear);
            public override double Value
            {
                get
                {
                    var defaultValue = OneYear;
                    if (!BaseFundamentalDataProvider.IsNone(typeof(double), defaultValue))
                    {
                        return defaultValue;
                    }
                    return base.Value;
                }
            }

            public override double GetPeriodValue(string period)
            {
                switch(period)
                {
                    case QuantConnect.Data.Fundamental.Period.ThreeMonths:
                        return ThreeMonths;
                    case QuantConnect.Data.Fundamental.Period.OneYear:
                        return OneYear;
                    case QuantConnect.Data.Fundamental.Period.ThreeYears:
                        return ThreeYears;
                    case QuantConnect.Data.Fundamental.Period.FiveYears:
                        return FiveYears;
                    default:
                        return NoValue;
                }
            }

            public override IReadOnlyDictionary<string, double> GetPeriodValues()
            {
                var result = new Dictionary<string, double>();
                foreach (var kvp in new[] { new Tuple<string, double>("1Y", OneYear), new Tuple<string, double>("3M", ThreeMonths), new Tuple<string, double>("3Y", ThreeYears), new Tuple<string, double>("5Y", FiveYears) })
                {
                    if (!BaseFundamentalDataProvider.IsNone(typeof(double), kvp.Item2))
                    {
                        result[kvp.Item1] = kvp.Item2;
                    }
                }
                return result;
            }
        }

        private class TestMultiPeriodFieldLong : MultiPeriodFieldLong
        {
            protected override string DefaultPeriod => "OneYear";

            public long ThreeMonths { get; set; } = NoValue;
            public long OneYear { get; set; } = NoValue;
            public override bool HasValue => !BaseFundamentalDataProvider.IsNone(typeof(long), OneYear);
            public override long Value
            {
                get
                {
                    var defaultValue = OneYear;
                    if (!BaseFundamentalDataProvider.IsNone(typeof(long), defaultValue))
                    {
                        return defaultValue;
                    }
                    return base.Value;
                }
            }

            public override long GetPeriodValue(string period)
            {
                switch (period)
                {
                    case QuantConnect.Data.Fundamental.Period.ThreeMonths:
                        return ThreeMonths;
                    case QuantConnect.Data.Fundamental.Period.OneYear:
                        return OneYear;
                    default:
                        return NoValue;
                }
            }

            public override IReadOnlyDictionary<string, long> GetPeriodValues()
            {
                var result = new Dictionary<string, long>();
                foreach (var kvp in new[] { new Tuple<string, long>("1Y", OneYear), new Tuple<string, long>("3M", ThreeMonths) })
                {
                    if (!BaseFundamentalDataProvider.IsNone(typeof(long), kvp.Item2))
                    {
                        result[kvp.Item1] = kvp.Item2;
                    }
                }
                return result;
            }
        }
    }
}
