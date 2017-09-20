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
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating FOREX asset types and requesting history on them in bulk. As FOREX uses
    /// QuoteBars you should request slices or
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="forex" />
    public class BasicTemplateForexAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 5, 7);  //Set Start Date
            SetEndDate(2014, 5, 15);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddForex("EURUSD");
            AddForex("NZDUSD");

            var dailyHistory = History(5, Resolution.Daily);
            var hourHistory = History(5, Resolution.Hour);
            var minuteHistory = History(5, Resolution.Minute);
            var secondHistory = History(5, Resolution.Second);

            // Log values from history request of second-resolution data
            foreach (var data in secondHistory)
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
            if (!Portfolio.Invested)
            {
                SetHoldings("EURUSD", .5);
                SetHoldings("NZDUSD", .5);
                Log(string.Join(", ", data.Values));
            }
        }
    }
}