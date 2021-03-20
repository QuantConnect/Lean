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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Quiver;

namespace QuantConnect.Algorithm.CSharp.AltData
{
    /// <summary>
    /// Quiver Quantitative is a provider of alternative data.
    /// This algorithm shows how to consume the <see cref="QuiverWallStreetBets"/>
    /// </summary>
    public class QuiverWallStreetBetsDataAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2020, 6, 1);
            SetCash(100000);

            var aapl = AddEquity("AAPL", Resolution.Daily).Symbol;
            var quiverWSBSymbol = AddData<QuiverWallStreetBets>(aapl).Symbol;
            var history = History<QuiverWallStreetBets>(quiverWSBSymbol, 60, Resolution.Daily);

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
        }
    }
}
