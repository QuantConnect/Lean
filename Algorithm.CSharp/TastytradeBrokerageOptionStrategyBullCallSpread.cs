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
using QuantConnect.Data;
using QuantConnect.Orders;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    public class TastytradeBrokerageOptionStrategyBullCallSpread : QCAlgorithm
    {
        private Symbol _applOption;

        private List<OrderTicket> _orderTickets;

        public override void Initialize()
        {
            SetBrokerageModel(Brokerages.BrokerageName.Tastytrade);

            var aapl = AddEquity("AAPL").Symbol;
            var aaplOption = AddOption(aapl);
            aaplOption.SetFilter(o => o.Strikes(-5, 5).Expiration(0, 30));

            _applOption = aaplOption.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (_orderTickets != null)
            {
                return;
            }

            if (!slice.OptionChains.TryGetValue(_applOption, out var chain))
            {
                return;
            }

            // Get the nearest expiry date of the contracts
            var expiry = chain.Min(x => x.Expiry);

            // Select the call Option contracts with the nearest expiry and sort by strike price
            var calls = chain.Where(x => x.Expiry == expiry && x.Right == OptionRight.Call && x.Strike % 5 == 0).OrderBy(x => x.Strike).Take(2).ToArray();

            if (calls.Length < 2)
            {
                return;
            }

            var legs = new List<Leg>(2)
            {
                Leg.Create(calls[0], 1),
                Leg.Create(calls[1], -1)
            };

            var limitPrice = Securities[calls[0]].AskPrice - Securities[calls[1]].BidPrice - 0.05m;

            _orderTickets = ComboLimitOrder(legs, 1, limitPrice);
        }
    }
}
