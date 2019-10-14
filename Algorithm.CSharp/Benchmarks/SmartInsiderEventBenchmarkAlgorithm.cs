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
 *
*/

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SmartInsider;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class SmartInsiderEventBenchmarkAlgorithm : QCAlgorithm
    {
        private List<Security> _securities;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2005, 1, 1);
            SetEndDate(2019, 1, 1);

            var tickers = new List<string> {"AAPL", "AMZN", "MSFT", "IBM", "FB", "QQQ",
                "IWM", "BAC", "BNO", "AIG", "UW", "WM" };
            _securities = new List<Security>();

            foreach (var ticker in tickers)
            {
                var equity = AddEquity(ticker, Resolution.Daily);
                _securities.Add(equity);

                AddData<SmartInsiderIntention>(equity.Symbol, Resolution.Daily);
                AddData<SmartInsiderTransaction>(equity.Symbol, Resolution.Daily);
            }
        }

        public override void OnData(Slice data)
        {
            var intentions = data.Get<SmartInsiderIntention>();
            var transactions = data.Get<SmartInsiderTransaction>();

            foreach (var security in _securities)
            {
                SmartInsiderIntention intention = security.Data.Get<SmartInsiderIntention>();
                SmartInsiderTransaction transaction = security.Data.Get<SmartInsiderTransaction>();

                if (!security.HoldStock && intention != null && transaction != null && intentions.Count == transactions.Count)
                {
                    SetHoldings(security.Symbol, 1d / _securities.Count);
                }
            }
        }
    }
}