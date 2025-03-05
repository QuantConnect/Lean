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
using QuantConnect.Commands;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of different callback commands call
    /// </summary>
    public class CallbackCommandRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            AddEquity("SPY");
            AddEquity("BAC");
            AddEquity("IBM");
            AddCommand<BoolCommand>();
            AddCommand<VoidCommand>();

            var potentialCommand = new VoidCommand
            {
                Target = new[] { "BAC" },
                Quantity = 10,
                Parameters = new() { { "tag", "Signal X" } }
            };
            var commandLink = Link(potentialCommand);
            Notify.Email("email@address", "Trade Command Event", $"Signal X trade\nFollow link to trigger: {commandLink}");

            var commandLink2 = Link(new { Symbol = "SPY", Parameters = new Dictionary<string, int>() { { "Quantity", 10 } } });
            Notify.Email("email@address", "Untyped Command Event", $"Signal Y trade\nFollow link to trigger: {commandLink2}");

            // We need to create a project on QuantConnect to test the BroadcastCommand method
            // and use the ProjectId in the BroadcastCommand call
            ProjectId = 21805137;

            // All live deployments receive the broadcasts below
            var broadcastResult = BroadcastCommand(potentialCommand);
            var broadcastResult2 = BroadcastCommand(new { Symbol = "SPY", Parameters = new Dictionary<string, int>() { { "Quantity", 10 } } });
        }

        /// <summary>
        /// Handle generic command callback
        /// </summary>
        public override bool? OnCommand(dynamic data)
        {
            Buy(data.Symbol, data.parameters["quantity"]);
            return true;
        }

        private class VoidCommand : Command
        {
            public DateTime TargetTime { get; set; }
            public string[] Target { get; set; }
            public decimal Quantity { get; set; }
            public Dictionary<string, string> Parameters { get; set; }
            public override bool? Run(IAlgorithm algorithm)
            {
                if (TargetTime != algorithm.Time)
                {
                    return null;
                }

                ((QCAlgorithm)algorithm).Order(Target[0], Quantity, tag: Parameters["tag"]);
                return null;
            }
        }
        private class BoolCommand : Command
        {
            public bool? Result { get; set; }
            public override bool? Run(IAlgorithm algorithm)
            {
                var shouldTrade = MyCustomMethod();
                if (shouldTrade.HasValue && shouldTrade.Value)
                {
                    ((QCAlgorithm)algorithm).Buy("IBM", 1);
                }
                return shouldTrade;
            }

            private bool? MyCustomMethod()
            {
                return Result;
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "271.453%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101691.92"},
            {"Net Profit", "1.692%"},
            {"Sharpe Ratio", "8.854"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.609%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.005"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.222"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-14.565"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.97"},
            {"Total Fees", "$3.44"},
            {"Estimated Strategy Capacity", "$56000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.93%"},
            {"OrderListHash", "3da9fa60bf95b9ed148b95e02e0cfc9e"}
        };
    }
}
