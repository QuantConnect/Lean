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
 *
*/

using System;
using System.Globalization;
using System.IO;
using NodaTime;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.PsychSignal
{
    /// <summary>
    /// PsychSignal sentiment data implementation.
    /// Created as part of a subscription request from AddData{T}
    /// and consumed by algorithms running on LEAN.
    /// </summary>
    public class PsychSignalSentiment : BaseData
    {
        /// <summary>
        /// The EndTime of the bar represents the time the data ended and
        /// the time it should be emitted in backtesting/live trading
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period + FixedLiveOffset; }
            set { Time = value - Period; }
        }

        /// <summary>
        /// Time from the start of the bar until the end of the bar per each data point
        /// </summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Time it takes for data to become available on the PsychSignal API after
        /// the time advances to the next minute
        /// </summary>
        private static readonly TimeSpan FixedLiveOffset = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Bullish intensity as reported by psychsignal
        /// </summary>
        public decimal BullIntensity { get; set; }

        /// <summary>
        /// Bearish intensity as reported by psychsignal.
        /// </summary>
        public decimal BearIntensity { get; set; }

        /// <summary>
        /// Bullish intensity minus bearish intensity
        /// </summary>
        public decimal BullMinusBear { get; set; }

        /// <summary>
        /// Total bullish scored messages.
        /// This is the total nubmer of messages classified as bullish in a minute
        /// </summary>
        public int BullScoredMessages { get; set; }

        /// <summary>
        /// Total bearish scored messages.
        /// This is the total number of messages classified as bearish in a minute
        /// </summary>
        public int BearScoredMessages { get; set; }

        /// <summary>
        /// Bull/Bear message ratio.
        /// Calculated by dividing BullScoredMessages by BearScoredMessages
        /// </summary>
        /// <remarks>If bearish messages equals zero, then the resulting value equals zero</remarks>
        public decimal BullBearMessageRatio { get; set; }

        /// <summary>
        /// Total messages scanned.
        /// This includes bull/bear messages and messages that couldn't be classified
        /// </summary>
        /// <remarks>
        /// Sometimes, there will be no bull/bear rated messages, but nonetheless had messages scanned.
        /// This field describes the total fields that were scanned in a minute
        /// </remarks>
        public int TotalScoredMessages { get; set; }

        /// <summary>
        /// Retrieve Psychsignal data from disk and return it to user's custom data subscription
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in livemode, false for backtesting mode</param>
        /// <returns></returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                throw new InvalidOperationException();
            }

            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "psychsignal",
                    $"{config.Symbol.Value.ToLowerInvariant()}",
                    Invariant($"{date:yyyyMMdd}.zip")
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Reads a single entry from psychsignal's data source.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Line of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     Instance of the T:BaseData object containing psychsignal specific data
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            try
            {
                var csv = line.Split(',');

                var timestamp = date.AddMilliseconds(Convert.ToDouble(csv[0], CultureInfo.InvariantCulture));
                var bullIntensity = Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture);
                var bearIntensity = Convert.ToDecimal(csv[2], CultureInfo.InvariantCulture);
                var bullMinusBear = Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture);
                var bullScoredMessages = Convert.ToInt32(csv[4], CultureInfo.InvariantCulture);
                var bearScoredMessages = Convert.ToInt32(csv[5], CultureInfo.InvariantCulture);
                var bullBearMessageRatio = Convert.ToDecimal(csv[6], CultureInfo.InvariantCulture);
                var totalScannedMessages = Convert.ToInt32(csv[7], CultureInfo.InvariantCulture);

                return new PsychSignalSentiment
                {
                    EndTime = timestamp,
                    Symbol = config.Symbol,
                    BullIntensity = bullIntensity,
                    BearIntensity = bearIntensity,
                    BullMinusBear = bullMinusBear,
                    BullScoredMessages = bullScoredMessages,
                    BearScoredMessages = bearScoredMessages,
                    BullBearMessageRatio = bullBearMessageRatio,
                    TotalScoredMessages = totalScannedMessages
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clones the data into a new object. We override this method to ensure
        /// that class properties are cloned and not set to null during a cloning event
        /// </summary>
        /// <returns>New BaseData derived instance containing the same data as the original object</returns>
        public override BaseData Clone()
        {
            return new PsychSignalSentiment
            {
                Time = Time,
                Symbol = Symbol,
                BullIntensity = BullIntensity,
                BearIntensity = BearIntensity,
                BullMinusBear = BullMinusBear,
                BullScoredMessages = BullScoredMessages,
                BearScoredMessages = BearScoredMessages,
                BullBearMessageRatio = BullBearMessageRatio,
                TotalScoredMessages = TotalScoredMessages
            };
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }
    }
}
