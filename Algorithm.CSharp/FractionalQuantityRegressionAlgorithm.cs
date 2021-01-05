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
using System;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for fractional forex pair
    /// </summary>
    public class FractionalQuantityRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2015, 11, 12);
            SetEndDate(2016, 04, 01);

            //Set the cash for the strategy:
            SetCash(100000);
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            SetTimeZone(NodaTime.DateTimeZone.Utc);
            var security = AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Daily, Market.GDAX, false, 1, true);

            // The default buying power model for the Crypto security type is now CashBuyingPowerModel.
            // Since this test algorithm uses leverage we need to set a buying power model with margin.
            security.SetBuyingPowerModel(new SecurityMarginModel(3.3m));

            var con = new TradeBarConsolidator(1);
            SubscriptionManager.AddConsolidator("BTCUSD", con);
            con.DataConsolidated += DataConsolidated;
            SetBenchmark(security.Symbol);
        }

        private void DataConsolidated(object sender, TradeBar e)
        {
            var quantity = Math.Truncate((Portfolio.Cash + Portfolio.TotalFees) / Math.Abs(e.Value + 1));
            if (!Portfolio.Invested)
            {
                Order("BTCUSD", quantity);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity)
            {
                Order("BTCUSD", 0.1);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity + 0.1m)
            {
                Order("BTCUSD", 0.01);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity + 0.11m)
            {
                Order("BTCUSD", -0.02);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity + 0.09m)
            {
                //should fail (below minimum order quantity)
                Order("BTCUSD", 0.00001);

                SetHoldings("BTCUSD", -2.0m);
                SetHoldings("BTCUSD", 2.0m);
                Quit();
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "6"},
            {"Average Win", "6.02%"},
            {"Average Loss", "-2.40%"},
            {"Compounding Annual Return", "915.480%"},
            {"Drawdown", "5.500%"},
            {"Expectancy", "1.338"},
            {"Net Profit", "11.400%"},
            {"Sharpe Ratio", "9.507"},
            {"Probabilistic Sharpe Ratio", "76.768%"},
            {"Loss Rate", "33%"},
            {"Win Rate", "67%"},
            {"Profit-Loss Ratio", "2.51"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.507"},
            {"Annual Variance", "0.257"},
            {"Information Ratio", "9.507"},
            {"Tracking Error", "0.507"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2651.01"},
            {"Fitness Score", "0.467"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "53.455"},
            {"Return Over Maximum Drawdown", "165.408"},
            {"Portfolio Turnover", "0.468"},
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
            {"OrderListHash", "-89452746"}
        };
    }
}
