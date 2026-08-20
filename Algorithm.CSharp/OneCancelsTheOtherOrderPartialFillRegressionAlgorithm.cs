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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for the reduce rule of a one-cancels-the-other (OCO) order group. It buys 100 SPY and
    /// then places a 2 leg OCO group to sell the same 100 shares back. A custom fill model fills the stop market leg
    /// once, partially, and then stops filling it, so the limit leg is the one that finishes the group. Neither leg
    /// looks at the market price, so the case is reproduced on every run instead of depending on one bar reaching a
    /// trigger price.
    ///
    /// When one leg executes X units, every other open leg must be reduced by X. Here the stop leg executes 30 of
    /// the 100 shares, so the limit leg must shrink from 100 to 70 and sell only 70. Without the reduce the limit
    /// leg would still be for 100 and the group would sell 130 shares while it was only given 100.
    /// </summary>
    public class OneCancelsTheOtherOrderPartialFillRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// The quantity the group is allowed to execute in total, across every leg
        /// </summary>
        private const decimal GroupQuantity = 100m;

        private Symbol _spy;
        private PartialStopFillModel _fillModel;
        private List<OrderTicket> _tickets;

        /// <summary>
        /// How much the whole group has executed so far, across every leg. Both legs are allowed to execute here,
        /// what must never happen is the total passing the quantity the group was given
        /// </summary>
        private decimal _groupExecutedQuantity;

        private bool _stopLegReportedPartialFill;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 10);

            var equity = AddEquity("SPY", Resolution.Hour);
            _spy = equity.Symbol;

            // the stop leg fills 30 shares per bar, the limit leg fills all 100 at once. Neither decision looks at
            // the market price, so the run does not depend on a bar reaching a trigger price
            _fillModel = new PartialStopFillModel(stopSliceQuantity: 30m);
            equity.SetFillModel(_fillModel);
        }

        public override void OnData(Slice slice)
        {
            // trade exactly once
            if (_tickets != null || !slice.ContainsKey(_spy))
            {
                return;
            }

            MarketOrder(_spy, GroupQuantity);

            // both trigger prices sit 30% away from the market and are never reached in this window, so every leg
            // fill in this algorithm comes from the custom fill model and never from the price of a bar
            _tickets = OneCancelsTheOtherOrder(_spy, -GroupQuantity,
                limitPrice: Securities[_spy].Price * 1.30m,
                stopPrice: Securities[_spy].Price * 0.70m);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // OneCancelsTheOtherOrder returns the limit leg first and the stop market leg second
            if (_tickets == null || (orderEvent.OrderId != _tickets[0].OrderId && orderEvent.OrderId != _tickets[1].OrderId))
            {
                // not one of our group legs, for example the entry market order
                return;
            }

            if (!orderEvent.Status.IsFill())
            {
                return;
            }

            if (orderEvent.OrderId == _tickets[1].OrderId && orderEvent.Status == OrderStatus.PartiallyFilled)
            {
                _stopLegReportedPartialFill = true;
            }

            _groupExecutedQuantity += orderEvent.FillQuantity;

            // this is the rule under test. Both legs may execute, but every unit one leg executes is taken off the
            // others, so the group can never execute more than the quantity it was given
            if (Math.Abs(_groupExecutedQuantity) > GroupQuantity)
            {
                throw new RegressionTestException(
                    $"Leg {orderEvent.OrderId} executed {orderEvent.FillQuantity} units and took the one-cancels-the-other " +
                    $"group to {Math.Abs(_groupExecutedQuantity)} units in total, but the group was only given {GroupQuantity}. " +
                    "Every unit a leg executes must be taken off the other legs.");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_tickets == null || _tickets.Count != 2)
            {
                throw new RegressionTestException("Expected the one-cancels-the-other order group to have been placed with 2 legs.");
            }

            if (!_stopLegReportedPartialFill)
            {
                throw new RegressionTestException(
                    "Expected the stop leg to report at least one PartiallyFilled order event, otherwise this algorithm is not " +
                    "testing the partial fill rule at all.");
            }

            var executedQuantity = Math.Abs(_tickets[0].QuantityFilled) + Math.Abs(_tickets[1].QuantityFilled);
            if (executedQuantity != GroupQuantity)
            {
                throw new RegressionTestException(
                    $"The group executed {executedQuantity} units in total but it was given {GroupQuantity}: " +
                    $"limit leg {_tickets[0].QuantityFilled}, stop leg {_tickets[1].QuantityFilled}.");
            }

            // the stop leg executed 30, so the limit leg must have been reduced from 100 to 70. This is the assertion
            // that actually proves the reduce happened, rather than the group simply stopping after the first leg
            var expectedLimitQuantity = -(GroupQuantity - Math.Abs(_tickets[1].QuantityFilled));
            if (_tickets[0].Quantity != expectedLimitQuantity)
            {
                throw new RegressionTestException(
                    $"Expected the limit leg to have been reduced to {expectedLimitQuantity} after the stop leg executed " +
                    $"{_tickets[1].QuantityFilled}, but it is still for {_tickets[0].Quantity}.");
            }

            // the limit leg finishes the group at its reduced size, which completes it and cancels the stop leg
            if (_tickets[0].Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Expected the limit leg to end up Filled, but it was {_tickets[0].Status}.");
            }

            if (_tickets[1].Status != OrderStatus.Canceled)
            {
                throw new RegressionTestException($"Expected the stop leg to be canceled by the group, but it was {_tickets[1].Status}.");
            }

            if (Portfolio.Invested)
            {
                throw new RegressionTestException(
                    $"Expected no open position at the end of the algorithm, but SPY holdings are {Portfolio[_spy].Quantity}.");
            }
        }

        /// <summary>
        /// Fill model that drives both group legs from its own state instead of from the market price: the stop market
        /// leg comes back partially filled in fixed slices until it is complete, and the limit leg comes back
        /// completely filled. Both legs only fill while the exchange is open, so a bar outside market hours leaves the
        /// whole group untouched rather than letting the limit leg fill on its own
        /// </summary>
        private class PartialStopFillModel : ImmediateFillModel
        {
            private readonly decimal _stopSliceQuantity;
            private bool _stopLegFilled;

            public PartialStopFillModel(decimal stopSliceQuantity)
            {
                _stopSliceQuantity = stopSliceQuantity;
            }

            public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
            {
                // a fresh order event carries the order's current status and a zero fill quantity, which is how this
                // model says "no fill this bar"
                var fill = new OrderEvent(order, asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone), OrderFee.Zero);
                if (!IsExchangeOpen(asset, false) || _stopLegFilled)
                {
                    // after the one slice this leg goes quiet, so the limit leg is evaluated on the next bar and
                    // finishes the group at whatever size the reduce left it
                    return fill;
                }

                _stopLegFilled = true;
                fill.FillPrice = asset.Price;
                fill.FillQuantity = Math.Sign(order.Quantity) * Math.Min(_stopSliceQuantity, order.AbsoluteQuantity);
                fill.Status = OrderStatus.PartiallyFilled;

                return fill;
            }

            public override OrderEvent LimitFill(Security asset, LimitOrder order)
            {
                var fill = new OrderEvent(order, asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone), OrderFee.Zero);
                if (!IsExchangeOpen(asset, false))
                {
                    return fill;
                }

                // fills whatever this leg is for at this moment, which is the point: if the reduce worked the leg is
                // for 70 by now, not the 100 it was submitted with
                fill.FillPrice = asset.Price;
                fill.FillQuantity = order.Quantity;
                fill.Status = OrderStatus.Filled;

                return fill;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 106;

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
            {"Total Orders", "3"},
            {"Average Win", "0.08%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "6.272%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100161.25"},
            {"Net Profit", "0.161%"},
            {"Sharpe Ratio", "1.896"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "56.608%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.021"},
            {"Beta", "-0.003"},
            {"Annual Standard Deviation", "0.009"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-7.254"},
            {"Tracking Error", "0.234"},
            {"Treynor Ratio", "-5.838"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$60000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "4.76%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "1ea7cb9b558326235e679b380e31cb01"}
        };
    }
}
