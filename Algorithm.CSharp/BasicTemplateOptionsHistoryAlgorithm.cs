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

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example demonstrating how to access to options history for a given underlying equity security.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="filter selection" />
    /// <meta name="tag" content="history" />
    public class BasicTemplateOptionsHistoryAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            // this test opens position in the first day of trading, lives through stock split (7 for 1), and closes adjusted position on the second day
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(1000000);

            var option = AddOption("GOOG");
            // add the initial contract filter
            option.SetFilter(-2, +2, TimeSpan.Zero, TimeSpan.FromDays(180));

            // set the pricing model for Greeks and volatility
            // find more pricing models https://www.quantconnect.com/lean/documentation/topic27704.html
            option.PriceModel = OptionPriceModels.CrankNicolsonFD();
            // set the warm-up period for the pricing model
            SetWarmup(TimeSpan.FromDays(4));
            // set the benchmark to be the initial cash
            SetBenchmark(d => 1000000);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (IsWarmingUp) return;
            if (!Portfolio.Invested)
            {
                foreach (var chain in slice.OptionChains)
                {
                    var underlying = Securities[chain.Key.Underlying];
                    foreach (var contract in chain.Value)
                    {
                        Log($"{contract.Symbol.Value}," +
                            $"Bid={contract.BidPrice.ToStringInvariant()} " +
                            $"Ask={contract.AskPrice.ToStringInvariant()} " +
                            $"Last={contract.LastPrice.ToStringInvariant()} " +
                            $"OI={contract.OpenInterest.ToStringInvariant()} " +
                            $"σ={underlying.VolatilityModel.Volatility.ToStringInvariant("0.000")} " +
                            $"NPV={contract.TheoreticalPrice.ToStringInvariant("0.000")} " +
                            $"Δ={contract.Greeks.Delta.ToStringInvariant("0.000")} " +
                            $"Γ={contract.Greeks.Gamma.ToStringInvariant("0.000")} " +
                            $"ν={contract.Greeks.Vega.ToStringInvariant("0.000")} " +
                            $"ρ={contract.Greeks.Rho.ToStringInvariant("0.00")} " +
                            $"Θ={(contract.Greeks.Theta / 365.0m).ToStringInvariant("0.00")} " +
                            $"IV={contract.ImpliedVolatility.ToStringInvariant("0.000")}"
                        );
                    }
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var change in changes.AddedSecurities)
            {
                // Only print options price
                if (change.Symbol.Value == "GOOG") continue;
                var history = History(change.Symbol, 10, Resolution.Minute);

                foreach (var data in history.OrderByDescending(x => x.Time).Take(3))
                {
                    Log($"History: {data.Symbol.Value}: {data.Time} > {data.Close}");
                }
            }
        }
    }
}
