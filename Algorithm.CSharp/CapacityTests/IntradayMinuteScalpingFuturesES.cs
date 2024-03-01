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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Scalps ES futures contracts (E-mini SP500) using an EMA cross strategy at minute resolution.
    /// This tests futures strategies that trade at a higher frequency, which
    /// should have a reduced capacity estimate as a result.
    /// </summary>
    /// <remarks>
    /// The insanely high capacity estimate of this strategy is realistic.
    /// ES notional contract value traded is around $600 Billion USD per day (!!!), which
    /// is what the capacity is set to.
    /// </remarks>
    public class IntradayMinuteScalpingFuturesES : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private Symbol _contract;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);
            SetWarmup(1000);

            var a = AddFuture("ES", Resolution.Minute, Market.CME);
            a.SetFilter(0, 10000);
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
                _fast = EMA(contract, 10);
                _slow = EMA(contract, 20);
                _contract = contract;
            }

            if (!_fast.IsReady || !_slow.IsReady)
            {
                return;
            }

            if (!Portfolio.ContainsKey(contract) || (Portfolio[contract].Quantity <= 0 && _fast > _slow))
            {
                SetHoldings(contract, 1);
            }
            else if (Portfolio.ContainsKey(contract) && Portfolio[contract].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(contract, -1);
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
            {"Total Orders", "1217"},
            {"Average Win", "2.69%"},
            {"Average Loss", "-0.93%"},
            {"Compounding Annual Return", "-99.756%"},
            {"Drawdown", "77.200%"},
            {"Expectancy", "-0.047"},
            {"Net Profit", "-40.013%"},
            {"Sharpe Ratio", "-0.52"},
            {"Probabilistic Sharpe Ratio", "19.865%"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "2.88"},
            {"Alpha", "-1.279"},
            {"Beta", "-3.686"},
            {"Annual Standard Deviation", "1.85"},
            {"Annual Variance", "3.422"},
            {"Information Ratio", "-0.463"},
            {"Tracking Error", "1.895"},
            {"Treynor Ratio", "0.261"},
            {"Total Fees", "$19843.10"},
            {"Estimated Strategy Capacity", "$560000000.00"},
            {"Fitness Score", "0.334"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.837"},
            {"Return Over Maximum Drawdown", "-1.402"},
            {"Portfolio Turnover", "1174.125"},
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
            {"OrderListHash", "f353843132df7b0604eff3a37b134ca2"}
        };
    }
}
