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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression test showcasing an algorithm without setting an <see cref="AlphaModel"/>,
    /// directly calling <see cref="QCAlgorithm.EmitInsights"/> and <see cref="QCAlgorithm.SetHoldings"/>.
    /// Note that calling <see cref="QCAlgorithm.SetHoldings"/> is useless because
    /// next time Lean calls the Portfolio construction model it will counter it with another order
    /// since it only knows of the emitted insights
    /// </summary>
    public class EmitInsightNoAlphaModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _symbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // set algorithm framework models except ALPHA
            SetUniverseSelection(new ManualUniverseSelectionModel(_symbol));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                var order = Transactions.GetOpenOrders(_symbol).FirstOrDefault();

                if (order != null)
                {
                    throw new Exception($"Unexpected open order {order}");
                }

                EmitInsights(Insight.Price(_symbol, Resolution.Daily, 10, InsightDirection.Down));

                // emitted insight should have triggered a new order
                order = Transactions.GetOpenOrders(_symbol).FirstOrDefault();

                if (order == null)
                {
                    throw new Exception("Expected open order for emitted insight");
                }
                if (order.Direction != OrderDirection.Sell
                    || order.Symbol != _symbol)
                {
                    throw new Exception($"Unexpected open order for emitted insight: {order}");
                }

                SetHoldings(_symbol, 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var holdings = Securities[_symbol].Holdings;
            if (Math.Sign(holdings.Quantity) != -1)
            {
                throw new Exception("Unexpected holdings");
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
        public long DataPoints => 48;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-72.241%"},
            {"Drawdown", "2.900%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "98259.71"},
            {"Net Profit", "-1.740%"},
            {"Sharpe Ratio", "-3.018"},
            {"Sortino Ratio", "-3.766"},
            {"Probabilistic Sharpe Ratio", "24.616%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.301"},
            {"Beta", "-0.998"},
            {"Annual Standard Deviation", "0.222"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-5.95"},
            {"Tracking Error", "0.445"},
            {"Treynor Ratio", "0.672"},
            {"Total Fees", "$19.23"},
            {"Estimated Strategy Capacity", "$540000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "100.02%"},
            {"OrderListHash", "2f1cb4d9ca245eeab8ff432e85b4f221"}
        };
    }
}
