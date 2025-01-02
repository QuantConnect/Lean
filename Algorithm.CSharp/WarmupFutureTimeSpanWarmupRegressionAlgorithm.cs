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
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    public class WarmupFutureTimeSpanWarmupRegressionAlgorithm : WarmupFutureRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetWarmUp(TimeSpan.FromHours(24 + 4));
        }

        public override void OnEndOfAlgorithm()
        {
            AssertDataTime(new DateTime(2013, 10, 07, 0, 0, 0), new DateTime(2013, 10, 08, 0, 0, 0), ChainWarmupTimes);
            AssertDataTime(new DateTime(2013, 10, 07, 0, 0, 0), new DateTime(2013, 10, 08, 0, 0, 0), ContinuousWarmupTimes);
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 19892;
    }
}
