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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add index asset types and trade index options on SPX.
    /// </summary>
    public class BasicTemplateIndexOptionsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spx;
        private ExponentialMovingAverage _emaSlow;
        private ExponentialMovingAverage _emaFast;
        protected virtual Resolution Resolution => Resolution.Minute;
        protected virtual int StartDay => 4;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 1, StartDay);
            SetEndDate(2021, 2, 1);
            SetCash(1000000);

            // Use indicator for signal; but it cannot be traded.
            // We will instead trade on SPX options
            _spx = AddIndex("SPX", Resolution).Symbol;
            var spxOptions = AddIndexOption(_spx, Resolution);
            spxOptions.SetFilter(filterFunc => filterFunc.CallsOnly());

            _emaSlow = EMA(_spx, 80);
            _emaFast = EMA(_spx, 200);

            Settings.DailyStrictEndTimeEnabled = true;
        }

        /// <summary>
        /// Index EMA Cross trading index options of the index.
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (!slice.Bars.ContainsKey(_spx))
            {
                Debug($"No SPX on {Time}");
                return;
            }

            // Warm up indicators
            if (!_emaSlow.IsReady)
            {
                Debug($"EMA slow not ready on {Time}");
                return;
            }

            foreach (var chain in slice.OptionChains.Values)
            {
                foreach (var contract in chain.Contracts.Values)
                {
                    if (contract.Expiry.Month == 3 && contract.Symbol.ID.StrikePrice == 3700m && contract.Right == OptionRight.Call && slice.QuoteBars.ContainsKey(contract.Symbol))
                    {
                        Log($"{Time} {contract.Strike}{(contract.Right == OptionRight.Call ? 'C' : 'P')} -- {slice.QuoteBars[contract.Symbol]}");
                    }

                    if (Portfolio.Invested)
                    {
                        continue;
                    }

                    if (_emaFast > _emaSlow && contract.Right == OptionRight.Call)
                    {
                        Liquidate(InvertOption(contract.Symbol));
                        MarketOrder(contract.Symbol, 1);
                    }
                    else if (_emaFast < _emaSlow && contract.Right == OptionRight.Put)
                    {
                        Liquidate(InvertOption(contract.Symbol));
                        MarketOrder(contract.Symbol, 1);
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio[_spx].TotalSaleVolume > 0)
            {
                throw new Exception("Index is not tradable.");
            }
            if (Portfolio.TotalSaleVolume == 0)
            {
                throw new Exception("Trade volume should be greater than zero by the end of this algorithm");
            }
        }

        public Symbol InvertOption(Symbol symbol)
        {
            return QuantConnect.Symbol.CreateOption(
                symbol.Underlying,
                symbol.ID.Market,
                symbol.ID.OptionStyle,
                symbol.ID.OptionRight == OptionRight.Call ? OptionRight.Put : OptionRight.Call,
                symbol.ID.StrikePrice,
                symbol.ID.Date);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "8220"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-100.000%"},
            {"Drawdown", "13.500%"},
            {"Expectancy", "-0.818"},
            {"Net Profit", "-13.517%"},
            {"Sharpe Ratio", "-2.678"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "89%"},
            {"Win Rate", "11%"},
            {"Profit-Loss Ratio", "0.69"},
            {"Alpha", "4.398"},
            {"Beta", "-0.989"},
            {"Annual Standard Deviation", "0.373"},
            {"Annual Variance", "0.139"},
            {"Information Ratio", "-12.816"},
            {"Tracking Error", "0.504"},
            {"Treynor Ratio", "1.011"},
            {"Total Fees", "$15207.00"},
            {"Estimated Strategy Capacity", "$8800000.00"},
            {"Fitness Score", "0.033"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-8.62"},
            {"Return Over Maximum Drawdown", "-7.81"},
            {"Portfolio Turnover", "302.321"},
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
            {"OrderListHash", "35b3f4b7a225468d42ca085386a2383e"}
        };
    }
}
