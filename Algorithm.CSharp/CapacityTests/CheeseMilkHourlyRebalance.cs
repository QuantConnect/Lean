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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests an illiquid asset that has bursts of liquidity around 11:00 A.M. Central Time
    /// with an hourly in and out strategy.
    /// </summary>
    public class CheeseMilkHourlyRebalance : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private Symbol _contract;
        private DateTime _lastTrade;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 2, 17);
            SetTimeZone(TimeZones.Chicago);
            SetCash(100000);
            SetWarmup(1000);

            var dc = AddFuture("DC", Resolution.Minute, Market.CME);
            dc.SetFilter(0, 10000);
        }

        public override void OnData(Slice data)
        {
            var contract = data.FutureChains.Values.SelectMany(c => c.Contracts.Values)
                .OrderBy(c => c.Symbol.ID.Date)
                .FirstOrDefault()?
                .Symbol;

            if (contract == null)
            {
                return;
            }

            if (_contract != contract || (_fast == null && _slow == null))
            {
                _fast = EMA(contract, 600);
                _slow = EMA(contract, 1200);
                _contract = contract;
            }

            if (!_fast.IsReady || !_slow.IsReady)
            {
                return;
            }

            if (Time - _lastTrade <= TimeSpan.FromHours(1) || Time.TimeOfDay <= new TimeSpan(10, 50, 0) || Time.TimeOfDay >= new TimeSpan(12, 30, 0))
            {
                return;
            }

            if (!Portfolio.ContainsKey(contract) || (Portfolio[contract].Quantity <= 0 && _fast > _slow))
            {
                SetHoldings(contract, 0.5);
                _lastTrade = Time;
            }
            else if (Portfolio.ContainsKey(contract) && Portfolio[contract].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(contract, -0.5);
                _lastTrade = Time;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "19"},
            {"Average Win", "39.16%"},
            {"Average Loss", "-8.81%"},
            {"Compounding Annual Return", "-99.857%"},
            {"Drawdown", "82.900%"},
            {"Expectancy", "-0.359"},
            {"Net Profit", "-57.725%"},
            {"Sharpe Ratio", "-0.555"},
            {"Probabilistic Sharpe Ratio", "10.606%"},
            {"Loss Rate", "88%"},
            {"Win Rate", "12%"},
            {"Profit-Loss Ratio", "4.45"},
            {"Alpha", "-1.188"},
            {"Beta", "0.603"},
            {"Annual Standard Deviation", "1.754"},
            {"Annual Variance", "3.075"},
            {"Information Ratio", "-0.759"},
            {"Tracking Error", "1.753"},
            {"Treynor Ratio", "-1.612"},
            {"Total Fees", "$2558.55"},
            {"Estimated Strategy Capacity", "$20000.00"},
            {"Fitness Score", "0.351"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.602"},
            {"Return Over Maximum Drawdown", "-1.415"},
            {"Portfolio Turnover", "14.226"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "4f5fd2fb25e957bd0cb7cb6d275ddb97"}
        };
    }
}
