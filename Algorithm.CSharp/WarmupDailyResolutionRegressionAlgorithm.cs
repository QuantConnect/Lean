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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting warming up with a lower resolution for speed is respected
    /// </summary>
    public class WarmupDailyResolutionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private long _previousSampleCount;
        private bool _warmedUpTradeBars;
        private bool _warmedUpQuoteBars;

        protected SimpleMovingAverage Sma { get; set; }
        protected TimeSpan ExpectedDataSpan { get; set; }
        protected TimeSpan ExpectedWarmupDataSpan { get; set; }

        public override void Initialize()
        {
            SetStartDate(2013, 10, 10);
            SetEndDate(2013, 10, 11);

            AddEquity("SPY", Resolution.Hour);
            ExpectedDataSpan = Resolution.Hour.ToTimeSpan();

            SetWarmUp(TimeSpan.FromDays(3), Resolution.Daily);
            ExpectedWarmupDataSpan = TimeSpan.FromHours(6.5);

            Sma = SMA("SPY", 2);
        }

        public override void OnData(Slice slice)
        {
            if (Sma.Samples <= _previousSampleCount)
            {
                throw new RegressionTestException("Indicator was not updated!");
            }
            _previousSampleCount = Sma.Samples;

            var tradeBars = slice.Get<TradeBar>();
            tradeBars.TryGetValue("SPY", out var trade);

            var quoteBars = slice.Get<QuoteBar>();
            quoteBars.TryGetValue("SPY", out var quote);

            var expectedPeriod = ExpectedDataSpan;
            if (Time <= StartDate)
            {
                expectedPeriod = ExpectedWarmupDataSpan;
                if (trade != null && trade.IsFillForward || quote != null && quote.IsFillForward)
                {
                    throw new RegressionTestException("Unexpected fill forwarded data!");
                }
            }

            if (expectedPeriod == TimeSpan.FromHours(6.5))
            {
                // let's assert the data's time are what we expect
                if (trade != null && trade.EndTime.Hour != 16)
                {
                    throw new RegressionTestException($"Unexpected data end time! {trade.EndTime}");
                }
                if (quote != null && quote.EndTime.Hour != 16)
                {
                    throw new RegressionTestException($"Unexpected data end time! {quote.EndTime}");
                }
            }
            else
            {
                // let's assert the data's time are what we expect
                if (trade != null && trade.EndTime.Ticks % expectedPeriod.Ticks != 0)
                {
                    throw new RegressionTestException($"Unexpected data end time! {trade.EndTime}");
                }
                if (quote != null && quote.EndTime.Ticks % expectedPeriod.Ticks != 0)
                {
                    throw new RegressionTestException($"Unexpected data end time! {quote.EndTime}");
                }
            }

            if (trade != null)
            {
                _warmedUpTradeBars |= IsWarmingUp;
                if (trade.Period != expectedPeriod)
                {
                    throw new RegressionTestException($"Unexpected period for trade data point {trade.Period} expected {expectedPeriod}. IsWarmingUp: {IsWarmingUp}");
                }
            }
            if (quote != null)
            {
                _warmedUpQuoteBars |= IsWarmingUp;
                if (quote.Period != expectedPeriod)
                {
                    throw new RegressionTestException($"Unexpected period for quote data point {quote.Period} expected {expectedPeriod}. IsWarmingUp: {IsWarmingUp}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_warmedUpTradeBars)
            {
                throw new RegressionTestException("Did not assert data during warmup!");
            }

            if (ExpectedWarmupDataSpan == TimeSpan.FromHours(6.5))
            {
                if (_warmedUpQuoteBars)
                {
                    throw new RegressionTestException("We should of not gotten any quote bar during warmup for daily resolution!");
                }
            }
            else if (!_warmedUpQuoteBars)
            {
                throw new RegressionTestException("Did not assert data during warmup!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 36;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
