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

using NodaTime;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Base class for regression algorithms testing that when a continuous future rollover happens,
    /// the continuous contract is updated correctly with the new contract data.
    /// The algorithms asserts the behavior for the case when the data time zone is the same as the exchange time zone.
    /// </summary>
    public class ContinuousFutureRolloverHourExchangeTimeZoneSameAsDataRegressionAlgorithm
        : ContinuousFutureRolloverBaseRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;

        protected override Offset ExchangeToDataTimeZoneOffset => Offset.Zero;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 7434;
    }
}
