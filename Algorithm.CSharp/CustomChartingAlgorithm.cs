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
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating custom charting support in QuantConnect.
    /// The entire charting system of quantconnect is adaptable. You can adjust it to draw whatever you'd like.
    /// Charts can be stacked, or overlayed on each other. Series can be candles, lines or scatter plots.
    /// Even the default behaviours of QuantConnect can be overridden.
    /// </summary>
    /// <meta name="tag" content="charting" />
    /// <meta name="tag" content="adding charts" />
    /// <meta name="tag" content="series types" />
    /// <meta name="tag" content="plotting indicators" />
    public class CustomChartingAlgorithm : QCAlgorithm
    {
        private decimal _fastMa;
        private decimal _slowMa;
        private decimal _lastPrice;
        private DateTime _resample;
        private TimeSpan _resamplePeriod;
        private readonly DateTime _startDate = new DateTime(2010, 3, 3);
        private readonly DateTime _endDate = new DateTime(2014, 3, 3);

        /// <summary>
        /// Called at the start of your algorithm to setup your requirements:
        /// </summary>
        public override void Initialize()
        {
            //Set the date range you want to run your algorithm:
            SetStartDate(_startDate);
            SetEndDate(_endDate);

            //Set the starting cash for your strategy:
            SetCash(100000);

            //Add any stocks you'd like to analyse, and set the resolution:
            // Find more symbols here: http://quantconnect.com/data
            var spy = AddSecurity(SecurityType.Equity, "SPY").Symbol;

            //Chart - Master Container for the Chart:
            var stockPlot = new Chart("Trade Plot");
            //On the Trade Plotter Chart we want 3 series: trades and price:
            var buyOrders = new Series("Buy", SeriesType.Scatter, 0);
            var sellOrders = new Series("Sell", SeriesType.Scatter, 0);
            var assetPrice = new Series("Price", SeriesType.Line, 0);
            stockPlot.AddSeries(buyOrders);
            stockPlot.AddSeries(sellOrders);
            stockPlot.AddSeries(assetPrice);
            AddChart(stockPlot);

            var avgCross = new Chart("Strategy Equity");
            var fastMa = new Series("FastMA", SeriesType.Line, 1);
            var slowMa = new Series("SlowMA", SeriesType.Line, 1);
            avgCross.AddSeries(fastMa);
            avgCross.AddSeries(slowMa);
            AddChart(avgCross);

            _resamplePeriod = TimeSpan.FromMinutes((_endDate - _startDate).TotalMinutes / 2000);

            // There's support for candlestick charts built-in:
            var dailySpyPlot = new Chart("Daily SPY");
            var spyCandlesticks = new CandlestickSeries("SPY");
            dailySpyPlot.AddSeries(spyCandlesticks);
            AddChart(dailySpyPlot);

            Consolidate<TradeBar>(spy, TimeSpan.FromDays(1), (bar) =>
            {
                Plot("Daily SPY", "SPY", bar);
            });
        }


        /// <summary>
        /// OnEndOfDay Event Handler - At the end of each trading day we fire this code.
        /// To avoid flooding, we recommend running your plotting at the end of each day.
        /// </summary>
        public override void OnEndOfDay(Symbol symbol)
        {
            //Log the end of day prices:
            Plot("Trade Plot", "Price", _lastPrice);
        }


        /// <summary>
        /// On receiving new tradebar data it will be passed into this function. The general pattern is:
        /// "public void OnData( CustomType name ) {...s"
        /// </summary>
        /// <param name="data">TradeBars data type synchronized and pushed into this function. The tradebars are grouped in a dictionary.</param>
        public void OnData(TradeBars data)
        {
            _lastPrice = data["SPY"].Close;

            if (_fastMa == 0) _fastMa = _lastPrice;
            if (_slowMa == 0) _slowMa = _lastPrice;

            _fastMa = (0.01m * _lastPrice) + (0.99m * _fastMa);
            _slowMa = (0.001m * _lastPrice) + (0.999m * _slowMa);

            if (Time > _resample)
            {
                _resample = Time.Add(_resamplePeriod);
                Plot("Strategy Equity", "FastMA", _fastMa);
                Plot("Strategy Equity", "SlowMA", _slowMa);
            }


            //On the 5th days when not invested buy:
            if (!Portfolio.Invested && Time.Day % 13 == 0)
            {
                Order("SPY", (int)(Portfolio.MarginRemaining / data["SPY"].Close));
                Plot("Trade Plot", "Buy", _lastPrice);
            }
            else if (Time.Day % 21 == 0 && Portfolio.Invested)
            {
                Plot("Trade Plot", "Sell", _lastPrice);
                Liquidate();
            }
        }
    }
}
