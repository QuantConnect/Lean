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
using QuantConnect.Data.Custom.PsychSignal;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example algorithm shows how to import and use psychsignal sentiment data.
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="psychsignal" />
    /// <meta name="tag" content="sentiment" />
    public class PsychSignalSentimentAlgorithm : QCAlgorithm
    {
        private const string Ticker = "AAPL";
        private Symbol _symbol;

        /// <summary>
        /// Initialize the algorithm with our custom data
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 9);
            SetCash(100000);

            _symbol = AddEquity(Ticker).Symbol;
            AddData<PsychSignalSentimentData>(Ticker);
        }

        /// <summary>
        /// Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment
        /// </summary>
        /// <param name="slice">Slice object containing the sentiment data</param>
        public override void OnData(Slice slice)
        {
            foreach (var message in slice.Get<PsychSignalSentimentData>().Values)
            {
                if (!Portfolio.Invested && Transactions.GetOpenOrders().Count == 0 && slice.ContainsKey(_symbol) && 
                    message.BullIntensity > 1.5m && message.BullScoredMessages > 3.0m)
                {
                    SetHoldings(_symbol, 0.25);
                }
                else if (Portfolio.Invested && message.BearIntensity > 1.5m && message.BearScoredMessages > 3.0m)
                {
                    Liquidate(_symbol);
                }
            }
        }
    }
}
