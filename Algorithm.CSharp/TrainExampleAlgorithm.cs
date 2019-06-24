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
using System.Threading;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example shows how we can execute a method that takes longer than Lean timeout limit
    /// This feature is useful for algorithms that train models
    /// </summary>
    /// <meta name="tag" content="using quantconnect" />
    public class TrainExampleAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date

            AddEquity("SPY");

            Schedule.On(
                DateRules.EveryDay("SPY"),
                TimeRules.AfterMarketOpen("SPY", 10),
                () => {
                    Train(
                        SleepTraining,
                        TimeSpan.FromSeconds(20),
                        () => { Debug($"Callback called at {Time:O}"); }
                    );
                }
            );
        }

        private void SleepTraining()
        {
            Thread.Sleep(10000);
            Debug($"Portfolio Invested: {Portfolio.Invested}.");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.Invested) return;

            SetHoldings("SPY", 1);
        }
    }
}