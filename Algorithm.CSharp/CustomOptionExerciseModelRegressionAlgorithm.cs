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
 *
*/

using System.Collections.Generic;
using QuantConnect.Securities.Option;
using QuantConnect.Orders.OptionExercise;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting we can specify a custom option exercise model
    /// </summary>
    public class CustomOptionExerciseModelRegressionAlgorithm : OptionAssignmentRegressionAlgorithm
    {
        public override void Initialize()
        {
            SetSecurityInitializer((security) =>
            {
                var option = security as Option;
                option?.SetOptionExerciseModel(new CustomOptionExerciseModel());
            });

            base.Initialize();
        }

        private class CustomOptionExerciseModel : DefaultExerciseModel
        {
            public override IEnumerable<OrderEvent> OptionExercise(Option option, OptionExerciseOrder order)
            {
                yield return new OrderEvent(order.Id,
                    option.Symbol,
                    option.LocalTime.ConvertToUtc(option.Exchange.TimeZone),
                    OrderStatus.Filled,
                    Extensions.GetOrderDirection(order.Quantity),
                    0.0m,
                    order.Quantity,
                    OrderFee.Zero,
                    "Tag")
                {
                    IsAssignment = false
                };
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "32"},
            {"Average Win", "6.14%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "26116903817855100000000000000%"},
            {"Drawdown", "0.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "257114"},
            {"Net Profit", "157.114%"},
            {"Sharpe Ratio", "107.743"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "95.713%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "60.088"},
            {"Beta", "-19.374"},
            {"Annual Standard Deviation", "0.593"},
            {"Annual Variance", "0.351"},
            {"Information Ratio", "106.234"},
            {"Tracking Error", "0.603"},
            {"Treynor Ratio", "-3.295"},
            {"Total Fees", "$16.00"},
            {"Estimated Strategy Capacity", "$87000.00"},
            {"Lowest Capacity Asset", "GOOCV 305RBQ20WHPNQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "10.93%"},
            {"OrderListHash", "3c5f9544f697725475491434f18803e6"}
        };
    }
}

