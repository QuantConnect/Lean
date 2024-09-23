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
using NUnit.Framework;
using QuantConnect.Indicators;
using System.Linq;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class IndicatorExtensionsTests
    {
        [Test, Parallelizable(ParallelScope.Self)]
        public void PipesDataUsingOfFromFirstToSecond()
        {
            var first = new SimpleMovingAverage(2);
            var second = new Delay(1);

            // this is a configuration step, but returns the reference to the second for method chaining
            second.Of(first);

            var data1 = new IndicatorDataPoint(DateTime.UtcNow, 1m);
            var data2 = new IndicatorDataPoint(DateTime.UtcNow, 2m);
            var data3 = new IndicatorDataPoint(DateTime.UtcNow, 3m);
            var data4 = new IndicatorDataPoint(DateTime.UtcNow, 4m);

            // sma has one item
            first.Update(data1);
            Assert.IsFalse(first.IsReady);
            Assert.AreEqual(0m, second.Current.Value);

            // sma is ready, delay will repeat this value
            first.Update(data2);
            Assert.IsTrue(first.IsReady);
            Assert.IsFalse(second.IsReady);
            Assert.AreEqual(1.5m, second.Current.Value);

            // delay is ready, and repeats its first input
            first.Update(data3);
            Assert.IsTrue(second.IsReady);
            Assert.AreEqual(1.5m, second.Current.Value);

            // now getting the delayed data
            first.Update(data4);
            Assert.AreEqual(2.5m, second.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void PipesDataFirstWeightedBySecond()
        {
            const int period = 4;
            var value = new Identity("Value");
            var weight = new Identity("Weight");

            var third = value.WeightedBy(weight, period);

            var data = Enumerable.Range(1, 10).ToList();
            var window = Enumerable.Reverse(data).Take(period);
            var current = window.Sum(x => 2 * x * x) / (decimal)window.Sum(x => x);

            foreach (var item in data)
            {
                value.Update(new IndicatorDataPoint(DateTime.UtcNow, Convert.ToDecimal(2 * item)));
                weight.Update(new IndicatorDataPoint(DateTime.UtcNow, Convert.ToDecimal(item)));
            }

            Assert.AreEqual(current, third.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void NewDataPushesToDerivedIndicators()
        {
            var identity = new Identity("identity");
            var sma = new SimpleMovingAverage(3);

            identity.Updated += (sender, consolidated) =>
            {
                sma.Update(consolidated);
            };

            identity.Update(DateTime.UtcNow, 1m);
            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(sma.IsReady);
            Assert.AreEqual(2m, sma.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void MultiChainSMA()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the SMA of the delay of the identity
            var sma = delay.Of(identity).SMA(2);

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(sma.IsReady);

            Assert.AreEqual(1.5m, sma.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void MultiChainEMA()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the EMA of chained methods
            var ema = delay.Of(identity).EMA(2, 1);

            // Assert.IsTrue(ema. == 1);
            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(ema.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(ema.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(ema.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(ema.IsReady);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void MultiChainMAX()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the MAX of the delay of the identity
            var max = delay.Of(identity).MAX(2);

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(max.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(max.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(max.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(max.IsReady);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void MultiChainMIN()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the MIN of the delay of the identity
            var min = delay.Of(identity).MIN(2);

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(min.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(min.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(min.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(min.IsReady);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void PlusAddsLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Plus(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(2m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(2m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(2m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(7m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void PlusAddsLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Plus(5);

            left.Update(DateTime.Today, 1m);
            Assert.AreEqual(6m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(7m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void MinusSubtractsLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Minus(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(0m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(-1m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void MinusSubtractsLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Minus(1);

            left.Update(DateTime.Today, 1m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void OverDividesLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Over(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(3m / 4m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void OverDividesLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Over(2);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 4m);
            Assert.AreEqual(2m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void OverHandlesDivideByZero()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Over(right);
            var updatedEventFired = false;
            composite.Updated += delegate { updatedEventFired = true; };

            left.Update(DateTime.Today, 1m);
            Assert.IsFalse(updatedEventFired);
            right.Update(DateTime.Today, 0m);
            Assert.IsFalse(updatedEventFired);

            // submitting another update to right won't cause an update without corresponding update to left
            right.Update(DateTime.Today, 1m);
            Assert.IsFalse(updatedEventFired);
            left.Update(DateTime.Today, 1m);
            Assert.IsTrue(updatedEventFired);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TimesMultipliesLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Times(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(12m, composite.Current.Value);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TimesMultipliesLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Times(10);

            left.Update(DateTime.Today, 1m);
            Assert.AreEqual(10m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(20m, composite.Current.Value);

        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void WorksForIndicatorsOfDifferentTypes()
        {
            var indicatorA1 = new TestIndicatorA("1");
            var indicatorA2 = new TestIndicatorA("2");

            indicatorA1.Over(indicatorA2);
            indicatorA1.Minus(indicatorA2);
            indicatorA1.Times(indicatorA2);
            indicatorA1.Plus(indicatorA2);
            indicatorA1.Of(indicatorA2);

            var indicatorB1 = new TestIndicatorB("1");
            var indicatorB2 = new TestIndicatorB("2");
            indicatorB1.Over(indicatorB2);
            indicatorB1.Minus(indicatorB2);
            indicatorB1.Times(indicatorB2);
            indicatorB1.Plus(indicatorB2);
            indicatorB1.Of(indicatorB2);
        }

        protected static TestCaseData[] IndicatorOfDifferentBaseCases()
        {
            // Helper for getting all permutations of the indicators listed below
            static IEnumerable<IEnumerable<T>>
                GetPermutations<T>(IEnumerable<T> list, int length)
            {
                if (length == 1) return list.Select(t => new T[] { t });
                return GetPermutations(list, length - 1)
                    .SelectMany(t => list.Where(o => !t.Contains(o)),
                        (t1, t2) => t1.Concat(new T[] { t2 }));
            }

            // Define our indicators to test on
            var testIndicators = new IIndicator[]
            {
                new TestIndicator<BaseData>("BD"),
                new TestIndicator<QuoteBar>("QB"),
                new TestIndicator<TradeBar>("TB"),
                new TestIndicator<IndicatorDataPoint>("IDP")
            };

            // Methods defined in CompositeTestRunner
            var methods = new string[]
            {
                "minus", "plus", "over", "times"
            };

            // Create every combination of indicators
            var combinations = GetPermutations(testIndicators, 2);

            // Create a case for each method with each combination of indicators
            var cases = new List<TestCaseData>();
            foreach (var combo in combinations)
            {
                foreach (var method in methods)
                {
                    var newCase = new TestCaseData(combo, method);
                    cases.Add(newCase);
                }
            }

            return cases.ToArray();
        }

        [TestCaseSource(nameof(IndicatorOfDifferentBaseCases))]
        public void DifferentBaseIndicators(IEnumerable<IIndicator> indicators, string method)
        {
            CompositeTestRunner(indicators.ElementAt(0), indicators.ElementAt(1), method);
        }
        
        [TestCaseSource(nameof(IndicatorOfDifferentBaseCases))]
        public void DifferentBaseIndicatorsPy(IEnumerable<IIndicator> indicators, string method)
        {
            using (Py.GIL())
            {
                CompositeTestRunner(indicators.ElementAt(0).ToPython(), indicators.ElementAt(1).ToPython(), method);
            }
        }

        public static void CompositeTestRunner(dynamic left, dynamic right, string method)
        {
            // Reset before every test; the permutation setup in test cases uses the same instance for each permutation
            left.Reset();
            right.Reset();

            double expected;
            CompositeIndicator compositeIndicator;

            switch (method)
            {
                case "minus":
                    expected = -5; // 5 - 10
                    compositeIndicator = IndicatorExtensions.Minus(left, right);
                    break;
                case "plus":
                    expected = 15; // 5 + 10
                    compositeIndicator = IndicatorExtensions.Plus(left, right);
                    break;
                case "over":
                    expected = .5; // 5 / 10
                    compositeIndicator = IndicatorExtensions.Over(left, right);
                    break;
                case "times":
                    expected = 50; // 5 * 10
                    compositeIndicator = IndicatorExtensions.Times(left, right);
                    break;
                default:
                    Assert.Fail($"Method '{method}' not handled by this test, please implement");
                    throw new ArgumentException($"Cannot proceed with test using method {method}");
            }

            // Check our values are all zero
            Assert.AreEqual(0, (int)right.Current.Value);
            Assert.AreEqual(0, (int)left.Current.Value);
            Assert.AreEqual(0, compositeIndicator.Current.Value);

            // Use our test indicator method to update left and right
            left.UpdateValue(5);
            right.UpdateValue(10);

            // Check final expected values, this ensures that composites are updating correctly
            Assert.AreEqual(5, (int)left.Current.Value);
            Assert.AreEqual(10, (int)right.Current.Value);
            Assert.AreEqual(expected, compositeIndicator.Current.Value);
        }

        [Test]
        public void MinusSubtractsLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator) IndicatorExtensions.Minus(left.ToPython(), 10);

                left.Update(DateTime.Today, 1);
                Assert.AreEqual(-9, composite.Current.Value);

                left.Update(DateTime.Today, 2);
                Assert.AreEqual(-8, composite.Current.Value);
            }
        }

        [Test]
        public void PlusAddsLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator)IndicatorExtensions.Plus(left.ToPython(), 10);

                left.Update(DateTime.Today, 1);
                Assert.AreEqual(11, composite.Current.Value);

                left.Update(DateTime.Today, 2);
                Assert.AreEqual(12, composite.Current.Value);
            }
        }

        [Test]
        public void OverDivdesLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator)IndicatorExtensions.Over(left.ToPython(), 5);

                left.Update(DateTime.Today, 10);
                Assert.AreEqual(2, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(4, composite.Current.Value);
            }
        }

        [Test]
        public void TimesMultipliesLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator)IndicatorExtensions.Times(left.ToPython(), 5);

                left.Update(DateTime.Today, 10);
                Assert.AreEqual(50, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(100, composite.Current.Value);
            }
        }

        [Test]
        public void TimesMultipliesLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Times(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(100, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(100, composite.Current.Value);
            }
        }

        [Test]
        public void OverDividesLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Over(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(1, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(1, composite.Current.Value);
            }
        }

        [Test]
        public void PlusAddsLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Plus(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(20, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(20, composite.Current.Value);
            }
        }

        [Test]
        public void MinusSubstractsLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Minus(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(0, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(0, composite.Current.Value);
            }
        }

        [Test]
        public void RunPythonRegressionAlgorithmWithIndicatorExtensions()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("IndicatorExtensionsSMAWithCustomIndicatorsRegressionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "0"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "0%"},
                    {"Drawdown", "0%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "0%"},
                    {"Sharpe Ratio", "0"},
                    {"Probabilistic Sharpe Ratio", "0%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0"},
                    {"Annual Standard Deviation", "0"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "0.717"},
                    {"Tracking Error", "0.593"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$0.00"},
                    {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                initialCash: 100000);
        }

        private class TestIndicatorA : IndicatorBase<IBaseData>
        {
            public TestIndicatorA(string name) : base(name)
            {
            }
            public override bool IsReady { get; }
            protected override decimal ComputeNextValue(IBaseData input)
            {
                throw new NotImplementedException();
            }
        }

        private class TestIndicatorB : IndicatorBase<IndicatorDataPoint>
        {
            public TestIndicatorB(string name) : base(name)
            {
            }
            public override bool IsReady
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
            protected override decimal ComputeNextValue(IndicatorDataPoint input)
            {
                throw new NotImplementedException();
            }
        }

        private class TestIndicator<T> : IndicatorBase<T>
            where T : IBaseData
        {
            public TestIndicator(string name)
                : base(name)
            {

            }

            public override bool IsReady
            {
                get
                {
                    return true;
                }
            }

            public void UpdateValue(int value)
            {
                Current = new IndicatorDataPoint(DateTime.MinValue, value);
                OnUpdated(Current);
            }

            protected override decimal ComputeNextValue(T input)
            {
                return input.Value;
            }
        }
    }
}
