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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fills;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example of custom fill model for security to only fill bars of data obtained after the order was placed. This is to encourage more
    /// pessimistic fill models and eliminate the possibility to fill on old market data that may not be relevant.
    /// </summary>
    public class ForwardDataOnlyFillModelAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 01);
            SetEndDate(2013, 10, 31);

            var security = AddEquity("SPY", Resolution.Hour);
            security.SetFillModel(new ForwardDataOnlyFillModel());

            Schedule.On(DateRules.WeekStart(), TimeRules.AfterMarketOpen(security.Symbol), Trade);
        }

        public void Trade()
        {
            if (!Portfolio.Invested)
            {
                if(Time.TimeOfDay != new TimeSpan(9, 30, 0))
                {
                    throw new Exception($"Unexpected event time {Time}");
                }

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
            if (orderEvent.Status == OrderStatus.Filled && (Time.Hour != 10 || Time.Minute != 0))
            {
                throw new Exception($"Unexpected fill time {Time}");
            }
        }

        public class ForwardDataOnlyFillModel : EquityFillModel
        {
            public override Fill Fill(FillModelParameters parameters)
            {
                var orderLocalTime = parameters.Order.Time.ConvertFromUtc(parameters.Security.Exchange.TimeZone);
                foreach (var dataType in new[] { typeof(QuoteBar), typeof(TradeBar), typeof(Tick)})
                {
                    if(parameters.Security.Cache.TryGetValue(dataType, out var data) && data.Count > 0 && orderLocalTime <= data[data.Count - 1].EndTime)
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
            {"Compounding Annual Return", "0.071%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.006%"},
            {"Sharpe Ratio", "3.363"},
            {"Probabilistic Sharpe Ratio", "81.116%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.425"},
            {"Tracking Error", "0.107"},
            {"Treynor Ratio", "0.382"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$62000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "9eac57692ae167cf5be63ad931b8a376"}
        };
    }
}
