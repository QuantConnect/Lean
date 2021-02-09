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
                if (cash.ConversionRateSecurity == null
                    || cash.ConversionRate == 0)
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "16.445%"},
            {"Drawdown", "4.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.913%"},
            {"Sharpe Ratio", "0.903"},
            {"Probabilistic Sharpe Ratio", "48.314%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.113"},
            {"Beta", "0.203"},
            {"Annual Standard Deviation", "0.156"},
            {"Annual Variance", "0.024"},
            {"Information Ratio", "0.001"},
            {"Tracking Error", "0.198"},
            {"Treynor Ratio", "0.697"},
            {"Total Fees", "$2.60"},
            {"Fitness Score", "0.041"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "1.617"},
            {"Return Over Maximum Drawdown", "3.406"},
            {"Portfolio Turnover", "0.052"},
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
            {"OrderListHash", "93ee31f1512205370510e22b1b1a2a0a"}
        };
    }
}
