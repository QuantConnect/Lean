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

using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Custom.PsychSignal
{
    /// <summary>
    /// PsychSignal consolidated data is output with this class when using
    /// <see cref="PsychSignalConsolidator"/>
    /// </summary>
    public class PsychSignalConsolidated : BaseData
    {
        /// <summary>
        /// OHLC Bullish intensity
        /// </summary>
        public Bar BullIntensity { get; set; }

        /// <summary>
        /// OHLC Bearish intensity
        /// </summary>
        public Bar BearIntensity { get; set; }

        /// <summary>
        /// OHLC Bullish intensity minus bearish intensity
        /// </summary>
        public Bar BullMinusBear { get; set; }

        /// <summary>
        /// Total bullish scored messages.
        /// This is the aggregate number of messages classified as bullish throughout the consolidated period.
        /// </summary>
        public int BullScoredMessages { get; set; }

        /// <summary>
        /// Total bearish scored messages.
        /// This is the aggregate number of messages classified as bearish throught the consolidated period.
        /// </summary>
        public int BearScoredMessages { get; set; }

        /// <summary>
        /// OHLC Bull/Bear message ratio.
        /// </summary>
        public Bar BullBearMessageRatio { get; set; }

        /// <summary>
        /// Aggregate total of messages scanned.
        /// </remarks>
        public int TotalScoredMessages { get; set; }

        /// <summary>
        /// Clones the data into a new object. We override this method to ensure
        /// that class properties are cloned and not set to null during a cloning event
        /// </summary>
        /// <returns>New BaseData derived instance containing the same data as the original object</returns>
        public override BaseData Clone()
        {
            return new PsychSignalConsolidated
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
    }
}
