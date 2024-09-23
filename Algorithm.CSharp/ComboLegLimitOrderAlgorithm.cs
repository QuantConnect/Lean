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
    /// Regression algorithm to test combo leg limit orders
    /// </summary>
    public class ComboLegLimitOrderAlgorithm : ComboOrderAlgorithm
    {
        private List<decimal> _originalLimitPrices = new();

        protected override IEnumerable<OrderTicket> PlaceComboOrder(List<Leg> legs, int quantity, decimal? limitPrice = null)
        {
            foreach (var leg in legs)
            {
                _originalLimitPrices.Add(leg.OrderPrice.Value);
                leg.OrderPrice *= 2; // Won't fill
            }

            return ComboLegLimitOrder(legs, quantity);
        }

        protected override void UpdateComboOrder(List<OrderTicket> tickets)
        {
            // Let's updated the limit prices to the original values
            for (int i = 0; i < tickets.Count; i++)
            {
                tickets[i].Update(new UpdateOrderFields { LimitPrice = _originalLimitPrices[i] });
            }
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();

            if (FillOrderEvents.Zip(OrderLegs).Any(x => x.Second.OrderPrice < x.First.FillPrice))
            {
                throw new RegressionTestException($"Limit price expected to be greater that the fill price for each order. Limit prices: {string.Join(",", OrderLegs.Select(x => x.OrderPrice))} Fill prices: {string.Join(",", FillOrderEvents.Select(x => x.FillPrice))}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 15023;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public override AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "198524"},
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
            {"Total Fees", "$26.00"},
            {"Estimated Strategy Capacity", "$58000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "30.22%"},
            {"OrderListHash", "ab6171073cd96df46fd9d7bce62f5594"}
        };
    }
}
