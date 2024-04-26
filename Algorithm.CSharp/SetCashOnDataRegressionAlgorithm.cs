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
using QuantConnect.Securities.CurrencyConversion;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression test reproduces the issue where a Cash instance is added
    /// during execution by the BrokerageTransactionHandler, in this case the
    /// algorithm will be adding it in OnData() to reproduce the same scenario.
    /// </summary>
    public class SetCashOnDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private bool _added;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 12, 01);  //Set Start Date
            SetEndDate(2014, 12, 21);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            AddEquity("SPY", Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!_added)
            {
                _added = true;
                // this should not be done by users but could be done by the BrokerageTransactionHandler
                // Users: see and use SetCash()
                Portfolio.CashBook.Add("EUR", 10,0);
            }
            else
            {
                var cash = Portfolio.CashBook["EUR"];
                if (cash.CurrencyConversion.GetType() == typeof(ConstantCurrencyConversion) || cash.ConversionRate == 0)
                {
                    throw new Exception("Expected 'EUR' Cash to be fully set");
                }

                var eurUsdSubscription = SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
                        includeInternalConfigs: true)
                    .Single();
                if (!eurUsdSubscription.IsInternalFeed)
                {
                    throw new Exception("Unexpected not internal 'EURUSD' Subscription");
                }
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
                Debug("Purchased Stock");
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
        public long DataPoints => 144;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "14.647%"},
            {"Drawdown", "4.800%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100819.38"},
            {"Net Profit", "0.819%"},
            {"Sharpe Ratio", "0.717"},
            {"Sortino Ratio", "1.053"},
            {"Probabilistic Sharpe Ratio", "46.877%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.001"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.149"},
            {"Annual Variance", "0.022"},
            {"Information Ratio", "1.091"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "0.108"},
            {"Total Fees", "$2.75"},
            {"Estimated Strategy Capacity", "$520000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "4.50%"},
            {"OrderListHash", "3813889e73d97a288cd4152db7ea5f60"}
        };
    }
}
