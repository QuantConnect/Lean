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

using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///  Example demonstrating importing custom forex volume data to use with your algorithm from FXCM.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="custom data" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="forex" />
    public class BasicTemplateFxcmVolumeAlgorithm : QCAlgorithm
    {
        private Symbol EURUSD;
        private CompositeIndicator<IndicatorDataPoint> fastVWMA;
        private CompositeIndicator<IndicatorDataPoint> slowVWMA;
        private readonly Identity volume = new Identity("volIdentity");

        /// <summary>
        ///     Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All
        ///     algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 05, 07);     //Set Start Date
            SetEndDate(2014, 05, 15);       //Set End Date
            SetCash(100000);                //Set Strategy Cash

            // Find more symbols here: https://www.quantconnect.com/data
            EURUSD = AddForex("EURUSD", Resolution.Minute).Symbol;

            AddData<FxcmVolume>("EURUSD_Vol", Resolution.Minute, DateTimeZone.Utc);
            var _price = Identity(EURUSD);
            fastVWMA = _price.WeightedBy(volume, period: 15);
            slowVWMA = _price.WeightedBy(volume, period: 300);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!slowVWMA.IsReady) return;
            if (!Portfolio.Invested || Portfolio[EURUSD].IsShort)
            {
                if (fastVWMA > slowVWMA)
                {
                    SetHoldings(EURUSD, percentage: 1);
                    Log(Time.ToString("g") + " Take a Long Position.");
                }
            }
            else
            {
                if (fastVWMA < slowVWMA)
                {
                    SetHoldings(EURUSD, percentage: -1);
                    Log(Time.ToString("g") + " Take a Short Position.");
                }
            }
        }

        public void OnData(FxcmVolume fxVolume)
        {
            volume.Update(new IndicatorDataPoint
            {
                Time = Time,
                Value = fxVolume.Volume
            });
        }
    }
}