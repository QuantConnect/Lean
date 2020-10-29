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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    public class ContinuousFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousFuture;
        private DateTime _lastDateLog;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(2013, 3, 1);
            SetCash(1000000);

            _continuousFuture = AddContinuousFuture("CL", Resolution.Minute, Market.NYMEX, false, 1, extendedMarketHours: false);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_lastDateLog.Date != Time.Date)
            {
                _lastDateLog = Time;

                var quotes = data.Get<QuoteBar>();
                if (quotes.ContainsKey(_continuousFuture.Symbol))
                {
                    Log($"{Time}-V1- {quotes[_continuousFuture.Symbol]}");
                    Log($"{Time}-V2- {Securities[_continuousFuture.Symbol].GetLastData()}");
                }

                if (Portfolio.Invested)
                {
                    Liquidate();
                }
                else
                {
                    // This works because we set this contract as tradable, even if it's a canonical security
                    Buy(_continuousFuture.Symbol, 1);

                    Buy(_continuousFuture.Underlying.Symbol, 1); // this works too -> 
                }

                // TODO: internally, once we have the start and end date we could use the 'ContinuousFutureUniverse.SelectSymbols(eachDay, null)' or similar to get the symbol for that date
                // and create the sub history requests for each contract.
                // Could do this at the QCAlgorithm side so that we don't need to change the history provider at all, but will mean direct access to the history provider wont work
                var response = History(new[] { _continuousFuture.Symbol }, 10000);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"{orderEvent}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
        };
    }
}