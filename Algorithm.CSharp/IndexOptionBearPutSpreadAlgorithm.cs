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
    public class IndexOptionBearPutSpreadAlgorithm : QCAlgorithm
    {
        private Symbol _spxw;
        private List<Leg> _legs = new();

        public override void Initialize()
        {
            SetStartDate(2022, 1, 1);
            SetEndDate(2022, 7, 1);
            SetCash(100000);

            var index = AddIndex("SPX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "SPXW", Resolution.Minute);
            option.SetFilter((x) => x.WeeklysOnly().Strikes(5, 10).Expiration(0, 0));

            _spxw = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            // Return if open position exists
            if (_legs.Any(x => Portfolio[x.Symbol].Invested)) return;

            // Get the OptionChain
            if (!slice.OptionChains.TryGetValue(_spxw, out var chain)) return;

            // Get the nearest expiry date of the contracts
            var expiry = chain.Min(x => x.Expiry);
            
            // Select the put Option contracts with the nearest expiry and sort by strike price
            var puts = chain.Where(x => x.Expiry == expiry && x.Right == OptionRight.Put)
                .OrderBy(x => x.Strike).ToArray();
            if (puts.Length < 2) return;

            // Create combo order legs
            _legs = new List<Leg>
            {
                Leg.Create(puts[0].Symbol, -1),
                Leg.Create(puts[^1].Symbol, 1)
            };
            ComboMarketOrder(_legs, 1);
        }
    }
}