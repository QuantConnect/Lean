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
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// This algorithm is used to benchmark the Lean engine data points per second
    /// </summary>
    /// <remarks>
    /// commit   | time (s) | K points/sec | Total points
    /// 9924b0a  | 47.50    | 338          | ~16M
    /// 9acf934  | 45.77    | 350          | ~16M
    /// </remarks>
    public class BenchmarkAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 09, 15);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Tick);
            AddSecurity(SecurityType.Equity, "AAPL", Resolution.Second);
            AddSecurity(SecurityType.Equity, "ADBE", Resolution.Minute);
            AddSecurity(SecurityType.Equity, "IBM", Resolution.Tick);
            AddSecurity(SecurityType.Equity, "JNJ", Resolution.Second);
            AddSecurity(SecurityType.Equity, "MSFT", Resolution.Minute);
            AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Tick);
            AddSecurity(SecurityType.Forex, "EURGBP", Resolution.Second);
            AddSecurity(SecurityType.Forex, "GBPUSD", Resolution.Minute);
            AddSecurity(SecurityType.Forex, "USDJPY", Resolution.Tick);
            AddSecurity(SecurityType.Forex, "NZDUSD", Resolution.Second);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", .75); // leave some room lest we experience a margin call!
                Debug("Purchased Stock");
            }
        }
    }
}