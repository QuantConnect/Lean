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

using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression Channel algorithm simply initializes the date range and cash
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="plotting indicators" />
    public class RegressionChannelAlgorithm : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private SecurityHolding _holdings;
        private RegressionChannel _rc;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2009, 1, 1);  //Set Start Date
            SetEndDate(2015, 1, 1);    //Set End Date
            SetCash(100000);           //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            var equity = AddEquity(_spy, Resolution.Minute);
            _holdings = equity.Holdings;
            _rc = RC(_spy, 30, 2, Resolution.Daily);

            var stockPlot = new Chart("Trade Plot");
            stockPlot.AddSeries(new Series("Buy", SeriesType.Scatter, 0));
            stockPlot.AddSeries(new Series("Sell", SeriesType.Scatter, 0));
            stockPlot.AddSeries(new Series("UpperChannel", SeriesType.Line, 0));
            stockPlot.AddSeries(new Series("LowerChannel", SeriesType.Line, 0));
            stockPlot.AddSeries(new Series("Regression", SeriesType.Line, 0));
            AddChart(stockPlot);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public void OnData(TradeBars data)
        {
            if (!_rc.IsReady || !data.ContainsKey(_spy)) return;
            var value = data[_spy].Value;

            if (_holdings.Quantity <= 0 && value < _rc.LowerChannel)
            {
                SetHoldings(_spy, 1);
                Plot("Trade Plot", "Buy", value);
            }

            if (_holdings.Quantity >= 0 && value > _rc.UpperChannel)
            {
                SetHoldings(_spy, -1);
                Plot("Trade Plot", "Sell", value);
            }
        }

        public override void OnEndOfDay()
        {
            Plot("Trade Plot", "UpperChannel", _rc.UpperChannel);
            Plot("Trade Plot", "LowerChannel", _rc.LowerChannel);
            Plot("Trade Plot", "Regression", _rc.LinearRegression);
        }
    }
}