// QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
// Lean Algorithmic Trading Engine v2.0. Copyright 2015 QuantConnect Corporation.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace System
namespace System.Collections.Generic
namespace QuantConnnect
namespace QuantConnect.Orders
namespace QuantConnect.Algorithm
namespace QuantConnect.Securities
namespace QuantConnect.Algorithm.FSharp

open System
open QuantConnect
open QuantConnect.Data.Market
open QuantConnect.Algorithm


// Declare algorithm name
type BasicTemplateAlgorithm() =

    //Reuse all the base class of QCAlgorithm
    inherit QCAlgorithm()

        //Implement core methods:
        override this.Initialize() =
            this.SetCash(100000)
            this.SetStartDate(2013, 10, 07)
            this.SetEndDate(2013, 10, 11)
            this.AddSecurity(SecurityType.Equity, "SPY", Nullable Resolution.Second) |> ignore

        //TradeBars Data Event
        member this.OnData(bar:TradeBars) =

            if not this.Portfolio.Invested then
                this.SetHoldings(this.Symbol("SPY"), 1);
            else
                ()