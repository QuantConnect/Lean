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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests that we receive the expected data from
    /// in the option universe filter.
    /// </summary>
    public class AddFutureOptionContractDataStreamingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private bool _optionFilterRan;
        private Future _es;

        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            SetEndDate(2020, 9, 23);

            var es18z20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2020, 12, 18)),
                Resolution.Minute);

            var es19h21 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, new DateTime(2021, 3, 19)),
                Resolution.Minute);

            AddFutureOptionContract(
                QuantConnect.Symbol.CreateOption(es18z20.Symbol, Market.CME, OptionStyle.American, OptionRight.Call, 3280m, new DateTime(2020, 12, 18)),
                Resolution.Minute);

            AddFutureOptionContract(
                QuantConnect.Symbol.CreateOption(es19h21.Symbol, Market.CME, OptionStyle.American, OptionRight.Put, 3700m, new DateTime(2021, 3, 19)),
                Resolution.Minute);
        }

        public override void OnData(Slice data)
        {
            if (_invested || !data.HasData)
            {
                return;
            }

            foreach (var qb in data.QuoteBars.Values)
            {
                if (qb.Symbol.SecurityType != SecurityType.Option)
                {
                    continue;
                }

                Log($"{Time} - {qb}");
            }
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = { Language.CSharp };

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            { "Total Trades", "2" },
            { "Average Win", "0%" },
            { "Average Loss", "0%" },
            { "Compounding Annual Return", "21631.964%" },
            { "Drawdown", "2.500%" },
            { "Expectancy", "0" },
            { "Net Profit", "2.993%" },
            { "Sharpe Ratio", "11.754" },
            { "Probabilistic Sharpe Ratio", "0%" },
            { "Loss Rate", "0%" },
            { "Win Rate", "0%" },
            { "Profit-Loss Ratio", "0" },
            { "Alpha", "0" },
            { "Beta", "0" },
            { "Annual Standard Deviation", "0.008" },
            { "Annual Variance", "0" },
            { "Information Ratio", "11.754" },
            { "Tracking Error", "0.008" },
            { "Treynor Ratio", "0" },
            { "Total Fees", "$3.70" },
            { "Fitness Score", "1" },
            { "Kelly Criterion Estimate", "0" },
            { "Kelly Criterion Probability Value", "0" },
            { "Sortino Ratio", "79228162514264337593543950335" },
            { "Return Over Maximum Drawdown", "79228162514264337593543950335" },
            { "Portfolio Turnover", "1.586" },
            { "Total Insights Generated", "0" },
            { "Total Insights Closed", "0" },
            { "Total Insights Analysis Completed", "0" },
            { "Long Insight Count", "0" },
            { "Short Insight Count", "0" },
            { "Long/Short Ratio", "100%" },
            { "Estimated Monthly Alpha Value", "$0" },
            { "Total Accumulated Estimated Alpha Value", "$0" },
            { "Mean Population Estimated Insight Value", "$0" },
            { "Mean Population Direction", "0%" },
            { "Mean Population Magnitude", "0%" },
            { "Rolling Averaged Population Direction", "0%" },
            { "Rolling Averaged Population Magnitude", "0%" },
            { "OrderListHash", "-1899680538" }
        };
    }
}
