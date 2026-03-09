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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    public class IndexOptionPutCalendarSpreadAlgorithm : QCAlgorithm
    {
        private Symbol _vixw, _vxz;
        private IEnumerable<OrderTicket> _tickets = Enumerable.Empty<OrderTicket>();
        private DateTime _firstExpiry = DateTime.MaxValue;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2023, 1, 1);
            SetCash(50000);

            _vxz = AddEquity("VXZ", Resolution.Minute).Symbol;

            var index = AddIndex("VIX", Resolution.Minute).Symbol;
            var option = AddIndexOption(index, "VIXW", Resolution.Minute);
            option.SetFilter((x) => x.Strikes(-2, 2).Expiration(15, 45));
            _vixw = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio[_vxz].Invested)
            {
                MarketOrder(_vxz, 100);
            }
            
            var indexOptionsInvested = _tickets.Where(x => Portfolio[x.Symbol].Invested).ToList();
            // Liquidate if the shorter term option is about to expire
            if (_firstExpiry < Time.AddDays(2) && _tickets.All(x => slice.ContainsKey(x.Symbol)))
            {
                foreach (var holding in indexOptionsInvested)
                {
                    Liquidate(holding.Symbol);
                }
            }
            // Return if there is any opening index option position
            else if (indexOptionsInvested.Count > 0)
            {
                return;
            }

            // Get the OptionChain
            if (!slice.OptionChains.TryGetValue(_vixw, out var chain)) return;

            // Get ATM strike price
            var strike = chain.MinBy(x => Math.Abs(x.Strike - chain.Underlying.Value)).Strike;
            
            // Select the ATM put Option contracts and sort by expiration date
            var puts = chain.Where(x => x.Strike == strike && x.Right == OptionRight.Put)
                            .OrderBy(x => x.Expiry).ToArray();
            if (puts.Length < 2) return;
            _firstExpiry = puts[0].Expiry;

            // Sell the put calendar spread
            var putCalendarSpread = OptionStrategies.PutCalendarSpread(_vixw, strike, _firstExpiry, puts[^1].Expiry);
            _tickets = Sell(putCalendarSpread, 1, asynchronous: true);
        }
    }
}
