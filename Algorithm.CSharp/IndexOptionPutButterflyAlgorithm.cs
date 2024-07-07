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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndexOptionPutButterflyAlgorithm : QCAlgorithm
    {
        private Symbol _spxw,
            _vxz;
        private decimal _multiplier;
        private IEnumerable<OrderTicket> _tickets = Enumerable.Empty<OrderTicket>();

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2021, 1, 1);
            SetCash(1000000);

            _vxz = AddEquity("VXZ", Resolution.Minute).Symbol;

            var index = AddIndex("SPX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "SPXW", Resolution.Minute);
            option.SetFilter((x) => x.IncludeWeeklys().Strikes(-3, 3).Expiration(15, 45));

            _spxw = option.Symbol;
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
            if (_tickets.Any(x => Portfolio[x.Symbol].Invested))
                return;

            // Get the OptionChain
            if (!slice.OptionChains.TryGetValue(_spxw, out var chain))
                return;

            // Get nearest expiry date
            var expiry = chain.Min(x => x.Expiry);

            // Select the put Option contracts with nearest expiry and sort by strike price
            var puts = chain.Where(x => x.Expiry == expiry && x.Right == OptionRight.Put).ToList();
            if (puts.Count < 3)
                return;
            var sortedPutStrikes = puts.Select(x => x.Strike).OrderBy(x => x).ToArray();

            // Select ATM put
            var atmStrike = puts.MinBy(x => Math.Abs(x.Strike - chain.Underlying.Value)).Strike;

            // Get the strike prices for the ITM & OTM contracts, make sure they're in equidistance
            var spread = Math.Min(
                atmStrike - sortedPutStrikes[0],
                sortedPutStrikes[^1] - atmStrike
            );
            var otmStrike = atmStrike - spread;
            var itmStrike = atmStrike + spread;
            if (!sortedPutStrikes.Contains(otmStrike) || !sortedPutStrikes.Contains(itmStrike))
                return;

            // Buy the put butterfly
            var putButterfly = OptionStrategies.PutButterfly(
                _spxw,
                itmStrike,
                atmStrike,
                otmStrike,
                expiry
            );
            var price = putButterfly.UnderlyingLegs.Sum(x =>
                Math.Abs(Securities[x.Symbol].Price * x.Quantity) * _multiplier
            );
            if (price > 0)
            {
                var quantity = Portfolio.TotalPortfolioValue / price;
                _tickets = Buy(putButterfly, (int)Math.Floor(quantity), asynchronous: true);
            }
        }
    }
}
