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

using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test combo limit orders
    /// </summary>
    public class ComboLimitOrderAlgorithm : ComboOrderAlgorithm
    {
        private decimal _limitPrice;
        private int _comboQuantity;

        private int _fillCount;

        private decimal _liquidatedQuantity;

        private bool _liquidated;

        protected override int ExpectedFillCount
        {
            get
            {
                // times 2 because of liquidation
                return OrderLegs.Count * 2;
            }
        }

        protected override IEnumerable<OrderTicket> PlaceComboOrder(List<Leg> legs, int quantity, decimal? limitPrice)
        {
            _limitPrice = limitPrice.Value;
            _comboQuantity = quantity;

            legs.ForEach(x => { x.OrderPrice = null; });

            return ComboLimitOrder(legs, quantity, _limitPrice);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            base.OnOrderEvent(orderEvent);

            if (orderEvent.Status == OrderStatus.Filled)
            {
                _fillCount++;
                if (_fillCount == OrderLegs.Count)
                {
                    Liquidate();
                }
                else if (_fillCount < 2 * OrderLegs.Count)
                {
                    _liquidatedQuantity += orderEvent.FillQuantity;
                }
                else if (_fillCount == 2 * OrderLegs.Count)
                {
                    _liquidated = true;
                    var totalComboQuantity = _comboQuantity * OrderLegs.Select(x => x.Quantity).Sum();

                    if (_liquidatedQuantity != totalComboQuantity)
                    {
                        throw new Exception($"Liquidated quantity {_liquidatedQuantity} does not match combo quantity {totalComboQuantity}");
                    }

                    if (Portfolio.TotalHoldingsValue != 0)
                    {
                        throw new Exception($"Portfolio value {Portfolio.TotalPortfolioValue} is not zero");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();

            if (_limitPrice == null)
            {
                throw new Exception("Limit price was not set");
            }

            var fillPricesSum = FillOrderEvents.Take(OrderLegs.Count).Select(x => x.FillPrice * x.FillQuantity / _comboQuantity).Sum();
            if (_limitPrice < fillPricesSum)
            {
                throw new Exception($"Limit price expected to be greater that the sum of the fill prices ({fillPricesSum}), but was {_limitPrice}");
            }

            if (!_liquidated)
            {
                throw new Exception("Combo order was not liquidated");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 475788;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$85.00"},
            {"Estimated Strategy Capacity", "$5000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "60.92%"},
            {"OrderListHash", "186986b27fac082600a064a8a59df604"}
        };
    }
}
