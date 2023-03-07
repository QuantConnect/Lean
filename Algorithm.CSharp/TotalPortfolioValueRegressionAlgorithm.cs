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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm aims to test the TotalPortfolioValue,
    /// verifying its correctly updated (GH issue 3272)
    /// </summary>
    public class  TotalPortfolioValueRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _symbols = new List<Symbol>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(2017, 1, 1);
            SetCash(100000);

            var securitiesToAdd = new List<string>
            {
                "SPY", "AAPL", "AAA", "GOOG", "GOOGL", "IBM", "QQQ", "FB", "WM", "WMI", "BAC", "USO", "IWM", "EEM", "BNO", "AIG"
            };
            foreach (var symbolStr in securitiesToAdd)
            {
                var security = AddEquity(symbolStr, Resolution.Daily);
                security.SetLeverage(100);
                _symbols.Add(security.Symbol);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.Invested)
            {
                Liquidate();
            }
            else
            {
                foreach (var symbol in _symbols)
                {
                    SetHoldings(symbol, 10m / _symbols.Count);
                }

                // We will add some cash just for testing, users should not do this
                var totalPortfolioValueSnapshot = Portfolio.TotalPortfolioValue;
                var accountCurrencyCash = Portfolio.CashBook[AccountCurrency];
                var existingAmount = accountCurrencyCash.Amount;

                // increase cash amount
                Portfolio.CashBook.Add(AccountCurrency, existingAmount * 1.1m, 1);

                if (totalPortfolioValueSnapshot * 1.1m != Portfolio.TotalPortfolioValue)
                {
                    throw new Exception($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot * 1.1m}");
                }

                // lets remove part of what we added
                Portfolio.CashBook[AccountCurrency].AddAmount(-existingAmount * 0.05m);

                if (totalPortfolioValueSnapshot * 1.05m != Portfolio.TotalPortfolioValue)
                {
                    throw new Exception($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot * 1.05m}");
                }

                // lets set amount back to original value
                Portfolio.CashBook[AccountCurrency].SetAmount(existingAmount);
                if (totalPortfolioValueSnapshot != Portfolio.TotalPortfolioValue)
                {
                    throw new Exception($"Unexpected TotalPortfolioValue {Portfolio.TotalPortfolioValue}." +
                        $" Expected: {totalPortfolioValueSnapshot}");
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5345;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3528"},
            {"Average Win", "0.68%"},
            {"Average Loss", "-0.71%"},
            {"Compounding Annual Return", "15.335%"},
            {"Drawdown", "64.100%"},
            {"Expectancy", "0.019"},
            {"Net Profit", "15.335%"},
            {"Sharpe Ratio", "0.704"},
            {"Probabilistic Sharpe Ratio", "29.509%"},
            {"Loss Rate", "48%"},
            {"Win Rate", "52%"},
            {"Profit-Loss Ratio", "0.96"},
            {"Alpha", "0.168"},
            {"Beta", "5"},
            {"Annual Standard Deviation", "0.864"},
            {"Annual Variance", "0.747"},
            {"Information Ratio", "0.651"},
            {"Tracking Error", "0.8"},
            {"Treynor Ratio", "0.122"},
            {"Total Fees", "$21388.54"},
            {"Estimated Strategy Capacity", "$510000.00"},
            {"Lowest Capacity Asset", "BNO UN3IMQ2JU1YD"},
            {"Return Over Maximum Drawdown", "0.239"},
            {"Portfolio Turnover", "7.203"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "de2f80e8ddec591cbcbc37617cc0e999"}
        };
    }
}
