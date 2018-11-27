(*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License")
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*)

namespace QuantConnect.Algorithm.FSharp

open System
open QuantConnect
open QuantConnect.Algorithm
open QuantConnect.Interfaces
open QuantConnect.Algorithm.FSharp.Utils

// Declare algorithm name
type BasicTemplateOptionsAlgorithm() =

    //Reuse all the base class of QCAlgorithm
    inherit QCAlgorithm()

    let underlyingTicker = "GOOG"
    //let underlying = QuantConnect.Symbol.Create(underlyingTicker, SecurityType.Equity, Market.USA)
    let optionSymbol = QuantConnect.Symbol.Create(underlyingTicker, SecurityType.Option, Market.USA)

    override this.Initialize() =
        this.SetStartDate(2015, 12, 24)
        this.SetEndDate(2015, 12, 24)
        this.SetCash(100000)

        let equity = this.AddEquity(underlyingTicker)
        let option = this.AddOption(underlyingTicker)

        // set our strike/expiry filter for this option chain
        option.SetFilter(fun u -> u.Strikes(-2, +2).Expiration(TimeSpan.Zero, TimeSpan.FromDays(180.0)))

        // use the underlying equity as the benchmark
        this.SetBenchmark(equity.Symbol)

    override this.OnData slice =
        if not this.Portfolio.Invested && this.IsMarketOpen(optionSymbol) then
            match slice.OptionChains.TryGetValue(optionSymbol) with
            | false, _    -> ()
            | true, chain ->
                query {
                    for x in chain do
                    sortByDescending x.Expiry
                    thenBy (abs (chain.Underlying.Price - x.Strike))
                    thenByDescending x.Right
                    select x
                }
                |> Seq.tryHead
                |> Option.iter (fun atmContract -> 
                    this.MarketOrder(atmContract.Symbol,1)          |> ignore
                    this.MarketOnCloseOrder(atmContract.Symbol, -1) |> ignore)

    override this.OnOrderEvent orderEvent =
        this.Log(orderEvent.ToString())

    interface IRegressionAlgorithmDefinition with
        member __.CanRunLocally = true

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        member __.Languages = [| Language.CSharp; Language.Python; Language.FSharp |]

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        member __.ExpectedStatistics = 
            idict [
                "Total Trades", "2"
                "Average Win", "0%"
                "Average Loss", "-0.28%"
                "Compounding Annual Return", "-78.105%"
                "Drawdown", "0.300%"
                "Expectancy", "-1"
                "Net Profit", "-0.280%"
                "Sharpe Ratio", "0"
                "Loss Rate", "100%"
                "Win Rate", "0%"
                "Profit-Loss Ratio", "0"
                "Alpha", "0"
                "Beta", "0"
                "Annual Standard Deviation", "0"
                "Annual Variance", "0"
                "Information Ratio", "0"
                "Tracking Error", "0"
                "Treynor Ratio", "0"
                "Total Fees", "$0.50"
            ]

    
                    
    


