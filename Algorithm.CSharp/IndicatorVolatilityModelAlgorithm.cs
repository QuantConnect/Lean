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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Volatility;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm illustrating the usage of the <see cref="IndicatorVolatilityModel"/> and
    /// how to handle splits and dividends to avoid price discontinuities
    /// </summary>
    public class IndicatorVolatilityModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int _indicatorPeriods = 7;

        private const DataNormalizationMode _dataNormalizationMode = DataNormalizationMode.Raw;

        private Symbol _aapl;

        private IIndicator _indicator;

        private int _splitsAndDividendsCount;

        private bool _volatilityChecked;

        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(100000);

            var equity = AddEquity("AAPL", Resolution.Daily, dataNormalizationMode: _dataNormalizationMode);
            _aapl = equity.Symbol;

            var std = new StandardDeviation(_indicatorPeriods);
            var mean = new SimpleMovingAverage(_indicatorPeriods);
            _indicator = std.Over(mean);
            equity.SetVolatilityModel(new IndicatorVolatilityModel(_indicator, (_, data, _) =>
            {
                if (data.Price > 0)
                {
                    std.Update(data.Time, data.Price);
                    mean.Update(data.Time, data.Price);
                }
            }));
        }

        public override void OnData(Slice slice)
        {
            if (slice.Splits.ContainsKey(_aapl) || slice.Dividends.ContainsKey(_aapl))
            {
                _splitsAndDividendsCount++;

                // On a split or dividend event, we need to reset and warm the indicator up as Lean does to BaseVolatilityModel's
                // to avoid big jumps in volatility due to price discontinuities
                _indicator.Reset();
                var equity = Securities[_aapl];
                var volatilityModel = equity.VolatilityModel as IndicatorVolatilityModel;
                volatilityModel.WarmUp(this, equity, equity.Resolution, _indicatorPeriods, _dataNormalizationMode);
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            if (symbol != _aapl || !_indicator.IsReady)
            {
                return;
            }

            _volatilityChecked = true;

            // This is expected only in this case, 0.05 is not a magical number of any kind.
            // Just making sure we don't get big jumps on volatility
            var volatility = Securities[_aapl].VolatilityModel.Volatility;
            if (volatility <= 0 || volatility > 0.05m)
            {
                throw new Exception(
                    "Expected volatility to stay less than 0.05 (not big jumps due to price discontinuities on splits and dividends), " +
                    $"but got {volatility}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_splitsAndDividendsCount == 0)
            {
                throw new Exception("Expected to get at least one split or dividend event");
            }

            if (!_volatilityChecked)
            {
                throw new Exception("Expected to check volatility at least once");
            }
        }

        private IIndicator UpdateIndicator(Security security, TradeBar bar)
        {
            _indicator.Update(bar);

            return _indicator;
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2022;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 42;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.025"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
