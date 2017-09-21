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
        // S&P 500 EMini futures
        private const string TickerSP500 = Futures.Indices.SP500EMini;
        public Symbol SymbolSP500 = QuantConnect.Symbol.Create(TickerSP500, SecurityType.Future, Market.USA);

        // Dow Jones ETF Options
        // Generally direct assignments like below are frowned upon as they skip the map files and may identify the wrong symbol.
        // e.g. OptionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);
        private const string UnderlyingTicker = "DIA";
        public readonly Symbol Underlying = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Equity, Market.USA);
        public readonly Symbol OptionSymbol = QuantConnect.Symbol.Create(UnderlyingTicker, SecurityType.Option, Market.USA);

        // Microsoft Equtiy
        private const string TickerMSFT = "MSFT";
        private readonly Symbol SymbolMSFT = QuantConnect.Symbol.Create(TickerMSFT, SecurityType.Equity, Market.USA);

        // EUR/USD FX spot pair
        private const string TickerEURUSD = "EURUSD";
        private Symbol SymbolEURUSD = QuantConnect.Symbol.Create(TickerEURUSD, SecurityType.Forex, Market.FXCM);

        private int barCount = 0;

        public override void Initialize()
        {
            SetStartDate(2016, 01, 28);
            SetEndDate(2016, 02, 29);
            SetCash(1000000);

            // setting futures
            var futureSP500 = AddFuture(TickerSP500, Resolution.Minute);
            // set our expiry filter for this futures chain
            futureSP500.SetFilter(TimeSpan.FromDays(10), TimeSpan.FromDays(182));

            // setting up options
            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);

            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            option.PriceModel = OptionPriceModels.BinomialCoxRossRubinstein();
            // option.EnableGreekApproximation = true;
            // set our expiry filter for this option chain
            option.SetFilter(-2, +2, TimeSpan.Zero, TimeSpan.FromDays(180));

            // setting up stock
            AddEquity(TickerMSFT);

            // setting up FX
            AddForex(TickerEURUSD);

            // specifying zero benchmark
            SetBenchmark(date => 0m);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            barCount++;

            if (barCount % 20 == 0)
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
                    if (slice.OptionChains.TryGetValue(OptionSymbol, out optionChain))
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
                    MarketOrder(SymbolMSFT, 100);

                    // trade FX pair
                    MarketOrder(SymbolEURUSD, 100000);
                }
                else
                {
                    Liquidate();
                }
            }

            if (barCount % 20 == 1)
            {
                Log(String.Format("P/L:{0:0.00}, Fees:{1:0.00}, Profit:{2:0.00}, Eq:{3:0.00}, Holdings:{4:0.00}, Vol: {5:0.00}, Margin: {6:0.00}",
                    Portfolio.TotalUnrealisedProfit,
                    Portfolio.TotalFees,
                    Portfolio.TotalProfit,
                    Portfolio.TotalPortfolioValue,
                    Portfolio.TotalHoldingsValue,
                    Portfolio.TotalSaleVolume,
                    Portfolio.TotalMarginUsed));

                foreach (var holding in Securities.Values.OrderByDescending(x => x.Holdings.AbsoluteQuantity))
                {
                    Log(String.Format(" - {0}, Avg Prc:{1:0.00}, Qty:{2:0.00}, Mkt Prc:{3:0.00}, Mkt Val:{4:0.00}, Unreal P/L: {5:0.00}, Fees: {6:0.00}, Vol: {7:0.00}",
                    holding.Symbol.Value,
                    holding.Holdings.AveragePrice,
                    holding.Holdings.Quantity,
                    holding.Holdings.Price,
                    holding.Holdings.HoldingsValue,
                    holding.Holdings.UnrealizedProfit,
                    holding.Holdings.TotalFees,
                    holding.Holdings.TotalSaleVolume));
                }
            }

            if (barCount % 20 == 2)
            {
                foreach (var chain in slice.OptionChains)
                {
                    var underlying = Securities[chain.Key.Underlying];
                    foreach (var contract in chain.Value)
                    {
                        Log(String.Format(@"{0} {1},B={2} A={3} L={4} OI={5} σ={6:0.00} NPV={7:0.00} Δ={8:0.00} Γ={9:0.00} ν={10:0.00} ρ={11:0.00} Θ={12:0.00} IV={13:0.00}",
                             Time.ToString(),
                             contract.Symbol.Value,
                             contract.BidPrice,
                             contract.AskPrice,
                             contract.LastPrice,
                             contract.OpenInterest,
                             underlying.VolatilityModel.Volatility,
                             contract.TheoreticalPrice,
                             contract.Greeks.Delta,
                             contract.Greeks.Gamma,
                             contract.Greeks.Vega,
                             contract.Greeks.Rho,
                             contract.Greeks.Theta / 365.0m,
                             contract.ImpliedVolatility));
                    }
                }

                foreach (var chain in slice.FutureChains)
                {
                    foreach (var contract in chain.Value)
                    {
                        Log(String.Format("{0}, {1}, B={2} A={3} L={4} OI={5}",
                                contract.Symbol.Value,
                                Time,
                                contract.BidPrice,
                                contract.AskPrice,
                                contract.LastPrice,
                                contract.OpenInterest));
                    }
                }
            }

            foreach (var kpv in slice.QuoteBars)
            {
                Console.WriteLine("---> QuoteBar: {0}, {1}, {2}", Time, kpv.Key.Value, kpv.Value.Close.ToString("0.0000"));
            }

            foreach (var kpv in slice.Bars)
            {
                Console.WriteLine("---> Bar: {0}, {1}, {2}", Time, kpv.Key.Value, kpv.Value.Close.ToString("0.0000"));
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