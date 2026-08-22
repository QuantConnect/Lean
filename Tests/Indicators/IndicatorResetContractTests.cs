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
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Asserts the reset contract against every indicator in the assembly rather than only
    /// the ones with a test class deriving from <see cref="CommonIndicatorTests{T}"/>.
    /// </summary>
    /// <remarks>
    /// A field left set by the first pass changes the second, which is what a reset defect
    /// looks like from the outside. Several periods, because a field assigned on an early
    /// return during warm-up is overwritten on the first update at any larger period.
    /// A type that cannot be constructed or fed is reported with its reason by
    /// <see cref="Assert.Ignore(string)"/>.
    /// </remarks>
    [TestFixture]
    public class IndicatorResetContractTests
    {
        private static readonly int[] Periods = { 1, 2, 14 };

        private const int MinimumSamples = 40;

        private static readonly DateTime StartDate = new DateTime(2020, 1, 1);

        private static readonly Symbol Target =
            new Symbol(SecurityIdentifier.GenerateEquity("SPY", Market.USA, mapSymbol: false), "SPY");

        private static readonly Symbol Reference =
            new Symbol(SecurityIdentifier.GenerateEquity("IBM", Market.USA, mapSymbol: false), "IBM");

        private static IEnumerable<TestCaseData> Cases()
        {
            var indicators = typeof(IndicatorBase).Assembly.GetTypes()
                .Where(type => type.IsClass && type.IsPublic && !type.IsAbstract && !type.IsGenericTypeDefinition)
                .Where(type => InputType(type) != null)
                .OrderBy(type => type.Name);

            foreach (var indicator in indicators)
            {
                foreach (var period in Periods)
                {
                    // {m} is the test method, without which the two contracts name their
                    // cases identically.
                    yield return new TestCaseData(indicator, period)
                        .SetName($"{{m}}({indicator.Name}, period {period.ToString(CultureInfo.InvariantCulture)})");
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(Cases))]
        public void ProducesTheSameValuesAfterReset(Type type, int period)
        {
            var indicator = Construct(type, period, out var rejected);
            if (indicator == null)
            {
                Assert.Ignore(Skip(type, period, rejected));
            }

            RegisterTrackedSymbols(indicator, type);
            var count = SampleCount(indicator, period);

            var before = new List<Sample>();
            var reason = Feed(indicator, type, count, before);
            if (reason != null)
            {
                Assert.Ignore(Skip(type, period, reason));
            }
            if (indicator.Samples == 0)
            {
                Assert.Ignore(Skip(type, period, "accepted the replay without recording a sample"));
            }

            indicator.Reset();

            var after = new List<Sample>();
            var second = Feed(indicator, type, count, after);

            // The series was accepted once already, so failing it now is itself a defect.
            Assert.IsNull(second, $"{Where(type, period)} accepted the series, then failed it after Reset: {second}");
            Assert.AreEqual(before.Count, after.Count, $"{Where(type, period)} produced fewer values after Reset");

            for (var i = 0; i < before.Count; i++)
            {
                var at = i.ToString(CultureInfo.InvariantCulture);
                Assert.AreEqual(before[i].Value, after[i].Value,
                    $"{Where(type, period)} returned a different value at index {at} after Reset");
                Assert.AreEqual(before[i].IsReady, after[i].IsReady,
                    $"{Where(type, period)} reported a different IsReady at index {at} after Reset");
            }
        }

        [Test]
        [TestCaseSource(nameof(Cases))]
        public void ResetsToDefaultState(Type type, int period)
        {
            var indicator = Construct(type, period, out var rejected);
            if (indicator == null)
            {
                Assert.Ignore(Skip(type, period, rejected));
            }

            RegisterTrackedSymbols(indicator, type);
            var count = SampleCount(indicator, period);

            var reason = Feed(indicator, type, count, new List<Sample>());
            if (reason != null)
            {
                Assert.Ignore(Skip(type, period, reason));
            }
            if (indicator.Samples == 0)
            {
                Assert.Ignore(Skip(type, period, "accepted the replay without recording a sample"));
            }

            indicator.Reset();

            // The assertion CommonIndicatorTests already makes, generic on the input type.
            var assert = typeof(TestHelper)
                .GetMethod(nameof(TestHelper.AssertIndicatorIsInDefaultState))
                .MakeGenericMethod(InputType(type));
            try
            {
                assert.Invoke(null, new object[] { indicator });
            }
            catch (TargetInvocationException exception)
            {
                // The helper asserts without a message.
                Assert.Fail($"{Where(type, period)} is not in its default state after Reset. "
                    + exception.InnerException?.Message);
            }
        }

        private static bool IsOption(ParameterInfo parameter)
        {
            return parameter.Name != null
                && parameter.Name.Contains("option", StringComparison.OrdinalIgnoreCase);
        }

        private static Symbol OptionOn(Symbol underlying)
        {
            return new Symbol(
                SecurityIdentifier.GenerateOption(
                    new DateTime(2020, 6, 19), underlying.ID, Market.USA, 300m, OptionRight.Call, OptionStyle.American),
                underlying.Value);
        }

        private static string Where(Type type, int period)
        {
            return $"{type.Name} at period {period.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string Skip(Type type, int period, string reason)
        {
            return $"{Where(type, period)}: {reason}";
        }

        // A repeating series hides a carried-over price. This one never revisits a level.
        private static decimal Price(int index)
        {
            return 100m + (0.37m * index) + (index % 5 == 0 ? 1.9m : 0m);
        }

        // 21 of 188 never became ready inside 40 bars at a period of 14.
        private static int SampleCount(IIndicator indicator, int period)
        {
            var warmUp = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod ?? period;
            return Math.Max(MinimumSamples, (2 * warmUp) + 2);
        }

        private static Type InputType(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(IndicatorBase<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }
            return null;
        }

        // Returns null and the reason the last candidate refused.
        private static IIndicator Construct(Type type, int period, out string rejected)
        {
            rejected = "has no constructor this fixture can fill";
            foreach (var constructor in type.GetConstructors().OrderBy(x => x.GetParameters().Length))
            {
                var arguments = Arguments(type, period, constructor.GetParameters());
                if (arguments == null)
                {
                    continue;
                }
                try
                {
                    return (IIndicator)constructor.Invoke(arguments);
                }
                catch (Exception exception)
                {
                    // FractalAdaptiveMovingAverage rejects an odd N, and it is not alone.
                    rejected = "was refused by every constructor, last saying: "
                        + exception.GetBaseException().Message;
                }
            }
            return null;
        }

        private static object[] Arguments(Type type, int period, ParameterInfo[] parameters)
        {
            var arguments = new object[parameters.Length];
            var integers = 0;
            var symbols = 0;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

                if (parameterType == typeof(Symbol) && !parameter.HasDefaultValue)
                {
                    // The option indicators read option.Underlying. Alpha rejects a target
                    // equal to its reference. Counted among the symbols, because
                    // Covariance(string, int, Symbol, Symbol) puts neither first.
                    arguments[i] = IsOption(parameter)
                        ? OptionOn(Target)
                        : symbols == 0 ? Target : Reference;
                    symbols++;
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                }
                else if (parameterType == typeof(string))
                {
                    arguments[i] = type.Name;
                }
                else if (parameterType == typeof(int))
                {
                    // Counted among the integers, so the first is the period the case names.
                    arguments[i] = period + (2 * integers);
                    integers++;
                }
                else if (parameterType == typeof(decimal))
                {
                    arguments[i] = 2m;
                }
                else if (parameterType == typeof(bool))
                {
                    arguments[i] = false;
                }
                else if (parameterType.IsEnum)
                {
                    arguments[i] = Enum.GetValues(parameterType).GetValue(0);
                }
                else if (InputType(parameterType) != null && !parameterType.IsAbstract && !parameterType.IsGenericTypeDefinition)
                {
                    arguments[i] = Construct(parameterType, period, out _);
                    if (arguments[i] == null)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return arguments;
        }

        // The breadth indicators report not ready until an asset is tracked.
        private static void RegisterTrackedSymbols(IIndicator indicator, Type type)
        {
            var add = type.GetMethod("Add", new[] { typeof(Symbol) });
            if (add == null)
            {
                return;
            }
            foreach (var symbol in new[] { Target, Reference })
            {
                try
                {
                    add.Invoke(indicator, new object[] { symbol });
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        // Returns the reason the indicator could not be driven, or null when it was.
        private static string Feed(IIndicator indicator, Type type, int count, List<Sample> samples)
        {
            var input = InputType(type);
            for (var i = 0; i < count; i++)
            {
                var time = StartDate.AddDays(i);
                var price = Price(i);

                try
                {
                    if (input == typeof(IndicatorDataPoint))
                    {
                        indicator.Update(new IndicatorDataPoint(Target, time, price));
                    }
                    else if (input.IsAssignableFrom(typeof(TradeBar)))
                    {
                        // A TradeBar satisfies IBaseDataBar, BaseData and IBaseData alike
                        indicator.Update(new TradeBar(time, Target, price, price + 1m, price - 1m, price + 0.5m, 1000 + i));
                    }
                    else
                    {
                        return $"takes {input.Name}, which this fixture does not feed";
                    }
                }
                catch (Exception exception)
                {
                    // An indicator that cannot survive the series says nothing about reset,
                    // so the exception is reported rather than failed.
                    return $"threw on sample {i.ToString(CultureInfo.InvariantCulture)}: "
                        + exception.GetBaseException().Message;
                }

                samples.Add(new Sample(indicator.Current.Value, indicator.IsReady));
            }
            return null;
        }

        private struct Sample
        {
            public Sample(decimal value, bool isReady)
            {
                Value = value;
                IsReady = isReady;
            }

            public decimal Value { get; }

            public bool IsReady { get; }
        }
    }
}
