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
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demo algorithm for manually testing one-cancels-the-other (OCO) order groups end to end against a live
    /// or paper brokerage. Alpaca is the first brokerage to support them - see
    /// Lean.Brokerages.Alpaca/Documentation/ADR/0001-one-cancels-the-other-orders.md for the brokerage-side design.
    /// Buys a small position at market, then places a 2-leg OCO exit (take-profit limit above the entry price,
    /// stop-loss below it) and logs every order event so the outcome is visible in the live log/console.
    /// </summary>
    /// <remarks>
    /// This is a manual/live testing aid, not part of the automated backtest regression suite - it deliberately
    /// does not implement IRegressionAlgorithmDefinition. For the automated backtest version of this scenario,
    /// see OneCancelsTheOtherOrderRegressionAlgorithm.
    /// </remarks>
    public class OneCancelsTheOtherOrderDemoAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private List<OrderTicket> _tickets;

        public override void Initialize()
        {
            // ignored when deployed live; only used if this is run as a quick local backtest sanity check first
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 31);
            SetCash(100000);

            _symbol = AddEquity("AAPL", Resolution.Minute).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (_tickets != null)
            {
                // the OCO exit group has already been placed, nothing left to do
                return;
            }

            if (!Portfolio.Invested)
            {
                Debug("Buying 10 AAPL at market to open the position the OCO exit group will close.");
                MarketOrder(_symbol, 10);
                return;
            }

            // just went long: place the exit as one OCO group (take profit +1%, stop loss -2% from here).
            // Tighten these offsets if you want one leg to trigger quickly for a faster manual test.
            var price = Securities[_symbol].Price;
            var takeProfitLimitPrice = Math.Round(price * 1.01m, 2);
            var stopLossStopPrice = Math.Round(price * 0.98m, 2);

            Debug($"Placing OCO exit group on {_symbol}: sell limit {takeProfitLimitPrice} (take profit) / sell stop {stopLossStopPrice} (stop loss)");

            _tickets = OneCancelsTheOtherOrder(
            [
                new LimitOrder(_symbol, -10, takeProfitLimitPrice, UtcTime),
                new StopMarketOrder(_symbol, -10, stopLossStopPrice, UtcTime)
            ]);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time}: {orderEvent}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (_tickets == null)
            {
                Debug("OCO exit group was never placed.");
                return;
            }

            foreach (var ticket in _tickets)
            {
                Debug($"Final status - Order {ticket.OrderId} ({ticket.OrderType}): {ticket.Status}");
            }
        }
    }
}
