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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class FedRateQLRiskFreeRateEstimatorTests
    {
        [Test]
        public void Estimate()
        {
            var spx = Symbols.SPX;
            var tz = TimeZones.NewYork;
            var optionSymbol = Symbol.CreateOption(spx.Value, spx.ID.Market, OptionStyle.European, OptionRight.Put, 4200,
                new DateTime(2021, 1, 15));
            var evaluationDate = new DateTime(2020, 3, 10);

            // setting up
            var equity = OptionPriceModelTests.GetEquity(spx, 100m, 0.25m, tz);
            var option = OptionPriceModelTests.GetOption(optionSymbol, equity, tz);
            var tick = new Tick { Time = evaluationDate, Value = 10m };

            // get the risk free rate
            var estimator = new TestFedRateQLRiskFreeRateEstimator();
            var result = estimator.Estimate(option, 
                new Slice(evaluationDate, new List<BaseData> { tick }, evaluationDate), 
                new OptionContract(optionSymbol, spx));

            AssertAreEqual(0.0175d, result);
        }

        [Test]
        public void LoadInterestRateProvider()
        {
            var estimator = new TestFedRateQLRiskFreeRateEstimator();
            var result = estimator.TestLoadInterestRateProvider();

            var expected = new[]
            {
                new InterestRateProvider
                {
                    Date = new DateTime(2020, 3, 4),
                    InterestRate = 0.0175d
                },
                new InterestRateProvider
                {
                    Date = new DateTime(2020, 3, 16),
                    InterestRate = 0.0025d
                }
            };

            AssertAreEqual(expected, result);
        }

        private void AssertAreEqual(object expected, object result)
        {
            foreach (var fieldInfo in expected.GetType().GetFields())
            {
                Assert.AreEqual(fieldInfo.GetValue(expected), fieldInfo.GetValue(result));
            }
        }

        public class TestFedRateQLRiskFreeRateEstimator : FedRateQLRiskFreeRateEstimator
        {
            public TestFedRateQLRiskFreeRateEstimator()
                : base()
            { 
            }

            public InterestRateProvider[] TestLoadInterestRateProvider()
            {
                return base.LoadInterestRateProvider();
            }
        }
    }
}
