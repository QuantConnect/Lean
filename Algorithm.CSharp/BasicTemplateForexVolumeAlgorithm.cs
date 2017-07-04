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
    ///     Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class BasicTemplateForexVolumeAlgorithm : QCAlgorithm
    {
        private Symbol EURUSD;
        private CompositeIndicator<IndicatorDataPoint> fastVWMA;
        private CompositeIndicator<IndicatorDataPoint> slowVWMA;
        private readonly Identity volume = new Identity("volIdentity");

        /// <summary>
        ///     Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All
        ///     algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(year: 2014, month: 05, day: 07); //Set Start Date
            SetEndDate(year: 2014, month: 05, day: 15); //Set End Date
            SetCash(startingCash: 100000); //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            EURUSD = AddForex("EURUSD", Resolution.Minute, Market.FXCM).Symbol;
            AddData<ForexVolume>("EURUSD", Resolution.Minute, DateTimeZone.Utc, fillDataForward: true);
            var _price = Identity(EURUSD);
            fastVWMA = _price.WeightedBy(volume, period: 15);
            slowVWMA = _price.WeightedBy(volume, period: 120);
        }

        /// <summary>
        ///     OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
#if DEBUG
            Log(string.Format("\nPair Data:\nAlgorithm Time {0:g}\nData Time {1:g}\n" +
                              "\tAsk: {2:F5}\t|\tBid: {3:F5}\n",
                Time, data.Time, data.QuoteBars[EURUSD].Ask.Close, data.QuoteBars[EURUSD].Bid.Close));
#endif
        }

        public void OnData(ForexVolume fxVolume)
        {
#if DEBUG
            Log(string.Format("\nVolume Data:\nAlgorithm Time {0:g}\nData Time {1:g}\n" +
                              "\tTransactions: {2:N}\t|\tVolume: {3:N}",
                Time, fxVolume.Time, fxVolume.Transanctions, fxVolume.Value));
#endif
            volume.Update(new IndicatorDataPoint
            {
                Time = Time,
                Value = fxVolume.Value
            });
        }
    }
}