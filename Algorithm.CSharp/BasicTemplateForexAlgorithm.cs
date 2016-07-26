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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class BasicTemplateForexAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddForex("EURUSD", Resolution.Minute);
            AddForex("GBPUSD", Resolution.Minute);
            AddForex("EURGBP", Resolution.Minute);

            History(5, Resolution.Daily);
            History(5, Resolution.Hour);
            History(5, Resolution.Minute);

            var history = History(System.TimeSpan.FromSeconds(5), Resolution.Second);

            foreach (var data in history.OrderBy(x => x.Time))
            {
                foreach (var key in data.Keys)
                {
                    Log(key.Value + ": " + data.Time + " > " + data[key].Value);
                }
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // Print to console to verify that data is coming in
            foreach (var key in data.Keys)
            {
                Log(key.Value + ": " + data[key].Time + " > " + data[key].Value);
            }
        }
    }
}