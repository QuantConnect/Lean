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
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add index asset types.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="indexes" />
    public class BasicTemplateIndiaIndexAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected Symbol Nifty { get; set; }
        protected Symbol NiftyETF { get; set; }
        private ExponentialMovingAverage _emaSlow;
        private ExponentialMovingAverage _emaFast;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetAccountCurrency("INR"); //Set Account Currency
            SetStartDate(2019, 1, 1);  //Set End Date
            SetEndDate(2019, 1, 5);    //Set End Date
            SetCash(1000000);          //Set Strategy Cash

            // Use indicator for signal; but it cannot be traded
            Nifty = AddIndex("NIFTY50", Resolution.Minute, Market.India).Symbol;

            //Trade Index based ETF
            NiftyETF = AddEquity("JUNIORBEES", Resolution.Minute, Market.India).Symbol;

            //Set Order Properties as per the requirements for order placement
            DefaultOrderProperties = new IndiaOrderProperties(exchange: Exchange.NSE);

            _emaSlow = EMA(Nifty, 80);
            _emaFast = EMA(Nifty, 200);
        }

        /// <summary>
        /// Index EMA Cross trading underlying.
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (!slice.Bars.ContainsKey(Nifty) || !slice.Bars.ContainsKey(NiftyETF))
            {
                return;
            }

            // Warm up indicators
            if (!_emaSlow.IsReady)
            {
                return;
            }

            if (_emaFast > _emaSlow)
            {
                if (!Portfolio.Invested)
                {
                    var marketTicket = MarketOrder(NiftyETF, 1);
                }
            }
            else
            {
                Liquidate();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio[Nifty].TotalSaleVolume > 0)
            {
                throw new RegressionTestException("Index is not tradable.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2882;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.386%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "999961.17"},
            {"Net Profit", "-0.004%"},
            {"Sharpe Ratio", "-328.371"},
            {"Sortino Ratio", "-328.371"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-23.595"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "₹36.00"},
            {"Estimated Strategy Capacity", "₹84000.00"},
            {"Lowest Capacity Asset", "JUNIORBEES UL"},
            {"Portfolio Turnover", "0.04%"},
            {"OrderListHash", "8790bec8175539e6d92e01608ac57733"}
        };
    }
}
