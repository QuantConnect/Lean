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
using System.IO;
using System.Globalization;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// The algorithm creates new indicator value with the existing indicator method by Indicator Extensions
    /// Demonstration of using local custom datasource CustomData to request the IBM and SPY daily data
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
        private const string _ibm = "IBM";
        private const string _spy = "SPY";
        private SimpleMovingAverage _smaIBM;
        private SimpleMovingAverage _smaSPY;
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
            AddData<CustomData>(_ibm, Resolution.Daily);
            AddData<CustomData>(_spy, Resolution.Daily);
            // Set up default Indicators, these are just 'identities' of the closing price
            _smaIBM = SMA(_ibm, 1);
            _smaSPY = SMA(_spy, 1);
            // This will create a new indicator whose value is smaSPY / smaIBM
            _ratio = _smaSPY.Over(_smaIBM);
        }

        /// <summary>
        /// Custom data event handler:
        /// </summary>
        /// <param name="data">CustomData - dictionary Bars of custom data</param>
        public void OnData(CustomData data)
        {
            // Wait for all indicators to fully initialize
            if (_smaIBM.IsReady && _smaSPY.IsReady && _ratio.IsReady)
            {
                if (!Portfolio.Invested && _ratio > 1)
                {
                    MarketOrder(_ibm, 100);
                }
                else if (_ratio < 1)
                {
                    Liquidate();
                }
                // plot all indicators
                PlotIndicator("SMA", _smaIBM, _smaSPY);
                PlotIndicator("Ratio", _ratio);
            }
        }
    }

    /// <summary>
    /// Custom data from local LEAN data
    /// </summary>
    public class CustomData : BaseData
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }

        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Time = value - Period; }
        }

        public TimeSpan Period
        {
            get { return QuantConnect.Time.OneDay; }
        }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var source = Path.Combine(Globals.DataFolder, "equity", "usa", config.Resolution.ToString().ToLower(), LeanData.GenerateZipFileName(config.Symbol, date, config.Resolution, config.TickType));
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var csv = line.ToCsv(6);
            var _scaleFactor = 1 / 10000m;

            var custom = new CustomData
            {
                Symbol = config.Symbol,
                Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture),
                Open = csv[1].ToDecimal() * _scaleFactor,
                High = csv[2].ToDecimal() * _scaleFactor,
                Low = csv[3].ToDecimal() * _scaleFactor,
                Close = csv[4].ToDecimal() * _scaleFactor,
                Value = csv[4].ToDecimal() * _scaleFactor
            };
            return custom;
        }
    }
}
