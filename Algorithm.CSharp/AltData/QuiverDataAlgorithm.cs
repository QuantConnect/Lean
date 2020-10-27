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
using QuantConnect.Data;
using QuantConnect.Data.Custom.Quiver;
using QuantConnect.Data.UniverseSelection;


namespace QuantConnect.Algorithm.CSharp
{ 
    /// <summary>
    /// Quiver Quantitative is a provider of alternative data.
    /// </summary>
    public class QuiverDataAlgorithm : QCAlgorithm
    {
        private Symbol _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);


        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2020, 6, 1);
            SetCash(100000);

            var aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
            // var quiverWikiSymbol = AddData<QuiverWikipedia>(abbv).Symbol;
            // var history = History<QuiverWikipedia>(quiverWikiSymbol, 60, Resolution.Daily);

            var quiverWSBSymbol = AddData<QuiverWallStreetBets>(aapl).Symbol;
            var history = History<QuiverWallStreetBets>(quiverWSBSymbol, 60, Resolution.Daily);

            // var quiverPBSymbol = AddData<QuiverPoliticalBeta>(aapl).Symbol;
            // var history = History<QuiverPoliticalBeta>(quiverPBSymbol, 60, Resolution.Daily);

            // Count the number of items we get from our history request
            Debug($"We got {history.Count()} items from our history request");



        }


        public override void OnData(Slice data)
        {
            var points = data.Get<QuiverWallStreetBets>();
            foreach (var point in points.Values)
            {
                // Go long in the stock if it was mentioned more than 5 times in the WallStreetBets daily discussion
                if (point.Mentions > 5)
                {
                    SetHoldings(point.Symbol.Underlying, 1);
                }
                // Go short in the stock if it was mentioned less than 5 times in the WallStreetBets daily discussion
                if (point.Mentions < 5)
                {
                    SetHoldings(point.Symbol.Underlying, -1);
                }
            }

            // Go long in the stock if it has a positive correlation with Trump's election odds
            // var points = data.Get<QuiverPoliticalBeta>();
            // foreach (var point in points.Values)
            // {
            //     if (point.TrumpBeta > 0)
            //         SetHoldings(point.Symbol.Underlying, 1);
            //     if (point.TrumpBeta < 0)
            //        SetHoldings(point.Symbol.Underlying, -1);
            // }

        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var r in changes.RemovedSecurities)
            {
                // If removed from the universe, liquidate and remove the custom data from the algorithm
                Liquidate(r.Symbol);
                RemoveSecurity(QuantConnect.Symbol.CreateBase(typeof(QuiverWikipedia), r.Symbol, Market.USA));
            }
        }
    }
}