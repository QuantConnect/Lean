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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

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

        [Test]
        public void DateTimeRoundDownFullDayDoesntRoundDownByDay()
        {
            var date = new DateTime(2000, 01, 01);
            var rounded = date.RoundDown(TimeSpan.FromDays(1));
            Assert.AreEqual(date, rounded);
        }

        [Test]
        public void GetBetterTypeNameHandlesRecursiveGenericTypes()
        {
            var type = typeof (Dictionary<List<int>, Dictionary<int, string>>);
            const string expected = "Dictionary<List<Int32>, Dictionary<Int32, String>>";
            var actual = type.GetBetterTypeName();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ExchangeRoundDownSkipsWeekends()
        {
            var time = new DateTime(2015, 05, 02, 18, 01, 00);
            var expected = new DateTime(2015, 05, 01);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.FXCM, null, SecurityType.Forex);
            var exchangeRounded = time.ExchangeRoundDown(Time.OneDay, hours, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownHandlesMarketOpenTime()
        {
            var time = new DateTime(2016, 1, 25, 9, 31, 0);
            var expected = time.Date;
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var exchangeRounded = time.ExchangeRoundDown(Time.OneDay, hours, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ExchangeRoundDownInTimeZoneSkipsWeekends()
        {
            // moment before EST market open in UTC (time + one day)
            var time = new DateTime(2017, 10, 01, 9, 29, 59).ConvertToUtc(TimeZones.NewYork);
            var expected = new DateTime(2017, 09, 29).ConvertFromUtc(TimeZones.NewYork);
            var hours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity);
            var exchangeRounded = time.ExchangeRoundDownInTimeZone(Time.OneDay, hours, TimeZones.Utc, false);
            Assert.AreEqual(expected, exchangeRounded);
        }

        [Test]
        public void ConvertsInt32FromString()
        {
            const string input = "12345678";
            var value = input.ToInt32();
            Assert.AreEqual(12345678, value);
        }

        [Test]
        public void ConvertsInt64FromString()
        {
            const string input = "12345678900";
            var value = input.ToInt64();
            Assert.AreEqual(12345678900, value);
        }

        [Test]
        public void ConvertsDecimalFromString()
        {
            const string input = "123.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(123.45678m, value);
        }

        [Test]
        public void ConvertsDecimalFromStringWithExtraWhiteSpace()
        {
            const string input = " 123.45678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(123.45678m, value);
        }

        [Test]
        public void ConvertsDecimalFromIntStringWithExtraWhiteSpace()
        {
            const string input = " 12345678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(12345678m, value);
        }

        [Test]
        public void ConvertsZeroDecimalFromString()
        {
            const string input = "0.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(0.45678m, value);
        }

        [Test]
        public void ConvertsOneNumberDecimalFromString()
        {
            const string input = "1.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(1.45678m, value);
        }

        [Test]
        public void ConvertsZeroDecimalValueFromString()
        {
            const string input = "0";
            var value = input.ToDecimal();
            Assert.AreEqual(0m, value);
        }

        [Test]
        public void ConvertsEmptyDecimalValueFromString()
        {
            const string input = "";
            var value = input.ToDecimal();
            Assert.AreEqual(0m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromString()
        {
            const string input = "-123.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-123.45678m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromStringWithExtraWhiteSpace()
        {
            const string input = " -123.45678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(-123.45678m, value);
        }

        [Test]
        public void ConvertsNegativeDecimalFromIntStringWithExtraWhiteSpace()
        {
            const string input = " -12345678 ";
            var value = input.ToDecimal();
            Assert.AreEqual(-12345678m, value);
        }

        [Test]
        public void ConvertsNegativeZeroDecimalFromString()
        {
            const string input = "-0.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-0.45678m, value);
        }

        [Test]
        public void ConvertsNegavtiveOneNumberDecimalFromString()
        {
            const string input = "-1.45678";
            var value = input.ToDecimal();
            Assert.AreEqual(-1.45678m, value);
        }

        [Test]
        public void ConvertsNegativeZeroDecimalValueFromString()
        {
            const string input = "-0";
            var value = input.ToDecimal();
            Assert.AreEqual(-0m, value);
        }

        [Test]
        public void ConvertsTimeSpanFromString()
        {
            const string input = "16:00";
            var timespan = input.ConvertTo<TimeSpan>();
            Assert.AreEqual(TimeSpan.FromHours(16), timespan);
        }

        [Test]
        public void ConvertsDictionaryFromString()
        {
            var expected = new Dictionary<string, int> {{"a", 1}, {"b", 2}};
            var input = JsonConvert.SerializeObject(expected);
            var actual = input.ConvertTo<Dictionary<string, int>>();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DictionaryAddsItemToExistsList()
        {
            const int key = 0;
            var list = new List<int> {1, 2};
            var dictionary = new Dictionary<int, List<int>> {{key, list}};
            Extensions.Add(dictionary, key, 3);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void DictionaryAddCreatesNewList()
        {
            const int key = 0;
            var dictionary = new Dictionary<int, List<int>>();
            Extensions.Add(dictionary, key, 1);
            Assert.IsTrue(dictionary.ContainsKey(key));
            var list = dictionary[key];
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0]);
        }

        [Test]
        public void SafeDecimalCasts()
        {
            var input = 2d;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(2m, output);
        }

        [Test]
        public void SafeDecimalCastRespectsUpperBound()
        {
            var input = (double) decimal.MaxValue;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(decimal.MaxValue, output);
        }

        [Test]
        public void SafeDecimalCastRespectsLowerBound()
        {
            var input = (double) decimal.MinValue;
            var output = input.SafeDecimalCast();
            Assert.AreEqual(decimal.MinValue, output);
        }

        [Test]
        [TestCase(1.200, "1.2")]
        [TestCase(1200, "1200")]
        [TestCase(123.456, "123.456")]
        public void NormalizeDecimalReturnsNoTrailingZeros(decimal input, string expectedOutput)
        {
            var output = input.Normalize();
            Assert.AreEqual(expectedOutput, output.ToString(CultureInfo.InvariantCulture));
        }

        [Test]
        public void RoundsDownInTimeZone()
        {
            var dataTimeZone = TimeZones.Utc;
            var exchangeTimeZone = TimeZones.EasternStandard;
            var time = new DateTime(2000, 01, 01).ConvertTo(dataTimeZone, exchangeTimeZone);
            var roundedTime = time.RoundDownInTimeZone(Time.OneDay, exchangeTimeZone, dataTimeZone);
            Assert.AreEqual(time, roundedTime);
        }

        [Test]
        public void GetStringBetweenCharsTests()
        {
            const string expected = "python3.6";

            // Different characters cases
            var input = "[ python3.6 ]";
            var actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            input = "[ python3.6 ] [ python2.7 ]";
            actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            input = "[ python2.7 [ python3.6 ] ]";
            actual = input.GetStringBetweenChars('[', ']');
            Assert.AreEqual(expected, actual);

            // Same character cases
            input = "\'python3.6\'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreEqual(expected, actual);

            input = "\' python3.6 \' \' python2.7 \'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreEqual(expected, actual);

            // In this case, it is not equal
            input = "\' python2.7 \' python3.6 \' \'";
            actual = input.GetStringBetweenChars('\'', '\'');
            Assert.AreNotEqual(expected, actual);
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertQuoteBar()
        {
            // Wrap a QuoteBar around a PyObject and convert it back
            var value = ConvertToPyObject(new QuoteBar());

            QuoteBar quoteBar;
            var canConvert = value.TryConvert(out quoteBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(quoteBar);
            Assert.IsAssignableFrom<QuoteBar>(quoteBar);
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertSMA()
        {
            // Wrap a SimpleMovingAverage around a PyObject and convert it back
            var value = ConvertToPyObject(new SimpleMovingAverage(14));

            IndicatorBase<IndicatorDataPoint> indicatorBaseDataPoint;
            var canConvert = value.TryConvert(out indicatorBaseDataPoint);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseDataPoint);
            Assert.IsAssignableFrom<SimpleMovingAverage>(indicatorBaseDataPoint);
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertATR()
        {
            // Wrap a AverageTrueRange around a PyObject and convert it back
            var value = ConvertToPyObject(new AverageTrueRange(14, MovingAverageType.Simple));

            IndicatorBase<IBaseDataBar> indicatorBaseDataBar;
            var canConvert = value.TryConvert(out indicatorBaseDataBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseDataBar);
            Assert.IsAssignableFrom<AverageTrueRange>(indicatorBaseDataBar);
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertAD()
        {
            // Wrap a AccumulationDistribution around a PyObject and convert it back
            var value = ConvertToPyObject(new AccumulationDistribution("AD"));

            IndicatorBase<TradeBar> indicatorBaseTradeBar;
            var canConvert = value.TryConvert(out indicatorBaseTradeBar);
            Assert.IsTrue(canConvert);
            Assert.IsNotNull(indicatorBaseTradeBar);
            Assert.IsAssignableFrom<AccumulationDistribution>(indicatorBaseTradeBar);

        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertFailCSharp()
        {
            // Try to convert a AccumulationDistribution as a QuoteBar
            var value = ConvertToPyObject(new AccumulationDistribution("AD"));

            QuoteBar quoteBar;
            bool canConvert = value.TryConvert(out quoteBar);
            Assert.IsFalse(canConvert);
            Assert.IsNull(quoteBar);
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertFailPython()
        {
            using (Py.GIL())
            {
                // Try to convert a python object as a IndicatorBase<TradeBar>
                var locals = new PyDict();
                PythonEngine.Exec("class A:\n    pass", null, locals.Handle);
                var value = locals.GetItem("A").Invoke();

                IndicatorBase<TradeBar> indicatorBaseTradeBar;
                bool canConvert = value.TryConvert(out indicatorBaseTradeBar);
                Assert.IsFalse(canConvert);
                Assert.IsNull(indicatorBaseTradeBar);
            }
        }

        [Test, Category("TravisExclude")]
        [TestCase("coarseSelector = lambda coarse: [ x.Symbol for x in coarse if x.Price % 2 == 0 ]")]
        [TestCase("def coarseSelector(coarse): return [ x.Symbol for x in coarse if x.Price % 2 == 0 ]")]
        public void PyObjectTryConvertToFunc(string code)
        {
            Func<IEnumerable<CoarseFundamental>, Symbol[]> coarseSelector;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec(code, null, locals.Handle);
                var pyObject = locals.GetItem("coarseSelector");
                pyObject.TryConvertToDelegate(out coarseSelector);
            }

            var coarse = Enumerable
                .Range(0, 9)
                .Select(x => new CoarseFundamental { Symbol = Symbol.Create(x.ToString(), SecurityType.Equity, Market.USA), Value = x });

            var symbols = coarseSelector(coarse);

            Assert.AreEqual(5, symbols.Length);
            foreach (var symbol in symbols)
            {
                var price = Convert.ToInt32(symbol.Value);
                Assert.AreEqual(0, price % 2);
            }
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertToAction1()
        {
            Action<int> action;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a): raise ValueError(a)", null, locals.Handle);
                var pyObject = locals.GetItem("raise_number");
                pyObject.TryConvertToDelegate(out action);
            }

            try
            {
                action(2);
                Assert.Fail();
            }
            catch (PythonException e)
            {
                Assert.AreEqual($"ValueError : {2}", e.Message);
            }
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertToAction2()
        {
            Action<int, decimal> action;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a, b): raise ValueError(a * b)", null, locals.Handle);
                var pyObject = locals.GetItem("raise_number");
                pyObject.TryConvertToDelegate(out action);
            }

            try
            {
                action(2, 3m);
                Assert.Fail();
            }
            catch (PythonException e)
            {
                Assert.AreEqual($"ValueError : {6}", e.Message);
            }
        }

        [Test, Category("TravisExclude")]
        public void PyObjectTryConvertToNonDelegateFail()
        {
            int action;

            using (Py.GIL())
            {
                var locals = new PyDict();
                PythonEngine.Exec("def raise_number(a, b): raise ValueError(a * b)", null, locals.Handle);
                var pyObject = locals.GetItem("raise_number");
                Assert.Throws<ArgumentException>(() => pyObject.TryConvertToDelegate(out action));
            }
        }

        [Test]
        public void BatchByDoesNotDropItems()
        {
            var list = new List<int> {1, 2, 3, 4, 5};
            var by2 = list.BatchBy(2).ToList();
            Assert.AreEqual(3, by2.Count);
            Assert.AreEqual(2, by2[0].Count);
            Assert.AreEqual(2, by2[1].Count);
            Assert.AreEqual(1, by2[2].Count);
            CollectionAssert.AreEqual(list, by2.SelectMany(x => x));
        }

        private PyObject ConvertToPyObject(object value)
        {
            using (Py.GIL())
            {
                return value.ToPython();
            }
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
