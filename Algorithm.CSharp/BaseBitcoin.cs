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
using NodaTime;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{

    /// <summary>
    /// Base class for Bitcoin algorithms. This base class provides standard behaviour for trading BTCUSD on Bitfinex
    /// trading on Bitfinex
    /// </summary>
    public abstract class BaseBitcoin : QCAlgorithm
    {

        const string _bitcoinSymbol = "BTCUSD";

        protected string BitcoinSymbol { get { return _bitcoinSymbol; } }

        public BaseBitcoin()
        {
            //better to set this in QCAlgorithm base class?
            SetBrokerageModel(BrokerageName.BitfinexBrokerage, AccountType.Margin);
            //is this already set through market hours db?
            SetTimeZone(DateTimeZone.Utc);
            //better to set this in config.json?
            Transactions.MarketOrderFillTimeout = new TimeSpan(0, 0, 20);
        }

        public override void Initialize()
        {
            
            SetStartDate(2015, 11, 10);
            SetEndDate(2016, 2, 20);
            var security = AddSecurity(SecurityType.Forex, BitcoinSymbol, Resolution.Tick, Market.Bitfinex, false, 3.3m, false);
            SetCash("USD", 1000, 1m);
            SetBenchmark(security.Symbol);
        }

        public void OnData(Ticks data)
        {
            foreach (var item in data)
            {
                foreach (var tick in item.Value)
                {
                    OnData(tick);
                }
            }
        }

        public abstract void OnData(Tick data);


    }
}
