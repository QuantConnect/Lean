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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of payments for cash dividends in backtesting. When data normalization mode is set
    /// to "Raw" the dividends are paid as cash directly into your portfolio.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="data event handlers" />
    /// <meta name="tag" content="dividend event" />
    public class DividendRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private decimal _sumOfDividends;
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(1998, 01, 01);  //Set Start Date
            SetEndDate(2006, 01, 01);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            _symbol = AddEquity("SPY", Resolution.Daily,
                dataNormalizationMode: DataNormalizationMode.Raw).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.Invested) return;
            SetHoldings(_symbol, .5);
        }

        /// <summary>
        /// Raises the data event.
        /// </summary>
        /// <param name="data">Data.</param>
        public override void OnDividends(Dividends data) // update this to Dividends dictionary
        {
            var dividend = data[_symbol];
            var holdings = Portfolio[_symbol];
            Debug($"{dividend.Time.ToStringInvariant("o")} >> DIVIDEND >> {dividend.Symbol} - " +
                $"{dividend.Distribution.ToStringInvariant("C")} - {Portfolio.Cash} - " +
                $"{holdings.Price.ToStringInvariant("C")}"
            );
            _sumOfDividends += dividend.Distribution * holdings.Quantity;
        }
        
        public override void OnEndOfAlgorithm()
        {
            // The expected value refers to sum of dividend payments
            if (Portfolio.TotalProfit != _sumOfDividends)
            {
                throw new Exception($"Total Profit: Expected {_sumOfDividends}. Actual {Portfolio.TotalProfit}");
            }

            var expectNetProfit = _sumOfDividends - Portfolio.TotalFees;
            if (Portfolio.TotalNetProfit != expectNetProfit)
            {
                throw new Exception($"Total Net Profit: Expected {expectNetProfit}. Actual {Portfolio.TotalNetProfit}");
            }

            if (Portfolio[_symbol].TotalDividends != _sumOfDividends)
            {
                throw new Exception($"{_symbol} Total Dividends: Expected {_sumOfDividends}. Actual {Portfolio[_symbol].TotalDividends}");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 16077;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "2.354%"},
            {"Drawdown", "28.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "120462.08"},
            {"Net Profit", "20.462%"},
            {"Sharpe Ratio", "-0.063"},
            {"Sortino Ratio", "-0.078"},
            {"Probabilistic Sharpe Ratio", "0.462%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.015"},
            {"Beta", "0.521"},
            {"Annual Standard Deviation", "0.083"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "-0.328"},
            {"Tracking Error", "0.076"},
            {"Treynor Ratio", "-0.01"},
            {"Total Fees", "$2.56"},
            {"Estimated Strategy Capacity", "$24000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "efe1c97f2ebdd14ee72b57b1b44a8f7a"}
        };
    }
}
