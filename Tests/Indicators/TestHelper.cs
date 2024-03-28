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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Provides helper methods for testing indicatora
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// Gets a stream of IndicatorDataPoints that can be fed to an indicator. The data stream starts at {DateTime.Today, 1m} and
        /// increasing at {1 second, 1m}
        /// </summary>
        /// <param name="count">The number of data points to stream</param>
        /// <param name="valueProducer">Function to produce the value of the data, null to use the index</param>
        /// <returns>A stream of IndicatorDataPoints</returns>
        public static IEnumerable<IndicatorDataPoint> GetDataStream(int count, Func<int, decimal> valueProducer = null)
        {
            var reference = DateTime.Today;
            valueProducer = valueProducer ?? (x => x);
            for (int i = 0; i < count; i++)
            {
                yield return new IndicatorDataPoint(reference.AddSeconds(i), valueProducer.Invoke(i));
            }
        }

        /// <summary>
        /// Compare the specified indicator against external data using the spy_with_indicators.txt file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator(IndicatorBase<IndicatorDataPoint> indicator, string targetColumn, double epsilon = 1e-3)
        {
            TestIndicator(indicator, "spy_with_indicators.txt", targetColumn, (i, expected) => Assert.AreEqual(expected, (double) i.Current.Value, epsilon));
        }

        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="customAssertion">Sets custom assertion logic, parameter is the indicator, expected value from the file</param>
        public static void TestIndicator(IndicatorBase<IndicatorDataPoint> indicator, string externalDataFilename, string targetColumn, Action<IndicatorBase<IndicatorDataPoint>, double> customAssertion)
        {
            foreach (var parts in GetCsvFileStream(externalDataFilename))
            {
                if (!(parts.ContainsKey("Close") && parts.ContainsKey(targetColumn)))
                {
                    Assert.Fail("Didn't find one of 'Close' or '{0}' in the header.", targetColumn);
                    
                    break;
                }

                decimal close = parts.GetCsvValue("close").ToDecimal();
                DateTime date = Time.ParseDate(parts.GetCsvValue("date", "time"));

                var data = new IndicatorDataPoint(date, close);
                indicator.Update(data);

                if (!indicator.IsReady || parts.GetCsvValue(targetColumn).Trim() == string.Empty)
                {
                    continue;
                }

                double expected = Parse.Double(parts.GetCsvValue(targetColumn));
                customAssertion.Invoke(indicator, expected);
            }
        }


        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator(IndicatorBase<IBaseDataBar> indicator, string externalDataFilename, string targetColumn, double epsilon = 1e-3)
        {
            TestIndicator(indicator, externalDataFilename, targetColumn,
                (i, expected) => Assert.AreEqual(expected, (double)i.Current.Value, epsilon,
                    "Failed at " + i.Current.Time.ToStringInvariant("o")
                ));
        }


        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator(IndicatorBase<TradeBar> indicator, string externalDataFilename, string targetColumn, double epsilon = 1e-3)
        {
            TestIndicator(indicator, externalDataFilename, targetColumn,
                (i, expected) => Assert.AreEqual(expected, (double)i.Current.Value, epsilon,
                    "Failed at " + i.Current.Time.ToStringInvariant("o")
                ));
        }

        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="selector">A function that receives the indicator as input and outputs a value to match the target column</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator<T>(T indicator, string externalDataFilename, string targetColumn, Func<T, double> selector, double epsilon = 1e-3)
            where T : Indicator
        {
            TestIndicator(indicator, externalDataFilename, targetColumn,
                (i, expected) => Assert.AreEqual(expected, selector(indicator), epsilon,
                    "Failed at " + i.Current.Time.ToStringInvariant("o")
                ));
        }

        /// <summary>
        /// Compare the specified indicator against external data using the specified comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename">The external CSV file name</param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="customAssertion">Sets custom assertion logic, parameter is the indicator, expected value from the file</param>
        public static void TestIndicator(IndicatorBase<IBaseDataBar> indicator, string externalDataFilename, string targetColumn, Action<IndicatorBase<IBaseDataBar>, double> customAssertion)
        {
            // TODO : Collapse duplicate implementations -- type constraint shenanigans and after 4am

            foreach (var parts in GetCsvFileStream(externalDataFilename))
            {
                var tradebar = parts.GetTradeBar();

                indicator.Update(tradebar);

                if (!indicator.IsReady || parts.GetCsvValue(targetColumn).Trim() == string.Empty)
                {
                    continue;
                }

                double expected = Parse.Double(parts.GetCsvValue(targetColumn));
                customAssertion.Invoke(indicator, expected);
            }
        }

        /// <summary>
        /// Compare the specified indicator against external data using the specified comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename">The external CSV file name</param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="customAssertion">Sets custom assertion logic, parameter is the indicator, expected value from the file</param>
        public static void TestIndicator(IndicatorBase<TradeBar> indicator, string externalDataFilename, string targetColumn, Action<IndicatorBase<TradeBar>, double> customAssertion)
        {
            foreach (var parts in GetCsvFileStream(externalDataFilename))
            {
                var tradebar = parts.GetTradeBar();

                indicator.Update(tradebar);

                if (!indicator.IsReady || parts.GetCsvValue(targetColumn).Trim() == string.Empty)
                {
                    continue;
                }

                double expected = Parse.Double(parts.GetCsvValue(targetColumn));
                customAssertion.Invoke(indicator, expected);
            }
        }

        /// <summary>
        /// Updates the given consolidator with the entries from the given external CSV file
        /// </summary>
        /// <param name="renkoConsolidator">RenkoConsolidator instance to update</param>
        /// <param name="externalDataFilename">The external CSV file name</param>
        public static void UpdateRenkoConsolidator(IDataConsolidator renkoConsolidator, string externalDataFilename)
        {
            foreach (var parts in GetCsvFileStream(externalDataFilename))
            {
                var tradebar = parts.GetTradeBar();
                if (tradebar.Volume == 0)
                {
                    tradebar.Volume = 1;
                }
                renkoConsolidator.Update(tradebar);
            }
        }

        /// <summary>
        /// Tests a reset of the specified indicator after processing external data using the specified comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename">The external CSV file name</param>
        public static void TestIndicatorReset(IndicatorBase<IBaseDataBar> indicator, string externalDataFilename)
        {
            foreach (var data in GetTradeBarStream(externalDataFilename, false))
            {
                indicator.Update(data);
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            AssertIndicatorIsInDefaultState(indicator);
        }

        /// <summary>
        /// Tests a reset of the specified indicator after processing external data using the specified comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename">The external CSV file name</param>
        public static void TestIndicatorReset(IndicatorBase<TradeBar> indicator, string externalDataFilename)
        {
            foreach (var data in GetTradeBarStream(externalDataFilename, false))
            {
                indicator.Update(data);
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            AssertIndicatorIsInDefaultState(indicator);
        }

        /// <summary>
        /// Tests a reset of the specified indicator after processing external data using the specified comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename">The external CSV file name</param>
        public static void TestIndicatorReset(IndicatorBase<IndicatorDataPoint> indicator, string externalDataFilename)
        {
            var date = DateTime.Today;

            foreach (var data in GetTradeBarStream(externalDataFilename, false))
            {
                indicator.Update(date, data.Close);
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            AssertIndicatorIsInDefaultState(indicator);
        }

        /// <summary>
        /// Gets a stream of lines from the specified file
        /// </summary>
        /// <param name="externalDataFilename">The external CSV file name</param>
        public static IEnumerable<IReadOnlyDictionary<string, string>> GetCsvFileStream(string externalDataFilename)
        {
            var enumerator = File.ReadLines(Path.Combine("TestData", FileExtension.ToNormalizedPath(externalDataFilename))).GetEnumerator();
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            string[] header = enumerator.Current.Split(',');
            while (enumerator.MoveNext())
            {
                var values = enumerator.Current.Split(',');
                var headerAndValues = header.Zip(values, (h, v) => new {h, v});
                var dictionary = headerAndValues.ToDictionary(x => x.h.Trim(), x => x.v.Trim(), StringComparer.OrdinalIgnoreCase);
                yield return new ReadOnlyDictionary<string, string>(dictionary);
            }
        }

        /// <summary>
        /// Gets a stream of trade bars from the specified file
        /// </summary>
        public static IEnumerable<TradeBar> GetTradeBarStream(string externalDataFilename, bool fileHasVolume = true)
        {
            return GetCsvFileStream(externalDataFilename).Select(values => GetTradeBar(values, fileHasVolume));
        }

        /// <summary>
        /// Asserts that the indicator has zero samples, is not ready, and has the default value
        /// </summary>
        /// <param name="indicator">The indicator to assert</param>
        public static void AssertIndicatorIsInDefaultState<T>(IndicatorBase<T> indicator)
            where T : IBaseData
        {
            Assert.AreEqual(0m, indicator.Current.Value);
            Assert.AreEqual(DateTime.MinValue, indicator.Current.Time);
            Assert.AreEqual(0, indicator.Samples);
            Assert.IsFalse(indicator.IsReady);

            var fields = indicator.GetType().GetProperties()
                .Where(x => x.PropertyType.IsSubclassOfGeneric(typeof(IndicatorBase<T>)) ||
                            x.PropertyType.IsSubclassOfGeneric(typeof(IndicatorBase<TradeBar>)) ||
                            x.PropertyType.IsSubclassOfGeneric(typeof(IndicatorBase<IndicatorDataPoint>)));
            foreach (var field in fields)
            {
                var subIndicator = field.GetValue(indicator);

                if (subIndicator == null ||
                    subIndicator is ConstantIndicator<T> ||
                    subIndicator is ConstantIndicator<TradeBar> ||
                    subIndicator is ConstantIndicator<IndicatorDataPoint>)
                    continue;

                if (field.PropertyType.IsSubclassOfGeneric(typeof (IndicatorBase<T>)))
                {
                    AssertIndicatorIsInDefaultState(subIndicator as IndicatorBase<T>);
                }
                else if (field.PropertyType.IsSubclassOfGeneric(typeof(IndicatorBase<TradeBar>)))
                {
                    AssertIndicatorIsInDefaultState(subIndicator as IndicatorBase<TradeBar>);
                }
                else if (field.PropertyType.IsSubclassOfGeneric(typeof(IndicatorBase<IndicatorDataPoint>)))
                {
                    AssertIndicatorIsInDefaultState(subIndicator as IndicatorBase<IndicatorDataPoint>);
                }
            }
        }

        /// <summary>
        /// Gets a customAssertion action which will gaurantee that the delta between the expected and the
        /// actual continues to decrease with a lower bound as specified by the epsilon parameter.  This is useful
        /// for testing indicators which retain theoretically infinite information via methods such as exponential smoothing
        /// </summary>
        /// <param name="epsilon">The largest increase in the delta permitted</param>
        /// <returns></returns>
        public static Action<IndicatorBase<IndicatorDataPoint>, double> AssertDeltaDecreases(double epsilon)
        {
            double delta = double.MaxValue;
            return (indicator, expected) =>
            {
                // the delta should be forever decreasing
                var currentDelta = Math.Abs((double) indicator.Current.Value - expected);
                if (currentDelta - delta > epsilon)
                {
                    Assert.Fail("The delta increased!");
                    //Console.WriteLine(indicator.Value.Time.Date.ToShortDateString() + " - " + indicator.Value.Data.ToString("000.000") + " \t " + expected.ToString("000.000") + " \t " + currentDelta.ToString("0.000"));
                }
                delta = currentDelta;
            };
        }

        /// <summary>
        /// Grabs the first value from the set of keys
        /// </summary>
        private static string GetCsvValue(this IReadOnlyDictionary<string, string> dictionary, params string[] keys)
        {
            string value = null;
            if (keys.Any(key => dictionary.TryGetValue(key, out value)))
            {
                return value;
            }

            throw new ArgumentException("Unable to find column: " + string.Join(", ", keys));
        }

        /// <summary>
        /// Grabs the TradeBar values from the set of keys
        /// </summary>
        public static TradeBar GetTradeBar(this IReadOnlyDictionary<string, string> dictionary, bool forceVolumeColumn = false)
        {
            var sid = (dictionary.ContainsKey("symbol") || dictionary.ContainsKey("ticker"))
                ? SecurityIdentifier.GenerateEquity(dictionary.GetCsvValue("symbol", "ticker"), Market.USA)
                : SecurityIdentifier.Empty;

            return new TradeBar
            {
                Symbol = sid != SecurityIdentifier.Empty
                    ? new Symbol(sid, dictionary.GetCsvValue("symbol", "ticker"))
                    : Symbol.Empty,
                Time = Time.ParseDate(dictionary.GetCsvValue("date", "time")),
                Open = dictionary.GetCsvValue("open").ToDecimal(),
                High = dictionary.GetCsvValue("high").ToDecimal(),
                Low = dictionary.GetCsvValue("low").ToDecimal(),
                Close = dictionary.GetCsvValue("close").ToDecimal(),
                Volume = forceVolumeColumn || dictionary.ContainsKey("volume") ? Parse.Long(dictionary.GetCsvValue("volume"), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint) : 0
            };
        }
    }
}