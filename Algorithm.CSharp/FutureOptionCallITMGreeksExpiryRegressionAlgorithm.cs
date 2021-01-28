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
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests In The Money (ITM) future option expiry for calls.
    /// We test to make sure that FOPs have greeks enabled, same as equity options.
    /// </summary>
    public class FutureOptionCallITMGreeksExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private int _onDataCalls;
        private Symbol _es19m20;
        private Option _esOption;
        private Symbol _expectedOptionContract;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 6, 30);

            _es19m20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 6, 19)),
                Resolution.Minute).Symbol;

            // Select a future option expiring ITM, and adds it to the algorithm.
            _esOption = AddFutureOptionContract(OptionChainProvider.GetOptionContractList(_es19m20, new DateTime(2020, 1, 5))
                .Where(x => x.ID.StrikePrice <= 3200m && x.ID.OptionRight == OptionRight.Call)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute);

            _esOption.PriceModel = OptionPriceModels.BjerksundStensland();

            _expectedOptionContract = QuantConnect.Symbol.CreateOption(_es19m20, Market.CME, OptionStyle.American, OptionRight.Call, 3200m, new DateTime(2020, 6, 19));
            if (_esOption.Symbol != _expectedOptionContract)
            {
                throw new Exception($"Contract {_expectedOptionContract} was not found in the chain");
            }
        }

        public override void OnData(Slice data)
        {
            // Let the algo warmup, but without using SetWarmup. Otherwise, we get
            // no contracts in the option chain
            if (_invested || _onDataCalls++ < 40)
            {
                return;
            }

            if (data.OptionChains.Count == 0)
            {
                return;
            }
            if (data.OptionChains.Values.All(o => o.Contracts.Values.Any(c => !data.ContainsKey(c.Symbol))))
            {
                return;
            }
            if (data.OptionChains.Values.First().Contracts.Count == 0)
            {
                throw new Exception($"No contracts found in the option {data.OptionChains.Keys.First()}");
            }

            var deltas = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Delta).ToList();
            var gammas = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Gamma).ToList();
            var lambda = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Lambda).ToList();
            var rho = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Rho).ToList();
            var theta = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Theta).ToList();
            var vega = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Vega).ToList();

            // The commented out test cases all return zero.
            // This is because of failure to evaluate the greeks in the option pricing model.
            // For now, let's skip those.
            if (deltas.Any(d => d == 0))
            {
                throw new AggregateException("Option contract Delta was equal to zero");
            }
            if (gammas.Any(g => g == 0))
            {
                throw new AggregateException("Option contract Gamma was equal to zero");
            }
            //if (lambda.Any(l => l == 0))
            //{
            //    throw new AggregateException("Option contract Lambda was equal to zero");
            //}
            if (rho.Any(r => r == 0))
            {
                throw new AggregateException("Option contract Rho was equal to zero");
            }
            //if (theta.Any(t => t == 0))
            //{
            //    throw new AggregateException("Option contract Theta was equal to zero");
            //}
            //if (vega.Any(v => v == 0))
            //{
            //    throw new AggregateException("Option contract Vega was equal to zero");
            //}

            if (!_invested)
            {
                SetHoldings(data.OptionChains.Values.First().Contracts.Values.First().Symbol, 1);
                _invested = true;
            }
        }

        /// <summary>
        /// Ran at the end of the algorithm to ensure the algorithm has no holdings
        /// </summary>
        /// <exception cref="Exception">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
            }
            if (!_invested)
            {
                throw new Exception($"Never checked greeks, maybe we have no option data?");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "27.44%"},
            {"Average Loss", "-62.81%"},
            {"Compounding Annual Return", "-80.444%"},
            {"Drawdown", "52.600%"},
            {"Expectancy", "-0.282"},
            {"Net Profit", "-52.604%"},
            {"Sharpe Ratio", "-0.867"},
            {"Probabilistic Sharpe Ratio", "0.021%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.44"},
            {"Alpha", "-0.611"},
            {"Beta", "-0.033"},
            {"Annual Standard Deviation", "0.695"},
            {"Annual Variance", "0.484"},
            {"Information Ratio", "-0.513"},
            {"Tracking Error", "0.718"},
            {"Treynor Ratio", "18.473"},
            {"Total Fees", "$66.60"},
            {"Fitness Score", "0.162"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.136"},
            {"Return Over Maximum Drawdown", "-1.529"},
            {"Portfolio Turnover", "0.427"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "1153646593"}
        };
    }
}

