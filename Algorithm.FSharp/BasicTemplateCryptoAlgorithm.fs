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
open QuantConnect.Orders
open QuantConnect.Brokerages
open Interop
open QuantConnect.Interfaces

// Declare algorithm name
type BasicTemplateCryptoAlgorithm() =

    //Reuse all the base class of QCAlgorithm
    inherit QCAlgorithm()

    let mutable _fast = Unchecked.defaultof<_>
    let mutable _slow = Unchecked.defaultof<_>

    override this.Initialize() =

        this.SetStartDate(2018, 4, 4) // Set Start Date
        this.SetEndDate(2018, 4, 4) // Set End Date

        // Although typically real brokerages as GDAX only support a single account currency,
        // here we add both USD and EUR to demonstrate how to handle non-USD account currencies.
        // Set Strategy Cash (USD)
        this.SetCash(10000)

        // Set Strategy Cash (EUR)
        // EUR/USD conversion rate will be updated dynamically
        this.SetCash("EUR", 10000m, 1.23m)

        // Add some coins as initial holdings
        // When connected to a real brokerage, the amount specified in SetCash
        // will be replaced with the amount in your actual account.
        this.SetCash("BTC", 1m, 7300m)
        this.SetCash("ETH", 5m, 400m)

        // Note: the conversion rates above are required in backtesting (for now) because of this issue:
        // https://github.com/QuantConnect/Lean/issues/1859

        this.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash)

        // You can uncomment the following line when live trading with GDAX,
        // to ensure limit orders will only be posted to the order book and never executed as a taker (incurring fees).
        // Please note this statement has no effect in backtesting or paper trading.
        // DefaultOrderProperties = new GDAXOrderProperties { PostOnly = true }

        // Find more symbols here: http://quantconnect.com/data
        this.AddCrypto("BTCUSD") |> ignore
        this.AddCrypto("ETHUSD") |> ignore
        this.AddCrypto("BTCEUR") |> ignore
        let symbol = this.AddCrypto("LTCUSD").Symbol

        // create two moving averages
        _fast <- this.EMA(symbol, 30, Nullable Resolution.Minute)
        _slow <- this.EMA(symbol, 60, Nullable Resolution.Minute)

    override this.OnData data =
        match this.Time.Hour, this.Time.Minute with
        | (1,0) ->
            let limitPrice = Math.Round(this.Securities.["ETHUSD"].Price * 1.01m, 2)
            let quantity = this.Portfolio.CashBook.["ETH"].Amount
            this.LimitOrder(!>"ETHUSD", -quantity, limitPrice) |> ignore

        | (2,0) -> 
            let usdTotal = this.Portfolio.CashBook.["USD"].Amount
            let limitPrice = Math.Round(this.Securities.["BTCUSD"].Price * 0.95m, 2)
            // use only half of our total USD
            let quantity = usdTotal * 0.5m / limitPrice
            this.LimitOrder(!> "BTCUSD", quantity, limitPrice) |> ignore

        | (2,1) -> 
            let usdTotal = this.Portfolio.CashBook.["USD"].Amount
            let usdReserved = 
                this.Transactions.GetOpenOrders(fun x -> x.Direction = OrderDirection.Buy && x.Type = OrderType.Limit)
                |> Seq.cast<LimitOrder>
                |> Seq.filter (fun x-> x.Symbol = !> "BTCUSD" || x.Symbol = !> "ETHUSD")
                |> Seq.sumBy (fun x -> x.Quantity * x.LimitPrice)
            let usdAvailable = usdTotal - usdReserved

            // Submit a marketable buy limit order for ETH at 1% above the current price
            let limitPrice = Math.Round(this.Securities.["ETHUSD"].Price * 1.01m, 2)

            // use all of our available USD
            let quantity = usdAvailable / limitPrice

            // this order will be rejected for insufficient funds
            this.LimitOrder(!>"ETHUSD", quantity, limitPrice) |> ignore

            // use only half of our available USD
            let quantity = usdAvailable * 0.5m / limitPrice
            this.LimitOrder(!> "ETHUSD", quantity, limitPrice) |> ignore

        | (11,0) ->
            this.SetHoldings(!>"BTCUSD", 0m)

        | (12,0) ->
            this.Buy( !> "BTCEUR", 1m) |> ignore
            // Submit a sell limit order at 10% above market price
            let limitPrice = Math.Round(this.Securities.["BTCEUR"].Price * 1.1m, 2)
            this.LimitOrder(!>"BTCEUR", -1, limitPrice) |> ignore

        | (13,0) -> 
            this.Transactions.CancelOpenOrders(!>"BTCEUR") |> ignore

        | (t,_) when t > 13 ->
            match _fast > _slow, this.Portfolio.CashBook.["LTC"].Amount with
            | true, 0m              -> this.Buy((!> "LTCUSD":Symbol), 10) |> ignore
            | false, a when a > 0m  -> this.Liquidate(!> "LTCUSD") |>  ignore
            | _                     -> ()

        | (_,_)  -> ()                  
                  
    override this.OnOrderEvent orderEvent = 
        this.Debug(sprintf "%A %A" this.Time orderEvent)

    override this.OnEndOfAlgorithm() =
        this.Log(sprintf "%A - TotalPortfolioValue:%A" this.Time this.Portfolio.TotalPortfolioValue)


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
                "Total Trades", "10"
                "Average Win", "0%"
                "Average Loss", "-0.18%"
                "Compounding Annual Return", "-99.992%"
                "Drawdown", "3.800%"
                "Expectancy", "-1"
                "Net Profit", "-2.545%"
                "Sharpe Ratio", "-16.028"
                "Loss Rate", "100%"
                "Win Rate", "0%"
                "Profit-Loss Ratio", "0"
                "Alpha", "-5.47"
                "Beta", "326.539"
                "Annual Standard Deviation", "0.201"
                "Annual Variance", "0.04"
                "Information Ratio", "-16.112"
                "Tracking Error", "0.2"
                "Treynor Ratio", "-0.01"
                "Total Fees", "$85.27"
            ]

    