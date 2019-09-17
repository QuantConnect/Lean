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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to create a multi asset class trading strategy.
    /// It is designed for test purposes and can be used with paper brokerage. All asset classes are not
    /// necessarily supported by some brokers. See our website for details.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="futures" />
    /// <meta name="tag" content="equity" />
    /// <meta name="tag" content="options" />
    public class BasicTemplateMultiAssetAlgorithm : QCAlgorithm
    {
        private int _barCount = 0;
        private Symbol _equitySymbol;
        private Symbol _forexSymbol;
        private Symbol _futureSymbol;
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2016, 01, 28);
            SetEndDate(2016, 02, 29);
            SetCash(1000000);

            // setting up Microsoft Equity
            _equitySymbol = AddEquity("MSFT").Symbol;

            // setting up EUR/USD FX spot pair
            _forexSymbol = AddForex("EURUSD").Symbol;

            // setting up S&P 500 EMini futures
            var futureSP500 = AddFuture(Futures.Indices.SP500EMini);
            _futureSymbol = futureSP500.Symbol;

            // set our expiry filter for this futures chain
            futureSP500.SetFilter(TimeSpan.FromDays(10), TimeSpan.FromDays(182));

            // setting up Dow Jones ETF Options
            var option = AddOption("DIA");
            _optionSymbol = option.Symbol;

            option.PriceModel = OptionPriceModels.BinomialCoxRossRubinstein();
            // option.EnableGreekApproximation = true;
            // set our expiry filter for this option chain
            option.SetFilter(-2, +2, TimeSpan.Zero, TimeSpan.FromDays(180));

            // specifying zero benchmark
            SetBenchmark(date => 0m);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            _barCount++;

            if (_barCount % 20 == 0)
            {
                if (!Portfolio.Invested)
                {
                    foreach (var chain in slice.FutureChains)
                    {
                        // find the front contract expiring no earlier than in 90 days
                        var contract = (
                            from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                            where futuresContract.Expiry > Time.Date.AddDays(90)
                            select futuresContract
                            ).FirstOrDefault();

                        // if found, trade it
                        if (contract != null)
                        {
                            MarketOrder(contract.Symbol, 1);
                        }
                    }

                    OptionChain optionChain;
                    if (slice.OptionChains.TryGetValue(_optionSymbol, out optionChain))
                    {
                        // find a farthest ATM contract
                        var contract = optionChain
                            .OrderBy(x => Math.Abs(optionChain.Underlying.Price - x.Strike))
                            .ThenByDescending(x => x.Expiry)
                            .FirstOrDefault();

                        // if found, trade it
                        if (contract != null)
                        {
                            MarketOrder(contract.Symbol, 1);
                        }
                    }

                    // trade MSFT
                    MarketOrder(_equitySymbol, 100);

                    // trade FX pair
                    MarketOrder(_forexSymbol, 100000);
                }
                else
                {
                    Liquidate();
                }
            }

            if (_barCount % 20 == 1)
            {
                Log($"P/L:{Portfolio.TotalUnrealisedProfit.ToStringInvariant("0.00")}, " +
                    $"Fees:{Portfolio.TotalFees.ToStringInvariant("0.00")}, " +
                    $"Profit:{Portfolio.TotalProfit.ToStringInvariant("0.00")}, " +
                    $"Eq:{Portfolio.TotalPortfolioValue.ToStringInvariant("0.00")}, " +
                    $"Holdings:{Portfolio.TotalHoldingsValue.ToStringInvariant("0.00")}, " +
                    $"Vol: {Portfolio.TotalSaleVolume.ToStringInvariant("0.00")}, " +
                    $"Margin: {Portfolio.TotalMarginUsed.ToStringInvariant("0.00")}"
                );

                foreach (var holding in Securities.Values.OrderByDescending(x => x.Holdings.AbsoluteQuantity))
                {
                    Log($" - {holding.Symbol.Value}, " +
                        $"Avg Prc:{holding.Holdings.AveragePrice.ToStringInvariant("0.00")}, " +
                        $"Qty:{holding.Holdings.Quantity.ToStringInvariant("0.00")}, " +
                        $"Mkt Prc:{holding.Holdings.Price.ToStringInvariant("0.00")}, " +
                        $"Mkt Val:{holding.Holdings.HoldingsValue.ToStringInvariant("0.00")}, " +
                        $"Unreal P/L: {holding.Holdings.UnrealizedProfit.ToStringInvariant("0.00")}, " +
                        $"Fees: {holding.Holdings.TotalFees.ToStringInvariant("0.00")}, " +
                        $"Vol: {holding.Holdings.TotalSaleVolume.ToStringInvariant("0.00")}"
                    );
                }
            }

            if (_barCount % 20 == 2)
            {
                foreach (var chain in slice.OptionChains)
                {
                    var underlying = Securities[chain.Key.Underlying];
                    foreach (var contract in chain.Value)
                    {
                        Log($"{Time.ToStringInvariant()} {contract.Symbol.Value}," +
                            $"B={contract.BidPrice.ToStringInvariant()} " +
                            $"A={contract.AskPrice.ToStringInvariant()} " +
                            $"L={contract.LastPrice.ToStringInvariant()} " +
                            $"OI={contract.OpenInterest.ToStringInvariant()} " +
                            $"σ={underlying.VolatilityModel.Volatility:0.00} " +
                            $"NPV={contract.TheoreticalPrice.ToStringInvariant("0.00")} " +
                            $"Δ={contract.Greeks.Delta.ToStringInvariant("0.00")} " +
                            $"Γ={contract.Greeks.Gamma.ToStringInvariant("0.00")} " +
                            $"ν={contract.Greeks.Vega.ToStringInvariant("0.00")} " +
                            $"ρ={contract.Greeks.Rho.ToStringInvariant("0.00")} " +
                            $"Θ={(contract.Greeks.Theta / 365.0m).ToStringInvariant("0.00")} " +
                            $"IV={contract.ImpliedVolatility.ToStringInvariant("0.00")}"
                        );
                    }
                }

                foreach (var chain in slice.FutureChains)
                {
                    foreach (var contract in chain.Value)
                    {
                        Log($"{contract.Symbol.Value}, {Time}, " +
                            $"B={contract.BidPrice} " +
                            $"A={contract.AskPrice} " +
                            $"L={contract.LastPrice} " +
                            $"OI={contract.OpenInterest}"
                        );
                    }
                }
            }

            foreach (var kpv in slice.QuoteBars)
            {
                Log($"---> QuoteBar: {Time}, {kpv.Key.Value}, {kpv.Value.Close:0.0000}");
            }

            foreach (var kpv in slice.Bars)
            {
                Log($"---> Bar: {Time}, {kpv.Key.Value}, {kpv.Value.Close.ToStringInvariant("0.0000")}");
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the evemts</param>
        /// <remarks>This method can be called asynchronously and so should only be used by seasoned C# experts. Ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }
    }
}