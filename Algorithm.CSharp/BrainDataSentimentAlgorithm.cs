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

using QuantConnect.Data.Custom.BrainData;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example algorithm shows how to import and use braindata averaged sentiment data.
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="braindata" />
    /// <meta name="tag" content="sentiment" />
    public class BrainDataSentimentAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private readonly string _ticker = "AAPL";

        /// <summary>
        /// Initialize the algorithm with our custom data
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetEndDate(2019, 1, 1);
            SetCash(100000);

            _symbol = AddEquity(_ticker, Resolution.Daily).Symbol;
            AddData<BrainDataSentimentWeekly>(_ticker, Resolution.Daily);
            AddData<BrainDataSentimentMonthly>(_ticker, Resolution.Daily);
        }

        /// <summary>
        /// Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment
        /// </summary>
        /// <param name="message">Weekly sentiment data object</param>
        public void OnData(BrainDataSentimentWeekly message)
        {
            if (!Portfolio.Invested && Transactions.GetOpenOrders().Count == 0 && message.SentimentScore > 0.07m)
            {
                Log($"Weekly: Order placed for {_ticker}");
                SetHoldings(_symbol, 0.5);
            }
            else if (Portfolio.Invested && message.SentimentScore < -0.05m && Portfolio.ContainsKey(_symbol))
            {
                Log($"Weekly: Liquidating {_ticker}");
                Liquidate(_symbol);
            }
        }

        /// <summary>
        /// Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment
        /// </summary>
        /// <param name="message">Weekly sentiment data object</param>
        public void OnData(BrainDataSentimentMonthly message)
        {
            if (!Portfolio.Invested && Transactions.GetOpenOrders().Count == 0 && message.SentimentScore > 0.07m)
            {
                Log($"Monthly: Order placed for {_ticker}");
                SetHoldings(_symbol, 0.5);
            }
            else if (Portfolio.Invested && message.SentimentScore < -0.05m && Portfolio.ContainsKey(_symbol))
            {
                Log($"Monthly: Liquidating {_ticker}");
                Liquidate(_symbol);
            }
        }
    }
}
