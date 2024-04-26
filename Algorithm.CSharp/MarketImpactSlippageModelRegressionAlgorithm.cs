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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;

namespace QuantConnect.Algorithm.CSharp
{
    public class MarketImpactSlippageModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 13);
            SetCash(10000000);

            var spy = AddEquity("SPY", Resolution.Daily);
            var aapl = AddEquity("AAPL", Resolution.Daily);

            spy.SetSlippageModel(new MarketImpactSlippageModel(this));
            aapl.SetSlippageModel(new MarketImpactSlippageModel(this));
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            SetHoldings("SPY", 0.5d);
            SetHoldings("AAPL", -0.5d);
        }

        /// <summary>
        /// OnOrderEvent is called whenever an order is updated
        /// </summary>
        /// <param name="orderEvent">Order Event</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            { 
                Debug($"Price: {Securities[orderEvent.Symbol].Price}, filled price: {orderEvent.FillPrice}, quantity: {orderEvent.FillQuantity}");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 53;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 506;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "9"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "-92.587%"},
            {"Drawdown", "4.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "10000000"},
            {"End Equity", "9649851.28"},
            {"Net Profit", "-3.501%"},
            {"Sharpe Ratio", "-2.93"},
            {"Sortino Ratio", "-2.869"},
            {"Probabilistic Sharpe Ratio", "7.355%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-3.354"},
            {"Beta", "1.244"},
            {"Annual Standard Deviation", "0.306"},
            {"Annual Variance", "0.094"},
            {"Information Ratio", "-20.199"},
            {"Tracking Error", "0.142"},
            {"Treynor Ratio", "-0.722"},
            {"Total Fees", "$1858.99"},
            {"Estimated Strategy Capacity", "$330000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "21.04%"},
            {"OrderListHash", "25dfeccc74ffa5cb6ae15f6310411d1b"}
        };
    }
}
