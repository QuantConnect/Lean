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


using System.Collections.Generic;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides a test implementation of a holdings provider
    /// </summary>
    public class HoldingsProvider : IHoldingsProvider
    {
        private readonly IDictionary<string, Holding> _holdings;

        public HoldingsProvider(IDictionary<string, Holding> holdings)
        {
            _holdings = holdings;
        }

        public HoldingsProvider()
        {
            _holdings = new Dictionary<string, Holding>();
        }

        public Holding this[string symbol]
        {
            get { return _holdings[symbol]; }
            set { _holdings[symbol] = value; }
        }

        public Holding GetHoldings(Symbol symbol)
        {
            Holding holding;
            _holdings.TryGetValue(symbol.Value, out holding);
            return holding;
        }

        public bool TryGetValue(string symbol, out Holding holding)
        {
            return _holdings.TryGetValue(symbol, out holding);
        }
    }
}