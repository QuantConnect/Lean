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
using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class UniverseSelectionAlgorithm : QCAlgorithm
    {
        private const int Count = 100;
        private int dataCount = 0;
        private DateTime last;


        private class SelectionData
        {
            public readonly ExponentialMovingAverage EMA50;
            public readonly ExponentialMovingAverage EMA100;

            public SelectionData()
            {
                EMA50 = new ExponentialMovingAverage(50);
                EMA100 = new ExponentialMovingAverage(100);
            }

            public bool Update(DateTime time, decimal value)
            {
                return EMA50.Update(time, value) && EMA100.Update(time, value);
            }
        }

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Leverage = 2.0m;
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2001, 01, 01);  //Set Start Date
            SetEndDate(2002, 01, 01);    //Set End Date
            SetCash(10000000);             //Set Strategy Cash

            var averages = new ConcurrentDictionary<string, SelectionData>();
            SetUniverse(coarse =>
            {
                return (from cf in coarse
                        let avg = averages.GetOrAdd(cf.Symbol, sym => new SelectionData())
                        where avg.Update(cf.EndTime, cf.Price)
                        // only pick symbols who have their 50 day ema over their 100 day ema
                        where avg.EMA50 > avg.EMA100
                        // prefer symbols with a larger delta by percentage between the two averages
                        orderby (avg.EMA50 - avg.EMA100)/cf.Price descending
                        select cf.Symbol).Take(Count);
            });
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            dataCount++;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Count != 0)
            {
                Log("Security added: " + string.Join(",", changes.AddedSecurities.Select(x => x.Symbol)));
                foreach (var security in changes.AddedSecurities.OrderBy(x => x.Symbol))
                {
                    if (!security.HoldStock)
                    {
                        SetHoldings(security.Symbol, 0.0075);
                    }
                }
            }

            if (changes.RemovedSecurities.Count != 0)
            {
                Log("Security removed: " + string.Join(",", changes.RemovedSecurities.Select(x => x.Symbol)));
                foreach (var security in changes.RemovedSecurities.OrderBy(x => x.Symbol))
                {
                    var previousOrders = Transactions.GetOrders(x => x.Symbol == security.Symbol).OrderByDescending(x => x.Time);
                    if (security.HoldStock && previousOrders.First().Time + TimeSpan.FromDays(30) < Time)
                    {
                        Log("Liquidating: " + security.Symbol);
                        Liquidate(security.Symbol);
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
        }

        public override void OnEndOfAlgorithm()
        {
            Console.WriteLine("SecuritiesCount: " + Securities.Count + " DataCount: " + dataCount);
        }
    }
}