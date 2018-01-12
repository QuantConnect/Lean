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
using QuantConnect.Data.Custom.Intrinio;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash. This is a skeleton
    /// framework you can use for designing an algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BasicTemplateIntrinioEconomicData : QCAlgorithm
    {
        private Symbol _uso;    // United States Oil Fund LP
        private Symbol _bno;    // United States Brent Oil Fund LP

        Identity _wti = new Identity("WTI");
        Identity _brent = new Identity("Brent");
        private CompositeIndicator<IndicatorDataPoint> _spread;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2010, 01, 01); //Set Start Date
            SetEndDate(2013, 12, 31); //Set End Date
            SetCash(100000); //Set Strategy Cash

            // Set your Intrinino user and password.
            IntrinioConfig.SetUserAndPassword("<YourUser>", "<YourPassword>");
            // The Intrinio user and password can be also defined in the config.json file for local backtest.

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            _uso = AddEquity("USO", Resolution.Daily, leverage: 2m).Symbol;
            _bno = AddEquity("BNO", Resolution.Daily, leverage: 2m).Symbol;

            AddData<IntrinioEconomicData>(IntrinioEconomicDataSources.Commodities.CrudeOilWTI, Resolution.Daily);
            AddData<IntrinioEconomicData>(IntrinioEconomicDataSources.Commodities.CrudeOilBrent, Resolution.Daily);
            _spread = _brent.Minus(_wti);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if ((_spread > 0 && !Portfolio[_bno].IsLong) || 
                (_spread < 0 && !Portfolio[_uso].IsShort))
            {
                SetHoldings(_bno, 0.25 * Math.Sign(_spread));
                SetHoldings(_uso, -0.25 * Math.Sign(_spread));
            } 
        }

        public void OnData(IntrinioEconomicData economicData)
        {
            string oilMarket;
            if (economicData.Symbol.Value == IntrinioEconomicDataSources.Commodities.CrudeOilWTI)
            {
                oilMarket = "West Texas Intermediate";
                _wti.Update(economicData.Time, economicData.Price);
            }
            else
            {
                oilMarket = "Brent";
                _brent.Update(economicData.Time, economicData.Price);
            }
            // Log(string.Format("Crude Oil {0} price {1:F4}", oilMarket, economicData.Value));
        }
    }
}