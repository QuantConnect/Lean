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
using System.Linq;
using NodaTime;
using QuantConnect.Data.Custom;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class BasicTemplateForexVolumeAlgorithm : QCAlgorithm
    {
        private Symbol _eurusd;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 05, 07);  //Set Start Date
            SetEndDate(2014, 05, 15);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            _eurusd = AddForex("EURUSD", Resolution.Minute, Market.FXCM).Symbol;
            AddData<ForexVolume>("EURUSD", Resolution.Minute, DateTimeZone.Utc);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            var algorithmTime = Time;

            Log(string.Format("\nPair Data:\nAlgorithm Time {0:g}\nData Time {1:g}\n" +
                              "\tAsk: {2:F5}\t|\tBid: {3:F5}\n",
                              Time, data.Time, data.QuoteBars[_eurusd].Ask.Close, data.QuoteBars[_eurusd].Bid.Close));

        }

        public void OnData(ForexVolume fxVolume)
        {
            Log(string.Format("\nVolume Data:\nAlgorithm Time {0:g}\nData Time {1:g}\n" +
                              "\tTransactions: {2:N}\t|\tVolume: {3:N}",
                              Time, fxVolume.Time, fxVolume.Transanctions, fxVolume.Value));

        }
    }
}