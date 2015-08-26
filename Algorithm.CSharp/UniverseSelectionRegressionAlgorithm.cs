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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class UniverseSelectionRegressionAlgorithm : QCAlgorithm
    {
        private SecurityChanges _changes;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 22);  //Set Start Date
            SetEndDate(2014, 04, 07);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "GOOG", Resolution.Daily);

            SetUniverse(coarse =>
            {
                var startsWithGoo = coarse.Where(x => x.Symbol.StartsWith("GOO")).ToList();
                return from c in coarse
                       where c.Symbol == "GOOG" || c.Symbol == "GOOCV" || c.Symbol == "GOOAV" || c.Symbol == "GOOGL"
                       select c;
            });
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Transactions.OrdersCount == 0)
            {
                MarketOrder("GOOG", 100);
            }

            if (data.Delistings.Count != 0)
            {
                foreach (var kvp in data.Delistings)
                {
                    Liquidate(kvp.Key);
                }
            }

            if (Time.Date == new DateTime(2014, 04, 07))
            {
                Liquidate();
                return;
            }
            if (_changes != null && _changes.AddedSecurities.All(x => data.Bars.ContainsKey(x.Symbol)))
            {
                foreach (var security in _changes.AddedSecurities)
                {
                    Log("Purchasing: " + security.Symbol);
                    MarketOnOpenOrder(security.Symbol, 100);
                }
                foreach (var security in _changes.RemovedSecurities)
                {
                    Log("Selling: " + security.Symbol);
                    MarketOnOpenOrder(security.Symbol, -100);
                }
                _changes = null;
            }
        }

        #region Overrides of QCAlgorithm

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                Log("Filled " + Transactions.GetOrderById(orderEvent.OrderId));
            }
        }

        #endregion
    }
}