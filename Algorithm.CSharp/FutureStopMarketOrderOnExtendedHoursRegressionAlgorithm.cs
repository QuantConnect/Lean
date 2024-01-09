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

using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Continuous Futures Regression algorithm. 
    /// Asserting the behavior of stop market order <see cref="StopMarketOrder"/> in extended market hours 
    /// <seealso cref="Data.UniverseSelection.UniverseSettings.ExtendedMarketHours"/>
    /// </summary>
    public class FutureStopMarketOrderOnExtendedHoursRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _ticket;
        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2013, 10, 12);

            var future = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, extendedMarketHours: true);

            Schedule.On(DateRules.EveryDay(), TimeRules.At(19, 0), () =>
            {
                MarketOrder(future.Mapped, 1);
                _ticket = StopMarketOrder(future.Mapped, -1, future.Price * 0.999m);
            });
        }

        /// <summary>
        /// Data Event Handler: receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (_ticket == null || _ticket.Status != OrderStatus.Submitted)
            {
                return;
            }

            var stopPrice = _ticket.Get(OrderField.StopPrice);
            var bar = Securities[_ticket.Symbol].Cache.GetData<TradeBar>();

            if (stopPrice > bar.Low)
            {
                Log($"{stopPrice} -> {bar.Low}");
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"orderEvent: {orderEvent}");
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
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 75955;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "15368.360%"},
            {"Drawdown", "3.900%"},
            {"Expectancy", "0"},
            {"Net Profit", "8.204%"},
            {"Sharpe Ratio", "120.528"},
            {"Sortino Ratio", "838.746"},
            {"Probabilistic Sharpe Ratio", "93.657%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "60.47"},
            {"Beta", "2.335"},
            {"Annual Standard Deviation", "0.512"},
            {"Annual Variance", "0.262"},
            {"Information Ratio", "199.075"},
            {"Tracking Error", "0.307"},
            {"Treynor Ratio", "26.434"},
            {"Total Fees", "$10.75"},
            {"Estimated Strategy Capacity", "$50000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "69.32%"},
            {"OrderListHash", "eb1e11d1c499d08a6f2f02b17d241e5d"}
        };
    }
}
