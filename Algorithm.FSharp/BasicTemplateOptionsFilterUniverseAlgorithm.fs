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
open System.Linq
open QuantConnect
open QuantConnect.Algorithm
open QuantConnect.Securities

// Declare algorithm name
type BasicTemplateOptionsFilterUniverseAlgorithm() =
    inherit QCAlgorithm()

    let underlyingTicker = "GOOG"
    let underlying = QuantConnect.Symbol.Create(underlyingTicker, SecurityType.Equity, Market.USA)
    let optionSymbol = QuantConnect.Symbol.Create(underlyingTicker, SecurityType.Option, Market.USA)

    override this.Initialize() =

        this.SetStartDate(2015, 12, 24)
        this.SetEndDate(2015, 12, 24)
        this.SetCash(10000)

        let equity = this.AddEquity(underlyingTicker)
        let option = this.AddOption(underlyingTicker)

        let filter (universe:OptionFilterUniverse)  = 
            universe
                .WeeklysOnly()
                .Expiration(TimeSpan.Zero, TimeSpan.FromDays(10.))
                .Where(fun symbol -> symbol.ID.OptionRight = OptionRight.Put |> not 
                                     && universe.Underlying.Price - symbol.ID.StrikePrice < 60m)
        option.SetFilter(filter)

        this.SetBenchmark(equity.Symbol)

    override this.OnData slice =
        if not this.Portfolio.Invested then
            match slice.OptionChains.TryGetValue(optionSymbol) with
            | false,_ -> ()
            | true,chain ->
                query {
                    for optionContract in chain.OrderByDescending(fun x -> x.Strike) do
                    where (optionContract.Right = OptionRight.Call)
                    where (optionContract.Expiry = this.Time.Date)
                    where (optionContract.Strike < chain.Underlying.Price)
                    select optionContract
                    skip 2
                }
                |> Seq.tryHead
                |> Option.iter(fun contract -> 
                    this.MarketOrder(contract.Symbol,1m) |> ignore)
         
    override this.OnOrderEvent orderEvent = 
        this.Log (orderEvent.ToString())
