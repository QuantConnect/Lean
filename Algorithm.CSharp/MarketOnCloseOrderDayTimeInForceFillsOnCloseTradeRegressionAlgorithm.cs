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
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that MarketOnClose orders with a Day time in force don't expire at the close
    /// and are filled with the official close price, which the fill model waits for after the close
    /// </summary>
    public class MarketOnCloseOrderDayTimeInForceFillsOnCloseTradeRegressionAlgorithm : MarketOnCloseOrderFillsOnCloseTradeWithTickResolutionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            DefaultOrderProperties.TimeInForce = TimeInForce.Day;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            base.OnOrderEvent(orderEvent);

            if (orderEvent.Status == OrderStatus.Filled)
            {
                var order = Transactions.GetOrderById(orderEvent.OrderId);
                if (order.TimeInForce is not DayTimeInForce)
                {
                    throw new RegressionTestException($"Expected a Day time in force, but was {order.TimeInForce}");
                }

                var exchangeHours = Securities[orderEvent.Symbol].Exchange.Hours;
                var marketClose = exchangeHours.GetNextMarketClose(order.Time.ConvertFromUtc(exchangeHours.TimeZone), false);
                var fillTime = orderEvent.UtcTime.ConvertFromUtc(exchangeHours.TimeZone);
                if (fillTime <= marketClose)
                {
                    throw new RegressionTestException($"Expected the order to fill on the closing trade after the {marketClose} close, but filled at {fillTime}");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999997"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "dbc30717d5a9ecf2b04860c04fa88b0c"}
        };
    }
}
