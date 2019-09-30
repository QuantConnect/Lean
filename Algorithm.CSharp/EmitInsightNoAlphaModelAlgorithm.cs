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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-72.251%"},
            {"Drawdown", "2.800%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-1.741%"},
            {"Sharpe Ratio", "-4.242"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.414"},
            {"Beta", "-0.87"},
            {"Annual Standard Deviation", "0.171"},
            {"Annual Variance", "0.029"},
            {"Information Ratio", "-3.04"},
            {"Tracking Error", "0.356"},
            {"Treynor Ratio", "0.833"},
            {"Total Fees", "$10.77"},
            {"Total Insights Generated", "1"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "1"},
            {"Long/Short Ratio", "0%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}
