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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Future;

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
        private Future _SP500EMini;
        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2013, 10, 12);

            _SP500EMini = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, extendedMarketHours: true);

            Schedule.On(DateRules.EveryDay(), TimeRules.At(19, 0), () =>
            {
                // Don't place orders at the end of the last date, the market-on-stop order won't have time to fill
                if (Time.Date == EndDate.Date.AddDays(-1))
                {
                    return;
                }

                MarketOrder(_SP500EMini.Mapped, 1);
                _ticket = StopMarketOrder(_SP500EMini.Mapped, -1, _SP500EMini.Price * 1.1m);
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
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent == null)
            {
                return;
            }

            if (Transactions.GetOrderById(orderEvent.OrderId).Type != OrderType.StopMarket)
            {
                return;
            }

            if (orderEvent.Status == OrderStatus.Filled)
            {
                var time = MarketHoursDatabase.GetExchangeHours(_SP500EMini.SubscriptionDataConfig);

                if (!time.IsOpen(orderEvent.UtcTime, _SP500EMini.IsExtendedMarketHours))
                {
                    throw new RegressionTestException($"The Exchange hours was closed, verify 'extendedMarketHours' flag in {nameof(Initialize)} when added new security(ies).");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var stopMarketOrders = Transactions.GetOrders(x => x is StopMarketOrder);

            if (stopMarketOrders.Any(x => x.Status != OrderStatus.Filled))
            {
                throw new RegressionTestException("The Algorithms was not handled any StopMarketOrders");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 41486;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "10"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-6.419%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99891"},
            {"Net Profit", "-0.109%"},
            {"Sharpe Ratio", "-22.29"},
            {"Sortino Ratio", "-26.651"},
            {"Probabilistic Sharpe Ratio", "0.016%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.05"},
            {"Beta", "-0.006"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.76"},
            {"Tracking Error", "0.215"},
            {"Treynor Ratio", "8.829"},
            {"Total Fees", "$21.50"},
            {"Estimated Strategy Capacity", "$3400000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "138.95%"},
            {"OrderListHash", "957191893a3de4975ec14b2a3b2490de"}
        };
    }
}
