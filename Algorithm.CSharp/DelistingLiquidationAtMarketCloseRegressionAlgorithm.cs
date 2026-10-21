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
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that an equity held through its delisting is liquidated with a market on close order
    /// at the close of its last trading day, instead of at midnight once delisted
    /// </summary>
    public class DelistingLiquidationAtMarketCloseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        /// <summary>
        /// The ticker of the equity that gets delisted
        /// </summary>
        protected virtual string Ticker => "AAA.1";

        /// <summary>
        /// The resolution of the delisted equity data
        /// </summary>
        protected virtual Resolution Resolution => Resolution.Daily;

        /// <summary>
        /// The close of the last trading day of the delisted equity, in exchange time
        /// </summary>
        protected virtual DateTime ExpectedLiquidationTime => new DateTime(2007, 5, 18, 16, 0, 0);

        /// <summary>
        /// The algorithm start date, before the delisting
        /// </summary>
        protected virtual DateTime Start => new DateTime(2007, 5, 15);

        /// <summary>
        /// The algorithm end date, after the delisting
        /// </summary>
        protected virtual DateTime End => new DateTime(2007, 5, 25);

        public override void Initialize()
        {
            SetStartDate(Start);
            SetEndDate(End);
            SetCash(100000);

            _symbol = AddEquity(Ticker, Resolution).Symbol;
        }

        public override void OnData(Slice slice)
        {
            // buy once and hold through the delisting, so the engine has to liquidate the position
            if (Transactions.OrdersCount == 0 && slice.Bars.ContainsKey(_symbol))
            {
                SetHoldings(_symbol, 0.5);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var liquidations = Transactions.GetOrders(order => order.Tag == "Liquidate from delisting").ToList();
            if (liquidations.Count != 1)
            {
                throw new RegressionTestException($"Expected a single delisting liquidation, got {liquidations.Count}");
            }

            var liquidation = liquidations[0];
            if (liquidation.Type != OrderType.MarketOnClose || liquidation.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Expected a filled MarketOnClose delisting liquidation, got a {liquidation.Status} {liquidation.Type} order");
            }

            var security = Securities[_symbol];
            var fillTime = liquidation.LastFillTime.Value.ConvertFromUtc(security.Exchange.TimeZone);
            if (fillTime != ExpectedLiquidationTime)
            {
                throw new RegressionTestException($"Expected the delisting liquidation to fill at {ExpectedLiquidationTime}, the close of the last trading day, but it filled at {fillTime}");
            }
            if (!security.Exchange.Hours.IsOpen(fillTime.AddTicks(-1), extendedMarketHours: false))
            {
                throw new RegressionTestException($"Expected the delisting liquidation to fill during regular market hours, but it filled at {fillTime}");
            }
            if (Portfolio[_symbol].Invested)
            {
                throw new RegressionTestException($"Expected the {_symbol} position to be liquidated");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 78;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-2.26%"},
            {"Compounding Annual Return", "-54.214%"},
            {"Drawdown", "2.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "97742.95"},
            {"Net Profit", "-2.257%"},
            {"Sharpe Ratio", "-8.488"},
            {"Sortino Ratio", "-26.612"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.412"},
            {"Beta", "-0.394"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-5.95"},
            {"Tracking Error", "0.121"},
            {"Treynor Ratio", "1.267"},
            {"Total Fees", "$18.35"},
            {"Estimated Strategy Capacity", "$110000.00"},
            {"Lowest Capacity Asset", "AAA SEVKGI6HF885"},
            {"Portfolio Turnover", "9.02%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "e6a8f2b8eaca5a1e9a8d6323fc28f2cc"}
        };
    }
}
