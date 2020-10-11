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
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Uses daily data and a simple moving average cross to place trades and an ema for stop placement
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="trading and orders" />
    public class DailyAlgorithm : QCAlgorithm
    {
        private DateTime _lastAction;
        private MovingAverageConvergenceDivergence _macd;
        private ExponentialMovingAverage _ema;
        private readonly Symbol _ibm = QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA);
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);  //Set Start Date
            SetEndDate(2014, 01, 01);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "IBM", Resolution.Hour);
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);

            _macd = MACD(_spy, 12, 26, 9, MovingAverageType.Wilders, Resolution.Daily, Field.Close);
            _ema = EMA(_ibm, 15*6, Resolution.Hour, Field.SevenBar);

            Securities[_ibm].SetLeverage(1.0m);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            if (!_macd.IsReady) return;
            if (!data.ContainsKey(_ibm)) return;
            if (_lastAction.Date == Time.Date) return;
            _lastAction = Time;

            var holding = Portfolio[_spy];
            if (holding.Quantity <= 0 && _macd > _macd.Signal && data[_ibm].Price > _ema)
            {
                SetHoldings(_ibm, 0.25m);
            }
            else if (holding.Quantity >= 0 && _macd < _macd.Signal && data[_ibm].Price < _ema)
            {
                SetHoldings(_ibm, -0.25m);
            }
        }
    }
}