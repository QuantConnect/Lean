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
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// The algorithm creates new indicator value with the existing indicator method by Indicator Extensions
    /// Demonstration of using the external custom datasource Quandl to request the VIX and VXV daily data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="plotting indicators" />
    /// <meta name="tag" content="charting" />
    public class CustomDataIndicatorExtensionsAlgorithm : QCAlgorithm
    {
        private const string _vix = "CBOE/VIX";
        private const string _vxv = "CBOE/VXV";
        private SimpleMovingAverage _smaVIX;
        private SimpleMovingAverage _smaVXV;
        private IndicatorBase<IndicatorDataPoint> _ratio;

        /// <summary>
        /// Initialize the data and resolution you require for your strategy
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);
            SetEndDate(2018, 1, 1);
            SetCash(25000);

            // Define the symbol and "type" of our generic data
            AddData<QuandlVix>(_vix, Resolution.Daily);
            AddData<Quandl>(_vxv, Resolution.Daily);
            // Set up default Indicators, these are just 'identities' of the closing price
            _smaVIX = SMA(_vix, 1);
            _smaVXV = SMA(_vxv, 1);
            // This will create a new indicator whose value is smaVXV / smaVIX
            _ratio = _smaVXV.Over(_smaVIX);
        }

        /// <summary>
        /// Custom data event handler:
        /// </summary>
        /// <param name="data">Quandl - dictionary Bars of Quandl Data</param>
        public void OnData(Quandl data)
        {
            // Wait for all indicators to fully initialize
            if (_smaVIX.IsReady && _smaVXV.IsReady && _ratio.IsReady)
            {
                if (!Portfolio.Invested && _ratio > 1)
                {
                    MarketOrder(_vix, 100);
                }
                else if (_ratio < 1)
                {
                    Liquidate();
                }
                // plot all indicators
                PlotIndicator("SMA", _smaVIX, _smaVXV);
                PlotIndicator("Ratio", _ratio);
            }
        }
    }

    /// <summary>
    /// In CBOE/VIX data, there is a "vix close" column instead of "close" which is the 
    /// default column namein LEAN Quandl custom data implementation.
    /// This class assigns new column name to match the the external datasource setting.
    /// </summary>
    public class QuandlVix : Quandl
    {
        public QuandlVix() : base(valueColumnName: "vix close") { }
    }
}