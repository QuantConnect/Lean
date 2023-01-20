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
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndexOptionIronCondorAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private BollingerBands _bb;

        public override void Initialize()
        {
            SetStartDate(2019, 9, 1);
            SetEndDate(2019, 11, 1);
            SetCash(100000);

            var index = AddIndex("SPX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "SPXW", Resolution.Minute);
            option.SetFilter((x) => x.WeeklysOnly().Strikes(-5, 5).Expiration(0, 14));
            _symbol = option.Symbol;

            _bb = BB(index, 10, 2, resolution: Resolution.Daily);
            WarmUpIndicator(index, _bb);
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested) return;

            // Get the OptionChain
            if (!slice.OptionChains.TryGetValue(_symbol, out var chain)) return;

            // Get the closest expiry date
            var expiry = chain.Min(x => x.Expiry);
            var contracts = chain.Where(x => x.Expiry == expiry);

            // Separate the call and put contracts and sort by Strike to find OTM contracts
            var calls = contracts.Where(x => x.Right == OptionRight.Call)
                .OrderByDescending(x => x.Strike).ToArray();
            var puts = contracts.Where(x => x.Right == OptionRight.Put)
                .OrderBy(x => x.Strike).ToArray();

            if (calls.Count() == 0 || puts.Count() == 0) return;

            // Create combo order legs
            var price = _bb.Price.Current.Value;
            var quantity = 1;
            if (price > _bb.UpperBand.Current.Value || price < _bb.LowerBand.Current.Value)
            {
                quantity = -1;
            }
            
            var legs = new List<Leg>
            {
                Leg.Create(calls.First().Symbol, quantity),
                Leg.Create(puts.First().Symbol, quantity),
                Leg.Create(calls.Skip(2).First().Symbol, -quantity),
                Leg.Create(puts.Skip(2).First().Symbol, -quantity),
            };

            ComboMarketOrder(legs, 10, asynchronous: true);
        }
    }
}