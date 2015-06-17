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
namespace QuantConnect.Securities.Forex

open System
open System.Collections.Generic
open QuantConnect
open QuantConnect.Data.Market
open QuantConnect.Algorithm
open QuantConnect.Orders


// Declare algorithm name
type FSharpBasicTemplateAlgorithm() = 

    //Reuse all the base class of QCAlgorithm
    inherit QCAlgorithm()
        
        //Implement core methods:
        override this.Initialize() = printfn "woof"

        //TradeBars Data Event
        member this.OnData(bar:TradeBars) = printfn "woof"

        //Ticks Data Event
        member this.OnData(bar:Ticks) = printfn "woof"

        //Custom Events:
        override this.OnEndOfDay() = printfn "woof"
        override this.OnEndOfAlgorithm() = printfn "woof"
        override this.OnOrderEvent(orderEvent:OrderEvent) = printfn "woof"
        override this.OnMarginCallWarning() = printfn "woof"
        override this.OnMarginCall(orders)  = printfn "woof"