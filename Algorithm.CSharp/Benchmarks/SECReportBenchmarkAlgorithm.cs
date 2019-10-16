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
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class SECReportBenchmarkAlgorithm : QCAlgorithm
    {
        private List<Security> _securities;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetEndDate(2019, 1, 1);

            var tickers = new List<string> {"AAPL", "AMZN", "MSFT", "IBM", "FB", "QQQ",
                "IWM", "BAC", "BNO", "AIG", "UW", "WM" };
            _securities = new List<Security>();

            foreach (var ticker in tickers)
            {
                var equity = AddEquity(ticker);
                _securities.Add(equity);

                AddData<SECReport8K>(equity.Symbol, Resolution.Daily);
                AddData<SECReport10K>(equity.Symbol, Resolution.Daily);
            }
        }

        public override void OnData(Slice data)
        {
            foreach (var security in _securities)
            {
                SECReport8K report8K = security.Data.Get<SECReport8K>();
                SECReport10K report10K = security.Data.Get<SECReport10K>();

                if (!security.HoldStock && report8K != null && report10K != null)
                {
                    SetHoldings(security.Symbol, 1d / _securities.Count);
                }
            }
        }
    }
}