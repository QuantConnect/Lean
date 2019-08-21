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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm with a custom universe and benchmark, both using the same security.
    /// </summary>
    public class CustomUniverseWithBenchmarkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int ExpectedLeverage = 2;
        private Symbol _spy;
        private decimal _previousBenchmarkValue;
        private DateTime _previousTime;
        private decimal _previousSecurityValue;
        private bool _universeSelected;
        private bool _onDataWasCalled;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 4);
            SetEndDate(2013, 10, 11);

            // Hour resolution
            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            // Minute resolution
            AddUniverse("my-universe", x =>
                {
                    if(x.Day % 2 == 0)
                    {
                        _universeSelected = true;
                        return new List<string> {"SPY"};
                    }
                    _universeSelected = false;
                    return Enumerable.Empty<string>();
                }
            );

            // internal daily resolution
            SetBenchmark("SPY");

            Symbol symbol;
            if (!SymbolCache.TryGetSymbol("SPY", out symbol)
                || !ReferenceEquals(_spy, symbol))
            {
                throw new Exception("We expected 'SPY' to be added to the Symbol cache," +
                                    " since the algorithm is also using it");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            var security = Securities[_spy];
            _onDataWasCalled = true;

            var bar = data.Bars.Values.Single();
            if (_universeSelected)
            {
                if (bar.IsFillForward
                    || bar.Period != TimeSpan.FromMinutes(1))
                {
                    // bar should always be the Minute resolution one here
                    throw new Exception("Unexpected Bar error");
                }
                if (_previousTime.Date == data.Time.Date
                    && (data.Time - _previousTime) != TimeSpan.FromMinutes(1))
                {
                    throw new Exception("For the same date expected data updates every 1 minute");
                }
            }
            else
            {
                if (data.Time.Minute == 0
                    && _previousSecurityValue == security.Price)
                {
                    throw new Exception($"Security Price error. Price should change every new hour");
                }
                if (data.Time.Minute != 0
                    && _previousSecurityValue != security.Price)
                {
                    throw new Exception($"Security Price error. Price should not change every minute");
                }
            }
            _previousSecurityValue = security.Price;

            // assert benchmark updates only on date change
            var currentValue = Benchmark.Evaluate(data.Time);
            if (_previousTime.Date == data.Time.Date)
            {
                if (currentValue != _previousBenchmarkValue)
                {
                    throw new Exception($"Benchmark value error - expected: {_previousBenchmarkValue}, actual: {currentValue}. " +
                                        "Benchmark value should only change when there is a date change");
                }
            }
            else
            {
                if (currentValue == _previousBenchmarkValue)
                {
                    throw new Exception($"Benchmark value error - expected a new value, current {currentValue}" +
                                        "Benchmark value should change when there is a date change");
                }
            }
            _previousBenchmarkValue = currentValue;
            _previousTime = data.Time;

            // assert algorithm security is the correct one - not the internal one
            if (security.Leverage != ExpectedLeverage)
            {
                throw new Exception($"Leverage error - expected: {ExpectedLeverage}, actual: {security.Leverage}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_onDataWasCalled)
            {
                throw new Exception("OnData was not called");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"}
        };
    }
}