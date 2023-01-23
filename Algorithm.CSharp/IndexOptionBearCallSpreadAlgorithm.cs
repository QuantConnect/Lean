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

using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndexOptionBearCallSpreadAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2021, 1, 1);
            SetCash(100000);

            AddEquity("SPY", Resolution.Minute);

            var index = AddIndex("SPX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "SPXW", Resolution.Minute);
            option.SetFilter((x) => x.WeeklysOnly().Strikes(-5, 5).Expiration(40, 60));
            _symbol = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio["SPY"].Invested)
            {
                MarketOrder("SPY", 100);
            }
        
            // Return if hedge position presents
            if (Portfolio.Values.Any(x => x.Type == SecurityType.IndexOption && x.Invested)) return;

            // Get the OptionChain
            var chain = slice.OptionChains.get(_symbol);
            if (chain == null) return;

            // Get the nearest expiry date of the contracts
            var expiry = chain.Select(x => x.Expiry).OrderBy(x => x).First();
            
            // Select the call Option contracts with the nearest expiry and sort by strike price
            var calls = chain.Where(x => x.Expiry == expiry && x.Right == OptionRight.Call).OrderBy(x => x.Strike);
            if (calls.Count() == 0) return;

            // Create combo order legs
            var legs = new List<Leg>
            {
                Leg.Create(calls.First().Symbol, 1),
                Leg.Create(calls.Last().Symbol, -1)
            };
            ComboMarketOrder(legs, 1);
        }
    }
}