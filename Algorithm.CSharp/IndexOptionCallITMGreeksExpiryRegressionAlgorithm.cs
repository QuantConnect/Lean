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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests In The Money (ITM) index option expiry for calls.
    /// We test to make sure that index options have greeks enabled, same as equity options.
    /// </summary>
    public class IndexOptionCallITMGreeksExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _invested;
        private int _onDataCalls;
        private Symbol _spx;
        private Option _spxOption;
        private Symbol _expectedOptionContract;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 31);

            var spx = AddIndex("SPX", Resolution.Minute);
            spx.VolatilityModel = new StandardDeviationOfReturnsVolatilityModel(60, Resolution.Minute, TimeSpan.FromMinutes(1));
            _spx = spx.Symbol;

            // Select an index option expiring ITM, and adds it to the algorithm.
            _spxOption = AddIndexOptionContract(OptionChainProvider.GetOptionContractList(_spx, Time)
                .Where(x => x.ID.StrikePrice <= 3200m && x.ID.OptionRight == OptionRight.Call && x.ID.Date.Year == 2021 && x.ID.Date.Month == 1)
                .OrderByDescending(x => x.ID.StrikePrice)
                .Take(1)
                .Single(), Resolution.Minute);

            _spxOption.PriceModel = OptionPriceModels.BlackScholes();

            _expectedOptionContract = QuantConnect.Symbol.CreateOption(_spx, Market.USA, OptionStyle.European, OptionRight.Call, 3200m, new DateTime(2021, 1, 15));
            if (_spxOption.Symbol != _expectedOptionContract)
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
            var impliedVol = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.ImpliedVolatility).ToList();
            var vega = data.OptionChains.Values.OrderByDescending(y => y.Contracts.Values.Sum(x => x.Volume)).First().Contracts.Values.Select(x => x.Greeks.Vega).ToList();

            // The commented out test cases all return zero.
            // This is because of failure to evaluate the greeks in the option pricing model, most likely
            // due to us not clearing the default 30 day requirement for the volatility model to start being updated.
            if (deltas.Any(d => d == 0))
            {
                throw new AggregateException("Option contract Delta was equal to zero");
            }
            // Delta is 1, therefore we expect a gamma of 0
            if (gammas.Any(g => deltas.Any() && deltas[0] == 1 ? g != 0 : g == 0))
            {
                throw new AggregateException("Option contract Gamma was equal to zero");
            }
            if (lambda.Any(l => l == 0))
            {
                throw new AggregateException("Option contract Lambda was equal to zero");
            }
            if (rho.Any(r => r == 0))
            {
                throw new AggregateException("Option contract Rho was equal to zero");
            }
            if (theta.Any(t => t == 0))
            {
                throw new AggregateException("Option contract Theta was equal to zero");
            }
            // Vega will equal 0 if the quote price and IV are way too off, causing the price is not sensitive to volatility change
            if (vega.Zip(impliedVol, (v, iv) => (v, iv)).Any(x => x.v == 0 && x.iv < 10))
            {
                throw new AggregateException("Option contract Vega was equal to zero");
            }

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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 19908;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-54.58%"},
            {"Compounding Annual Return", "99.378%"},
            {"Drawdown", "7.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "104974"},
            {"Net Profit", "4.974%"},
            {"Sharpe Ratio", "5.19"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "89.439%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.674"},
            {"Beta", "-0.205"},
            {"Annual Standard Deviation", "0.321"},
            {"Annual Variance", "0.103"},
            {"Information Ratio", "4.505"},
            {"Tracking Error", "0.36"},
            {"Treynor Ratio", "-8.141"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$59000000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Portfolio Turnover", "2.19%"},
            {"OrderListHash", "025b99be4e9008421548aa498fece11e"}
        };
    }
}

