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

using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert we can update indicators that inherit from <see cref="IndicatorBase"/> with RenkoBar's
    /// </summary>
    public class IndicatorWithRenkoBarsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private MassIndex _mi;
        private WilderAccumulativeSwingIndex _wasi;
        private WilderSwingIndex _wsi;
        private Beta _b;
        private List<IIndicator> _indicators;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 09);

            AddEquity("SPY");
            AddEquity("AIG");

            var spyRenkoBarConsolidator = new RenkoConsolidator<TradeBar>(0.1m);
            spyRenkoBarConsolidator.DataConsolidated += OnSPYDataConsolidated;

            var aigRenkoBarConsolidator = new RenkoConsolidator<TradeBar>(0.05m);
            aigRenkoBarConsolidator.DataConsolidated += OnAIGDataConsolidated;

            SubscriptionManager.AddConsolidator("SPY", spyRenkoBarConsolidator);
            SubscriptionManager.AddConsolidator("AIG", aigRenkoBarConsolidator);

            _mi = new MassIndex("MassIndex", 9, 25);
            _wasi = new WilderAccumulativeSwingIndex("WilderAccumulativeSwingIndex", 8);
            _wsi = new WilderSwingIndex("WilderSwingIndex", 8);
            _b = new Beta("Beta", "AIG", "SPY", 3);
            _indicators = new List<IIndicator>() { _mi, _wasi, _wsi, _b };
        }

        public void OnSPYDataConsolidated(object sender, RenkoBar renkoBar)
        {
            _mi.Update(renkoBar);
            _wasi.Update(renkoBar);
            _wsi.Update(renkoBar);
            _b.Update(renkoBar);
        }

        public void OnAIGDataConsolidated(object sender, RenkoBar renkoBar)
        {
            _b.Update(renkoBar);
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var indicator in _indicators)
            {
                if (!indicator.IsReady)
                {
                    throw new Exception($"{indicator.Name} indicator should be ready");
                }
                else if (indicator.Current.Value == 0)
                {
                    throw new Exception($"The current value of {indicator.Name} indicator should be different than zero");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4709;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "5.524"},
            {"Tracking Error", "0.136"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
