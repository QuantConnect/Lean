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
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fills;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// 
    /// </summary>
    public class ForwardDataOnlyFillModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);
            SetEndDate(2013, 10, 31);

            var security = AddEquity("SPY", Resolution.Hour);
            security.SetFillModel(new ForwardDataOnlyFillModel());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                var ticket = Buy("SPY", 1);
                if(ticket.Status != OrderStatus.Submitted)
                {
                    throw new Exception($"Unexpected order status {ticket.Status}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"OnOrderEvent:: {orderEvent}");
        }

        public class ForwardDataOnlyFillModel : EquityFillModel
        {
            public override Fill Fill(FillModelParameters parameters)
            {
                var orderLocalTime = parameters.Order.Time.ConvertFromUtc(parameters.Security.Exchange.TimeZone);
                foreach (var dataType in new[] { typeof(QuoteBar), typeof(TradeBar), typeof(Tick)})
                {
                    if(parameters.Security.Cache.TryGetValue(dataType, out var data) && data.Count > 0 && orderLocalTime < data[data.Count - 1].EndTime)
                    {
                        return base.Fill(parameters);
                    }
                }
                return new Fill(new List<OrderEvent>());
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
        public long DataPoints => 330;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.056%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.005%"},
            {"Sharpe Ratio", "2.93"},
            {"Probabilistic Sharpe Ratio", "75.802%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.426"},
            {"Tracking Error", "0.107"},
            {"Treynor Ratio", "0.315"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$100000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "f06d138df73f4100d3c14a1b428beffc"}
        };
    }
}
