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
 *
*/

using System;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine
{
    public static class PerformanceBenchmarkAlgorithms
    {
        public static QCAlgorithm SingleSecurity_Second => new SingleSecurity_Second_BenchmarkTest();
        public static QCAlgorithm FiveHundredSecurity_Second => new FiveHundredSecurity_Second_BenchmarkTest();

        public static QCAlgorithm CreateBenchmarkAlgorithm(int securityCount, Resolution resolution)
        {
            // determine reasonable start/end dates
            var start = new DateTime(2000, 01, 01);

            // aim for 5MM data points
            var pointsPerSecurity = 5000000 / securityCount;
            var increment = resolution.ToTimeSpan();
            var incrementsPerDay = Time.OneDay.Ticks / increment.Ticks;
            var days = pointsPerSecurity / incrementsPerDay - 1;

            if (days < 0)
            {
                throw new Exception($"Unable to create {securityCount} subscriptions at {resolution} resolution. Consider using a larger resolution.");
            }

            var parameters = new Parameters(securityCount, resolution, start, start.AddDays(days));
            return new EquityBenchmarkAlgorithm(parameters);
        }

        private class SingleSecurity_Second_BenchmarkTest : QCAlgorithm
        {
            public SingleSecurity_Second_BenchmarkTest()
            {
                SubscriptionManager.SetDataManager(new DataManagerStub(this, new MockDataFeed()));
            }

            public override void Initialize()
            {
                SetStartDate(2008, 01, 01);
                SetEndDate(2009, 01, 01);
                SetCash(100000);
                SetBenchmark(time => 0m);
                AddEquity("SPY", Resolution.Second);
            }
        }

        private class FiveHundredSecurity_Second_BenchmarkTest : QCAlgorithm
        {
            public override void Initialize()
            {
                SetStartDate(2018, 02, 01);
                SetEndDate(2018, 02, 01);
                SetCash(100000);
                SetBenchmark(time => 0m);
                foreach (var symbol in QuantConnect.Algorithm.CSharp.Benchmarks.Symbols.Equity.All.Take(500))
                {
                    AddEquity(symbol, Resolution.Second);
                }
            }
        }

        private class EquityBenchmarkAlgorithm : QCAlgorithm
        {
            private readonly Parameters _parameters;

            public EquityBenchmarkAlgorithm(Parameters parameters)
            {
                _parameters = parameters;
            }

            public override void Initialize()
            {
                SetStartDate(_parameters.StartDate);
                SetEndDate(_parameters.EndDate);
                SetBenchmark(time => 0m);

                foreach (var symbol in QuantConnect.Algorithm.CSharp.Benchmarks.Symbols.Equity.All.Take(_parameters.SecurityCount))
                {
                    AddEquity(symbol, _parameters.Resolution);
                }
            }
        }

        public class Parameters
        {
            public int SecurityCount { get; set; }
            public Resolution Resolution { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }

            public Parameters(int securityCount, Resolution resolution, DateTime startDate, DateTime endDate)
            {
                SecurityCount = securityCount;
                Resolution = resolution;
                StartDate = startDate;
                EndDate = endDate;
            }
        }
    }
}