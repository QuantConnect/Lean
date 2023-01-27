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

using System;
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndexOptionCallButterflyAlgorithm : QCAlgorithm
    {
        private Symbol _option, _vxz;
        private decimal _multiplier;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2021, 1, 1);
            SetCash(1000000);

            _vxz = AddEquity("VXZ", Resolution.Minute).Symbol;

            var index = AddIndex("SPX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "SPXW", Resolution.Minute);
            option.SetFilter((x) => x.IncludeWeeklys().Strikes(-3, 3).Expiration(15, 45));

            _option = option.Symbol;
            _multiplier = option.SymbolProperties.ContractMultiplier;
        }

        public override void OnData(Slice slice)
        {
            // The order of magnitude per SPXW order's value is 10000 times of VXZ
            if (!Portfolio[_vxz].Invested)
            {
                MarketOrder(_vxz, 10000);
            }
            
            // Return if any opening index option position
            if (Portfolio.Values.Any(x => x.Type == SecurityType.IndexOption && x.Invested))
            {
                return;
            }

            // Get the OptionChain
            if (!slice.OptionChains.TryGetValue(_option, out var chain)) return;

            // Get nearest expiry date
            var expiry = chain.Min(x => x.Expiry);
            
            // Select the call Option contracts with nearest expiry and sort by strike price
            var calls = chain.Where(x => x.Expiry == expiry && x.Right == OptionRight.Call).ToList();
            if (calls.Count < 3) return;
            var sortedCalls = calls.OrderBy(x => x.Strike).ToArray();
            
            // Select ATM call
            var atmCall = calls.OrderBy(x => Math.Abs(x.Strike - chain.Underlying.Value)).First();

            // Create combo order legs
            var legs = new List<Leg>
            {
                Leg.Create(sortedCalls[0].Symbol, -1),
                Leg.Create(sortedCalls[^1].Symbol, -1),
                Leg.Create(atmCall.Symbol, 2)
            };
            var price = legs.Sum(x => Math.Abs(Securities[x.Symbol].Price * x.Quantity) * _multiplier);
            if (price > 0)
            {
                var quantity = Portfolio.TotalPortfolioValue / price;
                ComboMarketOrder(legs, -(int)Math.Floor(quantity), asynchronous: true);
            }
            
        }
    }
}