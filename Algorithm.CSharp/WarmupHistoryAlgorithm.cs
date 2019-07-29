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

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates using the history provider to retrieve data
    /// to warm up indicators before data is received.
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="using data" />
    public class WarmupHistoryAlgorithm : QCAlgorithm
    {
        private ExponentialMovingAverage fast, slow;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Second);

            fast = EMA("EURUSD", 60);
            slow = EMA("EURUSD", 3600);

            // 3601 because rolling window waits for one to fall off the back to be considered ready
            var history = History<QuoteBar>("EURUSD", 3601);
            foreach (var bar in history)
            {
                fast.Update(bar.EndTime, bar.Close);
                slow.Update(bar.EndTime, bar.Close);
            }

            Log($"FAST IS {(fast.IsReady ? "" : "NOT")} READY. Samples: {fast.Samples.ToStringInvariant()}");
            Log($"SLOW IS {(slow.IsReady ? "" : "NOT")} READY. Samples: {slow.Samples.ToStringInvariant()}");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (fast > slow)
            {
                SetHoldings("EURUSD", 1);
            }
            else
            {
                SetHoldings("EURUSD", -1);
            }
        }
    }
}