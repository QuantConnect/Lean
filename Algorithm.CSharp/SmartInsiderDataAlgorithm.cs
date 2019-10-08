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
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Custom.SmartInsider;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm demonstrating usage of SmartInsider data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="smart insider" />
    /// <meta name="tag" content="form 4" />
    /// <meta name="tag" content="insider trading" />
    public class SmartInsiderDataAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 7, 25);
            SetEndDate(2019, 8, 2);
            SetCash(100000);

            AddData<SmartInsiderIntention>("KO");
            AddData<SmartInsiderTransaction>("KO");

            _symbol = AddEquity("KO", Resolution.Daily).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
        }

        /// <summary>
        /// Insider transaction data will be provided to us here
        /// </summary>
        /// <param name="data">Transaction data</param>
        public void OnData(SmartInsiderTransaction data)
        {
            var hasOpenOrders = Transactions.GetOpenOrders().Any();

            if (!Portfolio.Invested && !hasOpenOrders)
            {
                if (data.BuybackPercentage > 0.0001m && data.VolumePercentage > 0.001m)
                {
                    Log($"Buying {_symbol.Value} due to stock transaction");
                    SetHoldings(_symbol, 0.50m);
                }
            }
        }

        /// <summary>
        /// Insider intention data will be provided to us here
        /// </summary>
        /// <param name="data">Intention data</param>
        public void OnData(SmartInsiderIntention data)
        {
            var hasOpenOrders = Transactions.GetOpenOrders().Any();

            if (!Portfolio.Invested && !hasOpenOrders)
            {
                if (data.Percentage > 0.0001m)
                {
                    Log($"Buying {_symbol.Value} due to intention to purchase stock");
                    SetHoldings(_symbol, 0.50m);
                }
            }
            else if (Portfolio.Invested && !hasOpenOrders)
            {
                if (data.Percentage < 0.00m)
                {
                    Log($"Liquidating {_symbol.Value}");
                    Liquidate(_symbol);
                }
            }
        }
    }
}
