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
    /// This regression algorithm tests that we only receive the option chain for a single future contract
    /// in the option universe filter.
    /// </summary>
    public class AddFutureOptionSingleOptionChainSelectedInUniverseFilterRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private bool _optionFilterRan;
        private Future _es;

        public override void Initialize()
        {
            SetStartDate(2020, 9, 22);
            SetEndDate(2020, 9, 23);

            _es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            _es.SetFilter((futureFilter) =>
            {
                return futureFilter.Expiration(0, 365).ExpirationCycle(new[] { 3, 12 });
            });

            AddFutureOption(_es.Symbol, contracts =>
            {
                _optionFilterRan = true;

                var expiry = new HashSet<DateTime>(contracts.Select(x => x.Underlying.ID.Date)).SingleOrDefault();
                // Cast to IEnumerable<Symbol> because OptionFilterContract overrides some LINQ operators like `Select` and `Where`
                // and cause it to mutate the underlying Symbol collection when using those operators.
                var symbol = new HashSet<Symbol>(((IEnumerable<Symbol>)contracts).Select(x => x.Underlying)).SingleOrDefault();

                if (expiry == null || symbol == null)
                {
                    throw new InvalidOperationException("Expected a single Option contract in the chain, found 0 contracts");
                }

                return contracts;
            });
        }

        public override void OnData(Slice data)
        {
            if (_invested || !data.HasData)
            {
                return;
            }

            foreach (var contracts in data.FutureChains.Values)
            {
                foreach (var contract in contracts.Contracts.Values.Select(x => x.Symbol))
                {
                    SetHoldings(contract, 0.25);
                    _invested = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();
            if (!_optionFilterRan)
            {
                throw new InvalidOperationException("Option chain filter was never ran");
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
